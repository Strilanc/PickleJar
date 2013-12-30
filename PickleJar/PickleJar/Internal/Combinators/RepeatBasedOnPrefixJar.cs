using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Strilanc.PickleJar.Internal.Bulk;
using System.Linq;
using Strilanc.PickleJar.Internal.RuntimeSpecialization;

namespace Strilanc.PickleJar.Internal.Combinators {
    internal struct RepeatBasedOnPrefixJar<T> : IJar<IReadOnlyList<T>>, IJarMetadataInternal {
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
        public bool IsBlittable { get { return false; } }
        public int? OptionalConstantSerializedLength { get { return null; } }
        public SpecializedParserParts TrySpecializeParser(Expression array, Expression offset, Expression count) {
            var countComp = _countPrefixJar.MakeInlinedParserComponents(array, offset, count);
            var itemsComp = _bulkItemJar.MakeInlinedParserComponents(array, offset, count, countComp.ValueGetter);
            return new SpecializedParserParts(
                parseDoer: Expression.Block(countComp.Storage.ForValueIfConsumedCountAlreadyInScope, new[] {countComp.ParseDoer, itemsComp.ParseDoer}),
                valueGetter: itemsComp.ValueGetter,
                consumedCountGetter: Expression.Add(countComp.ConsumedCountGetter, itemsComp.ConsumedCountGetter),
                storage: new SpecializedParserStorageParts(
                    variablesNeededForValue: itemsComp.Storage.ForValue,
                    variablesNeededForConsumedCount: itemsComp.Storage.ForConsumedCount.Concat(countComp.Storage.ForConsumedCount).ToArray()));
        }
        public SpecializedPackerParts? TrySpecializePacker(Expression value) {
            throw new NotImplementedException();
        }
    }
}
