using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Strilanc.PickleJar.Internal.Structured {
    /// <summary>
    /// SequenceParser is used to parse consecutive values that have the same type but potentially different serialized representations.
    /// </summary>
    internal sealed class SequenceParser<T> : IJarInternal<IReadOnlyList<T>> {
        public readonly IReadOnlyList<IJar<T>> SubParsers;
        public SequenceParser(IReadOnlyList<IJar<T>> subParsers) {
            SubParsers = subParsers;
        }
        public ParsedValue<IReadOnlyList<T>> Parse(ArraySegment<byte> data) {
            var values = new List<T>();
            var total = 0;
            foreach (var r in SubParsers.Select(p => p.Parse(data.Skip(total)))) {
                total += r.Consumed;
                values.Add(r.Value);
            }
            return new ParsedValue<IReadOnlyList<T>>(values, total);
        }
        public byte[] Pack(IReadOnlyList<T> values) {
            return values.Zip(SubParsers, (v, p) => p.Pack(v)).SelectMany(e => e).ToArray();
        }
        public bool AreMemoryAndSerializedRepresentationsOfValueGuaranteedToMatch { get { return false; } }
        public int? OptionalConstantSerializedLength { get { return SubParsers.Aggregate((int?)0, (a, e) => a + e.OptionalConstantSerializedLength()); } }
        public InlinedParserComponents TryMakeInlinedParserComponents(Expression array, Expression offset, Expression count) {
            return null;
        }
    }
}