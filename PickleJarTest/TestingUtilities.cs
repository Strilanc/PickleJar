using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

public static class TestingUtilities {
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
    public static void AssertNotEqualTo<T1, T2>(this T1 value1, T2 value2) {
        Assert.AreNotEqual(value1, value2);
    }
    public static void AssertSimilar<T>(this T value1, T value2) {
        Assert.IsTrue(Equals(value1, value2) || Enumerable.SequenceEqual((dynamic)value1, (dynamic)value2));
    }
    public static void AssertThrows(Action action) {
        try {
            action();
            Assert.Fail();
        } catch (Exception) {
        }
    }
    public static void AssertDoesNotThrow(Action action) {
        action();
    }
}
