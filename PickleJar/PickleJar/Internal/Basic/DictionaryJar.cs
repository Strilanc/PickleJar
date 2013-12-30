using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Strilanc.PickleJar.Internal.Combinators;
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
                packProjection: e => e.AccessMember(typeof(KeyValuePair<TKey, TValue>).GetProperty("Value")),
                desc: () => string.Format("{0}: {1}", key, valueJar));
        }

        public static IJar<IReadOnlyDictionary<TKey, TValue>> Create<TKey, TValue>(IEnumerable<KeyValuePair<TKey, IJar<TValue>>> keyedJars) {
            if (keyedJars == null) throw new ArgumentNullException("keyedJars");
            var _keyedJars = keyedJars.ToArray();
            if (_keyedJars.Select(e => e.Key).HasDuplicates()) throw new ArgumentOutOfRangeException("keyedJars", "Duplicate keys");

            var subJar = ListJar.Create(_keyedJars.Select(CreateKeyedJar));
            return AnonymousJar.CreateSpecialized<IReadOnlyDictionary<TKey, TValue>>(
                parseSpecializer: (array, offset, count) => {
                    var sub = subJar.MakeInlinedParserComponents(array, offset, count);
                    return new SpecializedParserParts(
                        parseDoer: sub.ParseDoer,
                        valueGetter: Expression.Call(typeof(CollectionUtil).GetMethod("ToDictionary", new[] {typeof(IEnumerable<KeyValuePair<TKey, TValue>>)}),
                                                     sub.ValueGetter),
                        consumedCountGetter: sub.ConsumedCountGetter,
                        storage: sub.Storage);
                },
                packSpecializer: value => {
                    // todo: check keys
                    //if (value.Count != _keyedJars.Length) throw new ArgumentException("value.Count != _keyedJars.Length");
                    return SpecializedPackerParts.FromSequence(keyedJars.Select(keyedSubJar => {
                        var indexProperty = typeof(IReadOnlyDictionary<TKey, TValue>).GetProperty("Item");
                        var val = Expression.MakeIndex(value, indexProperty, new[] { keyedSubJar.Key.ConstExpr() });
                        return keyedSubJar.Value.MakeSpecializedPacker(val);
                    }).ToArray());
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
