using System;
using System.Linq;
using System.Linq.Expressions;
using Strilanc.PickleJar.Internal.Basic;
using Strilanc.PickleJar.Internal.RuntimeSpecialization;

namespace Strilanc.PickleJar.Internal.Combinators {
    internal static class DataSizePrefixedJar {
        public static IJar<T> Create<T>(IJar<int> dataSizePrefixJar, IJar<T> itemJar, bool includePrefixInSize) {
            if (dataSizePrefixJar == null) throw new ArgumentNullException("dataSizePrefixJar");
            if (itemJar == null) throw new ArgumentNullException("itemJar");
            if (!dataSizePrefixJar.CanBeFollowed) throw new ArgumentException("!dataSizePrefixJar.CanBeFollowed");

            return AnonymousJar.CreateSpecialized<T>(
                parseSpecializer: CreateParseSpecializer(dataSizePrefixJar, itemJar, includePrefixInSize),
                packSpecializer: CreatePackSpecializer(dataSizePrefixJar, itemJar, includePrefixInSize),
                canBeFollowed: true,
                constLength: dataSizePrefixJar.OptionalConstantSerializedLength() + itemJar.OptionalConstantSerializedLength(),
                desc: () => string.Format("{0}.DataSizePrefixed({1}, {2})",
                                          itemJar,
                                          dataSizePrefixJar,
                                          includePrefixInSize ? "IncludingSizeOfPrefix" : "ExcludingSizeOfPrefix"));
        }

        private static ParseSpecializer CreateParseSpecializer<T>(IJar<int> dataSizePrefixJar, IJar<T> itemJar, bool includePrefixInSize) {
            return (array, offset, count) => {
                var sizeSpecial = dataSizePrefixJar.MakeInlinedParserComponents(array, offset, count);

                var sizeOfItem = sizeSpecial.ValueGetter;
                if (includePrefixInSize) sizeOfItem = sizeOfItem.Minus(sizeSpecial.ConsumedCountGetter);

                var checkSize = sizeOfItem.IsLessThan(0.ConstExpr()).IfThenDo(
                    Expression.Throw(
                        new InvalidOperationException("Item size is negative.").ConstExpr()));

                var itemSpecial = itemJar.MakeInlinedParserComponents(array,
                                                                      offset.Plus(sizeSpecial.ConsumedCountGetter),
                                                                      count.Minus(sizeSpecial.ConsumedCountGetter));

                var checkItem = itemSpecial.ConsumedCountGetter.IsNotEqualTo(sizeSpecial.ValueGetter).IfThenDo(
                    Expression.Throw(new LeftoverDataException().ConstExpr()));

                return new SpecializedParserParts(
                    parseDoer: Expression.Block(sizeSpecial.ParseDoer, checkSize, itemSpecial.ParseDoer, checkItem),
                    valueGetter: itemSpecial.ValueGetter,
                    consumedCountGetter: sizeSpecial.ConsumedCountGetter.Plus(itemSpecial.ConsumedCountGetter),
                    storage: new SpecializedParserStorageParts(
                        variablesNeededForConsumedCount: sizeSpecial.Storage.ForBoth.Concat(itemSpecial.Storage.ForConsumedCount),
                        variablesNeededForValue: itemSpecial.Storage.ForValue));
            };
        }
        private static PackSpecializer CreatePackSpecializer<T>(IJar<int> dataSizePrefixJar, IJar<T> itemJar, bool includePrefixInSize) {
            if (dataSizePrefixJar == null) throw new ArgumentNullException("dataSizePrefixJar");
            if (itemJar == null) throw new ArgumentNullException("itemJar");

            // if prefix has a constant packed size, things are a lot easier
            var n = dataSizePrefixJar.OptionalConstantSerializedLength();
            if (n.HasValue) return value => SpecializedPackerGivenConstantSizePrefix(dataSizePrefixJar, itemJar, value, includePrefixInSize);

            // things are a bit easier if the prefix can't affect itself
            if (!includePrefixInSize) return value => SpecializedPackerGivenPrefixSizeNotIncluded(dataSizePrefixJar, itemJar, value);

            // urgh
            return value => SpecializedPackerHandlingPotentiallyCyclicSize(dataSizePrefixJar, itemJar, value);
        }
        private static SpecializedPackerParts SpecializedPackerHandlingPotentiallyCyclicSize<T>(IJar<int> dataSizePrefixJar, IJar<T> itemJar, Expression value) {
            if (dataSizePrefixJar == null) throw new ArgumentNullException("dataSizePrefixJar");
            if (itemJar == null) throw new ArgumentNullException("itemJar");
            if (value == null) throw new ArgumentNullException("value");

            var itemPackerParts = itemJar.MakeSpecializedPacker(value);
            var varSizeOfItem = Expression.Variable(typeof(int), "sizeOfItem");
            var sizePackerParts = dataSizePrefixJar.MakeSpecializedPacker(varSizeOfItem);

            var totalSizeComputer = Expression.Block(
                itemPackerParts.SizePrecomputer,
                varSizeOfItem.AssignTo(itemPackerParts.PrecomputedSizeGetter),
                sizePackerParts.SizePrecomputer);
            var totalSizeGetter = itemPackerParts.PrecomputedSizeGetter.Plus(sizePackerParts.PrecomputedSizeGetter);
            var totalSizeStorage = itemPackerParts.PrecomputedSizeStorage
                                                  .Concat(sizePackerParts.PrecomputedSizeStorage)
                                                  .Concat(new[] {varSizeOfItem});

            var varExpectedSizeOfPrefix = Expression.Variable(typeof(int), "expectedSizeOfPrefix");
            PackDoer packDoer = (array, offset) => Expression.Block(
                new[] {varSizeOfItem},

                Expression.Block(
                    itemPackerParts.PrecomputedSizeGetter.Plus(sizePackerParts.PrecomputedSizeGetter),

                    // compute item size and expected prefix size (assuming prefix size not included in prefix value)
                    totalSizeComputer,

                    // recompute prefix size after including expected prefix size in prefix value
                    varExpectedSizeOfPrefix.AssignTo(sizePackerParts.PrecomputedSizeGetter),
                    varSizeOfItem.PlusEqual(varExpectedSizeOfPrefix),
                    sizePackerParts.SizePrecomputer,

                    // if the prefix size changed when the value was adjusted, there's a nasty cyclic dependency that we don't know how to resolve
                    varExpectedSizeOfPrefix.IsNotEqualTo(sizePackerParts.PrecomputedSizeGetter)
                                           .IfThenDo(Expression.Throw(new InvalidOperationException(
                                                                          "Prefixed size may not be well defined. The size includes its own serialized length, but the serialized length varies based on the size.")
                                                                          .ConstExpr()))),

                sizePackerParts.PackDoer(array, offset),
                itemPackerParts.PackDoer(array, offset));

            return new SpecializedPackerParts(
                sizePrecomputer: totalSizeComputer,
                precomputedSizeGetter: totalSizeGetter,
                precomputedSizeStorage: totalSizeStorage,
                packDoer: packDoer);
        }
        private static SpecializedPackerParts SpecializedPackerGivenPrefixSizeNotIncluded<T>(IJar<int> dataSizePrefixJar,
                                                                                             IJar<T> itemJar,
                                                                                             Expression value) {
            if (dataSizePrefixJar == null) throw new ArgumentNullException("dataSizePrefixJar");
            if (itemJar == null) throw new ArgumentNullException("itemJar");
            if (value == null) throw new ArgumentNullException("value");

            var itemSpecial = itemJar.MakeSpecializedPacker(value);
            var sizeSpecial = dataSizePrefixJar.MakeSpecializedPacker(itemSpecial.PrecomputedSizeGetter);

            return new SpecializedPackerParts(
                sizePrecomputer: itemSpecial.SizePrecomputer.FollowedBy(sizeSpecial.SizePrecomputer),
                precomputedSizeGetter: itemSpecial.PrecomputedSizeGetter.Plus(sizeSpecial.PrecomputedSizeGetter),
                precomputedSizeStorage: itemSpecial.PrecomputedSizeStorage.Concat(sizeSpecial.PrecomputedSizeStorage),
                packDoer: (array, offset) => Expression.Block(
                    itemSpecial.PrecomputedSizeStorage,
                    itemSpecial.SizePrecomputer,
                    sizeSpecial.PackDoer(array, offset),
                    itemSpecial.PackDoer(array, offset)));
        }

        private static SpecializedPackerParts SpecializedPackerGivenConstantSizePrefix<T>(IJar<int> dataSizePrefixJar,
                                                                                          IJar<T> itemJar,
                                                                                          Expression value,
                                                                                          bool includePrefixInSize) {
            if (dataSizePrefixJar == null) throw new ArgumentNullException("dataSizePrefixJar");
            if (itemJar == null) throw new ArgumentNullException("itemJar");
            if (value == null) throw new ArgumentNullException("value");
            var prefixSize = dataSizePrefixJar.OptionalConstantSerializedLength();
            if (!prefixSize.HasValue) throw new ArgumentException("dataSizePrefixJar is not constant sized");

            var itemPackerParts = itemJar.MakeSpecializedPacker(value);
            
            var varPrefixOffset = Expression.Variable(typeof(int), "offsetOfSizePrefix");
            PackDoer packDoer = (array, offset) => Expression.Block(
                new[] {varPrefixOffset},

                // stash offset for size prefix, advance to item
                varPrefixOffset.AssignTo(offset),
                offset.PlusEqual(prefixSize.Value),

                // write item
                itemPackerParts.PackDoer(array, offset),

                // determine item length from change in offset, write size prefix
                dataSizePrefixJar
                    .MakeSpecializedPacker(offset.Minus(varPrefixOffset)
                                                 .Minus(includePrefixInSize ? 0 : prefixSize.Value))
                    .PackDoer(array, varPrefixOffset));

            return new SpecializedPackerParts(
                sizePrecomputer: itemPackerParts.SizePrecomputer,
                precomputedSizeGetter: itemPackerParts.PrecomputedSizeGetter.Plus(prefixSize.Value),
                precomputedSizeStorage: itemPackerParts.PrecomputedSizeStorage,
                packDoer: packDoer);
        }
    }
}
