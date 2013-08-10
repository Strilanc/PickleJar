using Microsoft.VisualStudio.TestTools.UnitTesting;
using Strilanc.PickleJar.Internal;
using Strilanc.PickleJar.Internal.Values;

[TestClass]
public class Float64JarTest {
    [TestMethod]
    public void TestLittleEndian() {
        var jar = new Float64Jar(Endianess.LittleEndian);

        jar.AssertPicklesNoMoreNoLess(+0.0, 0, 0, 0, 0, 0, 0, 0, 0);
        jar.AssertPicklesNoMoreNoLess(-0.0, 0, 0, 0, 0, 0, 0, 0, 0x80);
        jar.AssertPicklesNoMoreNoLess(+1.0, 0, 0, 0, 0, 0, 0, 240, 0x3F);
        jar.AssertPicklesNoMoreNoLess(-1.0, 0, 0, 0, 0, 0, 0, 240, 0xBF);
        jar.AssertPicklesNoMoreNoLess(0.1, 154, 153, 153, 153, 153, 153, 185, 63);
        jar.AssertPicklesNoMoreNoLess(double.PositiveInfinity, 0, 0, 0, 0, 0, 0, 240, 0x7F);
        jar.AssertPicklesNoMoreNoLess(double.NegativeInfinity, 0, 0, 0, 0, 0, 0, 240, 0xFF);
        jar.AssertPicklesNoMoreNoLess(double.NaN, 0, 0, 0, 0, 0, 0, 248, 0xFF);

        jar.AssertCantParse();
        jar.AssertCantParse(1, 2, 3, 4, 5, 6, 7);
    }
    [TestMethod]
    public void TestBigEndian() {
        var jar = new Float64Jar(Endianess.BigEndian);

        jar.AssertPicklesNoMoreNoLess(+0.0, 0, 0, 0, 0, 0, 0, 0, 0);
        jar.AssertPicklesNoMoreNoLess(-0.0, 0x80, 0, 0, 0, 0, 0, 0, 0);
        jar.AssertPicklesNoMoreNoLess(+1.0, 0x3F, 240, 0, 0, 0, 0, 0, 0);
        jar.AssertPicklesNoMoreNoLess(-1.0, 0xBF, 240, 0, 0, 0, 0, 0, 0);
        jar.AssertPicklesNoMoreNoLess(0.1, 63, 185, 153, 153, 153, 153, 153, 154);
        jar.AssertPicklesNoMoreNoLess(double.PositiveInfinity, 0x7F, 240, 0, 0, 0, 0, 0, 0);
        jar.AssertPicklesNoMoreNoLess(double.NegativeInfinity, 0xFF, 240, 0, 0, 0, 0, 0, 0);
        jar.AssertPicklesNoMoreNoLess(double.NaN, 0xFF, 248, 0, 0, 0, 0, 0, 0);

        jar.AssertCantParse();
        jar.AssertCantParse(1, 2, 3, 4, 5, 6, 7);
    }
}
