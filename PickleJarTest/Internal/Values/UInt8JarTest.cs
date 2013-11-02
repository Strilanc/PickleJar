using Microsoft.VisualStudio.TestTools.UnitTesting;
using Strilanc.PickleJar;

[TestClass]
public class UInt8JarTest {
    [TestMethod]
    public void TestByte() {
        var jar = Jar.UInt8;
        jar.AssertPicklesNoMoreNoLess((byte)0, 0);
        jar.AssertPicklesNoMoreNoLess((byte)1, 1);
        jar.AssertPicklesNoMoreNoLess((byte)0x12, 0x12);
        jar.AssertPicklesNoMoreNoLess(byte.MaxValue, 0xFF);

        jar.AssertCantParse();
    }
}
