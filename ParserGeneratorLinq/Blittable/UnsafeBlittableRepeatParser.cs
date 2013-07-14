using System;

namespace ParserGenerator.Blittable {
    public sealed class UnsafeBlittableRepeatParser<T> : IArrayParser<T> {
        private readonly UnsafeBlitUtil.UnsafeArrayBlitParser<T> _parser;
        private readonly int _itemLength; 
        public UnsafeBlittableRepeatParser(IParser<T> subParser) {
            if (!subParser.IsBlittable) throw new ArgumentException("!subParser.IsBlittable", "subParser");
            _itemLength = subParser.OptionalConstantSerializedLength.Value;
            _parser = UnsafeBlitUtil.MakeUnsafeArrayBlitParser<T>();
        }

        public ParsedValue<T[]> Parse(ArraySegment<byte> data, int count) {
            var length = count*_itemLength;
            if (data.Count < length) throw new InvalidOperationException("Fragment");
            var relevantData = new ArraySegment<byte>(data.Array, data.Offset, length);
            var value = _parser(relevantData, count);
            return new ParsedValue<T[]>(value, length);
        }
        public bool IsValueBlittable { get { return true; } }
        public int? OptionalConstantSerializedValueLength { get { return _itemLength; } }
    }
}