using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using Strilanc.PickleJar.Internal.Misc;
using Strilanc.PickleJar.Internal.RuntimeSpecialization;

namespace Strilanc.PickleJar.Internal.Basic {
    internal static class NumericJar {
        private static HashSet<Type> StandardNumericTypes {
            get {
                return new HashSet<Type>(new[] {
                    typeof(byte),
                    typeof(sbyte),

                    typeof(short),
                    typeof(ushort),

                    typeof(int),
                    typeof(uint),

                    typeof(long),
                    typeof(ulong),

                    typeof(float),
                    typeof(double)
                });
            }
        }
        private static string NameForType<T>() {
            var t = typeof(T);
            if (t == typeof(byte)) return "UInt8";
            if (t == typeof(sbyte)) return "Int8";
            if (t == typeof(short)) return "Int16";
            if (t == typeof(ushort)) return "UInt16";
            if (t == typeof(int)) return "Int32";
            if (t == typeof(uint)) return "UInt32";
            if (t == typeof(long)) return "Int64";
            if (t == typeof(ulong)) return "UInt64";
            if (t == typeof(float)) return "Float32";
            if (t == typeof(double)) return "Float64";
            throw new ArgumentOutOfRangeException(typeof(T).Name);
        }

        public static IJar<TNumber> CreateForType<TNumber>(Endianess endianess) {
            if (!StandardNumericTypes.Contains(typeof(TNumber))) throw new ArgumentException("Unrecognized number type.");

            var size = Marshal.SizeOf(typeof(TNumber));

            var isLittleEndian = endianess == Endianess.LittleEndian;
            var isSystemEndian = size == 1 || isLittleEndian == BitConverter.IsLittleEndian;

            return AnonymousJar.CreateSpecialized(
                specializedParserMaker: (array, offset, count) => SpecializedNumberParserPartsForType<TNumber>(isSystemEndian, array, offset, count),
                packer: SpecializedNumberPackerForType<TNumber>(isSystemEndian),
                canBeFollowed: true,
                isBlittable: isSystemEndian,
                constLength: size,
                desc: () => string.Format("{0}{1}", NameForType<TNumber>(), isSystemEndian ? "" : isLittleEndian ? "LittleEndian" : "BigEndian"),
                components: null);
        }

        private static SpecializedParserParts SpecializedNumberParserPartsForType<TNumber>(bool isSystemEndian, Expression array, Expression offset, Expression count) {
            var varParsedNumber = Expression.Parameter(typeof(TNumber));
            return new SpecializedParserParts(
                parseDoer: Expression.Assign(varParsedNumber, SpecializedNumberParseExpressionForType<TNumber>(isSystemEndian, array, offset, count)),
                valueGetter: varParsedNumber,
                consumedCountGetter: Expression.Constant(Marshal.SizeOf(typeof(TNumber))),
                storage: new SpecializedParserResultStorageParts(new[] {varParsedNumber}, new ParameterExpression[0]));
        }

        private static Func<TNumber, byte[]> SpecializedNumberPackerForType<TNumber>(bool isSystemEndian) {
            var input = Expression.Parameter(typeof(TNumber));
            var body = SpecializedNumberPackExpressionForType<TNumber>(isSystemEndian, input);
            var method = Expression.Lambda<Func<TNumber, byte[]>>(body, input);
            return method.Compile();
        }

        private static Expression SpecializedNumberParseExpressionForType<TNumber>(bool isSystemEndian, Expression array, Expression offset, Expression count) {
            var boundsCheck = Expression.IfThen(Expression.LessThan(count, Expression.Constant(Marshal.SizeOf(typeof(TNumber)))),
                                                DataFragmentException.CachedThrowExpression);

            if (typeof(TNumber) == typeof(byte) || typeof(TNumber) == typeof(sbyte)) {
                var v = Expression.ArrayIndex(array, offset).ConvertIfNecessary<TNumber>();
                return Expression.Block(boundsCheck, v);
            }

            var value = Expression.Call(typeof(BitConverter).GetMethod("To" + typeof(TNumber).Name), array, offset);
            var result = ReverseBytesIf<TNumber>(value, !isSystemEndian);
            return Expression.Block(boundsCheck, result);
        }
        private static Expression SpecializedNumberPackExpressionForType<TNumber>(bool isSystemEndian, Expression value) {
            if (typeof(TNumber) == typeof(byte) || typeof(TNumber) == typeof(sbyte)) {
                return Expression.NewArrayInit(typeof(byte), value.ConvertIfNecessary<byte>());
            }

            var input = ReverseBytesIf<TNumber>(value, !isSystemEndian);
            var getByteMethods = typeof(BitConverter).GetMethod("GetBytes", new[] {typeof(TNumber)});
            return Expression.Call(getByteMethods, input);
        }

        private static Expression ConvertIfNecessary<TDesired>(this Expression expression) {
            return expression.Type == typeof(TDesired)
                 ? expression
                 : Expression.Convert(expression, typeof(TDesired));
        }
        private static Expression ReverseBytesIf<T>(this Expression expression, bool doReverse) {
            return doReverse
                 ? Expression.Call(typeof(TwiddleUtil).GetMethod("ReverseBytes", new[] {typeof(T)}), expression)
                 : expression;
        }
    }
}
