using System;
using System.Linq;
using System.Linq.Expressions;

namespace ParserGenerator {
    public sealed class UInt16Parser : IParser<UInt16> {
        private const int SerializedLength = 16 / 8;

        private readonly bool _needToReverseBytes;
        public bool IsBlittable { get { return !_needToReverseBytes; } }
        public int? OptionalConstantSerializedLength { get { return SerializedLength; } }
        public Expression TryParseInline(Expression array, Expression offset, Expression count) {
            if (_needToReverseBytes) return null;
            return Expression.New(typeof(ParsedValue<UInt16>).GetConstructors().Single(),
                Expression.Call(typeof(BitConverter).GetMethod("ToUInt16"), array, offset),
                Expression.Constant(SerializedLength));
        }

        public UInt16Parser(Endianess endianess) {
            if (endianess != Endianess.BigEndian && endianess != Endianess.LittleEndian)
                throw new ArgumentException("Unrecognized endianess", "endianess");
            var isLittleEndian = endianess == Endianess.LittleEndian;
            var isSystemEndian = isLittleEndian == BitConverter.IsLittleEndian;
            _needToReverseBytes = !isSystemEndian;
        }

        public ParsedValue<UInt16> Parse(ArraySegment<byte> data) {
            var value = BitConverter.ToUInt16(data.Array, data.Offset);
            if (_needToReverseBytes) value = value.ReverseBytes();
            return new ParsedValue<UInt16>(value, SerializedLength);
        }
    }
}
