using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Strilanc.Parsing.Internal.Misc;

namespace Strilanc.Parsing.Internal.StructuredParsers {
    sealed class SequencedParser<T1, T2> : IParserInternal<Tuple<T1, T2>> {
        public readonly IParser<T1> SubParser1;
        public readonly IParser<T2> SubParser2;
        public SequencedParser(IParser<T1> subParser1, IParser<T2> subParser2) {
            SubParser1 = subParser1;
            SubParser2 = subParser2;
        }
        public ParsedValue<Tuple<T1, T2>> Parse(ArraySegment<byte> data) {
            var r1 = SubParser1.Parse(data);
            var r2 = SubParser2.Parse(new ArraySegment<byte>(data.Array, data.Offset + r1.Consumed, data.Count - r1.Consumed));
            return new ParsedValue<Tuple<T1, T2>>(Tuple.Create(r1.Value, r2.Value), r1.Consumed + r2.Consumed);
        }
        public bool IsBlittable { get { return false; } }
        public int? OptionalConstantSerializedLength { get { return SubParser1.OptionalConstantSerializedLength() + SubParser2.OptionalConstantSerializedLength(); } }
        public Expression TryMakeParseFromDataExpression(Expression array, Expression offset, Expression count) {
            return null;
        }
        public Expression TryMakeGetValueFromParsedExpression(Expression parsed) {
            return null;
        }
        public Expression TryMakeGetCountFromParsedExpression(Expression parsed) {
            return null;
        }
    }
    sealed class SequencedParser<T> : IParserInternal<IReadOnlyList<T>> {
        public readonly IReadOnlyList<IParser<T>> SubParsers;
        public SequencedParser(IReadOnlyList<IParser<T>> subParsers) {
            SubParsers = subParsers;
        }
        public ParsedValue<IReadOnlyList<T>> Parse(ArraySegment<byte> data) {
            var values = new List<T>();
            var total = 0;
            foreach (var p in SubParsers) {
                var r = p.Parse(data.Skip(total));
                total += r.Consumed;
                values.Add(r.Value);
            }
            return new ParsedValue<IReadOnlyList<T>>(values, total);
        }
        public bool IsBlittable { get { return false; } }
        public int? OptionalConstantSerializedLength { get { return SubParsers.Aggregate((int?)0, (a, e) => a + e.OptionalConstantSerializedLength()); } }
        public Expression TryMakeParseFromDataExpression(Expression array, Expression offset, Expression count) {
            return null;
        }
        public Expression TryMakeGetValueFromParsedExpression(Expression parsed) {
            return null;
        }
        public Expression TryMakeGetCountFromParsedExpression(Expression parsed) {
            return null;
        }
    }
}