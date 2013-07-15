using System;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using Strilanc.Parsing.Internal.Misc;

namespace Strilanc.Parsing.Internal.NumberParsers {
    internal static class NumberParseBuilderUtil {
        public static Expression MakeParseFromDataExpression<T>(bool isSystemEndian, Expression array, Expression offset, Expression count) {
            var numberTypes = new[] {
                typeof (byte), typeof (short), typeof (int), typeof (long),
                typeof (sbyte), typeof (ushort), typeof (uint), typeof (ulong)
            };
            if (!numberTypes.Contains(typeof(T))) throw new ArgumentException("Unrecognized number type.");

            if (typeof (T) == typeof (byte)) {
                return Expression.ArrayIndex(array, offset);
            }
            if (typeof (T) == typeof (sbyte)) {
                return Expression.Convert(Expression.ArrayIndex(array, offset), typeof(sbyte));
            }

            var value = Expression.Call(typeof(BitConverter).GetMethod("To" + typeof(T).Name), array, offset);
            if (isSystemEndian) return value;
            return Expression.Call(typeof(TwiddleUtil).GetMethod("ReverseBytes", new[] { typeof(T) }), value);
        }
        public static Expression MakeGetValueFromParsedExpression(Expression parsed) {
            return parsed;
        }
        public static Expression MakeGetCountFromParsedExpression<T>(Expression parsed) {
            return Expression.Constant(Marshal.SizeOf(typeof(T)));
        }
    }
}