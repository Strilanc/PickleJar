using System;
using System.Linq.Expressions;
using System.Linq;

namespace Strilanc.PickleJar.Internal.Values {
    internal struct Float32Jar : IJarMetadataInternal, IJar<float> {
        private const int SerializedLength = 32/8;

        private readonly bool _isSystemEndian;
        public bool IsBlittable { get { return true; } }
        public int? OptionalConstantSerializedLength { get { return SerializedLength; } }
        public bool CanBeFollowed { get { return true; } }

        public Float32Jar(Endianess endianess) {
            if (endianess != Endianess.BigEndian && endianess != Endianess.LittleEndian)
                throw new ArgumentException("Unrecognized endianess", "endianess");
            var isLittleEndian = endianess == Endianess.LittleEndian;
            _isSystemEndian = isLittleEndian == BitConverter.IsLittleEndian;
        }

        public ParsedValue<float> Parse(ArraySegment<byte> data) {
            if (data.Count < SerializedLength) throw new DataFragmentException();
            var value = BitConverter.ToSingle(data.Array, data.Offset);
            if (!_isSystemEndian) value = value.ReverseBytes();
            return value.AsParsed(SerializedLength);
        }
        public byte[] Pack(float value) {
            var d = BitConverter.GetBytes(value);
            return _isSystemEndian ? d : d.Reverse().ToArray();
        }
        public InlinedParserComponents TryMakeInlinedParserComponents(Expression array, Expression offset, Expression count) {
            return ParserUtil.MakeInlinedNumberParserComponents<float>(_isSystemEndian, array, offset, count);
        }
        public override string ToString() {
            return "Float32";
        }
    }
}
