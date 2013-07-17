using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Strilanc.PickleJar.Internal;

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
}
