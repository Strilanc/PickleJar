using System;
using System.Linq.Expressions;
using System.Linq;

namespace Strilanc.PickleJar.Internal.Structured {
    /// <summary>
    /// TupleJar is used to parse consecutive values that have different types.
    /// </summary>
    internal sealed class TupleJar<T1, T2> : IJarMetadataInternal, IJar<Tuple<T1, T2>> {
        public readonly IJar<T1> SubJar1;
        public readonly IJar<T2> SubJar2;
        public TupleJar(IJar<T1> subJar1, IJar<T2> subJar2) {
            if (subJar1 == null) throw new ArgumentNullException("subJar1");
            if (subJar2 == null) throw new ArgumentNullException("subJar2");
            SubJar1 = subJar1;
            SubJar2 = subJar2;
        }
        public ParsedValue<Tuple<T1, T2>> Parse(ArraySegment<byte> data) {
            var r1 = SubJar1.Parse(data);
            var r2 = SubJar2.Parse(new ArraySegment<byte>(data.Array, data.Offset + r1.Consumed, data.Count - r1.Consumed));
            return new ParsedValue<Tuple<T1, T2>>(Tuple.Create(r1.Value, r2.Value), r1.Consumed + r2.Consumed);
        }
        public byte[] Pack(Tuple<T1, T2> value) {
            var r1 = SubJar1.Pack(value.Item1);
            var r2 = SubJar2.Pack(value.Item2);
            return r1.Concat(r2).ToArray();
        }
        public bool AreMemoryAndSerializedRepresentationsOfValueGuaranteedToMatch { get { return false; } }
        public int? OptionalConstantSerializedLength { get { return SubJar1.OptionalConstantSerializedLength() + SubJar2.OptionalConstantSerializedLength(); } }
        public InlinedParserComponents TryMakeInlinedParserComponents(Expression array, Expression offset, Expression count) {
            return null;
        }
        public override string ToString() {
            return string.Format(
                "{0}.Then({1})",
                SubJar1,
                SubJar2);
        }
    }
}
