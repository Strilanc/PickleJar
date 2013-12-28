using System;
using System.Text;

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
    }
}
