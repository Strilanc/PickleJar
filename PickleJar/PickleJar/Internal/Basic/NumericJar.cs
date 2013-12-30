using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using Strilanc.PickleJar.Internal.Misc;
using Strilanc.PickleJar.Internal.RuntimeSpecialization;
using System.Linq;

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

            return AnonymousJar.CreateSpecialized<TNumber>(
                parseSpecializer: (array, offset, count) => SpecializedNumberParserPartsForType<TNumber>(isSystemEndian, array, offset, count),
                packSpecializer: v => SpecializedNumberPackerPartsForType<TNumber>(v, endianess),
                canBeFollowed: true,
                isBlittable: isSystemEndian,
                constLength: size,
                desc: () => string.Format("{0}{1}", NameForType<TNumber>(), isSystemEndian ? "" : isLittleEndian ? "LittleEndian" : "BigEndian"),
                components: null);
        }

        private static SpecializedParserParts SpecializedNumberParserPartsForType<TNumber>(bool isSystemEndian, Expression array, Expression offset, Expression count) {
            var varParsedNumber = Expression.Parameter(typeof(TNumber));
            return new SpecializedParserParts(
                parseDoer: varParsedNumber.AssignTo(SpecializedNumberParseExpressionForType<TNumber>(isSystemEndian, array, offset, count)),
                valueGetter: varParsedNumber,
                consumedCountGetter: SizeOf<TNumber>().ConstExpr(),
                storage: new SpecializedParserStorageParts(new[] {varParsedNumber}, new ParameterExpression[0]));
        }

        private static SpecializedPackerParts SpecializedNumberPackerPartsForType<TNumber>(Expression value, Endianess endianess) {
            PackDoer packDoer;
            if (typeof(TNumber) == typeof(float) || typeof(TNumber) == typeof(double)) {
                packDoer = (array, offset) => {
                    var input = value.ReverseBytesIf<TNumber>(!endianess.IsSystemEndian());
                    var getByteMethods = typeof(BitConverter).GetMethod("GetBytes", new[] {typeof(TNumber)});
                    var output = Expression.Call(getByteMethods, input);
                    var copyMethod = typeof(Array).GetMethod("Copy", new[] {typeof(Array), typeof(Array), typeof(int)});
                    return Expression.Call(copyMethod, output, array, SizeOf<TNumber>().ConstExpr());
                };
            } else {
                packDoer = (array, offset) => SizeOf<TNumber>()
                                                  .Range()
                                                  .Select(i => array.AccessIndex(offset.Plus(i)).AssignTo(value.ExtractByte<TNumber>(i, endianess)))
                                                  .Block();
            }

            return new SpecializedPackerParts(
                sizePrecomputer: Expression.Empty(),
                precomputedSizeGetter: SizeOf<TNumber>().ConstExpr(),
                precomputedSizeStorage: new ParameterExpression[0],
                packDoer: packDoer);
        }

        private static Expression SpecializedNumberParseExpressionForType<TNumber>(bool isSystemEndian, Expression array, Expression offset, Expression count) {
            var boundsCheck = count.IsLessThan(SizeOf<TNumber>().ConstExpr()).IfThenDo(DataFragmentException.CachedThrowExpression);

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

            var input = value.ReverseBytesIf<TNumber>(!isSystemEndian);
            var getByteMethods = typeof(BitConverter).GetMethod("GetBytes", new[] {typeof(TNumber)});
            return Expression.Call(getByteMethods, input);
        }

        private static int SizeOf<TNumber>() {
            return Marshal.SizeOf(typeof(TNumber));
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
        private static Expression ExtractByte<TNumber>(this Expression expression, int littleEndianOffset, Endianess endianess) {
            if (!endianess.IsLittleEndian()) {
                return expression.ExtractByte<TNumber>(SizeOf<TNumber>() - littleEndianOffset - 1, Endianess.LittleEndian);
            }
            if (littleEndianOffset > 0) {
                return Expression.RightShift(expression, (littleEndianOffset * 8).ConstExpr()).ExtractByte<TNumber>(0, endianess);
            }
            return expression.ConvertIfNecessary<byte>();
        }
    }
}
