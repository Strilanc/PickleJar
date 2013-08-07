using Microsoft.VisualStudio.TestTools.UnitTesting;
using Strilanc.PickleJar.Internal.Values;

[TestClass]
public class ConstantJarTest {
    [TestMethod]
    public void TestConstantJar() {
        var jar = new ConstantJar<string>("test");
        jar.AssertPicklesNoMoreNoLess("test", new byte[0]);
    }
}
