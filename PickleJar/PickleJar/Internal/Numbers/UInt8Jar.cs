using System;
using System.Linq.Expressions;

namespace Strilanc.PickleJar.Internal.Numbers {
    internal struct UInt8Jar : IJarInternal<byte> {
        private const int SerializedLength = 1;
        public bool AreMemoryAndSerializedRepresentationsOfValueGuaranteedToMatch { get { return true; } }
        public int? OptionalConstantSerializedLength { get { return SerializedLength; } }
        public ParsedValue<byte> Parse(ArraySegment<byte> data) {
            if (data.Count < 1) throw new DataFragmentException();
            var value = data.Array[data.Offset];
            return value.AsParsed(SerializedLength);
        }

        public byte[] Pack(byte value) {
            return new[] { value };
        }
        public InlinedParserComponents TryMakeInlinedParserComponents(Expression array, Expression offset, Expression count) {
            return ParserUtil.MakeInlinedNumberParserComponents<byte>(true, array, offset, count);
        }
    }
}
