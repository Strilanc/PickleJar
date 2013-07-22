using Strilanc.PickleJar;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class CanonicalNameTest {
    [TestMethod]
    public void TestCanonize() {
        var c = CanonicalMemberName.Canonize("name");
        CanonicalMemberName.Canonize("Name").AssertEquals(c);
        CanonicalMemberName.Canonize("_name").AssertEquals(c);
        CanonicalMemberName.Canonize("_Name").AssertEquals(c);
        CanonicalMemberName.Canonize("get_name").AssertEquals(c);
        CanonicalMemberName.Canonize("_get_name").AssertEquals(c);
        CanonicalMemberName.Canonize("getName").AssertEquals(c);
        CanonicalMemberName.Canonize("setName").AssertEquals(c);
        CanonicalMemberName.Canonize("setName").AssertEquals(c);

        CanonicalMemberName.Canonize("NameStyle").AssertNotEqualTo(c);
        CanonicalMemberName.Canonize("FirstName").AssertNotEqualTo(c);
        CanonicalMemberName.Canonize("NameGet").AssertNotEqualTo(c);
        CanonicalMemberName.Canonize("Namer").AssertNotEqualTo(c);
        CanonicalMemberName.Canonize("NameName").AssertNotEqualTo(c);
    }
}
