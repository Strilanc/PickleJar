using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Strilanc.PickleJar.Internal {
    /// <summary>
    /// CollectionUtil contains internal utility and convenience methods related to collections such as sets, lists, and enumerables.
    /// </summary>
    internal static class CollectionUtil {
        /// <summary>
        /// Enumerates all of the ways that it's possible one item from each collection in a sequence.
        /// For example, the choice combinations of [[1,2],[3,4,5]] are (in some order): {[1,3],[1,4],[1,5],[2,3],[2,4],[2,5]}.
        /// </summary>
        public static IEnumerable<IReadOnlyList<T>> AllChoiceCombinations<T>(this IEnumerable<IEnumerable<T>> sequenceOfChoices) {
            using (var e = sequenceOfChoices.GetEnumerator().AllChoiceCombinationsOfRemainder()) {
                while (e.MoveNext()) {
                    yield return e.Current;
                }
            }
        }
        private static IEnumerator<IImmutableList<T>> AllChoiceCombinationsOfRemainder<T>(this IEnumerator<IEnumerable<T>> sequenceOfChoices) {
            if (!sequenceOfChoices.MoveNext()) {
                yield return ImmutableList.Create<T>();
                yield break;
            }

            var headChoices = sequenceOfChoices.Current;
            var tailChoices = sequenceOfChoices.AllChoiceCombinationsOfRemainder();
            using (var e = tailChoices) {
                while (e.MoveNext()) {
                    var tailChoice = e.Current;
                    foreach (var headChoice in headChoices) {
                        yield return tailChoice.Insert(0, headChoice);
                    }
                }
            }
        }

        /// <summary>
        /// Enumerates all of the ways that it's possible one item from each collection in a sequence.
        /// Enumerated items are volatile. They are invalidated when the enumeration continues.
        /// For example, the choice combinations of [[1,2],[3,4,5]] are (in some order): {[1,3],[1,4],[1,5],[2,3],[2,4],[2,5]}.
        /// </summary>
        public static IEnumerable<T[]> AllChoiceCombinationsVolatile<T>(this IEnumerable<IEnumerable<T>> sequenceOfChoices) {
            using (var e = sequenceOfChoices.GetEnumerator().AllChoiceCombinationsOfRemainderVolatile(0)) {
                while (e.MoveNext()) {
                    yield return e.Current;
                }
            }
        }
        private static IEnumerator<T[]> AllChoiceCombinationsOfRemainderVolatile<T>(this IEnumerator<IEnumerable<T>> sequenceOfChoices, int index) {
            if (!sequenceOfChoices.MoveNext()) {
                yield return new T[index];
                yield break;
            }

            var headChoices = sequenceOfChoices.Current;
            var tailChoices = sequenceOfChoices.AllChoiceCombinationsOfRemainderVolatile(index + 1);
            using (var e = tailChoices) {
                while (e.MoveNext()) {
                    var tailChoice = e.Current;
                    foreach (var headChoice in headChoices) {
                        tailChoice[index] = headChoice;
                        yield return tailChoice;
                    }
                }
            }
        }

        /// <summary>
        /// Returns a dictionary with key/value pairs corresponding to items from the given sequence.
        /// The key used for each item is the result of applying the given key selector function to the item.
        /// The value used for each item is the item itself.
        /// </summary>
        public static IReadOnlyDictionary<TKey, TValue> KeyedBy<TKey, TValue>(this IEnumerable<TValue> sequence, Func<TValue, TKey> keySelector) {
            return sequence.ToDictionary(keySelector, e => e);
        }

        /// <summary>
        /// Returns a dictionary where each item from the sequence is mapped to its index in the sequence.
        /// </summary>
        public static IReadOnlyDictionary<T, int> ToIndexMap<T>(this IEnumerable<T> sequence) {
            var i = 0;
            return sequence.ToDictionary(e => e, e => i++);
        }

        ///<summary>Returns an array segment over the same data, but with the start of the range advanced by the given count and the end kept fixed.</summary>
        public static ArraySegment<T> Skip<T>(this ArraySegment<T> segment, int count) {
            return new ArraySegment<T>(segment.Array, segment.Offset + count, segment.Count - count);
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
