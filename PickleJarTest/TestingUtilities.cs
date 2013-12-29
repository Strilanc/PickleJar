using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Strilanc.PickleJar;
using Strilanc.PickleJar.Internal;

public static class TestingUtilities {
    internal static IJar<T> TryCompileInlined<T>(this IJar<T> jar) {
        var meta = jar as IJarMetadataInternal;
        if (meta == null) return null;

        var array = Expression.Parameter(typeof(byte[]), "array");
        var offset = Expression.Parameter(typeof(int), "offset");
        var count = Expression.Parameter(typeof(int), "count");

        var inlined = meta.TrySpecializeParser(
            array,
            offset,
            count);
        if (inlined == null) return null;

        var parseBody = Expression.Block(
            inlined.Storage.ForBoth,
            inlined.ParseDoer,
            Expression.New(
                typeof(ParsedValue<T>).GetConstructor(new[] {typeof(T), typeof(int)}).NotNull(), 
                inlined.ValueGetter,
                inlined.ConsumedCountGetter));
        var parseMethod = Expression.Lambda<Func<byte[], int, int, ParsedValue<T>>>(
            parseBody,
            new[] {array, offset, count});

        var c = parseMethod.Compile();

        return Jar.Create(
            e => c(e.Array, e.Offset, e.Count),
            jar.Pack,
            jar.CanBeFollowed);
    }
    public static void AssertPicklesNoMoreNoLess<T>(this IJar<T> jar, T value, params byte[] data) {
        jar.AssertPickles(value, data, ignoresExtraData: true, failsOnLessData: true);
    }
    public static void AssertPickles<T>(this IJar<T> jar, T value, byte[] data, bool ignoresExtraData, bool failsOnLessData) {
        // packs to same data
        var packed = jar.Pack(value);
        data.AssertSequenceEquals(packed);

        // parses to same value, consumes correct data
        var snippets = new[] {
            new byte[] {},
            new byte[] {1},
            new byte[] {2, 3, 5}
        };
        foreach (var v in from prefix in snippets
                          from suffix in snippets
                          from includedSuffix in ignoresExtraData ? snippets : snippets.Take(1)
                          let d = prefix.Concat(data).Concat(includedSuffix).Concat(suffix).ToArray()
                          select jar.Parse(new ArraySegment<byte>(d, prefix.Length, data.Length + includedSuffix.Length))) {
            v.Consumed.AssertEquals(data.Length);
            v.Value.AssertSimilar(value);
        }

        if (failsOnLessData && data.Length > 0) {
            AssertThrows(() => jar.Parse(data.Take(data.Length - 1).ToArray()));
        }

        // optimized form also works?
        var optimized = jar.TryCompileInlined();
        if (optimized != null) optimized.AssertPickles(value, data, ignoresExtraData, failsOnLessData);
    }
    public static void AssertCanParse<T>(this IJar<T> jar, params byte[] data) {
        var surrounding = new[] {
            new byte[] {},
            new byte[] {1,2,3}
        };
        foreach (var s in surrounding) {
            var d = s.Concat(data).ToArray();
            AssertDoesNotThrow(() => jar.Parse(new ArraySegment<byte>(d, s.Length, data.Length)));
        }

        var optimized = jar.TryCompileInlined();
        if (optimized != null) optimized.AssertCanParse(data);        
    }
    public static void AssertCantParse<T>(this IJar<T> jar, params byte[] data) {
        var surrounding = new[] {
            new byte[] {},
            new byte[] {1,2,3}
        };
        foreach (var p1 in surrounding) {
            foreach (var p2 in surrounding) {
                var d = p1.Concat(data).Concat(p2).ToArray();
                AssertThrows(() => jar.Parse(new ArraySegment<byte>(d, p1.Length, data.Length)));
            }
        }

        var optimized = jar.TryCompileInlined();
        if (optimized != null) optimized.AssertCantParse(data);
    }
    public static void AssertCantPack<T>(this IJar<T> jar, T value) {
        AssertThrows(() => jar.Pack(value));

        var optimized = jar.TryCompileInlined();
        if (optimized != null) optimized.AssertCantPack(value);
    }
    public static void AssertCanPack<T>(this IJar<T> jar, T value) {
        AssertDoesNotThrow(() => jar.Pack(value));

        var optimized = jar.TryCompileInlined();
        if (optimized != null) optimized.AssertCanPack(value);
    }

    public static void AssertTrue(this bool value) {
        Assert.IsTrue(value);
    }
    public static void AssertFalse(this bool value) {
        Assert.IsFalse(value);
    }
    public static T InvokeWithDefaultOnFail<T>(Func<T> v) {
        try {
            var r = v();
            Assert.AreNotEqual(r, default(T));
            return r;
        } catch {
            return default(T);
        }
    }
    public static void AssertEquals<T1, T2>(this T1 value1, T2 value2) {
        Assert.AreEqual(value1, value2);
    }
    public static void AssertSequenceEquals<T>(this IEnumerable<T> value1, IEnumerable<T> value2) {
        var d1 = value1.ToArray();
        var d2 = value2.ToArray();
        if (!d1.SequenceEqual(d2)) {
            Assert.Fail("Expected {0} to be sequence as {1}", string.Join(", ", d1), string.Join(", ", d2));
        }
    }
    public static void AssertNotEqualTo<T1, T2>(this T1 value1, T2 value2) {
        Assert.AreNotEqual(value1, value2);
    }
    public static void AssertSimilar<T>(this T value1, T value2) {
        bool b = Equals(value1, value2) 
            || (value1.GetType().GetInterfaces().Contains(typeof(IEnumerable)) && Enumerable.SequenceEqual((dynamic)value1, (dynamic)value2));
        if (!b) Assert.AreEqual(value1, value2);
    }
    public static void AssertThrows(Action action) {
        try {
            action();
        } catch (Exception) {
            return;
        }
        Assert.Fail("Expected method to throw, but it ran succesfully.");
    }
    public static void AssertThrows<T>(Func<T> action) {
        T result;
        try {
            result = action();
        } catch {
            return;
        }
        Assert.Fail("Expected method to throw, but it returned {0} instead.", result);
    }
    public static void AssertDoesNotThrow(Action action) {
        action();
    }
    public static void AssertDoesNotThrow<T>(Func<T> action) {
        action();
    }
}
