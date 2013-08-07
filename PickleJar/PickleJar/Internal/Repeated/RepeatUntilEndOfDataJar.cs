using System;
using System.Collections.Generic;
using Strilanc.PickleJar.Internal.Bulk;

namespace Strilanc.PickleJar.Internal.Repeated {
    internal sealed class RepeatUntilEndOfDataJar<T> : IJar<IReadOnlyList<T>> {
        private readonly IBulkJar<T> _bulkItemJar;
        public bool CanBeFollowed { get { return false; } }

        public RepeatUntilEndOfDataJar(IBulkJar<T> bulkItemJar) {
            if (bulkItemJar == null) throw new ArgumentNullException("bulkItemJar");
            _bulkItemJar = bulkItemJar;
        }

        public ParsedValue<IReadOnlyList<T>> Parse(ArraySegment<byte> data) {
            var itemJar = _bulkItemJar.ItemJar;
            var result = new List<T>();
            var t = 0;
            while (data.Count - t > 0) {
                var e = itemJar.Parse(data.Skip(t));
                result.Add(e.Value);
                t += e.Consumed;
            }
            return new ParsedValue<IReadOnlyList<T>>(result.ToArray(), t);
        }

        public byte[] Pack(IReadOnlyList<T> value) {
            return _bulkItemJar.Pack(value);
        }

        public override string ToString() {
            return string.Format(
                "{0}.RepeatUntilEndOfData()",
                _bulkItemJar);
        }
    }
}
