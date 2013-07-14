using System;
using System.Linq.Expressions;

namespace ParserGenerator {
    internal sealed class ExpressionArrayParser<T> : IArrayParser<T> {
        private readonly IParser<T> _itemParser;
        private readonly Func<byte[], int, int, int, ParsedValue<T[]>> _parser;
        public ExpressionArrayParser(IParser<T> itemParser) {
            _itemParser = itemParser;
            _parser = MakeParser();
        }
        public ParsedValue<T[]> Parse(ArraySegment<byte> data, int count) {
            return _parser(data.Array, data.Offset, data.Count, count);
        }
        private Func<byte[], int, int, int, ParsedValue<T[]>> MakeParser() {
            var dataArray = Expression.Parameter(typeof(byte[]), "dataArray");
            var dataOffset = Expression.Parameter(typeof(int), "dataOffset");
            var dataCount = Expression.Parameter(typeof(int), "dataCount");
            var itemCount = Expression.Parameter(typeof(int), "itemCount");

            var parser = MakeParser(dataArray, dataOffset, dataCount, itemCount);
            var method = Expression.Lambda<Func<byte[], int, int, int, ParsedValue<T[]>>>(
                parser,
                new[] {dataArray, dataOffset, dataCount, itemCount});

            return method.Compile();
        }
        private Expression MakeParser(Expression dataArray, Expression dataOffset, Expression dataCount, Expression itemCount) {
            var result = Expression.Variable(typeof (T[]), "result");
            var total = Expression.Variable(typeof(int), "total");
            var index = Expression.Variable(typeof(int), "index");

            var invokeParse = _itemParser.MakeParseFromDataExpression(dataArray, Expression.Add(dataOffset, total), Expression.Subtract(dataCount, total));
            var parsed = Expression.Variable(invokeParse.Type, "parsed");
            var parsedValue = _itemParser.MakeGetValueFromParsedExpression(parsed);
            var parsedConsumed = _itemParser.MakeGetCountFromParsedExpression(parsed);

            var b = Expression.Label();
            return Expression.Block(
                new[] { result, total, index, parsed },
                new Expression[] {
                    Expression.Assign(result, Expression.NewArrayBounds(typeof(T), itemCount)),
                    Expression.Assign(total, Expression.Constant(0)),
                    Expression.Assign(index, Expression.Constant(0)),
                    Expression.Loop(
                        Expression.Block(
                            Expression.IfThen(Expression.GreaterThanOrEqual(index, itemCount), Expression.Break(b)),
                            Expression.Assign(parsed, invokeParse),
                            Expression.AddAssign(total, parsedConsumed),
                            Expression.Assign(Expression.ArrayAccess(result, Expression.PostIncrementAssign(index)), parsedValue)),
                        b),
                    Expression.New(typeof (ParsedValue<T[]>).GetConstructor(new[] {typeof (T[]), typeof (int)}), result, total)
                });
        }
        public bool IsValueBlittable { get { return _itemParser.IsBlittable; } }
        public int? OptionalConstantSerializedValueLength { get { return _itemParser.OptionalConstantSerializedLength; } }
    }
}

