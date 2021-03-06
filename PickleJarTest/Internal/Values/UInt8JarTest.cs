﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Strilanc.PickleJar.Internal.Values;

[TestClass]
public class UInt8JarTest {
    [TestMethod]
    public void TestByte() {
        var jar = new UInt8Jar();
        jar.AssertPicklesNoMoreNoLess((byte)0, 0);
        jar.AssertPicklesNoMoreNoLess((byte)1, 1);
        jar.AssertPicklesNoMoreNoLess((byte)0x12, 0x12);
        jar.AssertPicklesNoMoreNoLess(byte.MaxValue, 0xFF);

        jar.AssertCantParse();
    }
}
