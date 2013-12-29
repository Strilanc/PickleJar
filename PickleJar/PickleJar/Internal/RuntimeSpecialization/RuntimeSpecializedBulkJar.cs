using System.Linq.Expressions;
using System.Linq;
using Strilanc.PickleJar.Internal.Bulk;

namespace Strilanc.PickleJar.Internal.RuntimeSpecialization {
    /// <summary>
    /// RuntimeSpecializedBulkJar is an IBulkJar that specializes based on the given item jar by compiling code at runtime.
    /// For example, RuntimeSpecializedBulkJar will inline the item parsing method into the parse loop when possible.
    /// </summary>
    internal static class RuntimeSpecializedBulkJar {
        public static IBulkJar<T> MakeBulkParser<T>(IJar<T> itemJar) {
            InlinerBulkMaker parseCompMaker = (array, offset, count, itemCount) => MakeAndCompileSpecializedParserComponents(itemJar, array, offset, count, itemCount);
            return AnonymousBulkJar.CreateFrom(
                itemJar,
                parseCompMaker,
                // todo: compile at runtime
                values => values.SelectMany(itemJar.Pack).ToArray(),
                () => string.Format("{0}", itemJar),
                null);

        }

        public static SpecializedParserParts MakeAndCompileSpecializedParserComponents<T>(IJar<T> itemJar, Expression array, Expression offset, Expression count, Expression itemCount) {
            var resultArray = Expression.Variable(typeof(T[]), "resultArray");
            var resultConsumed = Expression.Variable(typeof(int), "totalConsumed");
            var loopIndex = Expression.Variable(typeof(int), "i");

            var inlinedParseComponents = itemJar.MakeInlinedParserComponents(array, Expression.Add(offset, resultConsumed), Expression.Subtract(count, resultConsumed));

            var initStatements = Expression.Block(
                resultArray.AssignTo(Expression.NewArrayBounds(typeof(T), itemCount)),
                resultConsumed.AssignTo(0.ConstExpr()),
                loopIndex.AssignTo(0.ConstExpr()));

            var loopExit = Expression.Label();
            var parseDoer = Expression.Block(
                inlinedParseComponents.Storage.ForBoth.Concat(new[] {loopIndex}),
                initStatements,
                Expression.Loop(
                    Expression.Block(
                        Expression.IfThen(Expression.GreaterThanOrEqual(loopIndex, itemCount), Expression.Break(loopExit)),
                        inlinedParseComponents.ParseDoer,
                        Expression.AddAssign(resultConsumed, inlinedParseComponents.ConsumedCountGetter),
                        resultArray.AccessIndex(Expression.PostIncrementAssign(loopIndex))
                                   .AssignTo(inlinedParseComponents.ValueGetter)),
                    loopExit));

            var storage = new SpecializedParserResultStorageParts(
                variablesNeededForValue: new[] {resultArray},
                variablesNeededForConsumedCount: new[] {resultConsumed});
            return new SpecializedParserParts(
                parseDoer: parseDoer,
                valueGetter: resultArray,
                consumedCountGetter: resultConsumed,
                storage: storage);
        }
    }
}

