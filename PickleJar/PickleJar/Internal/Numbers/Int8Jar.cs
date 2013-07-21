﻿using System;
using System.Linq.Expressions;

namespace Strilanc.PickleJar.Internal.Numbers {
    internal struct Int8Jar : IJarInternal<sbyte> {
        private const int SerializedLength = 1;
        public bool AreMemoryAndSerializedRepresentationsOfValueGuaranteedToMatch { get { return true; } }
        public int? OptionalConstantSerializedLength { get { return SerializedLength; } }

        public ParsedValue<sbyte> Parse(ArraySegment<byte> data) {
            unchecked {
                if (data.Count < 1) throw new DataFragmentException();
                var value = (sbyte)data.Array[data.Offset];
                return value.AsParsed(SerializedLength);
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
