using System;
using System.Collections.Generic;

namespace Strilanc.PickleJar.Internal.Repeated {
    /// <summary>
    /// BulkParser is the simplest implementation of IBulkParser.
    /// BulkParser works in the most obvious way: applying the item parser again and again.
    /// The other array parsers are generally prefered because they perform dynamic optimizations.
    /// </summary>
    internal sealed class BulkParser<T> : IBulkParser<T> {
        private readonly IParser<T> _itemParser;
        public BulkParser(IParser<T> itemParser)  {
            _itemParser = itemParser;
        }
        public ParsedValue<IReadOnlyList<T>> Parse(ArraySegment<byte> data, int count) {
            var r = new T[count];
            var t = 0;
            for (var i = 0; i < count; i++) {
                var e = _itemParser.Parse(data.Skip(t));
                t += e.Consumed;
                r[i] = e.Value;
            }
            return new ParsedValue<IReadOnlyList<T>>(r, t);
        }
        public int? OptionalConstantSerializedValueLength { get { return _itemParser.OptionalConstantSerializedLength(); } }
    }
}

