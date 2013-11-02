using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Strilanc.PickleJar;

[TestClass]
public class UInt64JarTest {
    [TestMethod]
    public void TestBigEndian() {
        var jar = Jar.UInt64BigEndian;
        jar.AssertPicklesNoMoreNoLess(0u, 0, 0, 0, 0, 0, 0, 0, 0);
        jar.AssertPicklesNoMoreNoLess(1u, 0, 0, 0, 0, 0, 0, 0, 1);
        jar.AssertPicklesNoMoreNoLess(0x123456789ABCDEF0u, 0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0);
        jar.AssertPicklesNoMoreNoLess(UInt64.MaxValue, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF);

        jar.AssertCantParse();
        jar.AssertCantParse(1, 2, 3, 4, 5, 6, 7);
    }
    [TestMethod]
    public void TestLittleEndian() {
        var jar = Jar.UInt64LittleEndian;
        jar.AssertPicklesNoMoreNoLess(0u, 0, 0, 0, 0, 0, 0, 0, 0);
        jar.AssertPicklesNoMoreNoLess(1u, 1, 0, 0, 0, 0, 0, 0, 0);
        jar.AssertPicklesNoMoreNoLess(0x123456789ABCDEF0u, 0xF0, 0xDE, 0xBC, 0x9A, 0x78, 0x56, 0x34, 0x12);
        jar.AssertPicklesNoMoreNoLess(UInt64.MaxValue, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF);

        jar.AssertCantParse();
        jar.AssertCantParse(1, 2, 3, 4, 5, 6, 7);
    }
}
