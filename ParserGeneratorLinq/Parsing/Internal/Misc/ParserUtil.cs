using System;
using System.Linq.Expressions;

namespace Strilanc.Parsing.Internal.Misc {
    internal static class ParserUtil {
        public static bool IsBlittable<T>(this IParser<T> parser) {
            var r = parser as IParserInternal<T>;
            return r != null && r.IsBlittable;
        }
        public static int? OptionalConstantSerializedLength<T>(this IParser<T> parser) {
            var r = parser as IParserInternal<T>;
            return r == null ? null : r.OptionalConstantSerializedLength;
        }
        public static Expression MakeParseFromDataExpression<T>(this IParser<T> parser, Expression array, Expression offset, Expression count) {
            var r = parser as IParserInternal<T>;
            return (r == null ? null : r.TryMakeParseFromDataExpression(array, offset, count))
                   ?? Expression.Call(
                       Expression.Constant(parser),
                       typeof(IParser<T>).GetMethod("Parse"),
                       new Expression[] { 
                           Expression.New(
                               typeof(ArraySegment<byte>).GetConstructor(new[] {typeof(byte[]), typeof(int), typeof(int)}), 
                               new[] {array, offset, count}) });
        }

        public static Expression MakeGetValueFromParsedExpression<T>(this IParser<T> parser, Expression parsed) {
            var r = parser as IParserInternal<T>;
            return (r == null ? null : r.TryMakeGetValueFromParsedExpression(parsed))
                   ?? Expression.MakeMemberAccess(parsed, typeof(ParsedValue<>).MakeGenericType(typeof(T)).GetField("Value"));
        }
        public static Expression MakeGetCountFromParsedExpression<T>(this IParser<T> parser, Expression parsed) {
            var r = parser as IParserInternal<T>;
            return (r == null ? null : r.TryMakeGetCountFromParsedExpression(parsed))
                   ?? Expression.MakeMemberAccess(parsed, typeof(ParsedValue<>).MakeGenericType(typeof(T)).GetField("Consumed"));
        }    
    }
}
