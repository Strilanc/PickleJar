using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Strilanc.PickleJar;
using Strilanc.PickleJar.Internal;
using Strilanc.PickleJar.Internal.Numbers;
using Strilanc.PickleJar.Internal.Structured;

[TestClass]
public class BlittableParserTest {
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
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
        var r = TypeJarBlit<TestStruct>.TryMake(new List<IFieldJar> {
            Jar.Int16LittleEndian.ForField("v01"),
            Jar.Int32LittleEndian.ForField("v2345")
        });
        var y = r.Parse(new byte[] { 0, 1, 2, 3, 4, 5, 0xFF });
        y.Consumed.AssertEquals(6);
        y.Value.AssertEquals(new TestStruct(0x0100, 0x05040302));
    }
    [TestMethod]
    public void TestValueParser2() {
        var r = TypeJarBlit<TestStruct2>.TryMake(new List<IFieldJar> {
            Jar.Int16LittleEndian.ForField("v01"),
            Jar.Int32LittleEndian.ForField("v2345")
        });
        var y = r.Parse(new byte[] { 0, 1, 2, 3, 4, 5, 0xFF });
        y.Consumed.AssertEquals(6);
        y.Value.AssertEquals(new TestStruct2 { v01 = 0x0100, v2345 = 0x05040302 });
    }

    [TestMethod]
    public void TestNumberParsers() {
        new Int8Jar().Parse(new byte[] { 1, 0xFF }).AssertEquals(new ParsedValue<sbyte>(0x01, 1));
        new UInt8Jar().Parse(new byte[] { 1, 0xFF }).AssertEquals(new ParsedValue<byte>(0x01, 1));

        new Int16Jar(Endianess.LittleEndian).Parse(new byte[] { 1, 2, 0xFF }).AssertEquals(new ParsedValue<short>(0x0201, 2));
        new Int16Jar(Endianess.BigEndian).Parse(new byte[] { 1, 2, 0xFF }).AssertEquals(new ParsedValue<short>(0x0102, 2));

        new UInt16Jar(Endianess.LittleEndian).Parse(new byte[] { 1, 2, 0xFF }).AssertEquals(new ParsedValue<ushort>(0x0201, 2));
        new UInt16Jar(Endianess.BigEndian).Parse(new byte[] { 1, 2, 0xFF }).AssertEquals(new ParsedValue<ushort>(0x0102, 2));

        new Int32Jar(Endianess.LittleEndian).Parse(new byte[] { 1, 2, 3, 4, 0xFF }).AssertEquals(new ParsedValue<int>(0x04030201, 4));
        new Int32Jar(Endianess.BigEndian).Parse(new byte[] { 1, 2, 3, 4, 0xFF }).AssertEquals(new ParsedValue<int>(0x01020304, 4));

        new UInt32Jar(Endianess.LittleEndian).Parse(new byte[] { 1, 2, 3, 4, 0xFF }).AssertEquals(new ParsedValue<uint>(0x04030201, 4));
        new UInt32Jar(Endianess.BigEndian).Parse(new byte[] { 1, 2, 3, 4, 0xFF }).AssertEquals(new ParsedValue<uint>(0x01020304, 4));

        new Int64Jar(Endianess.LittleEndian).Parse(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 0xFF }).AssertEquals(new ParsedValue<long>(0x0807060504030201, 8));
        new Int64Jar(Endianess.BigEndian).Parse(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 0xFF }).AssertEquals(new ParsedValue<long>(0x0102030405060708, 8));

        new UInt64Jar(Endianess.LittleEndian).Parse(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 0xFF }).AssertEquals(new ParsedValue<ulong>(0x0807060504030201, 8));
        new UInt64Jar(Endianess.BigEndian).Parse(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 0xFF }).AssertEquals(new ParsedValue<ulong>(0x0102030405060708, 8));
    }

    [TestMethod]
    public void TestNumberParserExpressions() {
        TestNumberParserExpression(Jar.Int16LittleEndian);
        TestNumberParserExpression(Jar.Int16BigEndian);
        TestNumberParserExpression(Jar.Int32LittleEndian);
        TestNumberParserExpression(Jar.Int32BigEndian);
        TestNumberParserExpression(Jar.Int64LittleEndian);
        TestNumberParserExpression(Jar.Int64BigEndian);
        TestNumberParserExpression(Jar.UInt16LittleEndian);
        TestNumberParserExpression(Jar.UInt16BigEndian);
        TestNumberParserExpression(Jar.UInt32LittleEndian);
        TestNumberParserExpression(Jar.UInt32BigEndian);
        TestNumberParserExpression(Jar.UInt64LittleEndian);
        TestNumberParserExpression(Jar.UInt64BigEndian);
        TestNumberParserExpression(Jar.UInt8);
        TestNumberParserExpression(Jar.Int8);
    }
    private static void TestNumberParserExpression<T>(IJar<T> exposedParser) {
        var parser = (IJarInternal<T>)exposedParser;
        var array = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 0xFF };
        var d = new ArraySegment<byte>(array, 0, array.Length);
        var inlinedParseComponents = parser.TryMakeInlinedParserComponents(Expression.Constant(array), Expression.Constant(0), Expression.Constant(array.Length));

        var body = Expression.Block(
            inlinedParseComponents.ResultStorage,
            inlinedParseComponents.PerformParse,
            Expression.New(
                typeof(ParsedValue<T>).GetConstructor(new[] { typeof(T), typeof(int) }).NotNull(), 
                inlinedParseComponents.AfterParseValueGetter, 
                inlinedParseComponents.AfterParseConsumedGetter));
        var method = Expression.Lambda<Func<ParsedValue<T>>>(body);
        var result = method.Compile()();
        var normal = parser.Parse(d);
        result.AssertEquals(normal);
    }
}
