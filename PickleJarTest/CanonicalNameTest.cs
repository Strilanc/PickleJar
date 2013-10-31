using Strilanc.PickleJar;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class CanonicalNameTest {
    [TestMethod]
    public void TestCanonize() {
        var c = MemberMatchInfo.Canonicalize("Name");
        MemberMatchInfo.Canonicalize("Name").AssertEquals(c);
        MemberMatchInfo.Canonicalize("_name").AssertEquals(c);
        MemberMatchInfo.Canonicalize("_Name").AssertEquals(c);
        MemberMatchInfo.Canonicalize("get_name").AssertEquals(c);
        MemberMatchInfo.Canonicalize("_get_name").AssertEquals(c);
        MemberMatchInfo.Canonicalize("getName").AssertEquals(c);
        MemberMatchInfo.Canonicalize("setName").AssertEquals(c);
        MemberMatchInfo.Canonicalize("setName").AssertEquals(c);

        MemberMatchInfo.Canonicalize("NameStyle").AssertNotEqualTo(c);
        MemberMatchInfo.Canonicalize("FirstName").AssertNotEqualTo(c);
        MemberMatchInfo.Canonicalize("NameGet").AssertNotEqualTo(c);
        MemberMatchInfo.Canonicalize("Namer").AssertNotEqualTo(c);
        MemberMatchInfo.Canonicalize("NameName").AssertNotEqualTo(c);
    }
}
