using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Strilanc.Parsing;
using Strilanc.Parsing.Internal.Misc;
using Strilanc.Parsing.Internal.NumberParsers;
using Strilanc.Parsing.Internal.RepetitionParsers;

[TestClass]
public class ExpressionArrayParserTest {
    public sealed class TestValidClass {
        public Int16 A;
        public Int16 B;
    }

    [TestMethod]
    public void TestArrayParse() {
        var r = new ExpressionArrayParser<TestValidClass>(
            new ParseBuilder {
                {"A", new Int16Parser(Endianess.BigEndian)},
                {"B", new Int16Parser(Endianess.LittleEndian)}
            }.BuildAsParserForType<TestValidClass>());
        var d = new ArraySegment<byte>(new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 0xFF}, 0, 9);
        var x0 = r.Parse(d, 0);
        x0.Consumed.AssertEquals(0);
        x0.Value.Length.AssertEquals(0);

        var x1 = r.Parse(d, 1);
        x1.Consumed.AssertEquals(4);
        x1.Value.Length.AssertEquals(1);
        x1.Value[0].A.AssertEquals((short)0x0102);
        x1.Value[0].B.AssertEquals((short)0x0403);

        var x2 = r.Parse(d, 2);
        x2.Consumed.AssertEquals(8);
        x2.Value.Length.AssertEquals(2);
        x2.Value[0].A.AssertEquals((short)0x0102);
        x2.Value[0].B.AssertEquals((short)0x0403);
        x2.Value[1].A.AssertEquals((short)0x0506);
        x2.Value[1].B.AssertEquals((short)0x0807);

        TestingUtilities.AssertThrows(() => r.Parse(d, 3));
    }

}
