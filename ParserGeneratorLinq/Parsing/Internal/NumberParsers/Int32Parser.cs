using System;
using System.Linq.Expressions;

namespace Strilanc.Parsing.Internal.NumberParsers {
    internal struct Int32Parser : IParserInternal<Int32> {
        private const int SerializedLength = 32 / 8;

        private readonly bool _isSystemEndian;
        public bool AreMemoryAndSerializedRepresentationsOfValueGuaranteedToMatch { get { return _isSystemEndian; } }
        public int? OptionalConstantSerializedLength { get { return SerializedLength; } }

        public Int32Parser(Endianess endianess) {
            if (endianess != Endianess.BigEndian && endianess != Endianess.LittleEndian)
                throw new ArgumentException("Unrecognized endianess", "endianess");
            var isLittleEndian = endianess == Endianess.LittleEndian;
            _isSystemEndian = isLittleEndian == BitConverter.IsLittleEndian;
        }

        public ParsedValue<Int32> Parse(ArraySegment<byte> data) {
            var value = BitConverter.ToInt32(data.Array, data.Offset);
            if (!_isSystemEndian) value = value.ReverseBytes();
            return new ParsedValue<Int32>(value, SerializedLength);
        }
        public Tuple<Expression, ParameterExpression[]> TryMakeParseFromDataExpression(Expression array, Expression offset, Expression count) {
            return NumberParseBuilderUtil.MakeParseFromDataExpression<Int32>(_isSystemEndian, array, offset, count);
        }
        public Expression TryMakeGetValueFromParsedExpression(Expression parsed) {
            return NumberParseBuilderUtil.MakeGetValueFromParsedExpression(parsed);
        }
        public Expression TryMakeGetConsumedFromParsedExpression(Expression parsed) {
            return NumberParseBuilderUtil.MakeGetConsumedFromParsedExpression<Int32>(parsed);
        }
    }
}
