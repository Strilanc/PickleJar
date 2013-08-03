using System;
using System.Linq.Expressions;
using System.Linq;

namespace Strilanc.PickleJar.Internal {
    internal delegate Expression MemberGetter(Expression instance);
    internal delegate Expression MemberSetter(Expression instance, Expression newValue);
    internal static class ReflectionUtil {
        public static MemberGetter PickMatchingMemberGetterForType(this IJarForMember jarForMember, Type type) {
            if (jarForMember == null) throw new ArgumentNullException("jarForMember");
            if (type == null) throw new ArgumentNullException("type");

            var result = TryPickMatchingMemberGetterForTypeHelper(type, jarForMember);
            if (result == null || result(Expression.Default(type)).Type != jarForMember.MemberMatchInfo.MemberType) {
                throw new ArgumentException(string.Format(
                    "Failed to bind {0} against {1}. Make sure there's a field, property, or getter with a matching name and type.",
                    jarForMember.MemberMatchInfo,
                    type));
            }
            return result;
        }
        private static MemberGetter TryPickMatchingMemberGetterForTypeHelper(Type type, IJarForMember jarForMember) {
            var field = type.GetFields().SingleOrDefault(e => e.IsPublic && e.MatchInfo() == jarForMember.MemberMatchInfo);
            if (field != null) return e => Expression.MakeMemberAccess(e, field);

            var property = type.GetProperties().SingleOrDefault(e => e.GetGetMethod(nonPublic: false) != null && e.MatchInfo() == jarForMember.MemberMatchInfo);
            if (property != null) return e => Expression.MakeMemberAccess(e, property);

            var method = type.GetMethods().SingleOrDefault(e =>
                e.IsPublic
                && !e.GetParameters().Any()
                && e.GetterMatchInfo() == jarForMember.MemberMatchInfo);
            if (method != null) return e => Expression.Call(e, method);

            return null;
        }
    }
}
