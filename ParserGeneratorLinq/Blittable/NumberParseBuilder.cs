using System;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;

namespace ParserGenerator {
    public static class NumberParseBuilderUtil {
        public static Expression MakeParseFromDataExpression<T>(bool isSystemEndian, Expression array, Expression offset, Expression count) {
            var numberTypes = new[] {
                typeof (byte), typeof (short), typeof (int), typeof (long),
                typeof (sbyte), typeof (ushort), typeof (uint), typeof (ulong)
            };
            if (!numberTypes.Contains(typeof(T))) throw new ArgumentException("Unrecognized number type.");

            var byteSize = Marshal.SizeOf(typeof(T));
            if (byteSize == 1) {
                return Expression.ArrayIndex(array, offset);
            }

            var value = Expression.Call(typeof(BitConverter).GetMethod("To" + typeof(T).Name), array, offset);
            if (isSystemEndian) return value;
            return Expression.Call(typeof(TwiddleUtil).GetMethod("ReverseBytes", new[] { typeof(T) }));
        }
        public static Expression MakeGetValueFromParsedExpression(Expression parsed) {
            return parsed;
        }
        public static Expression MakeGetCountFromParsedExpression<T>(Expression parsed) {
            return Expression.Constant(Marshal.SizeOf(typeof(T)));
        }
    }
}