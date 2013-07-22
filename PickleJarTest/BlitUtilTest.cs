using System;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Strilanc.PickleJar.Internal.Bulk;
using Strilanc.PickleJar.Internal.Structured;

[TestClass]
public class BlitUtilTest {
    [StructLayout(LayoutKind.Sequential, Pack=1)]
    public struct TestStruct {
        public readonly Int16 v01;
        public readonly Int32 v2345;
        public TestStruct(Int16 v01, Int32 v2345) {
            this.v01 = v01;
            this.v2345 = v2345;
        }
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TestStruct2 {
        public Int16 v01;
        public Int32 v2345;
    }

    [TestMethod]
    public void TestValueParser() {
        var r = TypeJarBlit<TestStruct>.MakeUnsafeBlitParser();
        var y = r(new byte[] { 0, 1, 2, 3, 4, 5 }, 0, 6);
        y.AssertEquals(new TestStruct(0x0100, 0x05040302));
    }
    [TestMethod]
    public void TestValueParser2() {
        var r = TypeJarBlit<TestStruct2>.MakeUnsafeBlitParser();
        var y = r(new byte[] { 0, 1, 2, 3, 4, 5 }, 0, 6);
        y.AssertEquals(new TestStruct2 {v01 = 0x0100, v2345 = 0x05040302});
    }

    [TestMethod]
    public void TestArrayParser() {
        var r = BulkJarBlit<TestStruct>.MakeUnsafeArrayBlitParser();
        var y = r(Enumerable.Range(0, 20).Select(e => (byte)e).ToArray(), 3, 0, 3*6);
        y.Length.AssertEquals(3);
        y[0].AssertEquals(new TestStruct(0x0100, 0x05040302));
        y[1].AssertEquals(new TestStruct(0x0706, 0x0B0A0908));
        y[2].AssertEquals(new TestStruct(0x0D0C, 0x11100F0E));
    }
}
