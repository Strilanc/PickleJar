using System;
using System.Linq;
using System.Linq.Expressions;

namespace ParserGenerator {
    public sealed class Int32Parser : IParser<Int32> {
        private const int SerializedLength = 32 / 8;

        private readonly bool _needToReverseBytes;
        public bool IsBlittable { get { return !_needToReverseBytes; } }
        public int? OptionalConstantSerializedLength { get { return SerializedLength; } }
        public Expression TryParseInline(Expression array, Expression offset, Expression count) {
            if (_needToReverseBytes) return null;
            return Expression.New(typeof(ParsedValue<Int32>).GetConstructors().Single(),
                Expression.Call(typeof(BitConverter).GetMethod("ToInt32"), array, offset),
                Expression.Constant(SerializedLength));
        }

        public Int32Parser(Endianess endianess) {
            if (endianess != Endianess.BigEndian && endianess != Endianess.LittleEndian)
                throw new ArgumentException("Unrecognized endianess", "endianess");
            var isLittleEndian = endianess == Endianess.LittleEndian;
            var isSystemEndian = isLittleEndian == BitConverter.IsLittleEndian;
            _needToReverseBytes = !isSystemEndian;
        }

        public ParsedValue<Int32> Parse(ArraySegment<byte> data) {
            var value = BitConverter.ToInt32(data.Array, data.Offset);
            if (_needToReverseBytes) value = value.ReverseBytes();
            return new ParsedValue<Int32>(value, SerializedLength);
        }
    }
}
