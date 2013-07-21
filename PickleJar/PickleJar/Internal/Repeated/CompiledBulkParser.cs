using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Strilanc.PickleJar.Internal.Repeated {
    /// <summary>
    /// CompiledBulkParser is an IBulkParser that performes dynamic optimization at runtime.
    /// In particular, CompiledBulkParser will attempt to inline the item parsing in order to avoid virtual calls.
    /// </summary>
    internal sealed class CompiledBulkParser<T> : IBulkParser<T> {
        private readonly IParser<T> _itemParser;
        private readonly Func<byte[], int, int, int, ParsedValue<IReadOnlyList<T>>> _parser;
        public CompiledBulkParser(IParser<T> itemParser) {
            _itemParser = itemParser;
            _parser = MakeParser();
        }
        public ParsedValue<IReadOnlyList<T>> Parse(ArraySegment<byte> data, int count) {
            return _parser(data.Array, data.Offset, data.Count, count);
        }
        private Func<byte[], int, int, int, ParsedValue<IReadOnlyList<T>>> MakeParser() {
            var dataArray = Expression.Parameter(typeof(byte[]), "dataArray");
            var dataOffset = Expression.Parameter(typeof(int), "dataOffset");
            var dataCount = Expression.Parameter(typeof(int), "dataCount");
            var itemCount = Expression.Parameter(typeof(int), "itemCount");
            var parameters = new[] { dataArray, dataOffset, dataCount, itemCount };

            var resultArray = Expression.Variable(typeof(T[]), "resultArray");
            var resultConsumed = Expression.Variable(typeof(int), "totalConsumed");
            var loopIndex = Expression.Variable(typeof(int), "i");

            var inlinedParseComponents = _itemParser.MakeInlinedParserComponents(dataArray, Expression.Add(dataOffset, resultConsumed), Expression.Subtract(dataCount, resultConsumed));

            var locals = new[] { resultArray, resultConsumed, loopIndex };
            var initStatements = Expression.Block(
                Expression.Assign(resultArray, Expression.NewArrayBounds(typeof (T), itemCount)),
                Expression.Assign(resultConsumed, Expression.Constant(0)),
                Expression.Assign(loopIndex, Expression.Constant(0)));
            
            var loopExit = Expression.Label();
            var loopStatements = Expression.Loop(
                Expression.Block(
                    inlinedParseComponents.ResultStorage,
                    Expression.IfThen(Expression.GreaterThanOrEqual(loopIndex, itemCount), Expression.Break(loopExit)),
                    inlinedParseComponents.PerformParse,
                    Expression.AddAssign(resultConsumed, inlinedParseComponents.AfterParseConsumedGetter),
                    Expression.Assign(
                        Expression.ArrayAccess(
                            resultArray, 
                            Expression.PostIncrementAssign(loopIndex)),
                        inlinedParseComponents.AfterParseValueGetter)),
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
        public int? OptionalConstantSerializedValueLength { get { return _itemParser.OptionalConstantSerializedLength(); } }
    }
}

