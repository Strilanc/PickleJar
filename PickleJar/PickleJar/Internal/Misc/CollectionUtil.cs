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

        public static IEnumerable<int> Range(this int count) {
            return Enumerable.Range(0, count);
        } 
        ///<summary>Determines if any of the items in the sequence is a null value. Throws if the collection itself is null.</summary>
        public static bool HasNulls<T>(this IEnumerable<T> sequence) where T : class {
            if (sequence == null) throw new ArgumentNullException("sequence");
            return sequence.Any(e => ReferenceEquals(e, null));
        }
        ///<summary>Determines if the sequence, or else any of the items in it, is a null value.</summary>
        public static bool IsOrHasNulls<T>(this IEnumerable<T> sequence) where T : class {
            return sequence == null || sequence.HasNulls();
        }
        /// <summary>
        /// Joins the items of a sequence, after applying ToString to them, into a single string.
        /// The result is prefixed by the 'before' string.
        /// The item representations are separated by the 'between' string.
        /// The result is suffixed by the 'after' represention.
        /// </summary>
        public static string StringJoinList<T>(this IEnumerable<T> sequence, string before, string between, string after) {
            return before + string.Join(between, sequence) + after;
        }

        /// <summary>
        /// Returns the first item in a list of structs, after casting to the struct's nullable type, but returns null if the list is empty.
        /// </summary>
        public static T? FirstOrNull<T>(this IEnumerable<T> sequence) where T : struct {
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

        /// <summary>
        /// Returns the largest item in the sequence, with respect to the default ordering of its result after projecting through a function.
        /// </summary>
        public static TItem MaxBy<TItem, TCompare>(this IEnumerable<TItem> sequence, Func<TItem, TCompare> compareSelector) {
            if (sequence == null) throw new ArgumentNullException("sequence");
            if (compareSelector == null) throw new ArgumentNullException("compareSelector");
            var comparer = Comparer<TCompare>.Default;
            return sequence.Aggregate((e1, e2) => comparer.Compare(compareSelector(e1), compareSelector(e2)) >= 0 ? e1 : e2);
        }

        /// <summary>
        /// Yields all the items from the underlying sequence, except the given number from the end.
        /// If there are fewer items in the sequence than the specified skip count, no items are yielded.
        /// If the skip count is negative or zero then all items from the underlying sequence are yielded.
        /// </summary>
        public static IEnumerable<T> SkipLast<T>(this IEnumerable<T> sequence, int skipCount) {
            if (sequence == null) throw new ArgumentNullException("sequence");
            if (skipCount < 0) skipCount = 0;
            var q = new Queue<T>(capacity: skipCount);
            foreach (var e in sequence) {
                q.Enqueue(e);
                if (q.Count > skipCount) {
                    yield return q.Dequeue();
                }
            }
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

        /// <summary>
        /// Accumulates a value across the input sequence, yielding the intermediate accumulation after each item.
        /// </summary>
        public static IEnumerable<TOut> StreamAggregate<TIn, TOut>(this IEnumerable<TIn> sequence, TOut seed, Func<TOut, TIn, TOut> acc) {
            return sequence.Select(e => seed = acc(seed, e));
        }
        /// <summary>
        /// Accumulates a value across the input sequence, yielding each item paired with the intermediate accumulation after processing the item.
        /// </summary>
        public static IEnumerable<Tuple<TIn, TStream>> StreamAggregateZip<TIn, TStream>(this IEnumerable<TIn> sequence, TStream seed, Func<TStream, TIn, TStream> acc) {
            return sequence.StreamAggregate(Tuple.Create(default(TIn), seed), (a, e) => Tuple.Create(e, acc(a.Item2, e)));
        }

        /// <summary>
        /// Determines if two dictionaries have the same set of keys mapped to values, ignoring what values the keys point to.
        /// </summary>
        public static bool HasSameKeyValuesAs<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, IReadOnlyDictionary<TKey, TValue> other) {
            return dictionary.Count == other.Count
                   && dictionary.All(other.Contains);
        }

        /// <summary>
        /// Concatenates the bytes from the arrays in a list into a single array of bytes.
        /// </summary>
        /// <remarks>
        /// Used via reflection by RuntimeSpecializedJar.
        /// Do not rename without updating references via reflections.
        /// </remarks>
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

        /// <summary>
        /// Returns a dictionary containing the key value pairs from the given sequence.
        /// </summary>
        /// <remarks>
        /// Used via reflection by DictionaryJar.
        /// Do not rename without updating references via reflections.
        /// </remarks>
        public static IReadOnlyDictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> keyValueSequence) {
            if (keyValueSequence == null) throw new ArgumentNullException("keyValueSequence");
            return keyValueSequence.ToDictionary(e => e.Key, e => e.Value);
        }

        /// <summary>
        /// Removes one of the given prefixes from the given text, returning the result.
        /// If the string is not prefixed by any of the prefixes, it is returned unchanged.
        /// </summary>
        public static string TrimUpToOnePrefix(this string text, params string[] prefixes) {
            return text.Substring(
                prefixes
                .Where(text.StartsWith)
                .Select(e => e.Length)
                .SingleOrDefault());
        }

        /// <summary>
        /// Partitions the sequence into pieces that start with and end before items which cause the given predicate to return true.
        /// The empty sequence yields no partitions.
        /// </summary>
        public static IEnumerable<IReadOnlyList<T>> StartNewPartitionWhen<T>(this IEnumerable<T> sequence, Func<T, bool> predicate) {
            if (sequence == null) throw new ArgumentNullException("sequence");
            if (predicate == null) throw new ArgumentNullException("predicate");

            var curPartition = new List<T>();
            foreach (var item in sequence) {
                if (curPartition.Count > 0 && predicate(item)) {
                    yield return curPartition.ToArray();
                    curPartition.Clear();
                }
                curPartition.Add(item);
            }
            if (curPartition.Count > 0) {
                yield return curPartition;
            }
        }

        public static bool HasDuplicates<T>(this IEnumerable<T> sequence) {
            var known = new HashSet<T>();
            return sequence.All(known.Add);
        }
    }
}
