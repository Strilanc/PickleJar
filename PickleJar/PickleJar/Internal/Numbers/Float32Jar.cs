using System;
using System.Linq.Expressions;

namespace Strilanc.PickleJar.Internal.Numbers {
    internal struct Float32Jar : IJarInternal<float> {
        private const int SerializedLength = 32/8;

        public bool AreMemoryAndSerializedRepresentationsOfValueGuaranteedToMatch { get { return true; } }
        public int? OptionalConstantSerializedLength { get { return SerializedLength; } }

        public ParsedValue<float> Parse(ArraySegment<byte> data) {
            if (data.Count < SerializedLength) throw new DataFragmentException();
            var value = BitConverter.ToSingle(data.Array, data.Offset);
            return value.AsParsed(SerializedLength);
        }
        public byte[] Pack(float value) {
            return BitConverter.GetBytes(value);
        }
        public InlinedParserComponents TryMakeInlinedParserComponents(Expression array, Expression offset, Expression count) {
            return ParserUtil.MakeInlinedNumberParserComponents<float>(true, array, offset, count);
        }
    }
}
