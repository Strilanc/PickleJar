using System;
using System.Collections;
using System.Collections.Generic;
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
    public static void AssertSequenceEquals<T>(this IEnumerable<T> value1, IEnumerable<T> value2) {
        Assert.IsTrue(value1.SequenceEqual(value2));
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
        Assert.Fail();
    }
    public static void AssertDoesNotThrow(Action action) {
        action();
    }
}
