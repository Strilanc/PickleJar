using System;
using System.Collections.Generic;
using System.Linq;

namespace Strilanc.PickleJar.Internal.Structured {
    internal sealed class DictionaryJar<TKey, TValue> : IJar<IReadOnlyDictionary<TKey, TValue>> {
        private readonly KeyValueJar<TKey, TValue>[] _keyedJars;
        private readonly IJar<IReadOnlyList<KeyValuePair<TKey, TValue>>> _sequencedJar;

        public bool CanBeFollowed { get { return _sequencedJar.CanBeFollowed; } }

        public DictionaryJar(IEnumerable<KeyValueJar<TKey, TValue>> keyedJars) {
            if (keyedJars == null) throw new ArgumentNullException("keyedJars");
            this._keyedJars = keyedJars.ToArray();
            this._sequencedJar = SequencedJarUtil.MakeSequencedJar(_keyedJars);
        }
        public ParsedValue<IReadOnlyDictionary<TKey, TValue>> Parse(ArraySegment<byte> data) {
            return _sequencedJar.Parse(data).Select(e => (IReadOnlyDictionary<TKey, TValue>)e.ToDictionary(p => p.Key, p => p.Value));
        }
        public byte[] Pack(IReadOnlyDictionary<TKey, TValue> value) {
            if (value.Count != _keyedJars.Length) throw new ArgumentException("value.Count != _keyedJars.Length");
            return _sequencedJar.Pack(_keyedJars.Select(e => new KeyValuePair<TKey, TValue>(e.Key, value[e.Key])).ToArray());
        }

        public override string ToString() {
            return _keyedJars.StringJoinList("{", ", ", "}");
        }
    }
}
