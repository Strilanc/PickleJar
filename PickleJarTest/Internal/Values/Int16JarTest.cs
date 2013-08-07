using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Strilanc.PickleJar.Internal;
using Strilanc.PickleJar.Internal.Values;

[TestClass]
public class Int16JarTest {
    [TestMethod]
    public void TestBigEndian() {
        var jar = new Int16Jar(Endianess.BigEndian);
        jar.AssertPicklesNoMoreNoLess((short)0, 0, 0);
        jar.AssertPicklesNoMoreNoLess((short)1, 0, 1);
        jar.AssertPicklesNoMoreNoLess((short)-1, 0xFF, 0xFF);
        jar.AssertPicklesNoMoreNoLess((short)0x1234, 0x12, 0x34);
        jar.AssertPicklesNoMoreNoLess(Int16.MaxValue, 0x7F, 0xFF);
        jar.AssertPicklesNoMoreNoLess(Int16.MinValue, 0x80, 0);

        jar.AssertCantParse();
        jar.AssertCantParse(1);
    }
    [TestMethod]
    public void TestLittleEndian() {
        var jar = new Int16Jar(Endianess.LittleEndian);
        jar.AssertPicklesNoMoreNoLess((short)0, 0, 0);
        jar.AssertPicklesNoMoreNoLess((short)1, 1, 0);
        jar.AssertPicklesNoMoreNoLess((short)-1, 0xFF, 0xFF);
        jar.AssertPicklesNoMoreNoLess((short)0x1234, 0x34, 0x12);
        jar.AssertPicklesNoMoreNoLess(Int16.MaxValue, 0xFF, 0x7F);
        jar.AssertPicklesNoMoreNoLess(Int16.MinValue, 0, 0x80);

        jar.AssertCantParse();
        jar.AssertCantParse(1);
    }
}
