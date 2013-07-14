using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Strilanc.Parsing;
using Strilanc.Parsing.Misc;
using Strilanc.Parsing.NumberParsers;
using Strilanc.Parsing.StructuredParsers;

[TestClass]
public class ExpressionTreeParserTest {
    public class TestValidClass {
        public Int16 MutableField;
        public readonly Int32 ReadOnlyField;
        private readonly Int32 _privateReadOnlyField;
        public Int32 GetPrivateReadOnlyField() {
            return _privateReadOnlyField;
        }
        public byte MutableProperty { get; set; }
        public TestValidClass(Int32 readOnlyField, Int32 privateReadOnlyField) {
            this.ReadOnlyField = readOnlyField;
            this._privateReadOnlyField = privateReadOnlyField;
        }
    }
    public class TestUnsureClass1 {
        private readonly Int32 _inaccessibleValue;
    }

    [TestMethod]
    public void TestBadParsers() {
        // need ability to set all readonlys, fail safe if can't
        TestingUtilities.AssertThrows(() => new ExpressionTreeParser<TestUnsureClass1>(new[] {
            new Int16Parser(Endianess.LittleEndian).ForField("InaccessibleValue")
        }));
        TestingUtilities.AssertThrows(() => new ExpressionTreeParser<TestUnsureClass1>(new IFieldParserOfUnknownType[0]));

        // types must match
        TestingUtilities.AssertThrows(() => new ExpressionTreeParser<TestValidClass>(new[] {
            new Int16Parser(Endianess.LittleEndian).ForField("MutableField"),
            new Int32Parser(Endianess.LittleEndian).ForField("ReadOnlyField"),
            new Int32Parser(Endianess.LittleEndian).ForField("PrivateReadOnlyField"),
            new UInt8Parser().ForField("MutableProperty") // <-- wrong type
        }));

        // readonly fields must be initialized
        TestingUtilities.AssertThrows(() => new ExpressionTreeParser<TestValidClass>(new[] {
            new Int16Parser(Endianess.LittleEndian).ForField("MutableField"),
            // missing ReadOnlyField
            new Int32Parser(Endianess.LittleEndian).ForField("PrivateReadOnlyField"),
            new Int8Parser().ForField("MutableProperty")
        }));

        // mutable fields are not required to be initialized
        TestingUtilities.AssertDoesNotThrow(() => new ExpressionTreeParser<TestValidClass>(new[] {
            // missing MutableField
            new Int32Parser(Endianess.LittleEndian).ForField("ReadOnlyField"),
            new Int32Parser(Endianess.LittleEndian).ForField("PrivateReadOnlyField")
            // missing MutableProperty
        }));
    }

    [TestMethod]
    public void TestReflectedParser() {
        var r = new ExpressionTreeParser<TestValidClass>(new[] {
            new UInt8Parser().ForField("MutableProperty"),
            new Int16Parser(Endianess.LittleEndian).ForField("MutableField"),
            new Int32Parser(Endianess.LittleEndian).ForField("ReadOnlyField"),
            new Int32Parser(Endianess.LittleEndian).ForField("PrivateReadOnlyField")
        });

        TestingUtilities.AssertThrows(() => r.Parse(new byte[] {0, 1, 2, 3, 4, 5, 6, 7, 8, 9}));
        var e = r.Parse(new byte[] {0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 0xFF});
        e.Consumed.AssertEquals(11);
        e.Value.MutableProperty.AssertEquals((byte)0x00);
        e.Value.MutableField.AssertEquals((short)0x0201);
        e.Value.ReadOnlyField.AssertEquals(0x06050403);
        e.Value.GetPrivateReadOnlyField().AssertEquals(0x0A090807);
    }

    public struct TestNoConstructorStruct {
        public int X;
        public int Y;
    }
    [TestMethod]
    public void TestNoConstructor() {
        var r = new ExpressionTreeParser<TestNoConstructorStruct>(new[] {
            Parse.Int32LittleEndian.ForField("X"),
            Parse.Int32LittleEndian.ForField("Y")
        });
        var e = r.Parse(new byte[] {1, 0, 0, 0, 2, 0, 0, 0, 0xFF});
        e.Consumed.AssertEquals(8);
        e.Value.AssertEquals(new TestNoConstructorStruct {X = 1, Y = 2});
    }
}
