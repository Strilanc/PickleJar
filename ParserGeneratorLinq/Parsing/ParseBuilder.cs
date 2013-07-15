using System.Collections;
using System.Collections.Generic;
using Strilanc.Parsing.Internal.StructuredParsers;
using Strilanc.Parsing.Internal.UnsafeParsers;

namespace Strilanc.Parsing {
    public static partial class Parse {
        public sealed class Builder<T> : ICollection<IFieldParserOfUnknownType> {
            private readonly List<IFieldParserOfUnknownType> _list = new List<IFieldParserOfUnknownType>();

            public IParser<T> Build() {
                return (IParser<T>)BlittableStructParser<T>.TryMake(_list)
                       ?? new ExpressionTreeParser<T>(_list);
            }

            public void Add<TItem>(CanonicalizingMemberName name, IParser<TItem> parser) {
                Add(new FieldParserOfUnknownType<TItem>(parser, name.ToString()));
            }
            public IEnumerator<IFieldParserOfUnknownType> GetEnumerator() {
                return _list.GetEnumerator();
            }
            IEnumerator IEnumerable.GetEnumerator() {
                return GetEnumerator();
            }
            public void Add(IFieldParserOfUnknownType item) {
                _list.Add(item);
            }
            public void Clear() {
                _list.Clear();
            }
            public bool Contains(IFieldParserOfUnknownType item) {
                return _list.Contains(item);
            }
            public void CopyTo(IFieldParserOfUnknownType[] array, int arrayIndex) {
                _list.CopyTo(array, arrayIndex);
            }
            public bool Remove(IFieldParserOfUnknownType item) {
                return _list.Remove(item);
            }
            public int Count { get { return _list.Count; } }
            public bool IsReadOnly { get { return ((ICollection<IFieldParserOfUnknownType>)_list).IsReadOnly; } }
        }
    }
}
