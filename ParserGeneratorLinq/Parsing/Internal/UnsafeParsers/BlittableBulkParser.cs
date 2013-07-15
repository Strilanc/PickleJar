using System;

namespace Strilanc.Parsing.Internal.UnsafeParsers {
    /// <summary>
    /// BlittableBulkParser is used to parse arrays of values when memcpy'ing them is valid.
    /// Using memcpy is possible when the in-memory representation exactly matches the serialized representation.
    /// BlittableBulkParser uses unsafe code, but is an order of magnitude (or two!) faster than other parsers.
    /// </summary>
    internal sealed class BlittableBulkParser<T> : IBulkParser<T> {
        private readonly UnsafeBlitUtil.UnsafeArrayBlitParser<T> _parser;
        private readonly int _itemLength;
        private BlittableBulkParser(IParserInternal<T> itemParser) {
            if (!itemParser.OptionalConstantSerializedLength.HasValue) throw new ArgumentException();
            _itemLength = itemParser.OptionalConstantSerializedLength.Value;
            _parser = UnsafeBlitUtil.MakeUnsafeArrayBlitParser<T>();
        }

        public static BlittableBulkParser<T> TryMake(IParser<T> itemParser) {
            if (itemParser == null) throw new ArgumentNullException("itemParser");
            var r = itemParser as IParserInternal<T>;
            if (r == null) return null;
            if (!r.AreMemoryAndSerializedRepresentationsOfValueGuaranteedToMatch) return null;
            if (!r.OptionalConstantSerializedLength.HasValue) return null;
            return new BlittableBulkParser<T>(r);
        }

        public ParsedValue<T[]> Parse(ArraySegment<byte> data, int count) {
            var length = count*_itemLength;
            if (data.Count < length) throw new InvalidOperationException("Fragment");
            var value = _parser(data.Array, count, data.Offset, length);
            return new ParsedValue<T[]>(value, length);
        }
        public int? OptionalConstantSerializedValueLength { get { return _itemLength; } }
    }
}