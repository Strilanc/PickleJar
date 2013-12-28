using System;
using System.Collections.Generic;
using System.Collections.Immutable;

/// <summary>
/// Some collection utilities used for testing, that reference the immutable collections package.
/// </summary>
public static class CollectionUtilExtra {
    /// <summary>
    /// Enumerates all of the ways that it's possible to choose one item from each collection from a sequence.
    /// Enumerated items are volatile. They are invalidated when the enumeration continues.
    /// For example, the choice combinations of [[1,2],[3,4,5]] are (in some order): {[1,3],[1,4],[1,5],[2,3],[2,4],[2,5]}.
    /// </summary>
    public static IEnumerable<T[]> AllChoiceCombinationsVolatile<T>(this IEnumerable<IEnumerable<T>> sequenceOfChoices) {
        if (sequenceOfChoices == null) throw new ArgumentNullException("sequenceOfChoices");
        using (var e = sequenceOfChoices.GetEnumerator().AllChoiceCombinationsVolatile_Enumerator(0)) {
            while (e.MoveNext()) {
                yield return e.Current;
            }
        }
    }
    private static IEnumerator<T[]> AllChoiceCombinationsVolatile_Enumerator<T>(this IEnumerator<IEnumerable<T>> remainingChoiceCollections, int index) {
        if (!remainingChoiceCollections.MoveNext()) {
            yield return new T[index];
            yield break;
        }

        var headChoices = remainingChoiceCollections.Current;
        var tailChoices = remainingChoiceCollections.AllChoiceCombinationsVolatile_Enumerator(index + 1);
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
    /// Enumerates all of the ways that it's possible one item from each collection in a sequence.
    /// For example, the choice combinations of [[1,2],[3,4,5]] are (in some order): {[1,3],[1,4],[1,5],[2,3],[2,4],[2,5]}.
    /// </summary>
    public static IEnumerable<IReadOnlyList<T>> AllChoiceCombinations<T>(this IEnumerable<IEnumerable<T>> sequenceOfChoices) {
        if (sequenceOfChoices == null) throw new ArgumentNullException("sequenceOfChoices");
        using (var enumerator = sequenceOfChoices.GetEnumerator().AllChoiceCombinations_Enumerator()) {
            while (enumerator.MoveNext()) {
                yield return enumerator.Current;
            }
        }
    }
    private static IEnumerator<IImmutableList<T>> AllChoiceCombinations_Enumerator<T>(this IEnumerator<IEnumerable<T>> remainingChoiceCollections) {
        if (!remainingChoiceCollections.MoveNext()) {
            yield return ImmutableList.Create<T>();
            yield break;
        }

        var headChoices = remainingChoiceCollections.Current;
        var tailChoices = remainingChoiceCollections.AllChoiceCombinations_Enumerator();
        using (var e = tailChoices) {
            while (e.MoveNext()) {
                var tailChoice = e.Current;
                foreach (var headChoice in headChoices) {
                    yield return tailChoice.Insert(0, headChoice);
                }
            }
        }
    }
}
