using System;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;

namespace Strilanc.Parsing.Internal.NumberParsers {
    /// <summary>
    /// NumberParseBuilderUtil exists to avoid repeating code in all the number parsers.
    /// It knows how to make the inlined expressions used to parse raw numbers more quickly.
    /// </summary>
    internal static class NumberParseBuilderUtil {
        public static Tuple<Expression, ParameterExpression[]> MakeParseFromDataExpression<T>(bool isSystemEndian, Expression array, Expression offset, Expression count) {
            return Tuple.Create(MakeParseFromDataExpressionHelper<T>(isSystemEndian, array, offset, count), new ParameterExpression[0]);
        }
        private static Expression MakeParseFromDataExpressionHelper<T>(bool isSystemEndian, Expression array, Expression offset, Expression count) {
            var numberTypes = new[] {
                typeof (byte), typeof (short), typeof (int), typeof (long),
                typeof (sbyte), typeof (ushort), typeof (uint), typeof (ulong)
            };
            if (!numberTypes.Contains(typeof(T))) throw new ArgumentException("Unrecognized number type.");

            if (typeof(T) == typeof(byte)) {
                return Expression.ArrayIndex(array, offset);
            }
            if (typeof(T) == typeof(sbyte)) {
                return Expression.Convert(Expression.ArrayIndex(array, offset), typeof(sbyte));
            }

            var value = Expression.Call(typeof(BitConverter).GetMethod("To" + typeof(T).Name), array, offset);
            if (isSystemEndian) return value;
            return Expression.Call(typeof(TwiddleUtil).GetMethod("ReverseBytes", new[] { typeof(T) }), value);
        }
        public static Expression MakeGetValueFromParsedExpression(Expression parsed) {
            return parsed;
        }
        public static Expression MakeGetConsumedFromParsedExpression<T>(Expression parsed) {
            return Expression.Constant(Marshal.SizeOf(typeof(T)));
        }
    }
}