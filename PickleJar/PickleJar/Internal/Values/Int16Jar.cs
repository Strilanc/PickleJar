using System;
using System.Linq.Expressions;

namespace Strilanc.PickleJar.Internal.Values {
    internal struct Int16Jar : IJarMetadataInternal, IJar<Int16> {
        private const int SerializedLength = 16/8;

        private readonly bool _isSystemEndian;
        public bool IsBlittable { get { return _isSystemEndian; } }
        public int? OptionalConstantSerializedLength { get { return SerializedLength; } }
        public bool CanBeFollowed { get { return true; } }

        public Int16Jar(Endianess endianess) {
            if (endianess != Endianess.BigEndian && endianess != Endianess.LittleEndian)
                throw new ArgumentException("Unrecognized endianess", "endianess");
            var isLittleEndian = endianess == Endianess.LittleEndian;
            _isSystemEndian = isLittleEndian == BitConverter.IsLittleEndian;
        }

        public ParsedValue<Int16> Parse(ArraySegment<byte> data) {
            if (data.Count < SerializedLength) throw new DataFragmentException();
            var value = BitConverter.ToInt16(data.Array, data.Offset);
            if (!_isSystemEndian) value = value.ReverseBytes();
            return value.AsParsed(SerializedLength);
        }
        public byte[] Pack(Int16 value) {
            var v = _isSystemEndian ? value : value.ReverseBytes();
            return BitConverter.GetBytes(v);
        }
        public InlinedParserComponents TryMakeInlinedParserComponents(Expression array, Expression offset, Expression count) {
            return ParserUtil.MakeInlinedNumberParserComponents<Int16>(_isSystemEndian, array, offset, count);
        }
        public override string ToString() {
            var end = _isSystemEndian ? ""
                    : BitConverter.IsLittleEndian ? "BigEndian"
                    : "LittleEndian";
            return "Int16" + end;
        }
    }
}
