using System;
using System.Linq;
using System.Linq.Expressions;

namespace ParserGenerator {
    public sealed class UInt64Parser : IParser<UInt64> {
        private const int SerializedLength = 64 / 8;

        private readonly bool _needToReverseBytes;
        public bool IsBlittable { get { return !_needToReverseBytes; } }
        public int? OptionalConstantSerializedLength { get { return SerializedLength; } }
        public Expression TryParseInline(Expression array, Expression offset, Expression count) {
            if (_needToReverseBytes) return null;
            return Expression.New(typeof(ParsedValue<UInt64>).GetConstructors().Single(),
                Expression.Call(typeof(BitConverter).GetMethod("ToUInt64"), array, offset),
                Expression.Constant(SerializedLength));
        }

        public UInt64Parser(Endianess endianess) {
            if (endianess != Endianess.BigEndian && endianess != Endianess.LittleEndian)
                throw new ArgumentException("Unrecognized endianess", "endianess");
            var isLittleEndian = endianess == Endianess.LittleEndian;
            var isSystemEndian = isLittleEndian == BitConverter.IsLittleEndian;
            _needToReverseBytes = !isSystemEndian;
        }

        public ParsedValue<UInt64> Parse(ArraySegment<byte> data) {
            var value = BitConverter.ToUInt64(data.Array, data.Offset);
            if (_needToReverseBytes) value = value.ReverseBytes();
            return new ParsedValue<UInt64>(value, SerializedLength);
        }
    }
}
