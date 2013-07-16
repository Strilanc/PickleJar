using System;
using System.Linq.Expressions;

namespace Strilanc.Parsing.Internal.StructuredParsers {
    /// <summary>
    /// TupleParser is used to parse consecutive values that have different types.
    /// </summary>
    internal sealed class TupleParser<T1, T2> : IParserInternal<Tuple<T1, T2>> {
        public readonly IParser<T1> SubParser1;
        public readonly IParser<T2> SubParser2;
        public TupleParser(IParser<T1> subParser1, IParser<T2> subParser2) {
            SubParser1 = subParser1;
            SubParser2 = subParser2;
        }
        public ParsedValue<Tuple<T1, T2>> Parse(ArraySegment<byte> data) {
            var r1 = SubParser1.Parse(data);
            var r2 = SubParser2.Parse(new ArraySegment<byte>(data.Array, data.Offset + r1.Consumed, data.Count - r1.Consumed));
            return new ParsedValue<Tuple<T1, T2>>(Tuple.Create(r1.Value, r2.Value), r1.Consumed + r2.Consumed);
        }
        public bool AreMemoryAndSerializedRepresentationsOfValueGuaranteedToMatch { get { return false; } }
        public int? OptionalConstantSerializedLength { get { return SubParser1.OptionalConstantSerializedLength() + SubParser2.OptionalConstantSerializedLength(); } }
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
