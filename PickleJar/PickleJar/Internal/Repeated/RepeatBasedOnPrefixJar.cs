using System;
using System.Collections.Generic;
using Strilanc.PickleJar.Internal.Bulk;
using System.Linq;

namespace Strilanc.PickleJar.Internal.Repeated {
    internal struct RepeatBasedOnPrefixJar<T> : IJar<IReadOnlyList<T>> {
        private readonly IJar<int> _countPrefixJar; 
        private readonly IBulkJar<T> _bulkItemJar;
        public bool CanBeFollowed { get { return true; } }

        public RepeatBasedOnPrefixJar(IJar<int> countPrefixJar, IBulkJar<T> bulkItemJar) {
            if (countPrefixJar == null) throw new ArgumentNullException("countPrefixJar");
            if (!countPrefixJar.CanBeFollowed) throw new ArgumentException("!countPrefixJar.CanBeFollowed");
            if (bulkItemJar == null) throw new ArgumentNullException("bulkItemJar");
            this._countPrefixJar = countPrefixJar;
            this._bulkItemJar = bulkItemJar;
        }

        public ParsedValue<IReadOnlyList<T>> Parse(ArraySegment<byte> data) {
            var count = _countPrefixJar.Parse(data);
            var array = _bulkItemJar.Parse(data.Skip(count.Consumed), count.Value);
            return new ParsedValue<IReadOnlyList<T>>(array.Value, count.Consumed + array.Consumed);
        }
        public byte[] Pack(IReadOnlyList<T> value) {
            if (value == null) throw new ArgumentNullException("value");
            var countData = _countPrefixJar.Pack(value.Count);
            var itemData = _bulkItemJar.Pack(value);
            return countData.Concat(itemData).ToArray();
        }

        public override string ToString() {
            return string.Format(
                "{0}.RepeatCountPrefixTimes({1})",
                _bulkItemJar,
                _countPrefixJar);
        }
    }
}
