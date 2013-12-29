using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Linq;
using Strilanc.PickleJar.Internal.Bulk;
using Strilanc.PickleJar.Internal.RuntimeSpecialization;
using Strilanc.PickleJar.Internal.Structured;
using Strilanc.PickleJar.Internal.Unsafe;

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

            return BlitBulkJar.TryMake(itemParser) ?? RuntimeSpecializedBulkJar.MakeBulkParser(itemParser);
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

        private static SpecializedParserParts MakeDefaultInlinedParserComponents(object parser, Type valueType, Expression array, Expression offset, Expression count) {
            var resultVar = Expression.Variable(typeof(ParsedValue<>).MakeGenericType(valueType), "parsed");
            var parse = Expression.Call(
                Expression.Constant(parser),
                typeof(IJar<>).MakeGenericType(valueType).GetMethod("Parse"),
                new Expression[] {
                    Expression.New(
                        typeof (ArraySegment<byte>).GetConstructor(new[] {typeof (byte[]), typeof (int), typeof (int)}).NotNull(),
                        new[] {array, offset, count})
                });

            return new SpecializedParserParts(
                parseDoer: resultVar.AssignTo(parse),
                valueGetter: resultVar.AccessMember(typeof(ParsedValue<>).MakeGenericType(valueType).GetField("Value")),
                consumedCountGetter: resultVar.AccessMember(typeof(ParsedValue<>).MakeGenericType(valueType).GetField("Consumed")),
                storage: new SpecializedParserStorageParts(new[] {resultVar}, new[] { resultVar }));
        }
        private static SpecializedPackerParts MakeDefaulPackerComponents(object jar, Type valueType, Expression value) {
            var capacityVar = Expression.Variable(typeof(byte[]), "packed");
            var computePack = jar.ConstExpr().CallInstanceMethod(
                "Pack",
                value);
            
            return new SpecializedPackerParts(
                capacityComputer: capacityVar.AssignTo(computePack.AccessMember("Length")),
                capacityGetter: capacityVar,
                capacityStorage: new[] { capacityVar },
                packDoer: (array, offset) => {
                    var copyMethod = typeof(Array).GetMethod("Copy", new[] { typeof(Array), typeof(Array), typeof(int) });
                    var varArray = Expression.Variable(typeof(byte[]), "result");
                    return Expression.Block(
                        new[] {varArray},
                        varArray.AssignTo(computePack),
                        Expression.Call(copyMethod, computePack, array, varArray.AccessMember("Length")));
                });
        }
        public static SpecializedPackerParts MakeSpecializedPacker<T>(this IJar<T> jar, Expression value) {
            var r = jar as IJarMetadataInternal;
            return (r == null ? null : r.TrySpecializePacker(value))
                   ?? MakeDefaulPackerComponents(jar, typeof(T), value);
        }
        public static SpecializedParserParts MakeInlinedParserComponents<T>(this IJar<T> jar, Expression array, Expression offset, Expression count) {
            var r = jar as IJarMetadataInternal;
            return (r == null ? null : r.TrySpecializeParser(array, offset, count))
                   ?? MakeDefaultInlinedParserComponents(jar, typeof(T), array, offset, count);
        }
        public static SpecializedParserParts MakeInlinedParserComponents(this JarMeta jar, Expression array, Expression offset, Expression count) {
            var r = jar.Jar as IJarMetadataInternal;
            return (r == null ? null : r.TrySpecializeParser(array, offset, count))
                   ?? MakeDefaultInlinedParserComponents(jar.Jar, jar.JarValueType, array, offset, count);
        }
        public static SpecializedPackerParts MakePackerParts(this JarMeta jar, Expression value) {
            var r = jar.Jar as IJarMetadataInternal;
            return (r == null ? null : r.TrySpecializePacker(value))
                   ?? MakeDefaulPackerComponents(jar.Jar, jar.JarValueType, value);
        }
        public static SpecializedPackerParts MakePackerParts(this IJarForMember jarForMember, Expression value) {
            var r = jarForMember.Jar as IJarMetadataInternal;
            return (r == null ? null : r.TrySpecializePacker(value))
                   ?? MakeDefaulPackerComponents(jarForMember.Jar, jarForMember.MemberMatchInfo.MemberType, value);
        }
        public static SpecializedParserParts MakeInlinedParserComponents(this IJarForMember jarForMember, Expression array, Expression offset, Expression count) {
            var r = jarForMember.Jar as IJarMetadataInternal;
            return (r == null ? null : r.TrySpecializeParser(array, offset, count))
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
        public static Expression Plus(this Expression expression, Expression other) {
            return Expression.Add(expression, other);
        }
        public static Expression Times(this Expression expression, Expression other) {
            return Expression.Multiply(expression, other);
        }
        public static Expression Times(this Expression expression, int other) {
            return expression.Times(other.ConstExpr());
        }
        public static Expression IsLessThan(this Expression expression, Expression other) {
            return Expression.LessThan(expression, other);
        }

        public static Expression Not(this Expression expression) {
            return Expression.Not(expression);
        }
        public static Expression IfThenDo(this Expression condition, Expression action) {
            return Expression.IfThen(condition, action);
        }
        public static Expression Plus(this Expression expression, int offset) {
            if (offset == 0) return expression;
            return expression.Plus(offset.ConstExpr());
        }
        public static Expression PlusEqual(this Expression expression, Expression other) {
            return Expression.AddAssign(expression, other);
        }
        public static Expression CallInstanceMethod(this Expression expression, MethodInfo method, params Expression[] arguments) {
            return Expression.Call(expression, method, arguments);
        }
        public static Expression CallInstanceMethod(this Expression expression, string unambiguousMethodName, params Expression[] arguments) {
            return expression.CallInstanceMethod(expression.Type.GetMethod(unambiguousMethodName), arguments);
        }
        public static Expression ForEach(this Expression collection, Func<Expression, Expression> currentValueToBody) {
            if (collection == null) throw new ArgumentNullException("collection");
            if (currentValueToBody == null) throw new ArgumentNullException("currentValueToBody");

            var itemType = collection.Type
                                     .GetInterfaces()
                                     .Single(e => e.IsGenericType && e.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                                     .GetGenericArguments()
                                     .Single();
            var enumeratorType = typeof(IEnumerator<>).MakeGenericType(itemType);
            var breakTarget = Expression.Label("forEachBreak");
            var varEnumerator = Expression.Variable(enumeratorType, "forEachEnumerator");
            var disposeMethod = typeof(IDisposable).GetMethod("Dispose");
            var moveNextMethod = typeof(IEnumerator).GetMethod("MoveNext");
            var currentProperty = enumeratorType.GetProperty("Current");
            return Expression.Block(
                new[] {varEnumerator},
                Expression.TryFinally(
                    Expression.Loop(
                        Expression.IfThenElse(varEnumerator.CallInstanceMethod(moveNextMethod),
                                              currentValueToBody(varEnumerator.AccessMember(currentProperty)),
                                              Expression.Break(breakTarget)),
                        breakTarget),
                    varEnumerator.CallInstanceMethod(disposeMethod)));
        }
        public static Expression AccessMember(this Expression expression, MemberInfo memberInfo) {
            return Expression.MakeMemberAccess(expression, memberInfo);
        }
        public static Expression AccessMember(this Expression expression, string unambiguousMemberName) {
            return Expression.MakeMemberAccess(expression, expression.Type.GetMember(unambiguousMemberName).Single());
        }
        public static Expression AssignTo(this Expression expression, Expression other) {
            return Expression.Assign(expression, other);
        }
        public static Expression AccessIndex(this Expression array, Expression index) {
            return Expression.ArrayAccess(array, index);
        }
        public static Expression AccessIndex(this Expression array, int index) {
            return array.AccessIndex(index.ConstExpr());
        }
        public static Expression ConstExpr<T>(this T value) {
            return Expression.Constant(value);
        }

        public static Expression FollowedBy(this Expression expression, Expression next) {
            return Expression.Block(expression, next);
        }
        public static Expression Block(this IEnumerable<Expression> expressions) {
            var exp = expressions.ToArray();
            if (exp.Length == 0) return Expression.Empty();
            if (exp.Length == 1) return exp.Single();
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
