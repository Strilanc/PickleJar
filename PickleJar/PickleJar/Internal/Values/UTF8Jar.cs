using System;
using System.Text;
using System.Linq;

namespace Strilanc.PickleJar.Internal.Values {
    internal struct UTF8Jar : IJar<string> {
        public ParsedValue<string> Parse(ArraySegment<byte> data) {
            var value = Encoding.UTF8.GetString(data.ToArray());
            return value.AsParsed(data.Count);
        }
        public byte[] Pack(string value) {
            return Encoding.UTF8.GetBytes(value);
        }
        public override string ToString() {
            return "Text[UTF8]";
        }
    }
}
