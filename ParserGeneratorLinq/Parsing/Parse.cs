using System;
using System.Linq.Expressions;
using Strilanc.Parsing.Internal.Misc;
using Strilanc.Parsing.Internal.NumberParsers;
using Strilanc.Parsing.Internal.RepetitionParsers;
using Strilanc.Parsing.Internal.UnsafeParsers;

namespace Strilanc.Parsing {
    public static class Parse {
        public static readonly IParser<sbyte> Int8 = new Int8Parser();

        public static readonly IParser<Int16> Int16LittleEndian = new Int16Parser(Endianess.LittleEndian);
        public static readonly IParser<Int16> Int16BigEndian = new Int16Parser(Endianess.BigEndian);

        public static readonly IParser<Int32> Int32LittleEndian = new Int32Parser(Endianess.LittleEndian);
        public static readonly IParser<Int32> Int32BigEndian = new Int32Parser(Endianess.BigEndian);

        public static readonly IParser<Int64> Int64LittleEndian = new Int64Parser(Endianess.LittleEndian);
        public static readonly IParser<Int64> Int64BigEndian = new Int64Parser(Endianess.BigEndian);

        public static readonly IParser<byte> UInt8 = new UInt8Parser();

        public static readonly IParser<UInt16> UInt16LittleEndian = new UInt16Parser(Endianess.LittleEndian);
        public static readonly IParser<UInt16> UInt16BigEndian = new UInt16Parser(Endianess.BigEndian);

        public static readonly IParser<UInt32> UInt32LittleEndian = new UInt32Parser(Endianess.LittleEndian);
        public static readonly IParser<UInt32> UInt32BigEndian = new UInt32Parser(Endianess.BigEndian);

        public static readonly IParser<UInt64> UInt64LittleEndian = new UInt64Parser(Endianess.LittleEndian);
        public static readonly IParser<UInt64> UInt64BigEndian = new UInt64Parser(Endianess.BigEndian);

        private static IArrayParser<T> Array<T>(this IParser<T> itemParser) {
            if (itemParser == null) throw new ArgumentNullException("itemParser");

            return (IArrayParser<T>)BlittableArrayParser<T>.TryMake(itemParser)
                   ?? new ExpressionArrayParser<T>(itemParser);
        }

        public static IParser<T[]> RepeatNTimes<T>(this IParser<T> itemParser, int constantRepeatCount) {
            return new FixedRepeatParser<T>(itemParser.Array(), constantRepeatCount);
        }
        public static IParser<T[]> RepeatCountPrefixTimes<T>(this IParser<T> itemParser, IParser<int> countPrefixParser) {
            return new CountPrefixedRepeatParser<T>(countPrefixParser, itemParser.Array());
        }
        public static IParser<T[]> RepeatUntilEndOfData<T>(this IParser<T> itemParser) {
            var n = itemParser.OptionalConstantSerializedLength();
            if (!n.HasValue) {
                return new GreedyRepeatParser<T>(itemParser);
            }

            var itemLength = n.Value;
            var counter = new AnonymousParser<int>(e => {
                if (e.Count % itemLength != 0) throw new InvalidOperationException("Fragment");
                return new ParsedValue<int>(e.Count/itemLength, 0);
            });
            return new CountPrefixedRepeatParser<T>(
                counter,
                itemParser.Array());
        }

        public static IParser<TOut> Select<TIn, TOut>(this IParser<TIn> parser, Func<TIn, TOut> projection) {
            if (parser == null) throw new ArgumentNullException("parser");
            if (projection == null) throw new ArgumentNullException("projection");
            return new AnonymousParser<TOut>(data => parser.Parse(data).Select(projection));
        }

        public static IParser<T> Where<T>(this IParser<T> parser, Func<T, bool> constraint) {
            return new AnonymousParser<T>(data => {
                var v = parser.Parse(data);
                if (!constraint(v.Value)) throw new InvalidOperationException("Data did not match Where constraint");
                return v;
            });
        }

        public static IParser<TOut> SelectMany<TIn, TMid, TOut>(this IParser<TIn> parser, Func<TIn, IParser<TMid>> midProjection, Func<TIn, TMid, TOut> resultProjection) {
            if (parser == null) throw new ArgumentNullException("parser");
            if (midProjection == null) throw new ArgumentNullException("midProjection");
            if (resultProjection == null) throw new ArgumentNullException("resultProjection");
            return new AnonymousParser<TOut>(data => {
                var parsedIn = parser.Parse(data);
                var parsedMid = midProjection(parsedIn.Value).Parse(data.Skip(parsedIn.Consumed));
                return resultProjection(parsedIn.Value, parsedMid.Value).AsParsed(parsedIn.Consumed + parsedMid.Consumed);
            });
        }
    }
}
