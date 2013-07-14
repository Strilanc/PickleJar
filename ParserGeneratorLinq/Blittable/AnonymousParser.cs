using System;
using System.Linq.Expressions;

namespace ParserGenerator {
    public sealed class AnonymousParser<T> : IParser<T> {
        private readonly Func<ArraySegment<byte>, ParsedValue<T>> _parse;
        public bool IsBlittable { get { return false; } }
        public int? OptionalConstantSerializedLength { get { return null; } }
        public Expression TryParseInline(Expression array, Expression offset, Expression count) {
            return null;
        }

        public AnonymousParser(Func<ArraySegment<byte>, ParsedValue<T>> parse) {
            _parse = parse;
        }

        public ParsedValue<T> Parse(ArraySegment<byte> data) {
            return _parse(data);
        }
    }
}
