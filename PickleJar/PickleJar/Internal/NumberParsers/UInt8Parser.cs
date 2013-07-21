using System;
using System.Linq.Expressions;

namespace Strilanc.PickleJar.Internal.NumberParsers {
    internal struct UInt8Parser : IJarInternal<byte> {
        public bool AreMemoryAndSerializedRepresentationsOfValueGuaranteedToMatch { get { return true; } }
        public int? OptionalConstantSerializedLength { get { return 1; } }
        public ParsedValue<byte> Parse(ArraySegment<byte> data) {
            if (data.Count < 1) throw new DataFragmentException();
            var value = data.Array[data.Offset];
            return new ParsedValue<byte>(value, 1);
        }

        public byte[] Pack(byte value) {
            return new[] { value };
        }
        public InlinedParserComponents TryMakeInlinedParserComponents(Expression array, Expression offset, Expression count) {
            return ParserUtil.MakeInlinedNumberParserComponents<byte>(true, array, offset, count);
        }
    }
}
