using System.Collections;
using System.Collections.Generic;
using Strilanc.PickleJar.Internal.StructuredParsers;
using Strilanc.PickleJar.Internal.UnsafeParsers;

namespace Strilanc.PickleJar {
    public static partial class Jar {
        /// <summary>
        /// Jar.Builder is used to build up a type parser by adding parsers to be matched against that type's required fields.
        /// Use the Add method, or the collection initialization syntax, to add named parsers to the builder.
        /// Use the Build method to produce a dynamically optimized parser for the type.
        /// </summary>
        public sealed class Builder<T> : ICollection<IFieldParser> {
            private readonly List<IFieldParser> _list = new List<IFieldParser>();

            ///<summary>Returns a dynamically optimized parser based on the field parsers that have been added so far.</summary>
            public IParser<T> Build() {
                return (IParser<T>)BlittableStructParser<T>.TryMake(_list)
                       ?? new CompiledReflectionParser<T>(_list);
            }

            public void Add<TItem>(CanonicalizingMemberName name, IParser<TItem> parser) {
                Add(new FieldParser<TItem>(parser, name));
            }
            IEnumerator<IFieldParser> IEnumerable<IFieldParser>.GetEnumerator() {
                return _list.GetEnumerator();
            }
            IEnumerator IEnumerable.GetEnumerator() {
                return _list.GetEnumerator();
            }
            public void Add(IFieldParser item) {
                _list.Add(item);
            }
            void ICollection<IFieldParser>.Clear() {
                _list.Clear();
            }
            bool ICollection<IFieldParser>.Contains(IFieldParser item) {
                return _list.Contains(item);
            }
            void ICollection<IFieldParser>.CopyTo(IFieldParser[] array, int arrayIndex) {
                _list.CopyTo(array, arrayIndex);
            }
            bool ICollection<IFieldParser>.Remove(IFieldParser item) {
                return _list.Remove(item);
            }
            int ICollection<IFieldParser>.Count { get { return _list.Count; } }
            bool ICollection<IFieldParser>.IsReadOnly { get { return ((ICollection<IFieldParser>)_list).IsReadOnly; } }
        }
    }
}
