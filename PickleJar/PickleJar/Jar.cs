using System;
using System.Collections.Generic;
using System.Text;
using Strilanc.PickleJar.Internal;
using Strilanc.PickleJar.Internal.Values;
using Strilanc.PickleJar.Internal.Repeated;
using Strilanc.PickleJar.Internal.Structured;

namespace Strilanc.PickleJar {
    /// <summary>
    /// The Jar class exposes utilities for accessing, creating, and combining jars (combination parsers and packers).
    /// </summary>
    public static partial class Jar {
        /// <summary>Returns a Jar that parses a single serialized byte into the congruent signed byte value.</summary>
        public static IJar<sbyte> Int8 { get { return new Int8Jar(); } }
        /// <summary>Returns a Jar that parses a single serialized byte into that same byte value.</summary>
        public static IJar<byte> UInt8 { get { return new UInt8Jar(); } }

        /// <summary>Returns a Jar that parses two bytes into the corresponding 2s-complement signed short with the least significant byte first.</summary>
        public static IJar<Int16> Int16LittleEndian { get { return new Int16Jar(Endianess.LittleEndian); } }
        /// <summary>Returns a Jar that parses two bytes into the corresponding 2s-complement signed short with the most significant byte first.</summary>
        public static IJar<Int16> Int16BigEndian { get { return new Int16Jar(Endianess.BigEndian); } }
        /// <summary>Returns a Jar that parses two bytes into the corresponding 2s-complement unsigned short with the least significant byte first.</summary>
        public static IJar<UInt16> UInt16LittleEndian { get { return new UInt16Jar(Endianess.LittleEndian); } }
        /// <summary>Returns a Jar that parses two bytes into the corresponding 2s-complement unsigned short with the most significant byte first.</summary>
        public static IJar<UInt16> UInt16BigEndian { get { return new UInt16Jar(Endianess.BigEndian); } }

        /// <summary>Returns a Jar that parses four bytes into the corresponding 2s-complement signed int with the least significant byte first.</summary>
        public static IJar<Int32> Int32LittleEndian { get { return new Int32Jar(Endianess.LittleEndian); } }
        /// <summary>Returns a Jar that parses four bytes into the corresponding 2s-complement signed int with the most significant byte first.</summary>
        public static IJar<Int32> Int32BigEndian { get { return new Int32Jar(Endianess.BigEndian); } }
        /// <summary>Returns a Jar that parses four bytes into the corresponding 2s-complement unsigned int with the least significant byte first.</summary>
        public static IJar<UInt32> UInt32LittleEndian { get { return new UInt32Jar(Endianess.LittleEndian); } }
        /// <summary>Returns a Jar that parses four bytes into the corresponding 2s-complement unsigned int with the most significant byte first.</summary>
        public static IJar<UInt32> UInt32BigEndian { get { return new UInt32Jar(Endianess.BigEndian); } }

        /// <summary>Returns a Jar that parses eight bytes into the corresponding 2s-complement signed long with the least significant byte first.</summary>
        public static IJar<Int64> Int64LittleEndian { get { return new Int64Jar(Endianess.LittleEndian); } }
        /// <summary>Returns a Jar that parses eight bytes into the corresponding 2s-complement signed long with the most significant byte first.</summary>
        public static IJar<Int64> Int64BigEndian { get { return new Int64Jar(Endianess.BigEndian); } }
        /// <summary>Returns a Jar that parses eight bytes into the corresponding 2s-complement unsigned long with the least significant byte first.</summary>
        public static IJar<UInt64> UInt64LittleEndian { get { return new UInt64Jar(Endianess.LittleEndian); } }
        /// <summary>Returns a Jar that parses eight bytes into the corresponding 2s-complement unsigned long with the most significant byte first.</summary>
        public static IJar<UInt64> UInt64BigEndian { get { return new UInt64Jar(Endianess.BigEndian); } }

        /// <summary>A jar for IEEE 32-bit single-precision floating point number.</summary>
        public static IJar<float> Float32 { get { return new Float32Jar(); } }
        /// <summary>A jar for IEEE 64-bit double-precision floating point number.</summary>
        public static IJar<double> Float64 { get { return new Float64Jar(); } }

        /// <summary>A jar for strings encoded in utf8.</summary>
        public static IJar<string> Utf8 {
            get {
                return new TextJar(Encoding.GetEncoding(Encoding.UTF8.WebName,
                                                        new EncoderExceptionFallback(),
                                                        new DecoderExceptionFallback()));
            }
        }
        /// <summary>A jar for strings encoded in ASCII.</summary>
        public static IJar<string> Ascii {
            get {
                return new TextJar(Encoding.GetEncoding(Encoding.ASCII.WebName,
                                                        new EncoderExceptionFallback(),
                                                        new DecoderExceptionFallback()));
            }
        }

        public static IJar<string> TextJar(Encoding encoding) {
            if (encoding == null) throw new ArgumentNullException("encoding");
            return new TextJar(encoding);
        }

        /// <summary>Returns a Jar that consumes all data up to a null terminator, and no more.</summary>
        public static IJar<T> NullTerminated<T>(this IJar<T> itemJar) {
            if (itemJar == null) throw new ArgumentNullException("itemJar");
            return new NullTerminatedJar<T>(itemJar);
        }
        /// <summary>Returns a Jar that repeatedly uses an item Jar a fixed number of times and puts the resulting item values into an array.</summary>
        public static IJar<IReadOnlyList<T>> RepeatNTimes<T>(this IJar<T> itemJar, int constantRepeatCount) {
            if (itemJar == null) throw new ArgumentNullException("itemJar");
            if (constantRepeatCount < 0) throw new ArgumentOutOfRangeException("constantRepeatCount");
            return new RepeatConstantNumberOfTimesJar<T>(itemJar.Bulk(), constantRepeatCount);
        }
        /// <summary>Returns a Jar that first parses a count then repeatedly uses an item Jar that number of times and puts the resulting item values into an array.</summary>
        public static IJar<IReadOnlyList<T>> RepeatCountPrefixTimes<T>(this IJar<T> itemJar, IJar<int> countPrefixJar) {
            if (itemJar == null) throw new ArgumentNullException("itemJar");
            if (countPrefixJar == null) throw new ArgumentNullException("countPrefixJar");
            return new RepeatBasedOnPrefixJar<T>(countPrefixJar, itemJar.Bulk());
        }
        /// <summary>Returns a Jar that first parses a size then parses an item that should consume exactly that amount of data.</summary>
        public static IJar<T> DataSizePrefixed<T>(this IJar<T> itemJar, IJar<int> dataSizePrefixJar) {
            if (itemJar == null) throw new ArgumentNullException("itemJar");
            if (dataSizePrefixJar == null) throw new ArgumentNullException("dataSizePrefixJar");
            return new DataSizePrefixedJar<T>(dataSizePrefixJar, itemJar);
        }
        /// <summary>
        /// Returns a Jar that repeatedly uses an item Jar until there's no data left and puts the resulting items into an array.
        /// If the Jar encounters a partial value at the end of the data, the entire parse fails.
        /// </summary>
        public static IJar<IReadOnlyList<T>> RepeatUntilEndOfData<T>(this IJar<T> itemJar) {
            if (itemJar == null) throw new ArgumentNullException("itemJar");

            var bulkItemJar = itemJar.Bulk();

            var n = itemJar.OptionalConstantSerializedLength();
            if (!n.HasValue) {
                return new RepeatUntilEndOfDataJar<T>(bulkItemJar);
            }

            var itemLength = n.Value;
            var counter = new AnonymousJar<int>(
                data => {
                    if (data.Count % itemLength != 0) throw new InvalidOperationException("Fragment");
                    return new ParsedValue<int>(data.Count / itemLength, 0);
                },
                item => new byte[0],
                canBeFollowed: false);
            return new RepeatBasedOnPrefixJar<T>(
                counter,
                bulkItemJar);
        }

        /// <summary>
        /// Returns a Jar that transforms values after parsing and before packing.
        /// </summary>
        public static IJar<TParsed> Select<TPacked, TParsed>(this IJar<TPacked> jar, Func<TPacked, TParsed> parseProjection, Func<TParsed, TPacked> packProjection) {
            if (jar == null) throw new ArgumentNullException("jar");
            if (parseProjection == null) throw new ArgumentNullException("parseProjection");
            if (packProjection == null) throw new ArgumentNullException("packProjection");
            return new AnonymousJar<TParsed>(
                data => jar.Parse(data).Select(parseProjection),
                e => jar.Pack(packProjection(e)),
                jar.CanBeFollowed);
        }

        /// <summary>
        /// Returns a Jar that applies the given Jar, but fails if running the value through the given constraint function does not return true.
        /// </summary>
        public static IJar<T> Where<T>(this IJar<T> jar, Func<T, bool> constraint) {
            if (jar == null) throw new ArgumentNullException("jar");
            if (constraint == null) throw new ArgumentNullException("constraint");
            return new AnonymousJar<T>(data => {
                var v = jar.Parse(data);
                if (!constraint(v.Value)) throw new InvalidOperationException("Data did not match Where constraint");
                return v;
            }, item => {
                if (!constraint(item)) throw new InvalidOperationException("Data did not match Where constraint");
                return jar.Pack(item);
            }, jar.CanBeFollowed);
        }

        /// <summary>
        /// A jar that consumes no data but always returns/expects the same value.
        /// </summary>
        public static IJar<T> Constant<T>(T constantValue) {
            return new ConstantJar<T>(constantValue);
        }
    }
}
