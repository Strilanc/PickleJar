using System;
using System.Linq;
using Strilanc.Value;

namespace Strilanc.PickleJar.Internal.Structured {
    internal sealed class NullTerminatedJar<T> : IJar<T> {
        private readonly IJar<T> _itemJar;

        public NullTerminatedJar(IJar<T> itemJar) {
            if (itemJar == null) throw new ArgumentNullException("itemJar");
            this._itemJar = itemJar;
        }

        public ParsedValue<T> Parse(ArraySegment<byte> data) {
            var index = data.IndexesOf((byte)0).MayFirst();
            if (!index.HasValue) throw new ArgumentException("Null terminator not count.");
            var n = index.ForceGetValue();
            var r = _itemJar.Parse(data.Take(n));
            if (r.Consumed != n) throw new ArgumentException("Leftover data.");
            return r;
        }
        public byte[] Pack(T value) {
            var itemData = _itemJar.Pack(value);
            if (itemData.Contains((byte)0)) throw new ArgumentException("Null terminated data contains a zero.");
            return itemData.Concat(new byte[] {0}).ToArray();
        }
    }
}
