﻿using System;
using System.Linq.Expressions;

namespace Strilanc.PickleJar.Internal.Values {
    internal struct UInt16Jar : IJarMetadataInternal, IJar<UInt16> {
        private const int SerializedLength = 16 / 8;

        private readonly bool _isSystemEndian;
        public bool IsBlittable { get { return _isSystemEndian; } }
        public int? OptionalConstantSerializedLength { get { return SerializedLength; } }
        public bool CanBeFollowed { get { return true; } }

        public UInt16Jar(Endianess endianess) {
            if (endianess != Endianess.BigEndian && endianess != Endianess.LittleEndian)
                throw new ArgumentException("Unrecognized endianess", "endianess");
            var isLittleEndian = endianess == Endianess.LittleEndian;
            _isSystemEndian = isLittleEndian == BitConverter.IsLittleEndian;
        }

        public ParsedValue<UInt16> Parse(ArraySegment<byte> data) {
            if (data.Count < SerializedLength) throw new DataFragmentException();
            var value = BitConverter.ToUInt16(data.Array, data.Offset);
            if (!_isSystemEndian) value = value.ReverseBytes();
            return value.AsParsed(SerializedLength);
        }
        public byte[] Pack(UInt16 value) {
            var v = _isSystemEndian ? value : value.ReverseBytes();
            return BitConverter.GetBytes(v);
        }

        public InlinedParserComponents TryMakeInlinedParserComponents(Expression array, Expression offset, Expression count) {
            return ParserUtil.MakeInlinedNumberParserComponents<UInt16>(_isSystemEndian, array, offset, count);
        }
        public override string ToString() {
            var end = _isSystemEndian ? ""
                    : BitConverter.IsLittleEndian ? "[BigEndian]"
                    : "[LittleEndian]";
            return "UInt16" + end;
        }
    }
}
