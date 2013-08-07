using System;

namespace Strilanc.PickleJar.Internal.Structured {
    /// <summary>
    /// jarForMember names and exposes a generic Jar with known type as a non-generic Jar of unknown type.
    /// It is used as part of parsing types via reflection: each of the type's fields must be matched to a corresponding field Jar.
    /// </summary>
    internal sealed class JarForMember<T> : IJarForMember {
        public readonly IJar<T> Jar;
        public MemberMatchInfo MemberMatchInfo { get; private set; }

        public Type FieldType { get { return typeof(T); } }
        object IJarForMember.Jar { get { return Jar; } }

        public JarForMember(IJar<T> jar, MemberMatchInfo name) {
            if (jar == null) throw new ArgumentNullException("jar");
            if (!jar.CanBeFollowed) throw new ArgumentException("!jar.CanBeFollowed");
            Jar = jar;
            MemberMatchInfo = name;
        }

        public override string ToString() {
            return string.Format(
                "{0} for {1}",
                Jar,
                MemberMatchInfo);
        }
    }
}
