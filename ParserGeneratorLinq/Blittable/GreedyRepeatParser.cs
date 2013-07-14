using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace ParserGenerator {
    public sealed class GreedyRepeatParser<T> : IParser<T[]> {
        public bool IsBlittable { get { return false; } }
        public int? OptionalConstantSerializedLength { get { return null; } }

        private readonly IParser<T> _itemParser;

        public GreedyRepeatParser(IParser<T> itemParser) {
            this._itemParser = itemParser;
        }

        public ParsedValue<T[]> Parse(ArraySegment<byte> data) {
            var result = new List<T>();
            var t = 0;
            while (data.Count - t > 0) {
                var e = _itemParser.Parse(data.Skip(t));
                result.Add(e.Value);
                t += data.Count;
            }
            return new ParsedValue<T[]>(result.ToArray(), t);
        }
        public Expression TryParseInline(Expression array, Expression offset, Expression count) {
            return null;
        }
    }
}
