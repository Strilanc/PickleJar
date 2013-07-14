using System;

namespace ParserGenerator {
    public struct Int8Parser : IParser<sbyte> {
        public bool IsBlittable { get { return true; } }
        public int? OptionalConstantSerializedLength { get { return 1; } }

        public ParsedValue<sbyte> Parse(ArraySegment<byte> data) {
            unchecked {
                var value = (sbyte)data.Array[data.Offset];
                return new ParsedValue<sbyte>(value, 1);
            }
        }
    }
}
