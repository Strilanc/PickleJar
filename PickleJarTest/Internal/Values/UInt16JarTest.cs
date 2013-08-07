using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Strilanc.PickleJar.Internal;
using Strilanc.PickleJar.Internal.Values;

[TestClass]
public class UInt16JarTest {
    [TestMethod]
    public void TestBigEndian() {
        var jar = new UInt16Jar(Endianess.BigEndian);
        jar.AssertPicklesNoMoreNoLess((ushort)0, 0, 0);
        jar.AssertPicklesNoMoreNoLess((ushort)1, 0, 1);
        jar.AssertPicklesNoMoreNoLess((ushort)0x1234, 0x12, 0x34);
        jar.AssertPicklesNoMoreNoLess(UInt16.MaxValue, 0xFF, 0xFF);

        jar.AssertCantParse();
        jar.AssertCantParse(1);
    }
    [TestMethod]
    public void TestLittleEndian() {
        var jar = new UInt16Jar(Endianess.LittleEndian);
        jar.AssertPicklesNoMoreNoLess((ushort)0, 0, 0);
        jar.AssertPicklesNoMoreNoLess((ushort)1, 1, 0);
        jar.AssertPicklesNoMoreNoLess((ushort)0x1234, 0x34, 0x12);
        jar.AssertPicklesNoMoreNoLess(UInt16.MaxValue, 0xFF, 0xFF);

        jar.AssertCantParse();
        jar.AssertCantParse(1);
    }
}
