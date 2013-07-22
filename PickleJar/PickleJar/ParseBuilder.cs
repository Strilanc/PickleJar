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
        public sealed class Builder<T> : ICollection<IMemberJar> {
            private readonly List<IMemberJar> _list = new List<IMemberJar>();

            ///<summary>Returns a dynamically optimized Jar based on the field parsers that have been added so far.</summary>
            public IJar<T> Build() {
                return (IJar<T>)TypeJarBlit<T>.TryMake(_list)
                       ?? new TypeJarCompiled<T>(_list);
            }

            public void Add<TItem>(CanonicalMemberName name, IJar<TItem> parser) {
                Add(new MemberJar<TItem>(parser, name));
            }
            IEnumerator<IMemberJar> IEnumerable<IMemberJar>.GetEnumerator() {
                return _list.GetEnumerator();
            }
            IEnumerator IEnumerable.GetEnumerator() {
                return _list.GetEnumerator();
            }
            public void Add(IMemberJar item) {
                _list.Add(item);
            }
            void ICollection<IMemberJar>.Clear() {
                _list.Clear();
            }
            bool ICollection<IMemberJar>.Contains(IMemberJar item) {
                return _list.Contains(item);
            }
            void ICollection<IMemberJar>.CopyTo(IMemberJar[] array, int arrayIndex) {
                _list.CopyTo(array, arrayIndex);
            }
            bool ICollection<IMemberJar>.Remove(IMemberJar item) {
                return _list.Remove(item);
            }
            int ICollection<IMemberJar>.Count { get { return _list.Count; } }
            bool ICollection<IMemberJar>.IsReadOnly { get { return ((ICollection<IMemberJar>)_list).IsReadOnly; } }
        }
    }
}
