using System;
using Strilanc.Parsing.Internal;
using Strilanc.Parsing.Internal.NumberParsers;
using Strilanc.Parsing.Internal.RepetitionParsers;

namespace Strilanc.Parsing {
    /// <summary>
    /// The Parse class exposes utilities for accessing, creating, and combining parsers.
    /// </summary>
    public static partial class Parse {
        public static IParser<sbyte> Int8 { get { return new Int8Parser(); } }

        public static IParser<Int16> Int16LittleEndian { get { return new Int16Parser(Endianess.LittleEndian); } }
        public static IParser<Int16> Int16BigEndian { get { return new Int16Parser(Endianess.BigEndian); } }

        public static IParser<Int32> Int32LittleEndian { get { return new Int32Parser(Endianess.LittleEndian); } }
        public static IParser<Int32> Int32BigEndian { get { return new Int32Parser(Endianess.BigEndian); } }

        public static IParser<Int64> Int64LittleEndian { get { return new Int64Parser(Endianess.LittleEndian); } }
        public static IParser<Int64> Int64BigEndian { get { return new Int64Parser(Endianess.BigEndian); } }

        public static IParser<byte> UInt8 { get { return new UInt8Parser(); } }

        public static IParser<UInt16> UInt16LittleEndian { get { return new UInt16Parser(Endianess.LittleEndian); } }
        public static IParser<UInt16> UInt16BigEndian { get { return new UInt16Parser(Endianess.BigEndian); } }

        public static IParser<UInt32> UInt32LittleEndian { get { return new UInt32Parser(Endianess.LittleEndian); } }
        public static IParser<UInt32> UInt32BigEndian { get { return new UInt32Parser(Endianess.BigEndian); } }

        public static IParser<UInt64> UInt64LittleEndian { get { return new UInt64Parser(Endianess.LittleEndian); } }
        public static IParser<UInt64> UInt64BigEndian { get { return new UInt64Parser(Endianess.BigEndian); } }

        public static IParser<T[]> RepeatNTimes<T>(this IParser<T> itemParser, int constantRepeatCount) {
            return new FixedRepeatParser<T>(itemParser.Bulk(), constantRepeatCount);
        }
        public static IParser<T[]> RepeatCountPrefixTimes<T>(this IParser<T> itemParser, IParser<int> countPrefixParser) {
            return new CountPrefixedRepeatParser<T>(countPrefixParser, itemParser.Bulk());
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
                itemParser.Bulk());
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
