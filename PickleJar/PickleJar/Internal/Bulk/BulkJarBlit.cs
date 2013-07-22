using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Linq;

namespace Strilanc.PickleJar.Internal.Bulk {
    /// <summary>
    /// BulkJarBlit is used to parse arrays of values when memcpy'ing them is valid.
    /// Using memcpy is possible when the in-memory representation exactly matches the serialized representation.
    /// BulkJarBlit uses unsafe code, but is an order of magnitude (or two!) faster than other parsers.
    /// </summary>
    internal sealed class BulkJarBlit<T> : IBulkJar<T> {
        public delegate T[] BlitParser(byte[] data, int itemCount, int offset, int length);

        public IJar<T> ItemJar { get; private set; }
        private readonly BlitParser _parser;
        private readonly int _itemLength;
        private BulkJarBlit(IJar<T> itemJar) {
            if (itemJar == null) throw new ArgumentNullException("itemJar");
            var len = itemJar.OptionalConstantSerializedLength();
            if (!len.HasValue) throw new ArgumentException();
            ItemJar = itemJar;
            _itemLength = len.Value;
            _parser = MakeUnsafeArrayBlitParser();
        }

        public static BulkJarBlit<T> TryMake(IJar<T> itemJar) {
            if (itemJar == null) throw new ArgumentNullException("itemJar");
            var r = itemJar as IJarMetadataInternal;
            if (r == null) return null;
            if (!r.AreMemoryAndSerializedRepresentationsOfValueGuaranteedToMatch) return null;
            if (!r.OptionalConstantSerializedLength.HasValue) return null;
            return new BulkJarBlit<T>(itemJar);
        }

        public ParsedValue<IReadOnlyList<T>> Parse(ArraySegment<byte> data, int count) {
            var length = count*_itemLength;
            if (data.Count < length) throw new InvalidOperationException("Fragment");
            var value = _parser(data.Array, count, data.Offset, length);
            return new ParsedValue<IReadOnlyList<T>>(value, length);
        }
        public int? OptionalConstantSerializedValueLength { get { return _itemLength; } }
        public byte[] Pack(IReadOnlyCollection<T> values) {
            // todo: optimize into blit
            return values.SelectMany(ItemJar.Pack).ToArray();
        }

        /// <summary>
        /// Emits a method that copies the contents of an array segment over the memory representation of anew  returned array of values.
        /// </summary>
        public static BlitParser MakeUnsafeArrayBlitParser() {
            var d = new DynamicMethod(
                name: "BlitParseArray" + typeof(T),
                returnType: typeof(T[]),
                parameterTypes: new[] { typeof(byte[]), typeof(int), typeof(int), typeof(int) },
                m: Assembly.GetExecutingAssembly().ManifestModule);

            // ____(byte[] array, int count, int offset, int length)
            var g = d.GetILGenerator();

            // T[] result;
            g.DeclareLocal(typeof(T[]));

            // void* resultPtr;
            g.DeclareLocal(typeof(void*), true);

            // result = new T[count];
            g.Emit(OpCodes.Ldarg_1);
            g.Emit(OpCodes.Newarr, typeof(T));
            g.Emit(OpCodes.Stloc_0);

            // fixed (void* resultPtr = result)
            g.Emit(OpCodes.Ldloc_0);
            g.Emit(OpCodes.Ldc_I4_0);
            g.Emit(OpCodes.Ldelema, typeof(T));
            g.Emit(OpCodes.Stloc_1);

            // Marshal.Copy(array, offset, (IntPtr)resultPtr, length);
            g.Emit(OpCodes.Ldarg_0);
            g.Emit(OpCodes.Ldarg_2);
            g.Emit(OpCodes.Ldloc_1);
            g.Emit(OpCodes.Conv_I);
            g.EmitCall(OpCodes.Call, typeof(IntPtr).GetMethod("op_Explicit", new[] { typeof(void*) }), null);
            g.Emit(OpCodes.Ldarg_3);
            g.EmitCall(OpCodes.Call, typeof(Marshal).GetMethod("Copy", new[] { typeof(byte[]), typeof(int), typeof(IntPtr), typeof(int) }), null);

            // return result
            g.Emit(OpCodes.Ldloc_0);
            g.Emit(OpCodes.Ret);

            return (BlitParser)d.CreateDelegate(typeof(BlitParser));
        }
    }
}