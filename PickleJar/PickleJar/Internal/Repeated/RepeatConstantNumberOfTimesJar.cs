using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Strilanc.PickleJar.Internal.Bulk;

namespace Strilanc.PickleJar.Internal.Repeated {
    internal sealed class RepeatConstantNumberOfTimesJar<T> : IJarMetadataInternal, IJar<IReadOnlyList<T>> {
        public bool IsBlittable { get { return false; } }
        public int? OptionalConstantSerializedLength { get { return _bulkItemJar.OptionalConstantSerializedValueLength * _constantCount; } }
        public bool CanBeFollowed { get { return true; } }

        private readonly int _constantCount;
        private readonly IBulkJar<T> _bulkItemJar;

        public RepeatConstantNumberOfTimesJar(IBulkJar<T> bulkItemJar, int constantCount) {
            if (bulkItemJar == null) throw new ArgumentNullException("bulkItemJar");
            if (constantCount < 0) throw new ArgumentOutOfRangeException("constantCount");
            _constantCount = constantCount;
            _bulkItemJar = bulkItemJar;
        }

        public ParsedValue<IReadOnlyList<T>> Parse(ArraySegment<byte> data) {
            return _bulkItemJar.Parse(data, _constantCount);
        }
        public byte[] Pack(IReadOnlyList<T> value) {
            if (value.Count != _constantCount) throw new ArgumentOutOfRangeException("value", "value.Count != _constantCount");
            return _bulkItemJar.Pack(value);
        }

        public InlinedParserComponents TryMakeInlinedParserComponents(Expression array, Expression offset, Expression count) {
            return _bulkItemJar.MakeInlinedParserComponents(array, offset, count, Expression.Constant(_constantCount));
        }
        public override string ToString() {
            return string.Format(
                "{0}.RepeatNTimes({1})",
                _bulkItemJar,
                _constantCount);
        }
    }
}
