using System;
using System.Collections.Generic;

namespace Strilanc.PickleJar.Internal.RepetitionParsers {
    /// <summary>
    /// GreedyRepeatParser parses contiguous values that are repeated until the last one ends at the end of the data.
    /// </summary>
    internal sealed class GreedyRepeatParser<T> : IParser<IReadOnlyList<T>> {
        private readonly IParser<T> _itemParser;

        public GreedyRepeatParser(IParser<T> itemParser) {
            this._itemParser = itemParser;
        }

        public ParsedValue<IReadOnlyList<T>> Parse(ArraySegment<byte> data) {
            var result = new List<T>();
            var t = 0;
            while (data.Count - t > 0) {
                var e = _itemParser.Parse(data.Skip(t));
                result.Add(e.Value);
                t += e.Consumed;
            }
            return new ParsedValue<IReadOnlyList<T>>(result.ToArray(), t);
        }
    }
}
