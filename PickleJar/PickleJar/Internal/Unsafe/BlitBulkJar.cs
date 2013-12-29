using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Linq;
using Strilanc.PickleJar.Internal.Bulk;
using Strilanc.PickleJar.Internal.RuntimeSpecialization;

namespace Strilanc.PickleJar.Internal.Unsafe {
    /// <summary>
    /// BlitBulkJar is used to parse arrays of values when memcpy'ing them is valid.
    /// Using memcpy is possible when the in-memory representation exactly matches the serialized representation.
    /// BlitBulkJar uses unsafe code, but is an order of magnitude (or two!) faster than other parsers.
    /// </summary>
    internal static class BlitBulkJar {
        public delegate T[] BlitParser<T>(byte[] data, int itemCount, int offset, int length);

        public static IBulkJar<T> TryMake<T>(IJar<T> itemJar) {
            if (itemJar == null) throw new ArgumentNullException("itemJar");
            var r = itemJar as IJarMetadataInternal;
            if (r == null) return null;
            if (!itemJar.CanBeFollowed) return null;
            if (!r.IsBlittable) return null;
            if (r.OptionalConstantSerializedLength.GetValueOrDefault() == 0) return null;
            InlinerBulkMaker c = (array, offset, count, itemCount) => BuildBlitBulkParserComponents(itemJar, array, offset, count, itemCount);

            return AnonymousBulkJar.CreateFrom(
                itemJar,
                c,
                // todo: optimize into blit
                values => values.SelectMany(itemJar.Pack).ToArray(),
                () => string.Format("{0}.Blit", itemJar),
                null);
        }

        public static SpecializedParserParts BuildBlitBulkParserComponents<T>(IJar<T> itemJar,
                                                                               Expression array,
                                                                               Expression offset,
                                                                               Expression count,
                                                                               Expression itemCount) {
            if (itemJar == null) throw new ArgumentNullException("itemJar");
            if (array == null) throw new ArgumentNullException("array");
            if (offset == null) throw new ArgumentNullException("offset");
            if (count == null) throw new ArgumentNullException("count");
            if (itemCount == null) throw new ArgumentNullException("itemCount");
            if (!itemJar.IsBlittable()) throw new ArgumentException("!itemJar.IsBlittable()");
            var itemLength = itemJar.OptionalConstantSerializedLength();
            if (!itemLength.HasValue) throw new ArgumentException("!itemJar.OptionalConstantSerializedLength().HasValue");

            var resultVar = Expression.Variable(typeof (T[]), "blitResultArray");
            var lengthVar = Expression.Variable(typeof (int), "length");
            var boundsCheck = Expression.IfThen(Expression.LessThan(count, lengthVar), DataFragmentException.CachedThrowExpression);
            var parseDoer = Expression.Block(
                lengthVar.AssignTo(Expression.MultiplyChecked(itemCount, Expression.Constant(itemLength.Value))),
                boundsCheck,
                resultVar.AssignTo(MakeUnsafeArrayBlitParserExpression<T>(array, offset, count, itemCount)));
            var storage = new SpecializedParserResultStorageParts(new[] {resultVar}, new[] {lengthVar});
            return new SpecializedParserParts(
                parseDoer: parseDoer,
                valueGetter: resultVar,
                consumedCountGetter: lengthVar,
                storage: storage);
        }

        public static byte[] BlitBytes(byte[] array, int itemCount, int offset, int length) {
            var result = new byte[itemCount];
            unsafe {
                fixed (byte* resultPtr = result) {
                    Marshal.Copy(array, offset, (IntPtr)resultPtr, length);
                }
            }
            return result;
        }
        public static Expression MakeUnsafeArrayBlitParserExpression<T>(Expression array, Expression offset, Expression count, Expression itemCount) {
            if (typeof (T) == typeof (byte))
                return Expression.Call(typeof (BlitBulkJar).GetMethod("BlitBytes"), array, itemCount, offset, count);

            var blitParser = MakeUnsafeArrayBlitParser<T>();
            return Expression.Invoke(Expression.Constant(blitParser), array, itemCount, offset, count);
        }
        /// <summary>
        /// Emits a method that copies the contents of an array segment over the memory representation of anew  returned array of values.
        /// </summary>
        public static BlitParser<T> MakeUnsafeArrayBlitParser<T>() {
            var d = new DynamicMethod(
                name: "BlitParseArray" + typeof (T),
                returnType: typeof (T[]),
                parameterTypes: new[] {typeof (byte[]), typeof (int), typeof (int), typeof (int)},
                m: Assembly.GetExecutingAssembly().ManifestModule);

            // ____(byte[] array, int count, int offset, int length)
            var g = d.GetILGenerator();

            // T[] result;
            g.DeclareLocal(typeof (T[]));

            // void* resultPtr;
            g.DeclareLocal(typeof (void*), true);

            // result = new T[count];
            g.Emit(OpCodes.Ldarg_1);
            g.Emit(OpCodes.Newarr, typeof (T));
            g.Emit(OpCodes.Stloc_0);

            // fixed (void* resultPtr = result)
            g.Emit(OpCodes.Ldloc_0);
            g.Emit(OpCodes.Ldc_I4_0);
            g.Emit(OpCodes.Ldelema, typeof (T));
            g.Emit(OpCodes.Stloc_1);

            // Marshal.Copy(array, offset, (IntPtr)resultPtr, length);
            g.Emit(OpCodes.Ldarg_0);
            g.Emit(OpCodes.Ldarg_2);
            g.Emit(OpCodes.Ldloc_1);
            g.Emit(OpCodes.Conv_I);
            g.EmitCall(OpCodes.Call, typeof (IntPtr).GetMethod("op_Explicit", new[] {typeof (void*)}), null);
            g.Emit(OpCodes.Ldarg_3);
            g.EmitCall(OpCodes.Call, typeof (Marshal).GetMethod("Copy", new[] {typeof (byte[]), typeof (int), typeof (IntPtr), typeof (int)}), null);

            // return result
            g.Emit(OpCodes.Ldloc_0);
            g.Emit(OpCodes.Ret);

            return (BlitParser<T>)d.CreateDelegate(typeof (BlitParser<T>));
        }
    }
}