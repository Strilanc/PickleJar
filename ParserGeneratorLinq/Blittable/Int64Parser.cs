using System;
using System.Linq;
using System.Linq.Expressions;

namespace ParserGenerator {
    public sealed class Int64Parser : IParser<Int64> {
        private const int SerializedLength = 64 / 8;

        private readonly bool _needToReverseBytes;
        public bool IsBlittable { get { return !_needToReverseBytes; } }
        public int? OptionalConstantSerializedLength { get { return SerializedLength; } }
        public Expression TryParseInline(Expression array, Expression offset, Expression count) {
            if (_needToReverseBytes) return null;
            return Expression.New(typeof(ParsedValue<Int64>).GetConstructors().Single(),
                Expression.Call(typeof(BitConverter).GetMethod("ToInt64"), array, offset),
                Expression.Constant(SerializedLength));
        }

        public Int64Parser(Endianess endianess) {
            if (endianess != Endianess.BigEndian && endianess != Endianess.LittleEndian)
                throw new ArgumentException("Unrecognized endianess", "endianess");
            var isLittleEndian = endianess == Endianess.LittleEndian;
            var isSystemEndian = isLittleEndian == BitConverter.IsLittleEndian;
            _needToReverseBytes = !isSystemEndian;
        }

        public ParsedValue<Int64> Parse(ArraySegment<byte> data) {
            var value = BitConverter.ToInt64(data.Array, data.Offset);
            if (_needToReverseBytes) value = value.ReverseBytes();
            return new ParsedValue<Int64>(value, SerializedLength);
        }
    }
}
