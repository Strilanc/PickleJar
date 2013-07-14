using System;
using System.Linq;
using System.Linq.Expressions;

namespace ParserGenerator {
    internal sealed class UInt64Parser : IParser<UInt64> {
        private const int SerializedLength = 64 / 8;

        private readonly bool _needToReverseBytes;
        public bool IsBlittable { get { return !_needToReverseBytes; } }
        public int? OptionalConstantSerializedLength { get { return SerializedLength; } }

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

        public Expression TryMakeParseFromDataExpression(Expression array, Expression offset, Expression count) {
            return NumberParseBuilderUtil.MakeParseFromDataExpression<UInt64>(!_needToReverseBytes, array, offset, count);
        }
        public Expression TryMakeGetValueFromParsedExpression(Expression parsed) {
            return NumberParseBuilderUtil.MakeGetValueFromParsedExpression(parsed);
        }
        public Expression TryMakeGetCountFromParsedExpression(Expression parsed) {
            return NumberParseBuilderUtil.MakeGetCountFromParsedExpression<UInt64>(parsed);
        }
    }
}
