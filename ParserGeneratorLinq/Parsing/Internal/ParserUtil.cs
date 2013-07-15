using System;
using System.Linq.Expressions;
using Strilanc.Parsing.Internal.RepetitionParsers;
using Strilanc.Parsing.Internal.UnsafeParsers;

namespace Strilanc.Parsing.Internal {
    /// <summary>
    /// ParserUtil contains internal utility and convenience methods related to parsing and optimizing parsers.
    /// </summary>
    internal static class ParserUtil {
        /// <summary>
        /// Returns an appropriate parser capable of bulk parsing operations, based on the given item parser.
        /// </summary>
        public static IBulkParser<T> Bulk<T>(this IParser<T> itemParser) {
            if (itemParser == null) throw new ArgumentNullException("itemParser");

            return (IBulkParser<T>)BlittableBulkParser<T>.TryMake(itemParser)
                   ?? new CompiledBulkParser<T>(itemParser);
        }

        public static bool IsMemoryRepresentationGuaranteedToMatch<T>(this IParser<T> parser) {
            var r = parser as IParserInternal<T>;
            return r != null && r.AreMemoryAndSerializedRepresentationsOfValueGuaranteedToMatch;
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
                               typeof(ArraySegment<byte>).GetConstructor(new[] {typeof(byte[]), typeof(int), typeof(int)}).NotNull(), 
                               new[] {array, offset, count}) });
        }

        public static Expression MakeGetValueFromParsedExpression<T>(this IParser<T> parser, Expression parsed) {
            var r = parser as IParserInternal<T>;
            return (r == null ? null : r.TryMakeGetValueFromParsedExpression(parsed))
                   ?? Expression.MakeMemberAccess(parsed, typeof(ParsedValue<>).MakeGenericType(typeof(T)).GetField("Value"));
        }
        public static Expression MakeGetConsumedFromParsedExpression<T>(this IParser<T> parser, Expression parsed) {
            var r = parser as IParserInternal<T>;
            return (r == null ? null : r.TryMakeGetConsumedFromParsedExpression(parsed))
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
