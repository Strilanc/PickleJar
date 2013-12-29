using Microsoft.VisualStudio.TestTools.UnitTesting;
using Strilanc.PickleJar.Internal.Basic;

[TestClass]
public class ConstantJarTest {
    [TestMethod]
    public void TestConstantJar() {
        var jar = ConstantJar.Create("test");
        jar.AssertPicklesNoMoreNoLess("test", new byte[0]);
    }
}
