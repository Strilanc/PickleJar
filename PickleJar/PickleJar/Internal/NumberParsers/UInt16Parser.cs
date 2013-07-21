using System;
using System.Linq.Expressions;

namespace Strilanc.PickleJar.Internal.NumberParsers {
    internal struct UInt16Parser : IJarInternal<UInt16> {
        private const int SerializedLength = 16 / 8;

        private readonly bool _isSystemEndian;
        public bool AreMemoryAndSerializedRepresentationsOfValueGuaranteedToMatch { get { return _isSystemEndian; } }
        public int? OptionalConstantSerializedLength { get { return SerializedLength; } }

        public UInt16Parser(Endianess endianess) {
            if (endianess != Endianess.BigEndian && endianess != Endianess.LittleEndian)
                throw new ArgumentException("Unrecognized endianess", "endianess");
            var isLittleEndian = endianess == Endianess.LittleEndian;
            _isSystemEndian = isLittleEndian == BitConverter.IsLittleEndian;
        }

        public ParsedValue<UInt16> Parse(ArraySegment<byte> data) {
            if (data.Count < SerializedLength) throw new DataFragmentException();
            var value = BitConverter.ToUInt16(data.Array, data.Offset);
            if (!_isSystemEndian) value = value.ReverseBytes();
            return new ParsedValue<UInt16>(value, SerializedLength);
        }
        public byte[] Pack(UInt16 value) {
            var v = _isSystemEndian ? value : value.ReverseBytes();
            return BitConverter.GetBytes(v);
        }

        public InlinedParserComponents TryMakeInlinedParserComponents(Expression array, Expression offset, Expression count) {
            return ParserUtil.MakeInlinedNumberParserComponents<UInt16>(_isSystemEndian, array, offset, count);
        }
    }
}
