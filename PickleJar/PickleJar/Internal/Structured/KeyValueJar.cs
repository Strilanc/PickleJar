using System;
using System.Collections.Generic;

namespace Strilanc.PickleJar.Internal.Structured {
    internal sealed class KeyValueJar<TKey, TValue> : IJar<KeyValuePair<TKey, TValue>> {
        public readonly TKey Key;
        private readonly IJar<TValue> _valueJar;

        public bool CanBeFollowed { get { return _valueJar.CanBeFollowed; } }

        public KeyValueJar(TKey key, IJar<TValue> valueJar) {
            if (ReferenceEquals(key, null)) throw new ArgumentNullException("key");
            if (valueJar == null) throw new ArgumentNullException("valueJar");
            this.Key = key;
            this._valueJar = valueJar;
        }
        public ParsedValue<KeyValuePair<TKey, TValue>> Parse(ArraySegment<byte> data) {
            return _valueJar.Parse(data).Select(val => new KeyValuePair<TKey, TValue>(Key, val));
        }
        public byte[] Pack(KeyValuePair<TKey, TValue> value) {
            if (!Equals(value.Key, Key)) throw new ArgumentException("value.Key != _key");
            return _valueJar.Pack(value.Value);
        }

        public override string ToString() {
            return string.Format(
                "{0}: {1}",
                Key,
                _valueJar);
        }
    }
}
