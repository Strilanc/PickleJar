using System;

namespace Strilanc.PickleJar.Internal.Structured {
    /// <summary>
    /// MemberAndJar names and exposes a generic Jar with known type as a non-generic Jar of unknown type.
    /// It is used as part of parsing types via reflection: each of the type's fields must be matched to a corresponding field Jar.
    /// </summary>
    internal sealed class MemberAndJar<T> : IMemberAndJar {
        public readonly IJar<T> Jar;
        public CanonicalMemberName CanonicalName { get; private set; }

        public Type FieldType { get { return typeof(T); } }
        object IMemberAndJar.Jar { get { return Jar; } }

        public MemberAndJar(IJar<T> jar, CanonicalMemberName name) {
            if (jar == null) throw new ArgumentNullException("jar");
            Jar = jar;
            CanonicalName = name;
        }
    }
}
