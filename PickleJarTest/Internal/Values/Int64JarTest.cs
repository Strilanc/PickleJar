using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Strilanc.PickleJar.Internal;
using Strilanc.PickleJar.Internal.Values;

[TestClass]
public class Int64JarTest {
    [TestMethod]
    public void TestBigEndian() {
        var jar = new Int64Jar(Endianess.BigEndian);
        jar.AssertPicklesNoMoreNoLess(0, 0, 0, 0, 0, 0, 0, 0, 0);
        jar.AssertPicklesNoMoreNoLess(1, 0, 0, 0, 0, 0, 0, 0, 1);
        jar.AssertPicklesNoMoreNoLess(-1, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF);
        jar.AssertPicklesNoMoreNoLess(0x123456789ABCDEF0, 0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0);
        jar.AssertPicklesNoMoreNoLess(Int64.MaxValue, 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF);
        jar.AssertPicklesNoMoreNoLess(Int64.MinValue, 0x80, 0, 0, 0, 0, 0, 0, 0);

        jar.AssertCantParse();
        jar.AssertCantParse(1, 2, 3, 4, 5, 6, 7);
    }
    [TestMethod]
    public void TestLittleEndian() {
        var jar = new Int64Jar(Endianess.LittleEndian);
        jar.AssertPicklesNoMoreNoLess(0, 0, 0, 0, 0, 0, 0, 0, 0);
        jar.AssertPicklesNoMoreNoLess(1, 1, 0, 0, 0, 0, 0, 0, 0);
        jar.AssertPicklesNoMoreNoLess(-1, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF);
        jar.AssertPicklesNoMoreNoLess(0x123456789ABCDEF0, 0xF0, 0xDE, 0xBC, 0x9A, 0x78, 0x56, 0x34, 0x12);
        jar.AssertPicklesNoMoreNoLess(Int64.MaxValue, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F);
        jar.AssertPicklesNoMoreNoLess(Int64.MinValue, 0, 0, 0, 0, 0, 0, 0, 0x80);

        jar.AssertCantParse();
        jar.AssertCantParse(1, 2, 3, 4, 5, 6, 7);
    }
}
