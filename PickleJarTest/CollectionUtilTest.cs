using System;
using Strilanc.PickleJar.Internal;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class CollectionUtilTest {
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
    public void TestToIndexMap() {
        var d = new[] { 'a', 'b', 'c' }.ToIndexMap();
        d.Count.AssertEquals(3);
        d['a'].AssertEquals(0);
        d['b'].AssertEquals(1);
        d['c'].AssertEquals(2);
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
