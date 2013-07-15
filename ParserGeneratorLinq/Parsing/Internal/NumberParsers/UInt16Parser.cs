using System;
using System.Linq.Expressions;
using Strilanc.Parsing.Internal.Misc;

namespace Strilanc.Parsing.Internal.NumberParsers {
    internal sealed class UInt16Parser : IParserInternal<UInt16> {
        private const int SerializedLength = 16 / 8;

        private readonly bool _needToReverseBytes;
        public bool IsBlittable { get { return !_needToReverseBytes; } }
        public int? OptionalConstantSerializedLength { get { return SerializedLength; } }

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

        public Expression TryMakeParseFromDataExpression(Expression array, Expression offset, Expression count) {
            return NumberParseBuilderUtil.MakeParseFromDataExpression<UInt16>(!_needToReverseBytes, array, offset, count);
        }
        public Expression TryMakeGetValueFromParsedExpression(Expression parsed) {
            return NumberParseBuilderUtil.MakeGetValueFromParsedExpression(parsed);
        }
        public Expression TryMakeGetCountFromParsedExpression(Expression parsed) {
            return NumberParseBuilderUtil.MakeGetCountFromParsedExpression<UInt16>(parsed);
        }
    }
}
