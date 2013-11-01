using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Linq;

namespace Strilanc.PickleJar.Internal.Bulk {
    /// <summary>
    /// BulkJarCompiled is an IBulkJar that specializes based on the given item jar by compiling code at runtime.
    /// For example, BulkJarCompiled will inline the item parsing method into the parse loop when possible.
    /// </summary>
    internal sealed class BulkJarCompiled<T> : IBulkJar<T> {
        public IJar<T> ItemJar { get; private set; }
        public int? OptionalConstantSerializedValueLength { get { return ItemJar.OptionalConstantSerializedLength(); } }
        private readonly Func<byte[], int, int, int, ParsedValue<IReadOnlyList<T>>> _parser;

        public BulkJarCompiled(IJar<T> itemJar) {
            if (itemJar == null) throw new ArgumentNullException("itemJar");
            if (!itemJar.CanBeFollowed) throw new ArgumentException("!itemJar.CanBeFollowed");
            ItemJar = itemJar;
            _parser = MakeAndCompileSpecializedParser();
        }

        public ParsedValue<IReadOnlyList<T>> Parse(ArraySegment<byte> data, int count) {
            return _parser(data.Array, data.Offset, data.Count, count);
        }
        private Func<byte[], int, int, int, ParsedValue<IReadOnlyList<T>>> MakeAndCompileSpecializedParser() {
            var dataArray = Expression.Parameter(typeof(byte[]), "dataArray");
            var dataOffset = Expression.Parameter(typeof(int), "dataOffset");
            var dataCount = Expression.Parameter(typeof(int), "dataCount");
            var itemCount = Expression.Parameter(typeof(int), "itemCount");
            var parameters = new[] { dataArray, dataOffset, dataCount, itemCount };

            var resultArray = Expression.Variable(typeof(T[]), "resultArray");
            var resultConsumed = Expression.Variable(typeof(int), "totalConsumed");
            var loopIndex = Expression.Variable(typeof(int), "i");

            var inlinedParseComponents = ItemJar.MakeInlinedParserComponents(dataArray, Expression.Add(dataOffset, resultConsumed), Expression.Subtract(dataCount, resultConsumed));

            var locals = new[] { resultArray, resultConsumed, loopIndex };
            var initStatements = Expression.Block(
                Expression.Assign(resultArray, Expression.NewArrayBounds(typeof (T), itemCount)),
                Expression.Assign(resultConsumed, Expression.Constant(0)),
                Expression.Assign(loopIndex, Expression.Constant(0)));
            
            var loopExit = Expression.Label();
            var loopStatements = Expression.Loop(
                Expression.Block(
                    inlinedParseComponents.Storage.ForBoth,
                    new[] {
                        Expression.IfThen(Expression.GreaterThanOrEqual(loopIndex, itemCount), Expression.Break(loopExit)),
                        inlinedParseComponents.ParseDoer,
                        Expression.AddAssign(resultConsumed, inlinedParseComponents.ConsumedCountGetter),
                        Expression.Assign(
                            Expression.ArrayAccess(
                                resultArray,
                                Expression.PostIncrementAssign(loopIndex)),
                            inlinedParseComponents.ValueGetter)
                    }),
                loopExit);

            var result = Expression.New(
                typeof(ParsedValue<IReadOnlyList<T>>).GetConstructor(new[] { typeof(IReadOnlyList<T>), typeof(int) }).NotNull(),
                resultArray,
                resultConsumed);

            var body = Expression.Block(
                locals,
                initStatements,
                loopStatements,
                result);

            var method = Expression.Lambda<Func<byte[], int, int, int, ParsedValue<IReadOnlyList<T>>>>(
                body,
                parameters);

            return method.Compile();
        }

        public byte[] Pack(IReadOnlyCollection<T> values) {
            // todo: compile at runtime
            return values.SelectMany(ItemJar.Pack).ToArray();
        }

        public override string ToString() {
            return string.Format("BulkCompiled[{0}]", ItemJar);
        }
    }
}

