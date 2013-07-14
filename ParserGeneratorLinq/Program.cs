using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Strilanc.Parsing;
using Strilanc.Parsing.Misc;
using Strilanc.Parsing.StructuredParsers;

public class Program {
    public struct Pointy2 {
        public Pointy P1;
        public Pointy P2;
    }

    [StructLayoutAttribute(LayoutKind.Sequential, Pack = 1)]
    public struct Pointy {
        public readonly int X;
        public readonly int Y;
        public readonly Int16 M;
        public readonly Int16 N;
        public Pointy(int x, int y, Int16 m, Int16 n) {
            X = x;
            Y = y;
            M = m;
            N = n;
        }

    }
    private static Pointy[] ParseFrom(ArraySegment<byte> data) {
        var r = new Pointy[5000];
        var j = data.Offset;
        for (var i = 0; i < 5000; i += 4) {
            r[i] = new Pointy(
                BitConverter.ToInt32(data.Array, j + 4),
                BitConverter.ToInt32(data.Array, j + 0),
                BitConverter.ToInt16(data.Array, j + 8),
                BitConverter.ToInt16(data.Array, j + 10));
            r[i+1] = new Pointy(
                BitConverter.ToInt32(data.Array, j + 16),
                BitConverter.ToInt32(data.Array, j + 12),
                BitConverter.ToInt16(data.Array, j + 20),
                BitConverter.ToInt16(data.Array, j + 22));
            r[i + 2] = new Pointy(
                BitConverter.ToInt32(data.Array, j + 28),
                BitConverter.ToInt32(data.Array, j + 24),
                BitConverter.ToInt16(data.Array, j + 32),
                BitConverter.ToInt16(data.Array, j + 34));
            r[i + 3] = new Pointy(
                BitConverter.ToInt32(data.Array, j + 40),
                BitConverter.ToInt32(data.Array, j + 36),
                BitConverter.ToInt16(data.Array, j + 44),
                BitConverter.ToInt16(data.Array, j + 46));
            j += 48;
        }
        return r;
    }
    static void Main() {
        var repeatBlitParser = 
            new ParseBuilder {
                {"y", Parse.Int32LittleEndian},
                {"x", Parse.Int32LittleEndian},
                {"m", Parse.Int16LittleEndian},
                {"n", Parse.Int16LittleEndian},
            }.BuildAsParserForType<Pointy>()
            .GreedyRepeat();

        var m = Enumerable.Repeat(new byte[] { 1, 0, 0, 0, 2, 0, 0, 0, 4, 0, 0, 0 }, 5000).SelectMany(e => e).ToArray();
        var m2 = new ArraySegment<byte>(m, 0, m.Length);

        for (var j = 0; j < 10; j++) {
            var s = new Stopwatch();
            s.Start();
            for (var i = 0; i < 20000; i++) {
                var y = ParseFrom(m2);
            }
            s.Stop();
            var t = s.Elapsed;
            s.Reset();
            s.Start();
            for (var i = 0; i < 20000; i++) {
                var z = repeatBlitParser.Parse(m2);
            }
            s.Stop();
            var t2 = s.Elapsed;
            var b = ParseFrom(m2).SequenceEqual(repeatBlitParser.Parse(m2).Value);
            Console.WriteLine(t.TotalSeconds);
            Console.WriteLine(t2.TotalSeconds);
            Console.WriteLine(b);
        }
        Console.ReadLine();
    }
    static string[] GetVarNames<T>(IParser<T> parser) {
        dynamic d = parser;
        if (parser.GetType().GetGenericTypeDefinition() == typeof (SelectManyParser<,,>)) {
            string lastName = d.Proj2.Parameters[1].Name;
            string firstName = d.Proj2.Parameters[0].Name;
            if (firstName.StartsWith("<>")) {
                return ((string[])GetVarNames(d.SubParser)).Concat(new[] {lastName}).ToArray();
            }
            return new[] {firstName, lastName};
        } else if (parser.GetType().GetGenericTypeDefinition() == typeof(SelectParser<,>)) {
            throw new NotImplementedException();
        } else {
            throw new NotImplementedException();
        }
    }
}
public struct CanonicalizingMemberName {
    private readonly string _name;
    private readonly string _canonicalName;
    public CanonicalizingMemberName(string name) {
        _name = name;
        _canonicalName = name.ToLowerInvariant().Replace("_", "");
    }

    public override bool Equals(object obj) {
        return obj is CanonicalizingMemberName
               && Equals(_canonicalName, ((CanonicalizingMemberName)obj)._canonicalName);
    }
    public static implicit operator CanonicalizingMemberName(string name) {
        return new CanonicalizingMemberName(name);
    }
    public override int GetHashCode() {
        return _canonicalName == null ? 0 : _canonicalName.GetHashCode();
    }
    public override string ToString() {
        return _name;
    }
}
public static class Util {
    public static Dictionary<K, V> KeyedBy<K, V>(this IEnumerable<V> sequence, Func<V, K> keySelector) {
        return sequence.ToDictionary(keySelector, e => e);
    }
    public static Dictionary<T, int> ToIndexMap<T>(this IEnumerable<T> sequence) {
        var i = 0;
        return sequence.ToDictionary(e => e, e => i++);
    }

    public static ParsedValue<T> Parse<T>(this IParser<T> parser, byte[] data) {
        return parser.Parse(new ArraySegment<byte>(data, 0, data.Length));
    }
    public static SelectParser<T, R> Select<T, R>(this IParser<T> parser, Expression<Func<T, R>> proj) {
        return new SelectParser<T, R>(parser, proj);
    }
    public static SelectManyParser<T, M, R> SelectMany<T, M, R>(this IParser<T> parser, Expression<Func<T, IParser<M>>> proj1, Expression<Func<T, M, R>> proj2) {
        return new SelectManyParser<T, M, R>(parser, proj1, proj2);
    }
    public static ArraySegment<T> Skip<T>(this ArraySegment<T> segment, int count) {
        return new ArraySegment<T>(segment.Array, segment.Offset + count, segment.Count - count);
    }
    public static bool IsSameOrSubsetOf<T>(this IEnumerable<T> sequence, IEnumerable<T> other) {
        var r = new HashSet<T>(other);
        return sequence.All(r.Contains);
    }
    public static bool HasSameSetOfItemsAs<T>(this IEnumerable<T> sequence, IEnumerable<T> other) {
        var r1 = new HashSet<T>(other);
        var r2 = new HashSet<T>(sequence);
        return r1.Count == r2.Count && r2.All(r1.Contains);
    }

    public static int FieldOffsetOf(this Type type, FieldInfo field) {
        return (int)Marshal.OffsetOf(type, field.Name);
    }
    public static bool IsBlittable<T>() {
        return IsBlittable(typeof(T));
    }
    public static bool IsBlittable(this Type type) {
        var blittablePrimitives = new[] {
            typeof(byte),
            typeof(sbyte),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong)
        };
        return blittablePrimitives.Contains(type)
            || (type.IsArray && type.GetElementType().IsValueType && type.GetElementType().IsBlittable())
            || type.GetFields().All(e => e.FieldType.IsBlittable());
    }

    public static CanonicalizingMemberName CanonicalName(this MemberInfo member) {
        return new CanonicalizingMemberName(member.Name);
    }
    public static CanonicalizingMemberName CanonicalName(this IFieldParserOfUnknownType parser) {
        return new CanonicalizingMemberName(parser.Name);
    }
    public static CanonicalizingMemberName CanonicalName(this ParameterInfo parameter) {
        return new CanonicalizingMemberName(parameter.Name);
    }
    public static IEnumerable<C> Stream<T, C>(this IEnumerable<T> sequence, C seed, Func<C, T, C> acc) {
        return sequence.Select(e => seed = acc(seed, e));
    }
    public static IEnumerable<Tuple<T, C>> StreamZip<T, C>(this IEnumerable<T> sequence, C seed, Func<C, T, C> acc) {
        return sequence.Stream(Tuple.Create(default(T), seed), (a,e) => Tuple.Create(e, acc(a.Item2, e)));
    }
    public static Expression Block(this IEnumerable<Expression> expressions) {
        var exp = expressions.ToArray();
        if (exp.Length == 0) return Expression.Empty();
        return Expression.Block(exp);
    }
    public static bool HasSameKeyValuesAs<K, V>(this IReadOnlyDictionary<K, V> dictionary, IReadOnlyDictionary<K, V> other) {
        return dictionary.Count == other.Count
               && dictionary.All(other.Contains);
    }
    public static IFieldParserOfUnknownType ForField<T>(this IParser<T> parser, string fieldName) {
        return new FieldParserOfUnknownType<T>(parser, fieldName);
    } 
}