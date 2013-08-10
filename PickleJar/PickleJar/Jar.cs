using System;
using System.Collections.Generic;
using System.Text;
using Strilanc.PickleJar.Internal;
using Strilanc.PickleJar.Internal.Values;
using Strilanc.PickleJar.Internal.Repeated;
using Strilanc.PickleJar.Internal.Structured;
using System.Linq;

namespace Strilanc.PickleJar {
    /// <summary>
    /// The Jar class exposes utilities for accessing, creating, and combining jars for pickling data (i.e. serialization and deserialization).
    /// </summary>
    public static partial class Jar {
        /// <summary>Pickles 8-bit signed integers against their 1 byte 2s-complement representation.</summary>
        public static IJar<sbyte> Int8 { get { return new Int8Jar(); } }
        /// <summary>Pickles 8-bit unsigned integers against their 1 byte 2s-complement representation.</summary>
        public static IJar<byte> UInt8 { get { return new UInt8Jar(); } }

        /// <summary>Pickles 16-bit signed integers against their 2 byte 2s-complement representation, with the least significant byte first.</summary>
        public static IJar<Int16> Int16LittleEndian { get { return new Int16Jar(Endianess.LittleEndian); } }
        /// <summary>Pickles 16-bit signed integers against their 2 byte 2s-complement representation, with the most significant byte first.</summary>
        public static IJar<Int16> Int16BigEndian { get { return new Int16Jar(Endianess.BigEndian); } }
        /// <summary>Pickles 16-bit unsigned integers against their 2 byte 2s-complement representation, with the least significant byte first.</summary>
        public static IJar<UInt16> UInt16LittleEndian { get { return new UInt16Jar(Endianess.LittleEndian); } }
        /// <summary>Pickles 16-bit unsigned integers against their 2 byte 2s-complement representation, with the most significant byte first.</summary>
        public static IJar<UInt16> UInt16BigEndian { get { return new UInt16Jar(Endianess.BigEndian); } }

        /// <summary>Pickles 32-bit signed integers against their 4 byte 2s-complement representation, with the least significant byte first.</summary>
        public static IJar<Int32> Int32LittleEndian { get { return new Int32Jar(Endianess.LittleEndian); } }
        /// <summary>Pickles 32-bit signed integers against their 4 byte 2s-complement representation, with the most significant byte first.</summary>
        public static IJar<Int32> Int32BigEndian { get { return new Int32Jar(Endianess.BigEndian); } }
        /// <summary>Pickles 32-bit unsigned integers against their 4 byte 2s-complement representation, with the least significant byte first.</summary>
        public static IJar<UInt32> UInt32LittleEndian { get { return new UInt32Jar(Endianess.LittleEndian); } }
        /// <summary>Pickles 32-bit unsigned integers against their 4 byte 2s-complement representation, with the most significant byte first.</summary>
        public static IJar<UInt32> UInt32BigEndian { get { return new UInt32Jar(Endianess.BigEndian); } }

        /// <summary>Pickles 64-bit signed integers against their 8 byte 2s-complement representation, with the least significant byte first.</summary>
        public static IJar<Int64> Int64LittleEndian { get { return new Int64Jar(Endianess.LittleEndian); } }
        /// <summary>Pickles 64-bit signed integers against their 8 byte 2s-complement representation, with the most significant byte first.</summary>
        public static IJar<Int64> Int64BigEndian { get { return new Int64Jar(Endianess.BigEndian); } }
        /// <summary>Pickles 64-bit unsigned integers against their 8 byte 2s-complement representation, with the least significant byte first.</summary>
        public static IJar<UInt64> UInt64LittleEndian { get { return new UInt64Jar(Endianess.LittleEndian); } }
        /// <summary>Pickles 64-bit unsigned integers against their 8 byte 2s-complement representation, with the most significant byte first.</summary>
        public static IJar<UInt64> UInt64BigEndian { get { return new UInt64Jar(Endianess.BigEndian); } }

        /// <summary>Pickles IEEE 32-bit single-precision floating point numbers into/outof the standard 4 byte representation, with the least significant byte first.</summary>
        public static IJar<float> Float32LittleEndian { get { return new Float32Jar(Endianess.LittleEndian); } }
        /// <summary>Pickles IEEE 32-bit single-precision floating point numbers into/outof the standard 4 byte representation, with the most significant byte first.</summary>
        public static IJar<float> Float32BigEndian { get { return new Float32Jar(Endianess.BigEndian); } }
        /// <summary>Pickles IEEE 64-bit double-precision floating point numbers into/outof the standard 8 byte representation, with the least significant byte first.</summary>
        public static IJar<double> Float64LittleEndian { get { return new Float64Jar(Endianess.LittleEndian); } }
        /// <summary>Pickles IEEE 64-bit double-precision floating point numbers into/outof the standard 8 byte representation, with the most significant byte first.</summary>
        public static IJar<double> Float64BigEndian { get { return new Float64Jar(Endianess.BigEndian); } }

        /// <summary>
        /// Pickles exactly one value into/outof no data.
        /// Parsing always consumes no data and returns the constant value.
        /// Packing fails if given a value besides the constant value, and otherwise succeeds with empty data.
        /// </summary>
        public static IJar<T> Constant<T>(T constantValue) {
            return new ConstantJar<T>(constantValue);
        }

        /// <summary>
        /// Pickles strings into/outof their UTF8 encoding.
        /// Fails with an exception when an encoding error is encountered.
        /// Consumes all data unless augmented with a method like NullTerminated or DataSizePrefixed.
        /// </summary>
        public static IJar<string> Utf8 {
            get {
                return new TextJar(Encoding.GetEncoding(Encoding.UTF8.WebName,
                                                        new EncoderExceptionFallback(),
                                                        new DecoderExceptionFallback()));
            }
        }
        /// <summary>
        /// Pickles strings into/outof their 7-bit ASCII encoding.
        /// Fails with an exception when an encoding error, such as a non-ascii character, is encountered.
        /// Consumes all data unless augmented with a method like NullTerminated or DataSizePrefixed.
        /// </summary>
        public static IJar<string> Ascii {
            get {
                return new TextJar(Encoding.GetEncoding(Encoding.ASCII.WebName,
                                                        new EncoderExceptionFallback(),
                                                        new DecoderExceptionFallback()));
            }
        }
        /// <summary>
        /// Pickles strings into/outof their representation in the given encoding.
        /// Consumes all data unless augmented with a method like NullTerminated or DataSizePrefixed.
        /// </summary>
        public static IJar<string> TextJar(Encoding encoding) {
            if (encoding == null) throw new ArgumentNullException("encoding");
            return new TextJar(encoding);
        }

        /// <summary>
        /// Augments a jar to pickle values into/outof a null-terminated representation.
        /// When packing, appends a zero byte after the serialized form.
        /// When parsing, looks for the zero byte and ensures all the data up to it (and no more) is parsed.
        /// </summary>
        public static IJar<T> NullTerminated<T>(this IJar<T> itemJar) {
            if (itemJar == null) throw new ArgumentNullException("itemJar");
            return new NullTerminatedJar<T>(itemJar);
        }

        /// <summary>
        /// Augments a jar to pickle values into/outof a representation prefixed by its size.</summary>
        public static IJar<T> DataSizePrefixed<T>(this IJar<T> itemJar, IJar<int> dataSizePrefixJar, bool includePrefixInSize) {
            if (itemJar == null) throw new ArgumentNullException("itemJar");
            if (dataSizePrefixJar == null) throw new ArgumentNullException("dataSizePrefixJar");
            return new DataSizePrefixedJar<T>(dataSizePrefixJar, itemJar, includePrefixInSize);
        }
        
        /// <summary>
        /// Augments a jar to pickle lists of a constant number of values into/outof the representations of those values one after another.
        /// </summary>
        public static IJar<IReadOnlyList<T>> RepeatNTimes<T>(this IJar<T> itemJar, int constantRepeatCount) {
            if (itemJar == null) throw new ArgumentNullException("itemJar");
            if (!itemJar.CanBeFollowed) throw new ArgumentException("!itemJar.CanBeFollowed", "itemJar");
            if (constantRepeatCount < 0) throw new ArgumentOutOfRangeException("constantRepeatCount");
            return new RepeatConstantNumberOfTimesJar<T>(itemJar.Bulk(), constantRepeatCount);
        }

        /// <summary>
        /// Augments a jar to pickle lists of values into/outof the representations of those values one after another, prefixed by a count pickled with another jar.
        /// </summary>
        public static IJar<IReadOnlyList<T>> RepeatCountPrefixTimes<T>(this IJar<T> itemJar, IJar<int> countPrefixJar) {
            if (itemJar == null) throw new ArgumentNullException("itemJar");
            if (!itemJar.CanBeFollowed) throw new ArgumentException("!itemJar.CanBeFollowed", "itemJar");
            if (countPrefixJar == null) throw new ArgumentNullException("countPrefixJar");
            return new RepeatBasedOnPrefixJar<T>(countPrefixJar, itemJar.Bulk());
        }

        /// <summary>
        /// Augments a jar to pickle lists of values into/outof the representations of those values one after another, until the end of the data.
        /// Consumes all data unless augmented with a method like NullTerminated or DataSizePrefixed.
        /// </summary>
        public static IJar<IReadOnlyList<T>> RepeatUntilEndOfData<T>(this IJar<T> itemJar) {
            if (itemJar == null) throw new ArgumentNullException("itemJar");
            if (!itemJar.CanBeFollowed) throw new ArgumentException("!itemJar.CanBeFollowed", "itemJar");

            var bulkItemJar = itemJar.Bulk();

            var n = itemJar.OptionalConstantSerializedLength();
            if (!n.HasValue) {
                return new RepeatUntilEndOfDataJar<T>(bulkItemJar);
            }

            // when the serialized item always has the same size, we can compute the count ahead of time
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
        /// Wraps a jar to transform values before pickling them.
        /// When parsing, the given jar parses out an internal value which is transformed into a value of the exposed type.
        /// When packing, the exposed value is transformed into a value of the internal type and then packed by the given jar.
        /// </summary>
        public static IJar<TExposed> Select<TInternal, TExposed>(this IJar<TInternal> jar, Func<TInternal, TExposed> parseProjection, Func<TExposed, TInternal> packProjection) {
            if (jar == null) throw new ArgumentNullException("jar");
            if (parseProjection == null) throw new ArgumentNullException("parseProjection");
            if (packProjection == null) throw new ArgumentNullException("packProjection");
            return new AnonymousJar<TExposed>(
                data => jar.Parse(data).Select(parseProjection),
                e => jar.Pack(packProjection(e)),
                jar.CanBeFollowed);
        }

        /// <summary>
        /// Augments a jar with a constraint.
        /// Parsing and packing fail when the involved value doesn't match the constraint.
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
        /// Creates a jar that pickles values of type T into/outof the representations of the given members one after another.
        /// </summary>
        public static IJar<T> BuildJarForType<T>(IEnumerable<IJarForMember> jarsForMembers) {
            if (jarsForMembers == null) throw new ArgumentNullException("jarsForMembers");
            var list = jarsForMembers.ToArray();
            return (IJar<T>)TypeJarBlit<T>.TryMake(list) 
                ?? new TypeJarCompiled<T>(list);
        }

        /// <summary>
        /// Creates a jar that pickles two values into/outof the representations of the given jars one after another.
        /// </summary>
        public static IJar<Tuple<T1, T2>> Then<T1, T2>(this IJar<T1> jar1, IJar<T2> jar2) {
            if (jar1 == null) throw new ArgumentNullException("jar1");
            if (jar2 == null) throw new ArgumentNullException("jar2");
            if (!jar1.CanBeFollowed) throw new ArgumentException("!jar1.CanBeFollowed", "jar1");

            return new TupleJar<T1, T2>(jar1, jar2);
        }
    }
}
