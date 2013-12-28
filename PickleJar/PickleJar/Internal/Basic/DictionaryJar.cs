using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Strilanc.PickleJar.Internal.RuntimeSpecialization;
using Strilanc.PickleJar.Internal.Structured;

namespace Strilanc.PickleJar.Internal.Basic {
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

            var subJar = ListJar.Create(_keyedJars.Select(CreateKeyedJar));
            return AnonymousJar.CreateSpecialized<IReadOnlyDictionary<TKey, TValue>>(
                specializedParserMaker: (array, offset, count) => {
                    var sub = subJar.MakeInlinedParserComponents(array, offset, count);
                    return new SpecializedParserParts(
                        parseDoer: sub.ParseDoer,
                        valueGetter: Expression.Call(typeof(CollectionUtil).GetMethod("ToDictionary", new[] {typeof(IEnumerable<KeyValuePair<TKey, TValue>>)}),
                                                     sub.ValueGetter),
                        consumedCountGetter: sub.ConsumedCountGetter,
                        storage: sub.Storage);
                },
                packer: value => {
                    if (value.Count != _keyedJars.Length) throw new ArgumentException("value.Count != _keyedJars.Length");
                    return subJar.Pack(_keyedJars.Select(e => new KeyValuePair<TKey, TValue>(e.Key, value[e.Key])).ToArray());                    
                },
                canBeFollowed: subJar.CanBeFollowed,
                isBlittable: subJar.IsBlittable(),
                constLength: subJar.OptionalConstantSerializedLength(),
                desc: () => _keyedJars
                    .Select(e => string.Format("{0}: {1}", e.Key, e.Value))
                    .StringJoinList("{", ", ", "}"),
                components: _keyedJars);
        }
    }
}
