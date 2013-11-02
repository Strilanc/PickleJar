using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Linq;
using Strilanc.PickleJar.Internal.Bulk;
using Strilanc.PickleJar.Internal.Structured;

namespace Strilanc.PickleJar.Internal {
    /// <summary>
    /// ParserUtil contains internal utility and convenience methods related to parsing and optimizing parsers.
    /// </summary>
    internal static class ParserUtil {
        /// <summary>
        /// Returns an appropriate Jar capable of bulk parsing operations, based on the given item Jar.
        /// </summary>
        public static IBulkJar<T> Bulk<T>(this IJar<T> itemParser) {
            if (itemParser == null) throw new ArgumentNullException("itemParser");

            return BulkJarBlit.TryMake(itemParser) ?? BulkJarCompiled.MakeBulkParser(itemParser);
        }
        public static bool IsBlittable<T>(this IJar<T> jar) {
            var data = jar as IJarMetadataInternal;
            return data != null && data.IsBlittable;
        }
        public static ParsedValue<T> AsParsed<T>(this T value, int consumed) {
            return new ParsedValue<T>(value, consumed);
        }
        public static ParsedValue<TOut> Select<TIn, TOut>(this ParsedValue<TIn> value, Func<TIn, TOut> projection) {
            return new ParsedValue<TOut>(projection(value.Value), value.Consumed);
        }

        public static bool CanBeFollowed(this IJarForMember jarForMember) {
            return (bool)typeof(IJar<>)
                .MakeGenericType(jarForMember.MemberMatchInfo.MemberType)
                .GetProperty("CanBeFollowed").GetGetMethod()
                .NotNull()
                .Invoke(jarForMember.Jar, new object[0]);
        }
        public static IJar<object> JarAsObjectJar(this Jar.NamedJarList.Entry namedJarEntry) {
            return (IJar<object>)typeof(ObjectJar<>)
                .MakeGenericType(namedJarEntry.JarValueType)
                .GetConstructor(new[] { typeof(IJar<>).MakeGenericType(namedJarEntry.JarValueType) })
                .NotNull()
                .Invoke(new[] { namedJarEntry.Jar });
        }
        public static IJarForMember ToJarForMember(this Jar.NamedJarList.Entry namedJarEntry) {
            return (IJarForMember)typeof(JarForMember<>)
                .MakeGenericType(namedJarEntry.JarValueType)
                .GetConstructor(new[] { typeof(IJar<>).MakeGenericType(namedJarEntry.JarValueType), typeof(MemberMatchInfo) })
                .NotNull()
                .Invoke(new[] { namedJarEntry.Jar, new MemberMatchInfo(namedJarEntry.Name, namedJarEntry.JarValueType) });
        }
        public static IJar<object> JarAsObjectJar(this IJarForMember jarForMember) {
            if (jarForMember == null) throw new ArgumentNullException("jarForMember");

            var jarType = jarForMember.MemberMatchInfo.MemberType;
            return (IJar<object>)typeof (ObjectJar<>)
                .MakeGenericType(jarType)
                .GetConstructor(new[] {typeof(IJar<>).MakeGenericType(jarType)})
                .NotNull()
                .Invoke(new[] {jarForMember.Jar});
        }

        public static bool AreMemoryAndSerializedRepresentationsOfValueGuaranteedToMatch<T>(this IJar<T> parser) {
            var r = parser as IJarMetadataInternal;
            return r != null && r.IsBlittable;
        }
        public static bool AreMemoryAndSerializedRepresentationsOfValueGuaranteedToMatch(this IJarForMember jar) {
            var r = jar.Jar as IJarMetadataInternal;
            return r != null && r.IsBlittable;
        }

        public static int? OptionalConstantSerializedLength<T>(this IJar<T> parser) {
            var r = parser as IJarMetadataInternal;
            return r == null ? null : r.OptionalConstantSerializedLength;
        }
        public static int? OptionalConstantSerializedLength(this IJarForMember jar) {
            var r = jar.Jar as IJarMetadataInternal;
            return r == null ? null : r.OptionalConstantSerializedLength;
        }

        private static InlinedParserComponents MakeDefaultInlinedParserComponents(object parser, Type valueType, Expression array, Expression offset, Expression count) {
            var resultVar = Expression.Variable(typeof(ParsedValue<>).MakeGenericType(valueType), "parsed");
            var parse = Expression.Call(
                Expression.Constant(parser),
                typeof(IJar<>).MakeGenericType(valueType).GetMethod("Parse"),
                new Expression[] {
                    Expression.New(
                        typeof (ArraySegment<byte>).GetConstructor(new[] {typeof (byte[]), typeof (int), typeof (int)}).NotNull(),
                        new[] {array, offset, count})
                });

            return new InlinedParserComponents(
                parseDoer: Expression.Assign(resultVar, parse),
                valueGetter: Expression.MakeMemberAccess(resultVar, typeof(ParsedValue<>).MakeGenericType(valueType).GetField("Value")),
                consumedCountGetter: Expression.MakeMemberAccess(resultVar, typeof(ParsedValue<>).MakeGenericType(valueType).GetField("Consumed")),
                storage: new ParsedValueStorage(new[] {resultVar}, new[] { resultVar }));
        }
        public static InlinedParserComponents MakeInlinedParserComponents<T>(this IJar<T> jar, Expression array, Expression offset, Expression count) {
            var r = jar as IJarMetadataInternal;
            return (r == null ? null : r.TryMakeInlinedParserComponents(array, offset, count))
                   ?? MakeDefaultInlinedParserComponents(jar, typeof(T), array, offset, count);
        }
        public static InlinedParserComponents MakeInlinedParserComponents(this JarMeta jar, Expression array, Expression offset, Expression count) {
            var r = jar.Jar as IJarMetadataInternal;
            return (r == null ? null : r.TryMakeInlinedParserComponents(array, offset, count))
                   ?? MakeDefaultInlinedParserComponents(jar.Jar, jar.JarValueType, array, offset, count);
        }
        public static InlinedParserComponents MakeInlinedParserComponents(this IJarForMember jarForMember, Expression array, Expression offset, Expression count) {
            var r = jarForMember.Jar as IJarMetadataInternal;
            return (r == null ? null : r.TryMakeInlinedParserComponents(array, offset, count))
                   ?? MakeDefaultInlinedParserComponents(jarForMember.Jar, jarForMember.MemberMatchInfo.MemberType, array, offset, count);
        }

        public static T NotNull<T>(this T value) where T : class {
            if (value == null) throw new NullReferenceException();
            return value;
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
                typeof(ulong),
                typeof(float),
                typeof(double)
            };
            if (type == typeof(string)) return false;
            return blittablePrimitives.Contains(type)
                   || (type.IsArray && type.GetElementType().IsValueType && type.GetElementType().IsBlittable())
                   || type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                          .All(e => e.FieldType != type && e.FieldType.IsValueType && e.FieldType.IsBlittable());
        }
        public static Expression Block(this IEnumerable<Expression> expressions) {
            var exp = expressions.ToArray();
            if (exp.Length == 0) return Expression.Empty();
            return Expression.Block(exp);
        }
        public static MemberMatchInfo MatchInfo(this PropertyInfo member) {
            return new MemberMatchInfo(member.Name, member.PropertyType);
        }
        public static MemberMatchInfo MatchInfo(this FieldInfo member) {
            return new MemberMatchInfo(member.Name, member.FieldType);
        }
        public static MemberMatchInfo MatchInfo(this ParameterInfo parameter) {
            return new MemberMatchInfo(parameter.Name, parameter.ParameterType);
        }
        public static MemberMatchInfo GetterMatchInfo(this MethodInfo method) {
            return new MemberMatchInfo(method.Name, method.ReturnType);
        }
        public static IJarForMember ForMember<T>(this IJar<T> parser, string memberNameMatcher) {
            return new JarForMember<T>(parser, new MemberMatchInfo(memberNameMatcher, typeof(T)));
        }
        public static Type GetMemberSettableType(this MemberInfo memberInfo) {
            var field = memberInfo as FieldInfo;
            if (field != null) return field.FieldType;

            var property = memberInfo as PropertyInfo;
            if (property != null) return property.PropertyType;

            var method = memberInfo as MethodInfo;
            if (method != null && method.GetParameters().Length == 1) return method.GetParameters().Single().ParameterType;

            throw new ArgumentException("Not a settable member.");
        }

    }
}
