using System;

namespace ParserGenerator {
    public struct UInt8Parser : IParser<byte> {
        public bool IsBlittable { get { return true; } }
        public int? OptionalConstantSerializedLength { get { return 1; } }

        public ParsedValue<byte> Parse(ArraySegment<byte> data) {
            var value = data.Array[data.Offset];
            return new ParsedValue<byte>(value, 1);
        }
    }
}
