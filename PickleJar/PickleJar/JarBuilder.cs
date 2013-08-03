using System.Collections;
using System.Collections.Generic;
using Strilanc.PickleJar.Internal.Structured;

namespace Strilanc.PickleJar {
    public static partial class Jar {
        /// <summary>
        /// Jar.Builder is used to build up a type Jar by adding parsers to be matched against that type's required fields.
        /// Use the Add method, or the collection initialization syntax, to add named parsers to the builder.
        /// Use the Build method to produce a dynamically optimized Jar for the type.
        /// </summary>
        public sealed class Builder<T> : ICollection<IJarForMember> {
            private readonly List<IJarForMember> _list = new List<IJarForMember>();

            ///<summary>Returns a dynamically optimized Jar based on the field parsers that have been added so far.</summary>
            public IJar<T> Build() {
                return (IJar<T>)TypeJarBlit<T>.TryMake(_list)
                       ?? new TypeJarCompiled<T>(_list);
            }

            public void Add<TItem>(MemberMatchInfo memberMatchInfo, IJar<TItem> parser) {
                Add(new JarForMember<TItem>(parser, memberMatchInfo));
            }
            public void Add<TItem>(string nameMatcher, IJar<TItem> parser) {
                Add(new JarForMember<TItem>(parser, new MemberMatchInfo(nameMatcher, typeof(TItem))));
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
    }
}
