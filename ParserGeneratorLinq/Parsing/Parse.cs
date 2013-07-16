using System;
using System.Collections.Generic;
using Strilanc.Parsing.Internal;
using Strilanc.Parsing.Internal.NumberParsers;
using Strilanc.Parsing.Internal.RepetitionParsers;

namespace Strilanc.Parsing {
    /// <summary>
    /// The Parse class exposes utilities for accessing, creating, and combining parsers.
    /// </summary>
    public static partial class Parse {
        /// <summary>Returns a parser that parses a single serialized byte into the congruent signed byte value.</summary>
        public static IParser<sbyte> Int8 { get { return new Int8Parser(); } }
        /// <summary>Returns a parser that parses a single serialized byte into that same byte value.</summary>
        public static IParser<byte> UInt8 { get { return new UInt8Parser(); } }

        /// <summary>Returns a parser that parses two bytes into the corresponding 2s-complement signed short with the least significant byte first.</summary>
        public static IParser<Int16> Int16LittleEndian { get { return new Int16Parser(Endianess.LittleEndian); } }
        /// <summary>Returns a parser that parses two bytes into the corresponding 2s-complement signed short with the most significant byte first.</summary>
        public static IParser<Int16> Int16BigEndian { get { return new Int16Parser(Endianess.BigEndian); } }
        /// <summary>Returns a parser that parses two bytes into the corresponding 2s-complement unsigned short with the least significant byte first.</summary>
        public static IParser<UInt16> UInt16LittleEndian { get { return new UInt16Parser(Endianess.LittleEndian); } }
        /// <summary>Returns a parser that parses two bytes into the corresponding 2s-complement unsigned short with the most significant byte first.</summary>
        public static IParser<UInt16> UInt16BigEndian { get { return new UInt16Parser(Endianess.BigEndian); } }

        /// <summary>Returns a parser that parses four bytes into the corresponding 2s-complement signed int with the least significant byte first.</summary>
        public static IParser<Int32> Int32LittleEndian { get { return new Int32Parser(Endianess.LittleEndian); } }
        /// <summary>Returns a parser that parses four bytes into the corresponding 2s-complement signed int with the most significant byte first.</summary>
        public static IParser<Int32> Int32BigEndian { get { return new Int32Parser(Endianess.BigEndian); } }
        /// <summary>Returns a parser that parses four bytes into the corresponding 2s-complement unsigned int with the least significant byte first.</summary>
        public static IParser<UInt32> UInt32LittleEndian { get { return new UInt32Parser(Endianess.LittleEndian); } }
        /// <summary>Returns a parser that parses four bytes into the corresponding 2s-complement unsigned int with the most significant byte first.</summary>
        public static IParser<UInt32> UInt32BigEndian { get { return new UInt32Parser(Endianess.BigEndian); } }

        /// <summary>Returns a parser that parses eight bytes into the corresponding 2s-complement signed long with the least significant byte first.</summary>
        public static IParser<Int64> Int64LittleEndian { get { return new Int64Parser(Endianess.LittleEndian); } }
        /// <summary>Returns a parser that parses eight bytes into the corresponding 2s-complement signed long with the most significant byte first.</summary>
        public static IParser<Int64> Int64BigEndian { get { return new Int64Parser(Endianess.BigEndian); } }
        /// <summary>Returns a parser that parses eight bytes into the corresponding 2s-complement unsigned long with the least significant byte first.</summary>
        public static IParser<UInt64> UInt64LittleEndian { get { return new UInt64Parser(Endianess.LittleEndian); } }
        /// <summary>Returns a parser that parses eight bytes into the corresponding 2s-complement unsigned long with the most significant byte first.</summary>
        public static IParser<UInt64> UInt64BigEndian { get { return new UInt64Parser(Endianess.BigEndian); } }

        /// <summary>Returns a parser that repeatedly uses an item parser a fixed number of times and puts the resulting item values into an array.</summary>
        public static IParser<IReadOnlyList<T>> RepeatNTimes<T>(this IParser<T> itemParser, int constantRepeatCount) {
            if (itemParser == null) throw new ArgumentNullException("itemParser");
            if (constantRepeatCount < 0) throw new ArgumentOutOfRangeException("constantRepeatCount");
            return new FixedRepeatParser<T>(itemParser.Bulk(), constantRepeatCount);
        }
        /// <summary>Returns a parser that first parses a count then repeatedly uses an item parser that number of times and puts the resulting item values into an array.</summary>
        public static IParser<IReadOnlyList<T>> RepeatCountPrefixTimes<T>(this IParser<T> itemParser, IParser<int> countPrefixParser) {
            if (itemParser == null) throw new ArgumentNullException("itemParser");
            if (countPrefixParser == null) throw new ArgumentNullException("countPrefixParser");
            return new CountPrefixedRepeatParser<T>(countPrefixParser, itemParser.Bulk());
        }
        /// <summary>
        /// Returns a parser that repeatedly uses an item parser until there's no data left and puts the resulting items into an array.
        /// If the parser encounters a partial value at the end of the data, the entire parse fails.
        /// </summary>
        public static IParser<IReadOnlyList<T>> RepeatUntilEndOfData<T>(this IParser<T> itemParser) {
            if (itemParser == null) throw new ArgumentNullException("itemParser");
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

        /// <summary>
        /// Returns a parser that applies the given parser, but then transform the resulting value by running it through a projection function.
        /// </summary>
        public static IParser<TOut> Select<TIn, TOut>(this IParser<TIn> parser, Func<TIn, TOut> projection) {
            if (parser == null) throw new ArgumentNullException("parser");
            if (projection == null) throw new ArgumentNullException("projection");
            return new AnonymousParser<TOut>(data => parser.Parse(data).Select(projection));
        }

        /// <summary>
        /// Returns a parser that applies the given parser, but fails if running the value through the given constraint function does not return true.
        /// </summary>
        public static IParser<T> Where<T>(this IParser<T> parser, Func<T, bool> constraint) {
            if (parser == null) throw new ArgumentNullException("parser");
            if (constraint == null) throw new ArgumentNullException("constraint");
            return new AnonymousParser<T>(data => {
                var v = parser.Parse(data);
                if (!constraint(v.Value)) throw new InvalidOperationException("Data did not match Where constraint");
                return v;
            });
        }

        /// <summary>
        /// Returns a parser that applies the given parser, derives a second parser from the resulting value, applies the second parser, and then derives a result from the two parsed values.
        /// </summary>
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
