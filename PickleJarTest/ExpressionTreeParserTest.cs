using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Strilanc.PickleJar;
using Strilanc.PickleJar.Internal;
using Strilanc.PickleJar.Internal.Values;
using Strilanc.PickleJar.Internal.Structured;

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

    public class ClassWithInternalState {
        private readonly Int32 _inaccessibleValue;
        public ClassWithInternalState() {
            _inaccessibleValue = 1;
        }
        public Int32 Get() {
            return _inaccessibleValue;
        }
    }

    [TestMethod]
    public void TestBadParsers() {
        // all parsers must be matched to some sort of setter
        TestingUtilities.AssertDoesNotThrow(() => new TypeJarCompiled<ClassWithInternalState>(new IJarForMember[0]));
        TestingUtilities.AssertThrows(() => new TypeJarCompiled<ClassWithInternalState>(new[] {
            new Int16Jar(Endianess.LittleEndian).ForMember("InaccessibleValue")
        }));

        // types must match
        TestingUtilities.AssertThrows(() => new TypeJarCompiled<TestValidClass>(new[] {
            new Int16Jar(Endianess.LittleEndian).ForMember("MutableField"),
            new Int32Jar(Endianess.LittleEndian).ForMember("ReadOnlyField"),
            new Int32Jar(Endianess.LittleEndian).ForMember("PrivateReadOnlyField"),
            new Int8Jar().ForMember("MutableProperty") // <-- wrong type
        }));

        // must have all values needed to invoke a constructor
        TestingUtilities.AssertThrows(() => new TypeJarCompiled<TestValidClass>(new[] {
            new Int16Jar(Endianess.LittleEndian).ForMember("MutableField"),
            // missing constructor parameter
            new Int32Jar(Endianess.LittleEndian).ForMember("PrivateReadOnlyField"),
            new Int8Jar().ForMember("MutableProperty")
        }));

        // mutable fields are not required to be initialized
        TestingUtilities.AssertDoesNotThrow(() => new TypeJarCompiled<TestValidClass>(new[] {
            new Int32Jar(Endianess.LittleEndian).ForMember("ReadOnlyField"),
            new Int32Jar(Endianess.LittleEndian).ForMember("PrivateReadOnlyField")
        }));
    }

    [TestMethod]
    public void TestReflectedParser() {
        var r = new TypeJarCompiled<TestValidClass>(new[] {
            new UInt8Jar().ForMember("MutableProperty"),
            new Int16Jar(Endianess.LittleEndian).ForMember("MutableField"),
            new Int32Jar(Endianess.LittleEndian).ForMember("ReadOnlyField"),
            new Int32Jar(Endianess.LittleEndian).ForMember("PrivateReadOnlyField")
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
        var r = new TypeJarCompiled<TestNoConstructorStruct>(new[] {
            Jar.Int32LittleEndian.ForMember("X"),
            Jar.Int32LittleEndian.ForMember("Y")
        });
        var e = r.Parse(new byte[] {1, 0, 0, 0, 2, 0, 0, 0, 0xFF});
        e.Consumed.AssertEquals(8);
        e.Value.AssertEquals(new TestNoConstructorStruct {X = 1, Y = 2});
    }
}
