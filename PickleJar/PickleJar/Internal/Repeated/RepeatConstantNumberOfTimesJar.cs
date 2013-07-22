using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Strilanc.PickleJar.Internal.Bulk;

namespace Strilanc.PickleJar.Internal.Repeated {
    internal sealed class RepeatConstantNumberOfTimesJar<T> : IJarInternal<IReadOnlyList<T>> {
        public bool AreMemoryAndSerializedRepresentationsOfValueGuaranteedToMatch { get { return false; } }
        public int? OptionalConstantSerializedLength { get { return _bulkItemJar.OptionalConstantSerializedValueLength * _count; } }

        private readonly int _count;
        private readonly IBulkJar<T> _bulkItemJar;

        public RepeatConstantNumberOfTimesJar(IBulkJar<T> bulkItemJar, int count) {
            if (bulkItemJar == null) throw new ArgumentNullException("bulkItemJar");
            if (count < 0) throw new ArgumentOutOfRangeException("count");
            _count = count;
            _bulkItemJar = bulkItemJar;
        }

        public ParsedValue<IReadOnlyList<T>> Parse(ArraySegment<byte> data) {
            return _bulkItemJar.Parse(data, _count);
        }
        public byte[] Pack(IReadOnlyList<T> value) {
            return _bulkItemJar.Pack(value);
        }

        public InlinedParserComponents TryMakeInlinedParserComponents(Expression array, Expression offset, Expression count) {
            return null;
        }
    }
}
