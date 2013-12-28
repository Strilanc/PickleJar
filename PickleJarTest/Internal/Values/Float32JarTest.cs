﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Strilanc.PickleJar.Internal;
using Strilanc.PickleJar.Internal.Basic;
using Strilanc.PickleJar.Internal.Misc;

[TestClass]
public class Float32JarTest {
    [TestMethod]
    public void TestLittleEndian() {
        var jar = NumericJar.CreateForType<float>(Endianess.LittleEndian);

        jar.AssertPicklesNoMoreNoLess(+0.0f, 0, 0, 0, 0);
        jar.AssertPicklesNoMoreNoLess(-0.0f, 0, 0, 0, 0x80);
        jar.AssertPicklesNoMoreNoLess(+1.0f, 0, 0, 0x80, 0x3F);
        jar.AssertPicklesNoMoreNoLess(-1.0f, 0, 0, 0x80, 0xBF);
        jar.AssertPicklesNoMoreNoLess(0.1f, 205, 204, 204, 61);
        jar.AssertPicklesNoMoreNoLess(float.PositiveInfinity, 0, 0, 0x80, 0x7F);
        jar.AssertPicklesNoMoreNoLess(float.NegativeInfinity, 0, 0, 0x80, 0xFF);
        jar.AssertPicklesNoMoreNoLess(float.NaN, 0, 0, 0xC0, 0xFF);

        jar.AssertCantParse();
        jar.AssertCantParse(1, 2, 3);
    }
    [TestMethod]
    public void TestBigEndian() {
        var jar = NumericJar.CreateForType<float>(Endianess.BigEndian);

        jar.AssertPicklesNoMoreNoLess(+0.0f, 0, 0, 0, 0);
        jar.AssertPicklesNoMoreNoLess(-0.0f, 0x80, 0, 0, 0);
        jar.AssertPicklesNoMoreNoLess(+1.0f, 0x3F, 0x80, 0, 0);
        jar.AssertPicklesNoMoreNoLess(-1.0f, 0xBF, 0x80, 0, 0);
        jar.AssertPicklesNoMoreNoLess(0.1f, 61, 204, 204, 205);
        jar.AssertPicklesNoMoreNoLess(float.PositiveInfinity, 0x7F, 0x80, 0, 0);
        jar.AssertPicklesNoMoreNoLess(float.NegativeInfinity, 0xFF, 0x80, 0, 0);
        jar.AssertPicklesNoMoreNoLess(float.NaN, 0xFF, 0xC0, 0, 0);

        jar.AssertCantParse();
        jar.AssertCantParse(1, 2, 3);
    }
}
