using System;

namespace Strilanc.PickleJar {
    public sealed class AnonymousParser<T> : IParser<T> {
        private readonly Func<ArraySegment<byte>, ParsedValue<T>> _parse;

        public AnonymousParser(Func<ArraySegment<byte>, ParsedValue<T>> parse) {
            _parse = parse;
        }
        public ParsedValue<T> Parse(ArraySegment<byte> data) {
            return _parse(data);
        }
    }
}
