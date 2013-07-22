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
        public sealed class Builder<T> : ICollection<IMemberAndJar> {
            private readonly List<IMemberAndJar> _list = new List<IMemberAndJar>();

            ///<summary>Returns a dynamically optimized Jar based on the field parsers that have been added so far.</summary>
            public IJar<T> Build() {
                return (IJar<T>)TypeJarBlit<T>.TryMake(_list)
                       ?? new TypeJarCompiled<T>(_list);
            }

            public void Add<TItem>(CanonicalMemberName name, IJar<TItem> parser) {
                Add(new MemberAndJar<TItem>(parser, name));
            }
            IEnumerator<IMemberAndJar> IEnumerable<IMemberAndJar>.GetEnumerator() {
                return _list.GetEnumerator();
            }
            IEnumerator IEnumerable.GetEnumerator() {
                return _list.GetEnumerator();
            }
            public void Add(IMemberAndJar item) {
                _list.Add(item);
            }
            void ICollection<IMemberAndJar>.Clear() {
                _list.Clear();
            }
            bool ICollection<IMemberAndJar>.Contains(IMemberAndJar item) {
                return _list.Contains(item);
            }
            void ICollection<IMemberAndJar>.CopyTo(IMemberAndJar[] array, int arrayIndex) {
                _list.CopyTo(array, arrayIndex);
            }
            bool ICollection<IMemberAndJar>.Remove(IMemberAndJar item) {
                return _list.Remove(item);
            }
            int ICollection<IMemberAndJar>.Count { get { return _list.Count; } }
            bool ICollection<IMemberAndJar>.IsReadOnly { get { return ((ICollection<IMemberAndJar>)_list).IsReadOnly; } }
        }
    }
}
