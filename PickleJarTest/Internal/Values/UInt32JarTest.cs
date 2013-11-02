using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Strilanc.PickleJar;

[TestClass]
public class UInt32JarTest {
    [TestMethod]
    public void TestBigEndian() {
        var jar = Jar.UInt32BigEndian;
        jar.AssertPicklesNoMoreNoLess(0u, 0, 0, 0, 0);
        jar.AssertPicklesNoMoreNoLess(1u, 0, 0, 0, 1);
        jar.AssertPicklesNoMoreNoLess(0x12345678u, 0x12, 0x34, 0x56, 0x78);
        jar.AssertPicklesNoMoreNoLess(UInt32.MaxValue, 0xFF, 0xFF, 0xFF, 0xFF);

        jar.AssertCantParse();
        jar.AssertCantParse(1, 2, 3);
    }
    [TestMethod]
    public void TestLittleEndian() {
        var jar = Jar.UInt32LittleEndian;
        jar.AssertPicklesNoMoreNoLess(0u, 0, 0, 0, 0);
        jar.AssertPicklesNoMoreNoLess(1u, 1, 0, 0, 0);
        jar.AssertPicklesNoMoreNoLess(0x12345678u, 0x78, 0x56, 0x34, 0x12);
        jar.AssertPicklesNoMoreNoLess(UInt32.MaxValue, 0xFF, 0xFF, 0xFF, 0xFF);

        jar.AssertCantParse();
        jar.AssertCantParse(1, 2, 3);
    }
}
