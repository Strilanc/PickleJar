using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using Strilanc.Parsing;
using Strilanc.Parsing.Internal.StructuredParsers;

public static class Util {
    public static Dictionary<TKey, TValue> KeyedBy<TKey, TValue>(this IEnumerable<TValue> sequence, Func<TValue, TKey> keySelector) {
        return sequence.ToDictionary(keySelector, e => e);
    }
    public static Dictionary<T, int> ToIndexMap<T>(this IEnumerable<T> sequence) {
        var i = 0;
        return sequence.ToDictionary(e => e, e => i++);
    }

    public static ParsedValue<T> Parse<T>(this IParser<T> parser, byte[] data) {
        return parser.Parse(new ArraySegment<byte>(data, 0, data.Length));
    }
    public static T NotNull<T>(this T value) where T : class {
        if (value == null) throw new NullReferenceException();
        return value;
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
    public static CanonicalizingMemberName CanonicalName(this ParameterInfo parameter) {
        return new CanonicalizingMemberName(parameter.Name);
    }
    public static IEnumerable<TOut> Stream<TIn, TOut>(this IEnumerable<TIn> sequence, TOut seed, Func<TOut, TIn, TOut> acc) {
        return sequence.Select(e => seed = acc(seed, e));
    }
    public static IEnumerable<Tuple<TIn, TStream>> StreamZip<TIn, TStream>(this IEnumerable<TIn> sequence, TStream seed, Func<TStream, TIn, TStream> acc) {
        return sequence.Stream(Tuple.Create(default(TIn), seed), (a,e) => Tuple.Create(e, acc(a.Item2, e)));
    }
    public static Expression Block(this IEnumerable<Expression> expressions) {
        var exp = expressions.ToArray();
        if (exp.Length == 0) return Expression.Empty();
        return Expression.Block(exp);
    }
    public static bool HasSameKeyValuesAs<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, IReadOnlyDictionary<TKey, TValue> other) {
        return dictionary.Count == other.Count
               && dictionary.All(other.Contains);
    }
    public static IFieldParser ForField<T>(this IParser<T> parser, string fieldName) {
        return new FieldParser<T>(parser, fieldName);
    } 
}