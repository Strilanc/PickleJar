using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Strilanc.PickleJar.Internal.Structured;

namespace Strilanc.PickleJar.Internal.Values {
    internal static class DictionaryJar {
        private static IJar<KeyValuePair<TKey, TValue>> CreateKeyedJar<TKey, TValue>(KeyValuePair<TKey, IJar<TValue>> keyValueJar) {
            if (ReferenceEquals(keyValueJar.Key, null)) throw new ArgumentNullException("keyValueJar", "keyValueKar.Key == null");
            if (keyValueJar.Value == null) throw new ArgumentNullException("keyValueJar", "keyValueKar.Value == null");
            var key = keyValueJar.Key;
            var valueJar = keyValueJar.Value;

            return ProjectionJar.CreateSpecialized<TValue, KeyValuePair<TKey, TValue>>(
                valueJar,
                parsedValueExpression => Expression.New(
                    typeof(KeyValuePair<TKey, TValue>).GetConstructor(new[] { typeof(TKey), typeof(TValue) }).NotNull(),
                    Expression.Constant(key),
                    parsedValueExpression),
                packProjection: e => {
                    if (!Equals(e.Key, key)) throw new ArgumentException("value.Key != key");
                    return e.Value;
                },
                desc: () => string.Format("{0}: {1}", key, valueJar),
                components: null);
        }

        public static IJar<IReadOnlyDictionary<TKey, TValue>> Create<TKey, TValue>(IEnumerable<KeyValuePair<TKey, IJar<TValue>>> keyedJars) {
            if (keyedJars == null) throw new ArgumentNullException("keyedJars");
            var _keyedJars = keyedJars.ToArray();
            if (_keyedJars.Select(e => e.Key).HasDuplicates()) throw new ArgumentOutOfRangeException("keyedJars", "Duplicate keys");

            var _sequencedJar = ListJar.Create(_keyedJars.Select(CreateKeyedJar));
            return new AnonymousJar<IReadOnlyDictionary<TKey, TValue>>(
                parse: data => {
                    return _sequencedJar.Parse(data).Select(e => (IReadOnlyDictionary<TKey, TValue>)e.ToDictionary(p => p.Key, p => p.Value));
                },
                pack: value => {
                    if (value.Count != _keyedJars.Length) throw new ArgumentException("value.Count != _keyedJars.Length");
                    return _sequencedJar.Pack(_keyedJars.Select(e => new KeyValuePair<TKey, TValue>(e.Key, value[e.Key])).ToArray());                    
                },
                canBeFollowed: _sequencedJar.CanBeFollowed,
                isBlittable: _sequencedJar.IsBlittable(),
                optionalConstantSerializedLength: _sequencedJar.OptionalConstantSerializedLength(),
                tryInlinedParserComponents: null,
                desc: () => _keyedJars
                    .Select(e => string.Format("{0}: {1}", e.Key, e.Value))
                    .StringJoinList("{", ", ", "}"),
                components: _keyedJars);
        }
    }
}
