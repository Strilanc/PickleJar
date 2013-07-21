using System;
using System.Linq.Expressions;
using System.Linq;

namespace Strilanc.PickleJar.Internal.Structured {
    /// <summary>
    /// TupleParser is used to parse consecutive values that have different types.
    /// </summary>
    internal sealed class TupleParser<T1, T2> : IJarInternal<Tuple<T1, T2>> {
        public readonly IJar<T1> SubParser1;
        public readonly IJar<T2> SubParser2;
        public TupleParser(IJar<T1> subParser1, IJar<T2> subParser2) {
            SubParser1 = subParser1;
            SubParser2 = subParser2;
        }
        public ParsedValue<Tuple<T1, T2>> Parse(ArraySegment<byte> data) {
            var r1 = SubParser1.Parse(data);
            var r2 = SubParser2.Parse(new ArraySegment<byte>(data.Array, data.Offset + r1.Consumed, data.Count - r1.Consumed));
            return new ParsedValue<Tuple<T1, T2>>(Tuple.Create(r1.Value, r2.Value), r1.Consumed + r2.Consumed);
        }
        public byte[] Pack(Tuple<T1, T2> value) {
            var r1 = SubParser1.Pack(value.Item1);
            var r2 = SubParser2.Pack(value.Item2);
            return r1.Concat(r2).ToArray();
        }
        public bool AreMemoryAndSerializedRepresentationsOfValueGuaranteedToMatch { get { return false; } }
        public int? OptionalConstantSerializedLength { get { return SubParser1.OptionalConstantSerializedLength() + SubParser2.OptionalConstantSerializedLength(); } }
        public InlinedParserComponents TryMakeInlinedParserComponents(Expression array, Expression offset, Expression count) {
            return null;
        }
    }
}
