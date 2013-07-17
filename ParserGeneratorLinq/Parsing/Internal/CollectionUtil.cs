using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Strilanc.Parsing.Internal {
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
    }
}
