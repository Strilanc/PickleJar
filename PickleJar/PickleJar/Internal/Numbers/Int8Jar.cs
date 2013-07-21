using System;
using System.Linq.Expressions;

namespace Strilanc.PickleJar.Internal.Numbers {
    internal struct Int8Jar : IJarInternal<sbyte> {
        public bool AreMemoryAndSerializedRepresentationsOfValueGuaranteedToMatch { get { return true; } }
        public int? OptionalConstantSerializedLength { get { return 1; } }

        public ParsedValue<sbyte> Parse(ArraySegment<byte> data) {
            unchecked {
                if (data.Count < 1) throw new DataFragmentException();
                var value = (sbyte)data.Array[data.Offset];
                return new ParsedValue<sbyte>(value, 1);
            }
        }

        public byte[] Pack(sbyte value) {
            unchecked {
                return new[] {(byte)value};
            }
        }
        public InlinedParserComponents TryMakeInlinedParserComponents(Expression array, Expression offset, Expression count) {
            return ParserUtil.MakeInlinedNumberParserComponents<sbyte>(true, array, offset, count);
        }
    }
}
