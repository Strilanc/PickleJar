using System;
using System.Reflection;
using System.Reflection.Emit;

namespace ParserGenerator.Blittable {
    public static class UnsafeBlitUtil {
        public delegate T UnsafeValueBlitParser<out T>(ArraySegment<byte> data);
        public delegate T[] UnsafeArrayBlitParser<out T>(ArraySegment<byte> data, int count);

        public static UnsafeValueBlitParser<T> MakeUnsafeValueBlitParser<T>() {
            var d = new DynamicMethod(
                name: "BlitParseValue" + typeof(T),
                returnType: typeof(T),
                parameterTypes: new[] { typeof(ArraySegment<byte>) },
                m: Assembly.GetExecutingAssembly().ManifestModule);

            var g = d.GetILGenerator();

            // T result = default(T);
            g.DeclareLocal(typeof(T));
            g.Emit(OpCodes.Ldloca_S, (byte)0);
            g.Emit(OpCodes.Initobj, typeof(T));

            // UnsafeCopyOver(data, (IntPtr)&result);
            g.Emit(OpCodes.Ldarg_0);
            g.Emit(OpCodes.Ldloca_S, (byte)0);
            g.Emit(OpCodes.Conv_U);
            g.EmitCall(OpCodes.Call, typeof(IntPtr).GetMethod("op_Explicit", new[] { typeof(void*) }), null);
            g.EmitCall(OpCodes.Call, typeof(UnsafeBlitUtil).GetMethod("UnsafeCopyOver"), null);

            // return result
            g.Emit(OpCodes.Ldloc_0);
            g.Emit(OpCodes.Ret);

            return (UnsafeValueBlitParser<T>)d.CreateDelegate(typeof(UnsafeValueBlitParser<T>));
        }

        public static UnsafeArrayBlitParser<T> MakeUnsafeArrayBlitParser<T>() {
            var d = new DynamicMethod(
                name: "BlitParseArray" + typeof(T),
                returnType: typeof(T[]),
                parameterTypes: new[] { typeof(ArraySegment<byte>), typeof(int) },
                m: Assembly.GetExecutingAssembly().ManifestModule);

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

            // UnsafeCopyOver((IntPtr)resultPtr);
            g.Emit(OpCodes.Ldarg_0);
            g.Emit(OpCodes.Ldloc_1);
            g.Emit(OpCodes.Conv_I);
            g.EmitCall(OpCodes.Call, typeof(IntPtr).GetMethod("op_Explicit", new[] { typeof(void*) }), null);
            g.EmitCall(OpCodes.Call, typeof(UnsafeBlitUtil).GetMethod("UnsafeCopyOver"), null);

            // return result
            g.Emit(OpCodes.Ldloc_0);
            g.Emit(OpCodes.Ret);

            return (UnsafeArrayBlitParser<T>)d.CreateDelegate(typeof(UnsafeArrayBlitParser<T>));
        }

        public static void UnsafeCopyOver(ArraySegment<byte> src, IntPtr dest) {
            unsafe {
                fixed (byte* dataArrayPtr = src.Array) {
                    var resultPtr8 = (ulong*)dest;
                    var dataPtr8 = (ulong*)(dataArrayPtr + src.Offset);

                    var i = 0;
                    while (i + 7 < src.Count) {
                        *resultPtr8 = *dataPtr8;
                        resultPtr8 += 1;
                        dataPtr8 += 1;
                        i += 8;
                    }

                    var dataPtr1 = (byte*)dataPtr8;
                    var resultPtr1 = (byte*)resultPtr8;
                    while (i < src.Count) {
                        *resultPtr1 = *dataPtr1;
                        resultPtr1 += 1;
                        dataPtr1 += 1;
                        i += 1;
                    }
                }
            }
        }
    }
}