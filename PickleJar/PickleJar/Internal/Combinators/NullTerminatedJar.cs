using System;
using System.Linq;

namespace Strilanc.PickleJar.Internal.Combinators {
    internal sealed class NullTerminatedJar<T> : IJar<T> {
        private readonly IJar<T> _itemJar;
        public bool CanBeFollowed { get { return true; } }

        public NullTerminatedJar(IJar<T> itemJar) {
            if (itemJar == null) throw new ArgumentNullException("itemJar");
            this._itemJar = itemJar;
        }

        public ParsedValue<T> Parse(ArraySegment<byte> data) {
            var index = data.IndexesOf((byte)0).FirstOrNull();
            if (!index.HasValue) throw new ArgumentException("Null terminator not found.");

            var itemDataLength = index.Value;
            var parsedItem = _itemJar.Parse(data.Take(itemDataLength));
            if (parsedItem.Consumed != itemDataLength) throw new LeftoverDataException();

            var dataLength = itemDataLength + 1;
            return parsedItem.Value.AsParsed(dataLength);
        }

        public byte[] Pack(T value) {
            var itemData = _itemJar.Pack(value);
            if (itemData.Contains((byte)0)) throw new ArgumentException("Null terminated data contains a zero.");
            return itemData.Concat(new byte[] { 0 }).ToArray();
        }
        public override string ToString() {
            return string.Format(
                "{0}.NullTerminated()",
                _itemJar);
        }
    }
}
