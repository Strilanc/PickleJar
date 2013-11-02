using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Strilanc.PickleJar.Internal;
using Strilanc.PickleJar.Internal.Structured;
using Strilanc.PickleJar.Internal.Values;

namespace Strilanc.PickleJar {
    public static partial class Jar {
        /// <summary>
        /// Jar.Builder is used to build up a type Jar by adding parsers to be matched against that type's required fields.
        /// Use the Add method, or the collection initialization syntax, to add named parsers to the builder.
        /// Use the Build method to produce a dynamically optimized Jar for the type.
        /// </summary>
        public sealed class Builder : ICollection<IJarForMember> {
            private readonly List<IJarForMember> _list = new List<IJarForMember>();

            /// <summary>Adds a jar, matched against a member via a MemberMatchInfo, to the builder.</summary>
            public void Add<TItem>(MemberMatchInfo memberMatchInfo, IJar<TItem> parser) {
                if (parser == null) throw new ArgumentNullException("parser");
                Add(new JarForMember<TItem>(parser, memberMatchInfo));
            }
            /// <summary>Adds a jar, matched against a member via a MemberMatchInfo derived from the given name, to the builder.</summary>
            /// <remarks>This method exists to enable easy use of the collection initializer syntax.</remarks>
            public void Add<TItem>(string nameMatcher, IJar<TItem> parser) {
                if (nameMatcher == null) throw new ArgumentNullException("nameMatcher");
                if (parser == null) throw new ArgumentNullException("parser");
                Add(new JarForMember<TItem>(parser, new MemberMatchInfo(nameMatcher, typeof (TItem))));
            }

            IEnumerator<IJarForMember> IEnumerable<IJarForMember>.GetEnumerator() {
                return _list.GetEnumerator();
            }
            IEnumerator IEnumerable.GetEnumerator() {
                return _list.GetEnumerator();
            }
            public void Add(IJarForMember item) {
                _list.Add(item);
            }
            void ICollection<IJarForMember>.Clear() {
                _list.Clear();
            }
            bool ICollection<IJarForMember>.Contains(IJarForMember item) {
                return _list.Contains(item);
            }
            void ICollection<IJarForMember>.CopyTo(IJarForMember[] array, int arrayIndex) {
                _list.CopyTo(array, arrayIndex);
            }
            bool ICollection<IJarForMember>.Remove(IJarForMember item) {
                return _list.Remove(item);
            }
            int ICollection<IJarForMember>.Count { get { return _list.Count; } }
            bool ICollection<IJarForMember>.IsReadOnly { get { return ((ICollection<IJarForMember>)_list).IsReadOnly; } }
        }

        [DebuggerDisplay("{ToString()}")]
        public sealed class NamedJarList : ICollection<NamedJarList.Entry> {
            [DebuggerDisplay("{ToString()}")]
            public struct Entry {
                private readonly string _name;
                private readonly object _jar;
                private readonly Type _jarValueType;

                public string Name { get { return _name ?? ""; } }
                public object Jar { get { return _jar ?? new ConstantJar<byte>(0); } }
                public Type JarValueType { get { return _jarValueType ?? typeof(byte); } }
                
                public Entry(string name, object jar, Type jarValueType) {
                    if (name == null) throw new ArgumentNullException("name");
                    if (jar == null) throw new ArgumentNullException("jar");
                    if (jarValueType == null) throw new ArgumentNullException("jarValueType");
                    if (!typeof(IJar<>).MakeGenericType(jarValueType).IsInstanceOfType(jar)) throw new ArgumentException("!(jar is IJar<jarValueType>)");
                    this._name = name;
                    this._jar = jar;
                    this._jarValueType = jarValueType;
                }
                
                public override string ToString() {
                    return string.Format("{0}: {1}", _name, _jar);
                }
            }

            private readonly HashSet<string> _usedNames = new HashSet<string>();
            private readonly List<Entry> _namedJars = new List<Entry>();

            public void Add<TItem>(string name, IJar<TItem> jar) {
                if (name == null) throw new ArgumentNullException("name");
                if (jar == null) throw new ArgumentNullException("jar");
                Add(new Entry(name, jar, typeof(TItem)));
            }

            IEnumerator<Entry> IEnumerable<Entry>.GetEnumerator() {
                return _namedJars.GetEnumerator();
            }
            IEnumerator IEnumerable.GetEnumerator() {
                return _namedJars.GetEnumerator();
            }
            public void Add(Entry item) {
                if (!_usedNames.Add(item.Name)) throw new InvalidOperationException("Duplicate name.");
                _namedJars.Add(item);
            }
            void ICollection<Entry>.Clear() {
                _namedJars.Clear();
                _usedNames.Clear();
            }
            bool ICollection<Entry>.Contains(Entry item) {
                return _namedJars.Contains(item);
            }
            void ICollection<Entry>.CopyTo(Entry[] array, int arrayIndex) {
                _namedJars.CopyTo(array, arrayIndex);
            }
            bool ICollection<Entry>.Remove(Entry item) {
                return _namedJars.Remove(item) && _usedNames.Remove(item.Name);
            }
            int ICollection<Entry>.Count { get { return _namedJars.Count; } }
            bool ICollection<Entry>.IsReadOnly { get { return ((ICollection<Entry>)_namedJars).IsReadOnly; } }

            public override string ToString() {
                return _namedJars.StringJoinList("[", ", ", "]");
            }
        }
    }

    [DebuggerDisplay("{ToString()}")]
    internal sealed class ObjectJar<T> : IJar<object> {
        private readonly IJar<T> _jar;
        public ObjectJar(IJar<T> jar) {
            if (jar == null) throw new ArgumentNullException("jar");
            _jar = jar;
        }
        public ParsedValue<object> Parse(ArraySegment<byte> data) {
            return _jar.Parse(data).Select(e => (object)e);
        }
        public byte[] Pack(object value) {
            return _jar.Pack((T)value);
        }
        public bool CanBeFollowed { get { return _jar.CanBeFollowed; } }
        public override string ToString() {
            return string.Format("{0}", _jar);
        }
    }
}

