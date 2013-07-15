using System;
using System.Linq.Expressions;

namespace Strilanc.Parsing.Internal.RepetitionParsers {
    /// <summary>
    /// FixedRepeatParser parses contiguous values that are repeated the same number of times every time.
    /// </summary>
    internal sealed class FixedRepeatParser<T> : IParserInternal<T[]> {
        public bool AreMemoryAndSerializedRepresentationsOfValueGuaranteedToMatch { get { return false; } }
        public int? OptionalConstantSerializedLength { get { return _subParser.OptionalConstantSerializedValueLength * _count; } }

        private readonly int _count;
        private readonly IBulkParser<T> _subParser;

        public FixedRepeatParser(IBulkParser<T> bulkParser, int count) {
            this._count = count;
            _subParser = bulkParser;
        }

        public ParsedValue<T[]> Parse(ArraySegment<byte> data) {
            return _subParser.Parse(data, _count);
        }
        public Expression TryMakeParseFromDataExpression(Expression array, Expression offset, Expression count) {
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
