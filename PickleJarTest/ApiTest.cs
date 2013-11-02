using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Strilanc.PickleJar;
using Strilanc.PickleJar.Internal;
using Strilanc.PickleJar.Internal.Values;

[TestClass]
public class ApiTest {
    [TestMethod]
    public void TestApiHasOnlyValidJars() {
        return;
        foreach (dynamic jar in JarsExposedByPublicApi()) {
            AssertJarIsValid(jar);
        }
    }

    private static IEnumerable<MethodInfo> FillInGenericParameters(MethodInfo method) {
        if (!method.IsGenericMethodDefinition) {
            yield return method;
            yield break;
        }

        var allIntTypeArgs = method.GetGenericArguments().Select(_ => typeof(int)).ToArray();
        yield return method.MakeGenericMethod(allIntTypeArgs);

        var allStringTypeArgs = method.GetGenericArguments().Select(_ => typeof(string)).ToArray();
        yield return method.MakeGenericMethod(allStringTypeArgs);
    }

    private static readonly IEnumerable<object> ApiJarGetters = 
        typeof(Jar) // <-- class with primitive jars and jar factory methods
        .GetProperties(BindingFlags.Static | BindingFlags.Public)
        .Select(e => e.GetValue(null))
        .ToArray();
    private static readonly IEnumerable<MethodInfo> ApiJarMakers = 
        typeof(Jar)
        .GetMethods(BindingFlags.Static | BindingFlags.Public)
        .SelectMany(FillInGenericParameters)
        .ToArray();

    private static IEnumerable<object> ChooseTestValues(Type type) {
        if (type == typeof(bool)) return new object[] { true, false };
        if (type == typeof(sbyte)) return new object[] { (sbyte)-100, (sbyte)-1, (sbyte)0, (sbyte)1, (sbyte)2, (sbyte)100 };
        if (type == typeof(short)) return new object[] { (short)-100, (short)-1, (short)0, (short)1, (short)2, (short)100 };
        if (type == typeof(int)) return new object[] { -100, -1, 0, 1, 2, 100 };
        if (type == typeof(long)) return new object[] { -100L, -1L, 0L, 1L, 2L, 100L };

        if (type == typeof(float)) return new object[] { -100f, -1f, 0f, 1f, 2f, 100f };
        if (type == typeof(double)) return new object[] { -100.0, -1.0, 0.0, 1.0, 2.0, 100.0 };

        if (type == typeof(byte)) return new object[] { (byte)0, (byte)1, (byte)2, (byte)100 };
        if (type == typeof(ushort)) return new object[] { (ushort)0, (ushort)1, (ushort)2, (ushort)100 };
        if (type == typeof(uint)) return new object[] { 0u, 1u, 2u, 100u };
        if (type == typeof(ulong)) return new object[] { 0ul, 1ul, 2ul, 100ul };

        if (type == typeof(IReadOnlyList<int>)) return new object[] { new int[0], new[] { -1 }, new[] { 2, 3, 5, 7 } };
        if (type == typeof(IReadOnlyList<string>)) return new object[] { new string[0], new[] { "" }, new[] { "a", "bra", "ca", "da" } };
        if (type == typeof(Func<int, bool>)) return new object[] { new Func<int, bool>(e1 => e1 % 2 == 0) };
        if (type == typeof(Func<string, bool>)) return new object[] { new Func<string, bool>(e1 => e1.Length % 2 == 0) };

        if (type == typeof(string)) return new[] { "a", "bra", "ca", "da" };

        // note: this function must currently be its own inverse, else Select's packer projection won't undo its parser projection
        if (type == typeof(Func<int, int>)) return new object[] { new Func<int, int>(e1 => e1 * -1) };
        if (type == typeof(Func<string, string>)) return new object[] { new Func<string, string>(e1 => new string(e1.Reverse().ToArray())) };
        if (type == typeof(IJar<IReadOnlyList<int>>)) return new object[] { Jar.Int32LittleEndian.RepeatNTimes(2) };
        if (type == typeof(Encoding)) return new object[] {Encoding.UTF32};
        if (type == typeof(IEnumerable<IJarForMember>)) return new object[] { null };

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof (Tuple<,>)) {
            return from v1 in ChooseTestValues(type.GetGenericArguments()[0])
                   from v2 in ChooseTestValues(type.GetGenericArguments()[1])
                   select type.GetConstructor(type.GetGenericArguments()).NotNull().Invoke(new[] {v1, v2});
        }

        var matchingJar = ApiJarGetters.Where(type.IsInstanceOfType).ToArray();
        if (matchingJar.Length > 0) return matchingJar;

        return new object[0];
        // todo: decide whether or not to bother with all these specialized types
        //if (type == typeof(Func<ArraySegment<byte>, ParsedValue<int>>)) {
        //    return new object[] { new Func<ArraySegment<byte>, ParsedValue<int>>(new Int32Jar(Endianess.BigEndian).Parse) };
        //}
        //if (type == typeof(Func<int, byte[]>)) {
        //    return new object[] { new Func<int, byte[]>(new Int32Jar(Endianess.BigEndian).Pack) };
        //}
        //if (type == typeof(Func<ArraySegment<byte>, ParsedValue<string>>)) {
        //    return new object[] { new Func<ArraySegment<byte>, ParsedValue<string>>(new TextJar(Encoding.UTF8).NullTerminated().Parse) };
        //}
        //if (type == typeof(Func<string, byte[]>)) {
        //    return new object[] { new Func<string, byte[]>(new TextJar(Encoding.UTF8).NullTerminated().Pack) };
        //}
        //throw new Exception(type.ToString());
    }
    private static IEnumerable<object> JarsExposedByPublicApi() {
        var derivedJars = (from jarMaker in ApiJarMakers
                           from args in jarMaker.GetParameters()
                                                .Select(e => ChooseTestValues(e.ParameterType))
                                                .AllChoiceCombinationsVolatile()
                           let e = TestingUtilities.InvokeWithDefaultOnFail(() => jarMaker.Invoke(null, args))
                           where e != null
                           select e
                          ).ToArray();
        (derivedJars.Length > 0).AssertTrue();

        return ApiJarGetters.Concat(derivedJars);
    }

    private static void AssertJarIsValid<T>(IJar<T> jar) {
        var data = Enumerable.Range(0, 8*3*5*7).Select(e => (byte)e).ToArray();
        var full = new ArraySegment<byte>(data);

        // ToString doesn't murder everyone?
        TestingUtilities.AssertDoesNotThrow(() => jar.ToString());

        // parsing appears to work correctly?
        var segments = new[] {
            full,
            full.Skip(data.Length/2),
            full.Skip(data.Length),
            new ArraySegment<byte>(data, 0, 0),
            new ArraySegment<byte>(new byte[0])
        };
        var vs = segments.Select(e => AssertParsesCorrectlyIfParses(jar, e)).ToArray();

        // round trips work?
        var packed = new List<byte[]>();
        foreach (var item in ChooseTestValues(typeof(T)).Cast<T>()) {
            byte[] itemData;
            try {
                itemData = jar.Pack(item);
                packed.Add(itemData);
            } catch (Exception) {
                continue;
            }
            var p = jar.Parse(new ArraySegment<byte>(itemData));
            itemData.Length.AssertEquals(p.Consumed);
            item.AssertSimilar(p.Value);
        }

        // metadata is good?
        var metadata = jar as IJarMetadataInternal;
        if (metadata != null) {
            // length matches, if specified?
            var len = metadata.OptionalConstantSerializedLength;
            if (len.HasValue) {
                if (len.Value > 0) {
                    TestingUtilities.AssertThrows(() => jar.Parse(new ArraySegment<byte>(data, 0, len.Value - 1)));
                }
                foreach (var b in vs.Where(b => !ReferenceEquals(b, null))) {
                    b.Value.Consumed.AssertEquals(len.Value);
                }
                foreach (var b in packed) {
                    b.Length.AssertEquals(len.Value);
                }
            }

            // inlined expression has same result?
            for (var i = 0; i < segments.Length; i++) {
                if (ReferenceEquals(null, vs[i])) continue;
                var inlined = metadata.TryMakeInlinedParserComponents(
                    Expression.Constant(segments[i].Array),
                    Expression.Constant(segments[i].Offset),
                    Expression.Constant(segments[i].Count));
                if (inlined == null) continue;

                var compiledValue = Expression.Lambda(
                    Expression.Block(
                        inlined.Storage.ForBoth,
                        inlined.ParseDoer,
                        inlined.ValueGetter)).Compile().DynamicInvoke();
                compiledValue.AssertSimilar(vs[i].Value.Value);

                var compiledConsumed = Expression.Lambda(
                    Expression.Block(
                        inlined.Storage.ForBoth,
                        inlined.ParseDoer,
                        inlined.ConsumedCountGetter)).Compile().DynamicInvoke();
                compiledConsumed.AssertSimilar(vs[i].Value.Consumed);
            }
        }
    }

    private static ParsedValue<T>? AssertParsesCorrectlyIfParses<T>(IJar<T> parser, ArraySegment<byte> data) {
        ParsedValue<T> v;
        try {
            v = parser.Parse(data);
        } catch {
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
