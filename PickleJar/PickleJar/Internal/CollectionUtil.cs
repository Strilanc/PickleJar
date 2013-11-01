using System;
using System.Collections.Generic;
using System.Linq;

namespace Strilanc.PickleJar.Internal {
    /// <summary>
    /// CollectionUtil contains internal utility and convenience methods related to collections such as sets, lists, and enumerables.
    /// </summary>
    internal static class CollectionUtil {
        public static IEnumerable<int> IndexesOf<T>(this IEnumerable<T> sequence, T item) {
            if (sequence == null) throw new ArgumentNullException("sequence");
            var i = 0;
            foreach (var e in sequence) {
                if (Equals(e, item)) yield return i;
                i++;
            }
        }

        public static T? NullableFirst<T>(this IEnumerable<T> sequence) where T : struct {
            return sequence.Select(e => (T?)e).FirstOrDefault();
        }

        /// <summary>
        /// Returns a dictionary with key/value pairs corresponding to items from the given sequence.
        /// The key used for each item is the result of applying the given key selector function to the item.
        /// The value used for each item is the item itself.
        /// </summary>
        public static IReadOnlyDictionary<TKey, TValue> KeyedBy<TKey, TValue>(this IEnumerable<TValue> sequence, Func<TValue, TKey> keySelector) {
            return sequence.ToDictionary(keySelector, e => e);
        }

        public static TItem MaxBy<TItem, TCompare>(this IEnumerable<TItem> sequence, Func<TItem, TCompare> compareSelector) {
            if (sequence == null) throw new ArgumentNullException("sequence");
            if (compareSelector == null) throw new ArgumentNullException("compareSelector");
            var comparer = Comparer<TCompare>.Default;
            return sequence.Aggregate((e1, e2) => comparer.Compare(compareSelector(e1), compareSelector(e2)) >= 0 ? e1 : e2);
        }

        ///<summary>Returns an array segment over the same data, but with the start of the range advanced by the given count and the end kept fixed.</summary>
        public static ArraySegment<T> Skip<T>(this ArraySegment<T> segment, int count) {
            return new ArraySegment<T>(segment.Array, segment.Offset + count, segment.Count - count);
        }
        ///<summary>Returns an array segment over the same data, but with the end of the range stopped at the given count.</summary>
        public static ArraySegment<T> Take<T>(this ArraySegment<T> segment, int count) {
            if (count > segment.Count) throw new ArgumentOutOfRangeException("count");
            return new ArraySegment<T>(segment.Array, segment.Offset, count);
        }
        ///<summary>Determines if all items from the second given sequence are present in the first given sequence.</summary>
        public static bool IsSameOrSubsetOf<T>(this IEnumerable<T> sequence, IEnumerable<T> other) {
            var r = new HashSet<T>(other);
            return sequence.All(r.Contains);
        }
        ///<summary>Determines if the two given sequences contain the same items, not counting duplicates.</summary>
        public static bool HasSameSetOfItemsAs<T>(this IEnumerable<T> sequence, IEnumerable<T> other) {
            var r1 = new HashSet<T>(other);
            var r2 = new HashSet<T>(sequence);
            return r1.Count == r2.Count && r2.All(r1.Contains);
        }

        public static IEnumerable<TOut> Stream<TIn, TOut>(this IEnumerable<TIn> sequence, TOut seed, Func<TOut, TIn, TOut> acc) {
            return sequence.Select(e => seed = acc(seed, e));
        }
        public static IEnumerable<Tuple<TIn, TStream>> StreamZip<TIn, TStream>(this IEnumerable<TIn> sequence, TStream seed, Func<TStream, TIn, TStream> acc) {
            return sequence.Stream(Tuple.Create(default(TIn), seed), (a, e) => Tuple.Create(e, acc(a.Item2, e)));
        }
        public static bool HasSameKeyValuesAs<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, IReadOnlyDictionary<TKey, TValue> other) {
            return dictionary.Count == other.Count
                   && dictionary.All(other.Contains);
        }

        public static byte[] Flatten(this List<byte[]> arrays) {
            var size = arrays.Select(e => e.Length).Sum();
            var result = new byte[size];
            var offset = 0;
            foreach (var array in arrays) {
                array.CopyTo(result, offset);
                offset += array.Length;
            }
            return result;
        }

        public static string TrimUpToOnePrefix(this string text, params string[] prefixes) {
            return text.Substring(
                prefixes
                .Where(text.StartsWith)
                .Select(e => e.Length)
                .SingleOrDefault());
        }

        public static IEnumerable<IReadOnlyList<T>> StartNewPartitionWhen<T>(this IEnumerable<T> sequence, Func<T, bool> predicate) {
            if (sequence == null) throw new ArgumentNullException("sequence");
            if (predicate == null) throw new ArgumentNullException("predicate");

            var curPartition = new List<T>();
            foreach (var e in sequence) {
                if (curPartition.Count > 0 && predicate(e)) {
                    yield return curPartition.ToArray();
                    curPartition.Clear();
                }
                curPartition.Add(e);
            }
            if (curPartition.Count > 0) {
                yield return curPartition;
            }
        }
    }
}
