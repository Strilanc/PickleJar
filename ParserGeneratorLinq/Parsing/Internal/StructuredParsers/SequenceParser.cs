using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Strilanc.Parsing.Internal.StructuredParsers {
    /// <summary>
    /// SequenceParser is used to parse consecutive values that have the same type but potentially different serialized representations.
    /// </summary>
    internal sealed class SequenceParser<T> : IParserInternal<IReadOnlyList<T>> {
        public readonly IReadOnlyList<IParser<T>> SubParsers;
        public SequenceParser(IReadOnlyList<IParser<T>> subParsers) {
            SubParsers = subParsers;
        }
        public ParsedValue<IReadOnlyList<T>> Parse(ArraySegment<byte> data) {
            var values = new List<T>();
            var total = 0;
            foreach (var r in SubParsers.Select(p => p.Parse(data.Skip(total)))) {
                total += r.Consumed;
                values.Add(r.Value);
            }
            return new ParsedValue<IReadOnlyList<T>>(values, total);
        }
        public bool AreMemoryAndSerializedRepresentationsOfValueGuaranteedToMatch { get { return false; } }
        public int? OptionalConstantSerializedLength { get { return SubParsers.Aggregate((int?)0, (a, e) => a + e.OptionalConstantSerializedLength()); } }
        public Tuple<Expression, ParameterExpression[]> TryMakeParseFromDataExpression(Expression array, Expression offset, Expression count) {
            return null;
        }
        public Expression TryMakeGetValueFromParsedExpression(Expression parsed) {
            return null;
        }
        public Expression TryMakeGetConsumedFromParsedExpression(Expression parsed) {
            return null;
        }
    }
}