using System;
using System.Linq.Expressions;

namespace Strilanc.PickleJar.Internal.Values {
    internal struct Float64Jar : IJarMetadataInternal, IJar<double> {
        private const int SerializedLength = 64/8;

        public bool AreMemoryAndSerializedRepresentationsOfValueGuaranteedToMatch { get { return true; } }
        public int? OptionalConstantSerializedLength { get { return SerializedLength; } }

        public ParsedValue<double> Parse(ArraySegment<byte> data) {
            if (data.Count < SerializedLength) throw new DataFragmentException();
            var value = BitConverter.ToDouble(data.Array, data.Offset);
            return value.AsParsed(SerializedLength);
        }
        public byte[] Pack(double value) {
            return BitConverter.GetBytes(value);
        }
        public InlinedParserComponents TryMakeInlinedParserComponents(Expression array, Expression offset, Expression count) {
            return ParserUtil.MakeInlinedNumberParserComponents<double>(true, array, offset, count);
        }
    }
}
