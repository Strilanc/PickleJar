using System;

namespace Strilanc.PickleJar {
    public sealed class AnonymousPacker<T> : IPacker<T> {
        private readonly Func<T, byte[]> _pack;

        public AnonymousPacker(Func<T, byte[]> pack) {
            if (pack == null) throw new ArgumentNullException("pack");
            _pack = pack;
        }
        public byte[] Pack(T value) {
            return _pack(value);
        }
    }
}