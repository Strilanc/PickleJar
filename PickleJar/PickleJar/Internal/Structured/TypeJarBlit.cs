using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace Strilanc.PickleJar.Internal.Structured {
    /// <summary>
    /// TypeJarBlit is used to parse values when memcpy'ing them is valid.
    /// Using memcpy is possible when the in-memory representation exactly matches the serialized representation.
    /// TypeJarBlit uses unsafe code, but is slightly faster than other parsers.
    /// </summary>
    internal sealed class TypeJarBlit<T> : IJarMetadataInternal, IJar<T> {
        public delegate T BlitParser(byte[] data, int offset, int length);

        private readonly int _length;
        private readonly BlitParser _parser;
        private TypeJarBlit(IEnumerable<IJarForMember> fieldParsers) {
            var len = fieldParsers.Aggregate((int?)0, (a, e) => a + e.OptionalConstantSerializedLength());
            if (!len.HasValue) throw new ArgumentException();
            _parser = MakeUnsafeBlitParser();
            _length = len.Value;
        }

        public ParsedValue<T> Parse(ArraySegment<byte> data) {
            if (data.Count < _length) throw new InvalidOperationException("Fragment");
            var value = _parser(data.Array, data.Offset, _length);
            return new ParsedValue<T>(value, _length);
        }
        public bool IsBlittable { get { return true; } }
        public int? OptionalConstantSerializedLength { get { return _length; } }

        public static TypeJarBlit<T> TryMake(IReadOnlyList<IJarForMember> fieldParsers) {
            if (!CanBlitParseWith(fieldParsers)) return null;
            return new TypeJarBlit<T>(fieldParsers);
        }

        private static bool CanBlitParseWith(IReadOnlyList<IJarForMember> fieldParsers) {
            if (fieldParsers == null) throw new ArgumentNullException("fieldParsers");

            // type has blittable representation?
            if (!ParserUtil.IsBlittable<T>()) return false;

            // all parsers have same constant length representation as value in memory?
            if (fieldParsers.Any(e => !e.AreMemoryAndSerializedRepresentationsOfValueGuaranteedToMatch())) return false;
            if (fieldParsers.Any(e => !e.OptionalConstantSerializedLength().HasValue)) return false;

            // type has no padding?
            var structLayout = typeof(T).StructLayoutAttribute;
            if (structLayout == null) return false;
            if (structLayout.Value != LayoutKind.Sequential) return false;
            if (structLayout.Pack != 1) return false;

            // parsers and struct fields have matching canonical names?
            var serialNames = fieldParsers.Select(e => e.MemberMatchInfo);
            var fieldNames = typeof(T).GetFields().Select(e => e.MatchInfo());
            if (!serialNames.HasSameSetOfItemsAs(fieldNames)) return false;

            // offsets implied by parser ordering matches offsets of the struct's fields?
            var memoryOffsets =
                typeof(T).GetFields().ToDictionary(
                    e => e.MatchInfo(),
                    e => (int?)typeof(T).FieldOffsetOf(e));
            var serialOffsets =
                fieldParsers
                    .StreamZip((int?)0, (a, e) => a + e.OptionalConstantSerializedLength())
                    .ToDictionary(e => e.Item1.MemberMatchInfo, e => e.Item2 - e.Item1.OptionalConstantSerializedLength());
            if (!serialOffsets.HasSameKeyValuesAs(memoryOffsets)) return false;

            return true;
        }

        public InlinedParserComponents TryMakeInlinedParserComponents(Expression array, Expression offset, Expression count) {
            return null;
        }

        /// <summary>
        /// Emits a method that copies the contents of an array segment over the memory representation of a value.
        /// </summary>
        public static BlitParser MakeUnsafeBlitParser() {
            var d = new DynamicMethod(
                name: "BlitParseValue" + typeof(T),
                returnType: typeof(T),
                parameterTypes: new[] { typeof(byte[]), typeof(int), typeof(int) },
                m: Assembly.GetExecutingAssembly().ManifestModule);

            // ____(byte[] array, int offset, int length)
            var g = d.GetILGenerator();

            // T result = default(T);
            g.DeclareLocal(typeof(T));
            g.Emit(OpCodes.Ldloca_S, (byte)0);
            g.Emit(OpCodes.Initobj, typeof(T));

            // Marshal.Copy(array, offset, (IntPtr)resultPtr, length);
            g.Emit(OpCodes.Ldarg_0);
            g.Emit(OpCodes.Ldarg_1);
            g.Emit(OpCodes.Ldloca_S, (byte)0);
            g.Emit(OpCodes.Conv_U);
            g.EmitCall(OpCodes.Call, typeof(IntPtr).GetMethod("op_Explicit", new[] { typeof(void*) }), null);
            g.Emit(OpCodes.Ldarg_2);
            g.EmitCall(OpCodes.Call, typeof(Marshal).GetMethod("Copy", new[] { typeof(byte[]), typeof(int), typeof(IntPtr), typeof(int) }), null);

            // return result
            g.Emit(OpCodes.Ldloc_0);
            g.Emit(OpCodes.Ret);

            return (BlitParser)d.CreateDelegate(typeof(BlitParser));
        }

        public byte[] Pack(T value) {
            throw new NotImplementedException();
        }

        public override string ToString() {
            return string.Format(
                "{0}[memcpy]",
                typeof(T));
        }
    }
}