using Microsoft.VisualStudio.TestTools.UnitTesting;
using Strilanc.PickleJar.Internal.Values;

[TestClass]
public class Int8JarTest {
    [TestMethod]
    public void TestByte() {
        var jar = new Int8Jar();
        jar.AssertPicklesNoMoreNoLess((sbyte)0, 0);
        jar.AssertPicklesNoMoreNoLess((sbyte)1, 1);
        jar.AssertPicklesNoMoreNoLess((sbyte)-1, 0xFF);
        jar.AssertPicklesNoMoreNoLess((sbyte)0x12, 0x12);
        jar.AssertPicklesNoMoreNoLess(sbyte.MaxValue, 0x7F);
        jar.AssertPicklesNoMoreNoLess(sbyte.MinValue, 0x80);

        jar.AssertCantParse();
    }
}
