namespace Strilanc.Parsing {
    public struct CanonicalizingMemberName {
        private readonly string _name;
        private readonly string _canonicalName;
        public CanonicalizingMemberName(string name) {
            _name = name;
            _canonicalName = name.ToLowerInvariant().Replace("_", "");
        }

        public override bool Equals(object obj) {
            return obj is CanonicalizingMemberName
                   && Equals(_canonicalName, ((CanonicalizingMemberName)obj)._canonicalName);
        }
        public static implicit operator CanonicalizingMemberName(string name) {
            return new CanonicalizingMemberName(name);
        }
        public override int GetHashCode() {
            return _canonicalName == null ? 0 : _canonicalName.GetHashCode();
        }
        public override string ToString() {
            return _name;
        }
    }
}