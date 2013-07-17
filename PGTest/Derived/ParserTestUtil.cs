using System;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Strilanc.Parsing;
using Strilanc.Parsing.Internal;

[TestClass]
public class ParserTestUtil {
    [TestMethod]
    public void TestAllPublicParsersAndCombinatorsAreValid() {
        var api = typeof(Parse);
        var parsers = api.GetProperties(BindingFlags.Static | BindingFlags.Public).Select(e => e.GetValue(null)).ToArray();

        var combinators = 
            api.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .Select(e => !e.IsGenericMethodDefinition ? e : e.MakeGenericMethod(e.GetGenericArguments().Select(_ => typeof(int)).ToArray()))
            .ToArray();
        Func<Type, object[]> getParams = t => {
            if (t == typeof(int)) return new object[] {int.MinValue, -1, 0, 1, 2, int.MaxValue};
            if (t == typeof(IParser<int>)) return new object[] { Parse.Int32LittleEndian, Parse.Int32BigEndian };
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Func<,>)) return new object[] { new Func<int, int>(e1 => e1 + 1) };
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Func<,,>)) return new object[] { new Func<int, int, int>((e1, e2) => e1 + e2 + 1) };
            var p = parsers.Where(e => e.GetType().IsInstanceOfType(t)).ToArray();
            if (p.Length > 0) return p;
            throw new Exception(t.ToString());
        };
        var derivedParsers = (from combinator in combinators
                              from args in combinator.GetParameters()
                                           .Select(e => getParams(e.ParameterType))
                                           .ToArray()
                                           .AllChoiceCombinationsVolatile()
                              let e = TestingUtilities.InvokeWithDefaultOnFail(() => combinator.Invoke(null, args))
                              where e != null
                              select e).ToArray();
        (derivedParsers.Length > 0).AssertTrue();

        foreach (var parser in parsers.Concat(derivedParsers)) {
            AssertIsValidIParser((dynamic)parser);
        }
    }

    public static void AssertIsValidIParser<T>(IParser<T> parser) {
        var data = Enumerable.Range(0, 8*3*5*7).Select(e => (byte)e).ToArray();
        var full = new ArraySegment<byte>(data);
        var v1 = AssertParsesCorrectlyIfParses(parser, full);
        var v2 = AssertParsesCorrectlyIfParses(parser, full.Skip(data.Length / 2));
        var v3 = AssertParsesCorrectlyIfParses(parser, full.Skip(data.Length));
        var v4 = AssertParsesCorrectlyIfParses(parser, new ArraySegment<byte>(data, 0, 0));
        var v5 = AssertParsesCorrectlyIfParses(parser, new ArraySegment<byte>(new byte[0]));
            
        var internalParser = parser as IParserInternal<T>;
        if (internalParser != null) {
            var len = internalParser.OptionalConstantSerializedLength;
            if (len.HasValue) {
                if (len.Value > 0) {
                    TestingUtilities.AssertThrows(() => parser.Parse(new ArraySegment<byte>(data, 0, len.Value - 1)));
                }
                foreach (var b in new[]{v1,v2,v3,v4,v5}.Where(b => b != null)) {
                    b.Value.Consumed.AssertEquals(len.Value);
                }
            }
        }
    }
    private static ParsedValue<T>? AssertParsesCorrectlyIfParses<T>(IParser<T> parser, ArraySegment<byte> data) {
        ParsedValue<T> v;
        try {
            v = parser.Parse(data);
        } catch (Exception) {
            return null;
        }
        (v.Consumed <= data.Count).AssertTrue();

        // only consumed data matters
        var v2 = parser.Parse(new ArraySegment<byte>(data.Array, data.Offset, v.Consumed));
        v.Consumed.AssertEquals(v2.Consumed);
        v.Value.AssertSimilar(v2.Value);

        return v;
    }
}
