using System;
using System.Linq.Expressions;

namespace ParserGenerator {
    public sealed class FixedRepeatParser<T> : IParser<T[]> {
        public bool IsBlittable { get { return false; } }
        public int? OptionalConstantSerializedLength { get { return _subParser.OptionalConstantSerializedValueLength * _count; } }
        public Expression TryParseInline(Expression array, Expression offset, Expression count) {
            return null;
        }

        private readonly int _count;
        private readonly IArrayParser<T> _subParser;

        public FixedRepeatParser(IArrayParser<T> arrayParser, int count) {
            this._count = count;
            _subParser = arrayParser;
        }

        public ParsedValue<T[]> Parse(ArraySegment<byte> data) {
            return _subParser.Parse(data, _count);
        }
    }
}
