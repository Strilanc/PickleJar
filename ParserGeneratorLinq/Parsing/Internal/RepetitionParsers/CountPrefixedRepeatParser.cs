using System;
using System.Collections.Generic;

namespace Strilanc.Parsing.Internal.RepetitionParsers {
    /// <summary>
    /// CountPrefixedRepeatParser parses contiguous values that are repeated and prefixed by some sort of serialized repetition count.
    /// </summary>
    internal struct CountPrefixedRepeatParser<T> : IParser<IReadOnlyList<T>> {
        private readonly IParser<int> _counter; 
        private readonly IBulkParser<T> _repeatParser;

        public CountPrefixedRepeatParser(IParser<int> counter, IBulkParser<T> subParser) {
            this._counter = counter;
            this._repeatParser = subParser;
        }

        public ParsedValue<IReadOnlyList<T>> Parse(ArraySegment<byte> data) {
            var count = _counter.Parse(data);
            var array = _repeatParser.Parse(data.Skip(count.Consumed), count.Value);
            return new ParsedValue<IReadOnlyList<T>>(array.Value, count.Consumed + array.Consumed);
        }
    }
}
