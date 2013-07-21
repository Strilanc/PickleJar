using System;
using System.Collections.Generic;
using Strilanc.PickleJar.Internal;
using Strilanc.PickleJar.Internal.Numbers;
using Strilanc.PickleJar.Internal.Repeated;

namespace Strilanc.PickleJar {
    /// <summary>
    /// The Jar class exposes utilities for accessing, creating, and combining parsers.
    /// </summary>
    public static partial class Jar {
        /// <summary>Returns a parser that parses a single serialized byte into the congruent signed byte value.</summary>
        public static IJar<sbyte> Int8 { get { return new Int8Jar(); } }
        /// <summary>Returns a parser that parses a single serialized byte into that same byte value.</summary>
        public static IJar<byte> UInt8 { get { return new UInt8Jar(); } }

        /// <summary>Returns a parser that parses two bytes into the corresponding 2s-complement signed short with the least significant byte first.</summary>
        public static IJar<Int16> Int16LittleEndian { get { return new Int16Jar(Endianess.LittleEndian); } }
        /// <summary>Returns a parser that parses two bytes into the corresponding 2s-complement signed short with the most significant byte first.</summary>
        public static IJar<Int16> Int16BigEndian { get { return new Int16Jar(Endianess.BigEndian); } }
        /// <summary>Returns a parser that parses two bytes into the corresponding 2s-complement unsigned short with the least significant byte first.</summary>
        public static IJar<UInt16> UInt16LittleEndian { get { return new UInt16Jar(Endianess.LittleEndian); } }
        /// <summary>Returns a parser that parses two bytes into the corresponding 2s-complement unsigned short with the most significant byte first.</summary>
        public static IJar<UInt16> UInt16BigEndian { get { return new UInt16Jar(Endianess.BigEndian); } }

        /// <summary>Returns a parser that parses four bytes into the corresponding 2s-complement signed int with the least significant byte first.</summary>
        public static IJar<Int32> Int32LittleEndian { get { return new Int32Jar(Endianess.LittleEndian); } }
        /// <summary>Returns a parser that parses four bytes into the corresponding 2s-complement signed int with the most significant byte first.</summary>
        public static IJar<Int32> Int32BigEndian { get { return new Int32Jar(Endianess.BigEndian); } }
        /// <summary>Returns a parser that parses four bytes into the corresponding 2s-complement unsigned int with the least significant byte first.</summary>
        public static IJar<UInt32> UInt32LittleEndian { get { return new UInt32Jar(Endianess.LittleEndian); } }
        /// <summary>Returns a parser that parses four bytes into the corresponding 2s-complement unsigned int with the most significant byte first.</summary>
        public static IJar<UInt32> UInt32BigEndian { get { return new UInt32Jar(Endianess.BigEndian); } }

        /// <summary>Returns a parser that parses eight bytes into the corresponding 2s-complement signed long with the least significant byte first.</summary>
        public static IJar<Int64> Int64LittleEndian { get { return new Int64Jar(Endianess.LittleEndian); } }
        /// <summary>Returns a parser that parses eight bytes into the corresponding 2s-complement signed long with the most significant byte first.</summary>
        public static IJar<Int64> Int64BigEndian { get { return new Int64Jar(Endianess.BigEndian); } }
        /// <summary>Returns a parser that parses eight bytes into the corresponding 2s-complement unsigned long with the least significant byte first.</summary>
        public static IJar<UInt64> UInt64LittleEndian { get { return new UInt64Jar(Endianess.LittleEndian); } }
        /// <summary>Returns a parser that parses eight bytes into the corresponding 2s-complement unsigned long with the most significant byte first.</summary>
        public static IJar<UInt64> UInt64BigEndian { get { return new UInt64Jar(Endianess.BigEndian); } }

        /// <summary>A jar for IEEE 32-bit single-precision floating point number.</summary>
        public static IJar<float> Float32 { get { return new Float32Jar(); } }
        /// <summary>A jar for IEEE 64-bit double-precision floating point number.</summary>
        public static IJar<double> Float64 { get { return new Float64Jar(); } }

        /// <summary>Returns a parser that repeatedly uses an item parser a fixed number of times and puts the resulting item values into an array.</summary>
        public static IParser<IReadOnlyList<T>> RepeatNTimes<T>(this IParser<T> itemParser, int constantRepeatCount) {
            if (itemParser == null) throw new ArgumentNullException("itemParser");
            if (constantRepeatCount < 0) throw new ArgumentOutOfRangeException("constantRepeatCount");
            return new FixedRepeatParser<T>(itemParser.Bulk(), constantRepeatCount);
        }
        /// <summary>Returns a parser that first parses a count then repeatedly uses an item parser that number of times and puts the resulting item values into an array.</summary>
        public static IParser<IReadOnlyList<T>> RepeatCountPrefixTimes<T>(this IParser<T> itemParser, IJar<int> countPrefixParser) {
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
            var counter = new AnonymousJar<int>(
                data => {
                    if (data.Count % itemLength != 0) throw new InvalidOperationException("Fragment");
                    return new ParsedValue<int>(data.Count / itemLength, 0);
                },
                item => new byte[0]);
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
        public static IJar<T> Where<T>(this IJar<T> parser, Func<T, bool> constraint) {
            if (parser == null) throw new ArgumentNullException("parser");
            if (constraint == null) throw new ArgumentNullException("constraint");
            return new AnonymousJar<T>(data => {
                var v = parser.Parse(data);
                if (!constraint(v.Value)) throw new InvalidOperationException("Data did not match Where constraint");
                return v;
            }, item => {
                if (!constraint(item)) throw new InvalidOperationException("Data did not match Where constraint");
                return parser.Pack(item);
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
