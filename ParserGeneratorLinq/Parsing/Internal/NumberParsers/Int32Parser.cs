using System;
using System.Linq.Expressions;

namespace Strilanc.Parsing.Internal.NumberParsers {
    internal sealed class Int32Parser : IParserInternal<Int32> {
        private const int SerializedLength = 32 / 8;

        private readonly bool _needToReverseBytes;
        public bool IsBlittable { get { return !_needToReverseBytes; } }
        public int? OptionalConstantSerializedLength { get { return SerializedLength; } }

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
        public Expression TryMakeParseFromDataExpression(Expression array, Expression offset, Expression count) {
            return NumberParseBuilderUtil.MakeParseFromDataExpression<Int32>(!_needToReverseBytes, array, offset, count);
        }
        public Expression TryMakeGetValueFromParsedExpression(Expression parsed) {
            return NumberParseBuilderUtil.MakeGetValueFromParsedExpression(parsed);
        }
        public Expression TryMakeGetCountFromParsedExpression(Expression parsed) {
            return NumberParseBuilderUtil.MakeGetCountFromParsedExpression<Int32>(parsed);
        }
    }
}
