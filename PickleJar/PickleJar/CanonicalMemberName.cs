namespace Strilanc.PickleJar {
    public struct CanonicalMemberName {
        private readonly string _name;
        private readonly string _canonicalName;
        public CanonicalMemberName(string name) {
            _name = name;
            _canonicalName = name.ToLowerInvariant().Replace("_", "");
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