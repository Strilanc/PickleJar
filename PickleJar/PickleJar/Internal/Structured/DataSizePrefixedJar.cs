using System;
using System.Linq;

namespace Strilanc.PickleJar.Internal.Structured {
    internal sealed class DataSizePrefixedJar<T> : IJar<T> {
        private readonly IJar<int> _dataSizePrefixJar; 
        private readonly IJar<T> _itemJar;

        public DataSizePrefixedJar(IJar<int> dataSizePrefixJar, IJar<T> itemJar) {
            if (dataSizePrefixJar == null) throw new ArgumentNullException("dataSizePrefixJar");
            if (itemJar == null) throw new ArgumentNullException("itemJar");
            this._dataSizePrefixJar = dataSizePrefixJar;
            this._itemJar = itemJar;
        }

        public ParsedValue<T> Parse(ArraySegment<byte> data) {
            var size = _dataSizePrefixJar.Parse(data);
            var item = _itemJar.Parse(data.Skip(size.Consumed).Take(size.Value));
            if (size.Consumed + item.Consumed != data.Count) throw new ArgumentException("Leftover data");
            return new ParsedValue<T>(item.Value, size.Consumed + item.Consumed);
        }
        public byte[] Pack(T value) {
            var itemData = _itemJar.Pack(value);
            var countData = _dataSizePrefixJar.Pack(itemData.Length);
            return countData.Concat(itemData).ToArray();
        }

        public override string ToString() {
            return string.Format(
                "{0}.DataSizePrefixed({1})",
                _itemJar,
                _dataSizePrefixJar);
        }
    }
}
