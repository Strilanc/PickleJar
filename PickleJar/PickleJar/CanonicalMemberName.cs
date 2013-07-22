using System;
using Strilanc.PickleJar.Internal;
using System.Linq;

namespace Strilanc.PickleJar {
    public struct CanonicalMemberName {
        private readonly string _name;
        private readonly string _canonicalName;
        public CanonicalMemberName(string name) {
            _name = name;
            _canonicalName = Canonize(name);
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

        public static bool operator ==(CanonicalMemberName name1, CanonicalMemberName name2) {
            return name1.Equals(name2);
        }
        public static bool operator !=(CanonicalMemberName name1, CanonicalMemberName name2) {
            return !name1.Equals(name2);
        }
        public override bool Equals(object obj) {
            return obj is CanonicalMemberName
                   && Equals(_canonicalName, ((CanonicalMemberName)obj)._canonicalName);
        }
        public static implicit operator CanonicalMemberName(string name) {
            return new CanonicalMemberName(name);
        }
        public override int GetHashCode() {
            return _canonicalName == null ? 0 : _canonicalName.GetHashCode();
        }
        public override string ToString() {
            return _name;
        }
    }
}