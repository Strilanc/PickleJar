using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Strilanc.PickleJar.Internal.Bulk;

namespace Strilanc.PickleJar.Internal.Repeated {
    /// <summary>
    /// FixedRepeatParser parses contiguous values that are repeated the same number of times every time.
    /// </summary>
    internal sealed class FixedRepeatParser<T> : IParserInternal<IReadOnlyList<T>> {
        public bool AreMemoryAndSerializedRepresentationsOfValueGuaranteedToMatch { get { return false; } }
        public int? OptionalConstantSerializedLength { get { return _subParser.OptionalConstantSerializedValueLength * _count; } }

        private readonly int _count;
        private readonly IBulkParser<T> _subParser;

        public FixedRepeatParser(IBulkParser<T> bulkParser, int count) {
            this._count = count;
            _subParser = bulkParser;
        }

        public ParsedValue<IReadOnlyList<T>> Parse(ArraySegment<byte> data) {
            return _subParser.Parse(data, _count);
        }
        public InlinedParserComponents TryMakeInlinedParserComponents(Expression array, Expression offset, Expression count) {
            return null;
        }
    }
}
