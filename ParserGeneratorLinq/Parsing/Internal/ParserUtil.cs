using System;
using System.Linq.Expressions;
using Strilanc.Parsing.Internal.RepetitionParsers;
using Strilanc.Parsing.Internal.UnsafeParsers;

namespace Strilanc.Parsing.Internal {
    internal static class ParserUtil {
        public static IArrayParser<T> Array<T>(this IParser<T> itemParser) {
            if (itemParser == null) throw new ArgumentNullException("itemParser");

            return (IArrayParser<T>)BlittableArrayParser<T>.TryMake(itemParser)
                   ?? new ExpressionArrayParser<T>(itemParser);
        }

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
        public static ParsedValue<T> AsParsed<T>(this T value, int consumed) {
            return new ParsedValue<T>(value, consumed);
        }
        public static ParsedValue<TOut> Select<TIn, TOut>(this ParsedValue<TIn> value, Func<TIn, TOut> projection) {
            return new ParsedValue<TOut>(projection(value.Value), value.Consumed);
        }
    }
}
