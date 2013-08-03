using Strilanc.PickleJar;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class CanonicalNameTest {
    [TestMethod]
    public void TestCanonize() {
        var c = MemberMatchInfo.Canonize("name");
        MemberMatchInfo.Canonize("Name").AssertEquals(c);
        MemberMatchInfo.Canonize("_name").AssertEquals(c);
        MemberMatchInfo.Canonize("_Name").AssertEquals(c);
        MemberMatchInfo.Canonize("get_name").AssertEquals(c);
        MemberMatchInfo.Canonize("_get_name").AssertEquals(c);
        MemberMatchInfo.Canonize("getName").AssertEquals(c);
        MemberMatchInfo.Canonize("setName").AssertEquals(c);
        MemberMatchInfo.Canonize("setName").AssertEquals(c);

        MemberMatchInfo.Canonize("NameStyle").AssertNotEqualTo(c);
        MemberMatchInfo.Canonize("FirstName").AssertNotEqualTo(c);
        MemberMatchInfo.Canonize("NameGet").AssertNotEqualTo(c);
        MemberMatchInfo.Canonize("Namer").AssertNotEqualTo(c);
        MemberMatchInfo.Canonize("NameName").AssertNotEqualTo(c);
    }
}
