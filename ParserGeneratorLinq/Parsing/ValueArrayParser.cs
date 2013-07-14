using System;

namespace ParserGenerator {
    internal sealed class ValueArrayParser<T> : IArrayParser<T> {
        private readonly IParser<T> _itemParser;
        public ValueArrayParser(IParser<T> itemParser)  {
            _itemParser = itemParser;
        }
        public ParsedValue<T[]> Parse(ArraySegment<byte> data, int count) {
            var r = new T[count];
            var t = 0;
            for (var i = 0; i < count; i++) {
                var e = _itemParser.Parse(data.Skip(t));
                t += e.Consumed;
                r[i] = e.Value;
            }
            return new ParsedValue<T[]>(r, t);
        }
        public bool IsValueBlittable { get { return _itemParser.IsBlittable; } }
        public int? OptionalConstantSerializedValueLength { get { return _itemParser.OptionalConstantSerializedLength; } }
    }
}

