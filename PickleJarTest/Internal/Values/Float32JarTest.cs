using Microsoft.VisualStudio.TestTools.UnitTesting;
using Strilanc.PickleJar.Internal.Values;

[TestClass]
public class Float32JarTest {
    [TestMethod]
    public void TestLittleEndian() {
        var jar = new Float32Jar();

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
}
