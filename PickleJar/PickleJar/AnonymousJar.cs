using System;

namespace Strilanc.PickleJar {
    public sealed class AnonymousJar<T> : IJar<T> {
        private readonly Func<ArraySegment<byte>, ParsedValue<T>> _parse;
        private readonly Func<T, byte[]> _pack;
        public bool CanBeFollowed { get; private set; }

        public AnonymousJar(Func<ArraySegment<byte>, ParsedValue<T>> parse, Func<T, byte[]> pack, bool canBeFollowed) {
            if (parse == null) throw new ArgumentNullException("parse");
            if (pack == null) throw new ArgumentNullException("pack");
            _parse = parse;
            _pack = pack;
            CanBeFollowed = canBeFollowed;
        }
        public ParsedValue<T> Parse(ArraySegment<byte> data) {
            return _parse(data);
        }
        public byte[] Pack(T value) {
            return _pack(value);
        }
    }
}