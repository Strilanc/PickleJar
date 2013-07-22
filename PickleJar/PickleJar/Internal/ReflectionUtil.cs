using System;
using System.Linq.Expressions;
using System.Linq;

namespace Strilanc.PickleJar.Internal {
    internal delegate Expression MemberGetter(Expression instance);
    internal delegate Expression MemberSetter(Expression instance, Expression newValue);
    internal static class ReflectionUtil {
        public static MemberGetter PickMatchingMemberGetterForType(this IMemberAndJar memberAndJar, Type type) {
            var result = PickMatchingMemberGetterForTypeHelper(type, memberAndJar);
            if (result(Expression.Default(type)).Type != memberAndJar.FieldType) {
                throw new ArgumentException(string.Format(
                    "The public field, property getter, or get method from the type {0} matched against the jar named {1} returns a {2} but the jar works with {3}.",
                    type,
                    memberAndJar.CanonicalName,
                    result(Expression.Default(type)).Type,
                    memberAndJar.FieldType));
            }
            return result;
        }
        private static MemberGetter PickMatchingMemberGetterForTypeHelper(Type type, IMemberAndJar memberAndJar) {
            var field = type.GetFields().SingleOrDefault(e => e.IsPublic && e.CanonicalName() == memberAndJar.CanonicalName);
            if (field != null) return e => Expression.MakeMemberAccess(e, field);

            var property = type.GetProperties().SingleOrDefault(e => e.GetGetMethod(nonPublic: false) != null && e.CanonicalName() == memberAndJar.CanonicalName);
            if (property != null) return e => Expression.MakeMemberAccess(e, property);

            var method = type.GetMethods().SingleOrDefault(e => e.IsPublic && e.CanonicalName() == memberAndJar.CanonicalName && !e.GetParameters().Any());
            if (method != null) return e => Expression.Call(e, method);

            throw new ArgumentException(string.Format(
                "Failed to matched a jar named {0} against a public field, property getter, or get method with a related name from the type {1}.",
                memberAndJar.CanonicalName,
                type));
        }
    }
}
