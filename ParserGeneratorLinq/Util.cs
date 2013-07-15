using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using Strilanc.Parsing;
using Strilanc.Parsing.Internal.Misc;
using Strilanc.Parsing.Internal.StructuredParsers;

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