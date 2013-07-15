using System;
using Strilanc.Parsing.Internal.Misc;

namespace Strilanc.Parsing.Internal.UnsafeParsers {
    internal sealed class BlittableArrayParser<T> : IArrayParser<T> {
        private readonly UnsafeBlitUtil.UnsafeArrayBlitParser<T> _parser;
        private readonly int _itemLength;
        private BlittableArrayParser(IParserInternal<T> itemParser) {
            _itemLength = itemParser.OptionalConstantSerializedLength.Value;
            _parser = UnsafeBlitUtil.MakeUnsafeArrayBlitParser<T>();
        }

        public static BlittableArrayParser<T> TryMake(IParser<T> itemParser) {
            if (itemParser == null) throw new ArgumentNullException("itemParser");
            var r = itemParser as IParserInternal<T>;
            if (r == null) return null;
            if (!r.IsBlittable) return null;
            if (!r.OptionalConstantSerializedLength.HasValue) return null;
            return new BlittableArrayParser<T>(r);
        }

        public ParsedValue<T[]> Parse(ArraySegment<byte> data, int count) {
            var length = count*_itemLength;
            if (data.Count < length) throw new InvalidOperationException("Fragment");
            var value = _parser(data.Array, count, data.Offset, length);
            return new ParsedValue<T[]>(value, length);
        }
        public bool IsValueBlittable { get { return true; } }
        public int? OptionalConstantSerializedValueLength { get { return _itemLength; } }
    }
}