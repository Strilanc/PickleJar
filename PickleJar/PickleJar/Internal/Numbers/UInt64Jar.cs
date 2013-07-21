using System;
using System.Linq.Expressions;

namespace Strilanc.PickleJar.Internal.Numbers {
    internal struct UInt64Jar : IJarInternal<UInt64> {
        private const int SerializedLength = 64 / 8;

        private readonly bool _isSystemEndian;
        public bool AreMemoryAndSerializedRepresentationsOfValueGuaranteedToMatch { get { return _isSystemEndian; } }
        public int? OptionalConstantSerializedLength { get { return SerializedLength; } }

        public UInt64Jar(Endianess endianess) {
            if (endianess != Endianess.BigEndian && endianess != Endianess.LittleEndian)
                throw new ArgumentException("Unrecognized endianess", "endianess");
            var isLittleEndian = endianess == Endianess.LittleEndian;
            _isSystemEndian = isLittleEndian == BitConverter.IsLittleEndian;
        }

        public ParsedValue<UInt64> Parse(ArraySegment<byte> data) {
            if (data.Count < SerializedLength) throw new DataFragmentException();
            var value = BitConverter.ToUInt64(data.Array, data.Offset);
            if (!_isSystemEndian) value = value.ReverseBytes();
            return new ParsedValue<UInt64>(value, SerializedLength);
        }
        public byte[] Pack(UInt64 value) {
            var v = _isSystemEndian ? value : value.ReverseBytes();
            return BitConverter.GetBytes(v);
        }

        public InlinedParserComponents TryMakeInlinedParserComponents(Expression array, Expression offset, Expression count) {
            return ParserUtil.MakeInlinedNumberParserComponents<UInt64>(_isSystemEndian, array, offset, count);
        }
    }
}
