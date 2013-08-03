using System;
using Strilanc.PickleJar.Internal;
using System.Linq;

namespace Strilanc.PickleJar {
    public struct MemberMatchInfo : IEquatable<MemberMatchInfo> {
        private readonly string _rawName;
        private readonly string _canonicalName;
        private readonly Type _memberType;
        public Type MemberType { get { return _memberType; } }

        public MemberMatchInfo(string name, Type type) {
            if (name == null) throw new ArgumentNullException("name");
            if (type == null) throw new ArgumentNullException("type");
            _rawName = name;
            _canonicalName = Canonize(name);
            _memberType = type;
        }
        public static string Canonize(string name) {
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

        public static bool operator ==(MemberMatchInfo name1, MemberMatchInfo name2) {
            return name1.Equals(name2);
        }
        public static bool operator !=(MemberMatchInfo name1, MemberMatchInfo name2) {
            return !name1.Equals(name2);
        }
        public override bool Equals(object obj) {
            return obj is MemberMatchInfo
                && Equals((MemberMatchInfo)obj);
        }
        public bool Equals(MemberMatchInfo other) {
            return _canonicalName == other._canonicalName
                && _memberType == other._memberType;
        }
        public override int GetHashCode() {
            return _canonicalName == null ? 0 : _canonicalName.GetHashCode();
        }
        public override string ToString() {
            return string.Format("name like '{0}' of type {1}", _rawName, _memberType);
        }
    }
}