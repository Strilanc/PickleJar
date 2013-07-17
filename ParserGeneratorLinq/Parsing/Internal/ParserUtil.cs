using System;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using Strilanc.Parsing.Internal.RepetitionParsers;
using Strilanc.Parsing.Internal.StructuredParsers;
using Strilanc.Parsing.Internal.UnsafeParsers;
using System.Linq;

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
        public static ParsedValue<T> AsParsed<T>(this T value, int consumed) {
            return new ParsedValue<T>(value, consumed);
        }
        public static ParsedValue<TOut> Select<TIn, TOut>(this ParsedValue<TIn> value, Func<TIn, TOut> projection) {
            return new ParsedValue<TOut>(projection(value.Value), value.Consumed);
        }

        public static bool AreMemoryAndSerializedRepresentationsOfValueGuaranteedToMatch<T>(this IParser<T> parser) {
            var r = parser as IParserInternal<T>;
            return r != null && r.AreMemoryAndSerializedRepresentationsOfValueGuaranteedToMatch;
        }
        public static bool AreMemoryAndSerializedRepresentationsOfValueGuaranteedToMatch(this IFieldParser parser) {
            var r = parser as IFieldParserInternal;
            return r != null && r.AreMemoryAndSerializedRepresentationsOfValueGuaranteedToMatch;
        }

        public static int? OptionalConstantSerializedLength<T>(this IParser<T> parser) {
            var r = parser as IParserInternal<T>;
            return r == null ? null : r.OptionalConstantSerializedLength;
        }
        public static int? OptionalConstantSerializedLength(this IFieldParser parser) {
            var r = parser as IFieldParserInternal;
            return r == null ? null : r.OptionalConstantSerializedLength;
        }

        private static InlinedParserComponents MakeDefaultInlinedParserComponents(object parser, Type valueType, Expression array, Expression offset, Expression count) {
            var resultVar = Expression.Variable(valueType, "parsed");
            var parse = Expression.Call(
                Expression.Constant(parser),
                typeof(IParser<>).MakeGenericType(valueType).GetMethod("Parse"),
                new Expression[] {
                    Expression.New(
                        typeof (ArraySegment<byte>).GetConstructor(new[] {typeof (byte[]), typeof (int), typeof (int)}).NotNull(),
                        new[] {array, offset, count})
                });

            return new InlinedParserComponents(
                performParse: Expression.Assign(resultVar, parse),
                afterParseValueGetter: Expression.MakeMemberAccess(resultVar, typeof(ParsedValue<>).MakeGenericType(valueType).GetField("Value")),
                afterParseConsumedGetter: Expression.MakeMemberAccess(resultVar, typeof(ParsedValue<>).MakeGenericType(valueType).GetField("Consumed")),
                resultStorage: new[] {resultVar});
        }
        public static InlinedParserComponents MakeInlinedParserComponents<T>(this IParser<T> parser, Expression array, Expression offset, Expression count) {
            var r = parser as IParserInternal<T>;
            return (r == null ? null : r.TryMakeInlinedParserComponents(array, offset, count))
                   ?? MakeDefaultInlinedParserComponents(parser, typeof(T), array, offset, count);
        }
        public static InlinedParserComponents MakeInlinedParserComponents(this IFieldParser fieldParser, Expression array, Expression offset, Expression count) {
            var r = fieldParser as IFieldParserInternal;
            return (r == null ? null : r.TryMakeInlinedParserComponents(array, offset, count))
                   ?? MakeDefaultInlinedParserComponents(fieldParser.Parser, fieldParser.ParserValueType, array, offset, count);
        }

        public static InlinedParserComponents MakeInlinedNumberParserComponents<T>(bool isSystemEndian, Expression array, Expression offset, Expression count) {
            var varParsedNumber = Expression.Parameter(typeof(T));
            return new InlinedParserComponents(
                performParse: Expression.Assign(varParsedNumber, MakeInlinedNumberParserExpression<T>(isSystemEndian, array, offset, count)),
                afterParseValueGetter: varParsedNumber,
                afterParseConsumedGetter: Expression.Constant(Marshal.SizeOf(typeof(T))),
                resultStorage: new[] {varParsedNumber});
        }
        private static Expression MakeInlinedNumberParserExpression<T>(bool isSystemEndian, Expression array, Expression offset, Expression count) {
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
    }
}
