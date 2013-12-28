using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Strilanc.PickleJar.Internal.Basic;
using Strilanc.PickleJar.Internal.Bulk;
using Strilanc.PickleJar.Internal.RuntimeSpecialization;

namespace Strilanc.PickleJar.Internal.Repeated {
    internal static class RepeatUntilEndOfDataJarUtil {
        public static IJar<IReadOnlyList<T>> MakeRepeatUntilEndOfDataJar<T>(IBulkJar<T> bulkItemJar) {
            if (bulkItemJar.ItemJar.OptionalConstantSerializedLength().GetValueOrDefault() > 0) {
                return AnonymousJar.CreateSpecialized<IReadOnlyList<T>>(
                    specializedParserMaker: (array, offset, count) => MakeInlinedParserComponentsForConstantLength(bulkItemJar, array, offset, count),
                    packer: bulkItemJar.Pack,
                    canBeFollowed: false,
                    isBlittable: false,
                    constLength: null,
                    desc: () => string.Format("{0}.RepeatUntilEndOfDatah()", bulkItemJar),
                    components: bulkItemJar);
            }

            return AnonymousJar.CreateSpecialized<IReadOnlyList<T>>(
                specializedParserMaker: (array, offset, count) => MakeInlinedParserComponentsForVaryingLength(bulkItemJar, array, offset, count),
                packer: bulkItemJar.Pack,
                canBeFollowed: false,
                isBlittable: false,
                constLength: null,
                desc: () => string.Format("{0}.RepeatUntilEndOfData()", bulkItemJar),
                components: bulkItemJar.ItemJar);
        }

        public static SpecializedParserParts MakeInlinedParserComponentsForConstantLength<T>(IBulkJar<T> bulkItemJar, Expression array, Expression offset, Expression count) {
            if (bulkItemJar == null) throw new ArgumentNullException("bulkItemJar");
            if (array == null) throw new ArgumentNullException("array");
            if (offset == null) throw new ArgumentNullException("offset");
            if (count == null) throw new ArgumentNullException("count");
            var n = bulkItemJar.ItemJar.OptionalConstantSerializedLength();
            if (!n.HasValue || n.Value == 0) throw new ArgumentException("not constant length");
            var itemLength = Expression.Constant(n.Value);

            var varItemCount = Expression.Variable(typeof(int), "itemCount");
            var itemCount = Expression.Block(
                Expression.IfThen(Expression.NotEqual(Expression.Modulo(count, itemLength), Expression.Constant(0)), DataFragmentException.CachedThrowExpression),
                Expression.Divide(count, itemLength));
            
            var itemsComp = bulkItemJar.MakeInlinedParserComponents(array, offset, count, varItemCount);
            return new SpecializedParserParts(
                parseDoer: Expression.Block(
                    new[] {varItemCount}.Concat(itemsComp.Storage.ForConsumedCountIfValueAlreadyInScope), 
                    Expression.Assign(varItemCount, itemCount), 
                    itemsComp.ParseDoer),
                valueGetter: itemsComp.ValueGetter,
                consumedCountGetter: count,
                storage: new SpecializedParserResultStorageParts(
                    variablesNeededForValue: itemsComp.Storage.ForValue,
                    variablesNeededForConsumedCount: new ParameterExpression[0]));
        }
        public static SpecializedParserParts MakeInlinedParserComponentsForVaryingLength<T>(IBulkJar<T> bulkItemJar, Expression array, Expression offset, Expression count) {
            if (bulkItemJar == null) throw new ArgumentNullException("bulkItemJar");
            if (array == null) throw new ArgumentNullException("array");
            if (offset == null) throw new ArgumentNullException("offset");
            if (count == null) throw new ArgumentNullException("count");

            var itemJar = bulkItemJar.ItemJar;

            var varResult = Expression.Variable(typeof (List<T>), "resultList_RepeatUntilOfData");
            var varConsumed = Expression.Variable(typeof (int), "consumed_RepeatUntilOfData");

            var itemComps = itemJar.MakeInlinedParserComponents(array, Expression.Add(offset, varConsumed), Expression.Subtract(count, varConsumed));

            var b = Expression.Label();
            var parseDoer = Expression.Block(
                Expression.Assign(varResult, Expression.New(typeof (List<T>).GetConstructor(new Type[0]).NotNull())),
                Expression.Assign(varConsumed, Expression.Constant(0)),
                Expression.Loop(
                    Expression.Block(
                        itemComps.Storage.ForBoth,
                        Expression.IfThen(Expression.Equal(varConsumed, count), Expression.Break(b)),
                        itemComps.ParseDoer,
                        Expression.Call(varResult, typeof(List<T>).GetMethod("Add", new[] {typeof(T)}), new[] {itemComps.ValueGetter}),
                        Expression.AddAssign(varConsumed, itemComps.ConsumedCountGetter)),
                    b));

            return new SpecializedParserParts(
                parseDoer: parseDoer,
                valueGetter: varResult,
                consumedCountGetter: varConsumed,
                storage: new SpecializedParserResultStorageParts(
                    variablesNeededForValue: new[] {varResult},
                    variablesNeededForConsumedCount: new[] {varConsumed}));
        }
    }
}
