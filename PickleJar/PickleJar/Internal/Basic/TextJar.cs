﻿using System;
using System.Linq.Expressions;
using System.Text;
using Strilanc.PickleJar.Internal.Misc;
using Strilanc.PickleJar.Internal.RuntimeSpecialization;

namespace Strilanc.PickleJar.Internal.Basic {
    internal sealed class TextJar : IJar<string> {
        private readonly Encoding _encoding;
        public bool CanBeFollowed { get { return false; } }
        public TextJar(Encoding encoding) {
            if (encoding == null) throw new ArgumentNullException("encoding");
            _encoding = encoding;
        }
        public ParsedValue<string> Parse(ArraySegment<byte> data) {
            var value = _encoding.GetString(data.Array, data.Offset, data.Count);
            return value.AsParsed(data.Count);
        }
        public byte[] Pack(string value) {
            return _encoding.GetBytes(value);
        }
        public override string ToString() {
            return string.Format("Text[{0}]", _encoding.EncodingName);
        }

        private SpecializedPackerParts SpecializedPackerParts(Expression value) {
            var varEncodedLength = Expression.Variable(typeof(int), "encodedLength");
            var getByteCountMethod = typeof(Encoding).GetMethod("GetByteCount", new[] { typeof(string) });
            return new SpecializedPackerParts(
                capacityComputer: varEncodedLength.AssignTo(_encoding.ConstExpr().CallInstanceMethod(getByteCountMethod, value)),
                capacityGetter: varEncodedLength,
                capacityStorage: new[] { varEncodedLength },
                packDoer: (array, offset) => {
                    var getBytesMethod = typeof(Encoding).GetMethod("GetBytes", new[] {typeof(string), typeof(int), typeof(int), typeof(byte[]), typeof(int)});
                    var stringLengthProperty = typeof(String).GetProperty("Length");
                    return _encoding.ConstExpr().CallInstanceMethod(
                        getBytesMethod,
                        value,
                        0.ConstExpr(),
                        value.AccessMember(stringLengthProperty),
                        array,
                        offset);
                });
        }
    }
}
