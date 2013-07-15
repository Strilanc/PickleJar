using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

public static class TestingUtilities {
    public static void AssertTrue(this bool value) {
        Assert.IsTrue(value);
    }
    public static void AssertEquals<T1, T2>(this T1 value1, T2 value2) {
        Assert.AreEqual(value1, value2);
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
