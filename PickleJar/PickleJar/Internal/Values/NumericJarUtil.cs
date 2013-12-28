using System;
using System.Linq.Expressions;
using System.Linq;
using System.Runtime.InteropServices;
using Strilanc.PickleJar.Internal.RuntimeSpecialization;

namespace Strilanc.PickleJar.Internal.Values {
    internal static class NumericJarUtil {
        private static Type[] StandardNumericTypes {
            get {
                return new[] {
                    typeof (byte),
                    typeof (sbyte), 
                    
                    typeof (short),
                    typeof (ushort), 
                    
                    typeof (int),
                    typeof (uint), 

                    typeof (long),
                    typeof (ulong),
                    
                    typeof (float), 
                    typeof (double)
                };
            }
        }
        private static string NameForType<T>() {
            var t = typeof (T);
            if (t == typeof (byte)) return "UInt8";
            if (t == typeof (sbyte)) return "Int8";
            if (t == typeof (short)) return "Int16";
            if (t == typeof (ushort)) return "UInt16";
            if (t == typeof (int)) return "Int32";
            if (t == typeof (uint)) return "UInt32";
            if (t == typeof (long)) return "Int64";
            if (t == typeof (ulong)) return "UInt64";
            if (t == typeof (float)) return "Float32";
            if (t == typeof (double)) return "Float64";
            throw new ArgumentOutOfRangeException(typeof(T).Name);
        }

        public static IJar<T> MakeStandardNumericJar<T>(Endianess endianess) {
            var size = Marshal.SizeOf(typeof(T));

            var isLittleEndian = endianess == Endianess.LittleEndian;
            var isSystemEndian = size == 1 || isLittleEndian == BitConverter.IsLittleEndian;

            return AnonymousJar.CreateFrom(
                parser: (array, offset, count) => MakeInlinedNumberParserComponents<T>(isSystemEndian, array, offset, count),
                packer: MakeInlinedNumberPacker<T>(isSystemEndian),
                canBeFollowed: true,
                isBlittable: isSystemEndian,
                constLength: size,
                desc: () => string.Format("{0}{1}", NameForType<T>(), isSystemEndian ? "" : isLittleEndian ? "LittleEndian" : "BigEndian"),
                components: null);
        }

        public static SpecializedParserParts MakeInlinedNumberParserComponents<T>(bool isSystemEndian, Expression array, Expression offset, Expression count) {
            var varParsedNumber = Expression.Parameter(typeof(T));
            return new SpecializedParserParts(
                parseDoer: Expression.Assign(varParsedNumber, MakeInlinedNumberParserExpression<T>(isSystemEndian, array, offset, count)),
                valueGetter: varParsedNumber,
                consumedCountGetter: Expression.Constant(Marshal.SizeOf(typeof(T))),
                storage: new SpecializedParserResultStorageParts(new[] { varParsedNumber }, new ParameterExpression[0]));
        }

        private static Func<T, byte[]> MakeInlinedNumberPacker<T>(bool isSystemEndian) {
            var value = Expression.Parameter(typeof (T));
            var body = MakeInlinedNumberPackerExpression<T>(isSystemEndian, value);
            var method = Expression.Lambda<Func<T, byte[]>>(body, new[] {value});
            return method.Compile();
        }
        private static Expression MakeInlinedNumberPackerExpression<T>(bool isSystemEndian, Expression value) {
            if (!StandardNumericTypes.Contains(typeof(T))) throw new ArgumentException("Unrecognized number type.");

            if (typeof (T) == typeof (byte) || typeof(T) == typeof(sbyte)) {
                var byteValue = typeof (T) == typeof (byte) ? value : Expression.Convert(value, typeof (byte));
                return Expression.NewArrayInit(typeof(byte), byteValue);
            }

            var input = ConditionalReverseExpression<T>(value, !isSystemEndian);
            var getByteMethods = typeof(BitConverter).GetMethod("GetBytes", new[] { typeof(T) });
            return Expression.Call(getByteMethods, input);
        }

        private static Expression ConditionalReverseExpression<T>(Expression expression, bool doReverse) {
            return doReverse 
                 ? Expression.Call(typeof (TwiddleUtil).GetMethod("ReverseBytes", new[] {typeof (T)}), expression) 
                 : expression;
        }
        private static Expression MakeInlinedNumberParserExpression<T>(bool isSystemEndian, Expression array, Expression offset, Expression count) {
            if (!StandardNumericTypes.Contains(typeof(T))) throw new ArgumentException("Unrecognized number type.");

            var boundsCheck = Expression.IfThen(Expression.LessThan(count, Expression.Constant(Marshal.SizeOf(typeof(T)))), DataFragmentException.CachedThrowExpression);

            if (typeof(T) == typeof(byte) || typeof(T) == typeof(sbyte)) {
                var v = (Expression)Expression.ArrayIndex(array, offset);
                v = typeof(T) == typeof(byte) ? v : Expression.Convert(Expression.ArrayIndex(array, offset), typeof(sbyte));
                return Expression.Block(boundsCheck, v);
            }

            var value = Expression.Call(typeof(BitConverter).GetMethod("To" + typeof(T).Name), array, offset);
            var result = ConditionalReverseExpression<T>(value, !isSystemEndian);
            return Expression.Block(boundsCheck, result);
        }
    }
}
