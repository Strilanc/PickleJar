using System;
using System.Linq;

namespace Strilanc.PickleJar.Internal.Structured {
    internal sealed class DataSizePrefixedJar<T> : IJar<T> {
        private readonly IJar<int> _dataSizePrefixJar; 
        private readonly IJar<T> _itemJar;
        private readonly bool _includePrefixInSize;
        public bool CanBeFollowed { get { return true; } }

        public DataSizePrefixedJar(IJar<int> dataSizePrefixJar, IJar<T> itemJar, bool includePrefixInSize) {
            if (dataSizePrefixJar == null) throw new ArgumentNullException("dataSizePrefixJar");
            if (!dataSizePrefixJar.CanBeFollowed) throw new ArgumentException("!dataSizePrefixJar.CanBeFollowed");
            if (itemJar == null) throw new ArgumentNullException("itemJar");
            this._dataSizePrefixJar = dataSizePrefixJar;
            this._itemJar = itemJar;
            this._includePrefixInSize = includePrefixInSize;
        }

        public ParsedValue<T> Parse(ArraySegment<byte> data) {
            var parsedSize = _dataSizePrefixJar.Parse(data);
            if (_includePrefixInSize && parsedSize.Value < parsedSize.Consumed) {
                throw new InvalidOperationException("_includePrefixInSize && size.Value < size.Consumed");
            }
            var itemSize = parsedSize.Value - (_includePrefixInSize ? parsedSize.Consumed : 0);

            var item = _itemJar.Parse(data.Skip(parsedSize.Consumed).Take(itemSize));
            if (parsedSize.Consumed + item.Consumed != data.Count) {
                throw new LeftoverDataException();
            }

            return new ParsedValue<T>(item.Value, parsedSize.Consumed + item.Consumed);
        }

        private byte[] PackSize(int size) {
            if (!_includePrefixInSize) return _dataSizePrefixJar.Pack(size);

            var constantSize = _dataSizePrefixJar.OptionalConstantSerializedLength();
            if (constantSize.HasValue) return _dataSizePrefixJar.Pack(size + constantSize.Value);

            // hopefully the different sizes have the same encoded length...
            var measuredSize = _dataSizePrefixJar.Pack(size).Length;
            var result = _dataSizePrefixJar.Pack(size + measuredSize);
            if (measuredSize != result.Length) {
                throw new InvalidOperationException(
                    "Prefixed size may not be well defined. The size includes its own serialized length, but the serialized length varies based on the size.");
            }

            return result;
        }
        public byte[] Pack(T value) {
            var itemData = _itemJar.Pack(value);
            var sizeData = PackSize(itemData.Length);
            return sizeData.Concat(itemData).ToArray();
        }

        public override string ToString() {
            return string.Format(
                "{0}.DataSizePrefixed({1})",
                _itemJar,
                _dataSizePrefixJar);
        }
    }
}
