using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Strilanc.PickleJar.Internal;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

public static class CollectionUtilExtra {
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
}

[TestClass]
public class CollectionUtilTest {
    [TestMethod]
    public void TestNullableFirst() {
        TestingUtilities.AssertThrows(() => ((IEnumerable<int>)null).NullableFirst());

        new int[0].NullableFirst().AssertEquals((int?)null);
        new[] { 1 }.NullableFirst().AssertEquals((int?)1);
        new[] { 2, 3, 4 }.NullableFirst().AssertEquals((int?)2);
    }
    [TestMethod]
    public void TestMaxBy() {
        TestingUtilities.AssertThrows(() => ((IEnumerable<int>)null).MaxBy(e => e));
        TestingUtilities.AssertThrows(() => new int[1].MaxBy((Func<int, int>)null));
        TestingUtilities.AssertThrows(() => new int[0].MaxBy(e => e));

        new[] { 1 }.MaxBy(e => e).AssertEquals(1);
        new[] { 1, 2, 3 }.MaxBy(e => e).AssertEquals(3);
        new[] { 1, 2, 3 }.MaxBy(e => e / 2).AssertEquals(2);
        new[] { 1, 3, 2 }.MaxBy(e => e / 2).AssertEquals(3);
        new[] { 1, 2, 3 }.MaxBy(e => -e).AssertEquals(1);
        new[] { 1, 2, 3 }.MaxBy(e => e % 3).AssertEquals(2);
    }
    [TestMethod]
    public void TestAllCombinations() {
        new int[0][]
            .AllChoiceCombinations()
            .Single()
            .Count
            .AssertEquals(0);

        new[] {new int[0]}
            .AllChoiceCombinations()
            .Count()
            .AssertEquals(0);

        new[] { new[] {0} }
            .AllChoiceCombinations()
            .Single()
            .SequenceEqual(new[] {0})
            .AssertTrue();

        new[] { new[] { 0, 1 } }
            .AllChoiceCombinations()
            .Zip(new[] { new[] {0}, new[] {1}}, (e1, e2) => e1.SequenceEqual(e2))
            .All(e => e)
            .AssertTrue();

        new[] { new[] { 0 }, new[] { 1 } }
            .AllChoiceCombinations()
            .Single()
            .SequenceEqual(new[] {0,1})
            .AssertTrue();


        new[] { new[] { 1, 2 }, new[] { 3 } }
            .AllChoiceCombinations()
            .Zip(new[] {new[] {1,3}, new[] {2,3}}, (e1, e2) => e1.SequenceEqual(e2))
            .All(e => e)
            .AssertTrue();

        new[] { new int[] { }, new[] { 3, 4, 5 } }
            .AllChoiceCombinations()
            .Count().AssertEquals(0);

        new[] { new[] { 1, 2 }, new[] { 3, 4, 5 } }
            .AllChoiceCombinations()
            .OrderBy(e => e[0])
            .ThenBy(e => e[1])
            .Zip(new[] {
                new[] {1, 3},
                new[] {1, 4},
                new[] {1, 5},
                new[] {2, 3},
                new[] {2, 4},
                new[] {2, 5}
            },
                 (e1, e2) => e1.SequenceEqual(e2))
            .All(e => e)
            .AssertTrue();
    }
    [TestMethod]
    public void TestAllCombinationsVolatile() {
        new int[0][]
            .AllChoiceCombinationsVolatile()
            .Single()
            .Length
            .AssertEquals(0);

        new[] { new int[0] }
            .AllChoiceCombinationsVolatile()
            .Count()
            .AssertEquals(0);

        new[] { new[] { 0 } }
            .AllChoiceCombinationsVolatile()
            .Single()
            .SequenceEqual(new[] { 0 })
            .AssertTrue();

        new[] { new[] { 0, 1 } }
            .AllChoiceCombinationsVolatile()
            .Zip(new[] { new[] { 0 }, new[] { 1 } }, (e1, e2) => e1.SequenceEqual(e2))
            .All(e => e)
            .AssertTrue();

        new[] { new[] { 0 }, new[] { 1 } }
            .AllChoiceCombinationsVolatile()
            .Single()
            .SequenceEqual(new[] { 0, 1 })
            .AssertTrue();


        new[] { new[] { 1, 2 }, new[] { 3 } }
            .AllChoiceCombinationsVolatile()
            .Zip(new[] { new[] { 1, 3 }, new[] { 2, 3 } }, (e1, e2) => e1.SequenceEqual(e2))
            .All(e => e)
            .AssertTrue();

        new[] { new int[] { }, new[] { 3, 4, 5 } }
            .AllChoiceCombinationsVolatile()
            .Count().AssertEquals(0);

        new[] { new[] { 1, 2 }, new[] { 3, 4, 5 } }
            .AllChoiceCombinationsVolatile()
            .Select(e => e.ToArray()) // remove volatility
            .OrderBy(e => e[0])
            .ThenBy(e => e[1])
            .Zip(new[] {
                new[] {1, 3},
                new[] {1, 4},
                new[] {1, 5},
                new[] {2, 3},
                new[] {2, 4},
                new[] {2, 5}
            },
                 (e1, e2) => e1.SequenceEqual(e2))
            .All(e => e)
            .AssertTrue();
    }
    [TestMethod]
    public void TestKeyedBy() {
        var d = new[] { 1, 2, 3 }.KeyedBy(e => e * e);
        d.Count.AssertEquals(3);
        d[1].AssertEquals(1);
        d[4].AssertEquals(2);
        d[9].AssertEquals(3);
    }
    [TestMethod]
    public void TestIndexesOf() {
        new string[0].IndexesOf("").AssertSequenceEquals(new int[0]);

        new[] { "a", "bra", "ca", "da" }.IndexesOf("abracadabra").AssertSequenceEquals(new int[0]);
        new[] { "a", "bra", "ca", "da" }.IndexesOf("a").AssertSequenceEquals(new[] { 0 });
        new[] { "a", "bra", "ca", "da" }.IndexesOf("bra").AssertSequenceEquals(new[] { 1 });
        new[] { "a", "bra", "ca", "da" }.IndexesOf("ca").AssertSequenceEquals(new[] { 2 });
        new[] { "a", "bra", "ca", "da" }.IndexesOf("da").AssertSequenceEquals(new[] { 3 });

        new[] { "a", "bra", "ca", "da", "bra" }.IndexesOf("bra").AssertSequenceEquals(new[] { 1, 4 });

        Enumerable.Repeat("spam", 100).IndexesOf("spam").AssertSequenceEquals(Enumerable.Range(0, 100));
    }
    [TestMethod]
    public void TestSkip() {
        var r = new ArraySegment<int>(new[] {1, 2, 3, 4, 5});
        var r2 = r.Skip(2);
        r2.Array.AssertEquals(r.Array);
        r2.Offset.AssertEquals(2);
        r2.Count.AssertEquals(3);
        var r3 = r2.Skip(3);
        r3.Count.AssertEquals(0);
    }
    [TestMethod]
    public void TestHasSameSetOfItemsAs() {
        new int[] { }.HasSameSetOfItemsAs(new int[] { }).AssertTrue();
        new int[2].HasSameSetOfItemsAs(new int[5]).AssertTrue();
        new[] { 1, 2, 3 }.HasSameSetOfItemsAs(new[] { 3, 1, 2 }).AssertTrue();
        new[] { 1, 2, 3 }.HasSameSetOfItemsAs(new[] { 1, 2 }).AssertFalse();
        new[] { 1, 3 }.HasSameSetOfItemsAs(new[] { 3, 1, 2 }).AssertFalse();
        new[] { 1, 2, 3 }.HasSameSetOfItemsAs(new[] { 3, 1, 4 }).AssertFalse();
        new[] { 1, 2, 3 }.HasSameSetOfItemsAs(new[] { 3, 1, 4 }).AssertFalse();
        new[] { 1, 3, 3 }.HasSameSetOfItemsAs(new[] { 1, 2, 3 }).AssertFalse();
        new[] { 1, 2, 3, 3 }.HasSameSetOfItemsAs(new[] { 1, 2, 3 }).AssertTrue();
    }
    [TestMethod]
    public void TestIsSameOrSubSetOf() {
        new int[] { }.IsSameOrSubsetOf(new int[] { }).AssertTrue();
        new int[2].IsSameOrSubsetOf(new int[5]).AssertTrue();
        new[] { 1, 2, 3 }.IsSameOrSubsetOf(new[] { 3, 1, 2 }).AssertTrue();
        new[] { 1, 2, 3 }.IsSameOrSubsetOf(new[] { 1, 2 }).AssertFalse();
        new[] { 1, 3 }.IsSameOrSubsetOf(new[] { 3, 1, 2 }).AssertTrue();
        new[] { 1, 2, 3 }.IsSameOrSubsetOf(new[] { 3, 1, 4 }).AssertFalse();
        new[] { 1, 2, 3 }.IsSameOrSubsetOf(new[] { 3, 1, 4 }).AssertFalse();
        new[] { 1, 3, 3 }.IsSameOrSubsetOf(new[] { 1, 2, 3 }).AssertTrue();
        new[] { 1, 2, 3, 3 }.IsSameOrSubsetOf(new[] { 1, 2, 3 }).AssertTrue();
    }
    [TestMethod]
    public void TestTrimUpToOnePrefix() {
        // bad cases
        TestingUtilities.AssertThrows(() => "ambiguous".TrimUpToOnePrefix("ambi", "amb").AssertEquals(""));
        TestingUtilities.AssertThrows(() => "null1".TrimUpToOnePrefix(null).AssertEquals(""));
        TestingUtilities.AssertThrows(() => "null2".TrimUpToOnePrefix("a", null).AssertEquals(""));
        
        // one
        "get".TrimUpToOnePrefix("get").AssertEquals("");
        "ge".TrimUpToOnePrefix("get").AssertEquals("ge");
        "get".TrimUpToOnePrefix("ge").AssertEquals("t");

        // two
        "getter".TrimUpToOnePrefix("get", "set").AssertEquals("ter");
        "setter".TrimUpToOnePrefix("get", "set").AssertEquals("ter");
        "letter".TrimUpToOnePrefix("get", "ses").AssertEquals("letter");

        // empty string
        "".TrimUpToOnePrefix("").AssertEquals("");
        "".TrimUpToOnePrefix(" ").AssertEquals("");
        " ".TrimUpToOnePrefix("").AssertEquals(" ");
    }
}
