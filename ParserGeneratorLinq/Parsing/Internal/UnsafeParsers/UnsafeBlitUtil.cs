using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace Strilanc.Parsing.Internal.UnsafeParsers {
    /// <summary>
    /// UnsafeBlitUtil contains utility methods used when 'parsing' with a memcpy.
    /// </summary>
    internal static class UnsafeBlitUtil {
        public delegate T UnsafeValueBlitParser<out T>(byte[] data, int offset, int length);
        public delegate T[] UnsafeArrayBlitParser<out T>(byte[] data, int itemCount, int offset, int length);

        /// <summary>
        /// Emits a method that copies the contents of an array segment over the memory representation of a value.
        /// </summary>
        public static UnsafeValueBlitParser<T> MakeUnsafeValueBlitParser<T>() {
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

            return (UnsafeValueBlitParser<T>)d.CreateDelegate(typeof(UnsafeValueBlitParser<T>));
        }

        /// <summary>
        /// Emits a method that copies the contents of an array segment over the memory representation of anew  returned array of values.
        /// </summary>
        public static UnsafeArrayBlitParser<T> MakeUnsafeArrayBlitParser<T>() {
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

            return (UnsafeArrayBlitParser<T>)d.CreateDelegate(typeof(UnsafeArrayBlitParser<T>));
        }
    }
}