using System;
using Strilanc.PickleJar.Internal;
using System.Linq;

namespace Strilanc.PickleJar {
    /// <summary>
    /// Stores the information needed to find a member of a memberType.
    /// Used by Jar.BuildJarForType to pair jars against members of a memberType.
    /// </summary>
    public struct MemberMatchInfo : IEquatable<MemberMatchInfo> {
        private readonly string _rawRawMemberName;
        private readonly string _canonicalMemberName;
        private readonly Type _memberType;

        /// <summary>The type of the member to be matched.</summary>
        public Type MemberType { get { return _memberType; } }

        /// <summary>
        /// Constructs a new MemberMatchInfo.
        /// </summary>
        /// <param name="rawMemberName">
        /// The raw name of the member to be matched.
        /// Things like casing and leading underscores are ignored when canonicalizing this raw name for matching.
        /// </param>
        /// <param name="memberType">The type of the member to be matched.</param>
        public MemberMatchInfo(string rawMemberName, Type memberType) {
            if (rawMemberName == null) throw new ArgumentNullException("rawMemberName");
            if (memberType == null) throw new ArgumentNullException("memberType");
            _rawRawMemberName = rawMemberName;
            _canonicalMemberName = Canonicalize(rawMemberName);
            _memberType = memberType;
        }
        internal static string Canonicalize(string name) {
            var tokens = name
                .Split('_')
                .Where(e => e.Length > 0)
                .SelectMany(e => e.StartNewPartitionWhen(Char.IsUpper))
                .Select(e => new string(e.ToArray()))
                .Select(e => e.ToLowerInvariant())
                .ToArray();
            var trimmedPrefixes = new[] {"set", "get"};
            if (tokens.Length > 0 && trimmedPrefixes.Contains(tokens[0])) {
                tokens = tokens.Skip(1).ToArray();
            }
            return string.Join("", tokens);
        }

        /// <summary>Determines if two member match infos will match the same members.</summary>
        public static bool operator ==(MemberMatchInfo name1, MemberMatchInfo name2) {
            return name1.Equals(name2);
        }
        /// <summary>Determines if two member match infos can match different members.</summary>
        public static bool operator !=(MemberMatchInfo name1, MemberMatchInfo name2) {
            return !name1.Equals(name2);
        }
        public override bool Equals(object obj) {
            return obj is MemberMatchInfo
                && Equals((MemberMatchInfo)obj);
        }
        public bool Equals(MemberMatchInfo other) {
            return _canonicalMemberName == other._canonicalMemberName
                && _memberType == other._memberType;
        }
        public override int GetHashCode() {
            return _canonicalMemberName == null ? 0 : _canonicalMemberName.GetHashCode();
        }
        public override string ToString() {
            return string.Format("Member of type {1} with name like '{0}'", _rawRawMemberName, _memberType);
        }
    }
}