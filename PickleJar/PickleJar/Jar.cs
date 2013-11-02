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
        public static IJar<sbyte> Int8 { get { return NumericJarUtil.MakeStandardNumericJar<sbyte>(0); } }
        /// <summary>Pickles 8-bit unsigned integers against their 1 byte 2s-complement representation.</summary>
        public static IJar<byte> UInt8 { get { return NumericJarUtil.MakeStandardNumericJar<byte>(0); } }

        /// <summary>Pickles 16-bit signed integers against their 2 byte 2s-complement representation, with the least significant byte first.</summary>
        public static IJar<Int16> Int16LittleEndian { get { return NumericJarUtil.MakeStandardNumericJar<Int16>(Endianess.LittleEndian); } }
        /// <summary>Pickles 16-bit signed integers against their 2 byte 2s-complement representation, with the most significant byte first.</summary>
        public static IJar<Int16> Int16BigEndian { get { return NumericJarUtil.MakeStandardNumericJar<Int16>(Endianess.BigEndian); } }
        /// <summary>Pickles 16-bit unsigned integers against their 2 byte 2s-complement representation, with the least significant byte first.</summary>
        public static IJar<UInt16> UInt16LittleEndian { get { return NumericJarUtil.MakeStandardNumericJar<UInt16>(Endianess.LittleEndian); } }
        /// <summary>Pickles 16-bit unsigned integers against their 2 byte 2s-complement representation, with the most significant byte first.</summary>
        public static IJar<UInt16> UInt16BigEndian { get { return NumericJarUtil.MakeStandardNumericJar<UInt16>(Endianess.BigEndian); } }

        /// <summary>Pickles 32-bit signed integers against their 4 byte 2s-complement representation, with the least significant byte first.</summary>
        public static IJar<Int32> Int32LittleEndian { get { return NumericJarUtil.MakeStandardNumericJar<Int32>(Endianess.LittleEndian); } }
        /// <summary>Pickles 32-bit signed integers against their 4 byte 2s-complement representation, with the most significant byte first.</summary>
        public static IJar<Int32> Int32BigEndian { get { return NumericJarUtil.MakeStandardNumericJar<Int32>(Endianess.BigEndian); } }
        /// <summary>Pickles 32-bit unsigned integers against their 4 byte 2s-complement representation, with the least significant byte first.</summary>
        public static IJar<UInt32> UInt32LittleEndian { get { return NumericJarUtil.MakeStandardNumericJar<UInt32>(Endianess.LittleEndian); } }
        /// <summary>Pickles 32-bit unsigned integers against their 4 byte 2s-complement representation, with the most significant byte first.</summary>
        public static IJar<UInt32> UInt32BigEndian { get { return NumericJarUtil.MakeStandardNumericJar<UInt32>(Endianess.BigEndian); } }

        /// <summary>Pickles 64-bit signed integers against their 8 byte 2s-complement representation, with the least significant byte first.</summary>
        public static IJar<Int64> Int64LittleEndian { get { return NumericJarUtil.MakeStandardNumericJar<Int64>(Endianess.LittleEndian); } }
        /// <summary>Pickles 64-bit signed integers against their 8 byte 2s-complement representation, with the most significant byte first.</summary>
        public static IJar<Int64> Int64BigEndian { get { return NumericJarUtil.MakeStandardNumericJar<Int64>(Endianess.BigEndian); } }
        /// <summary>Pickles 64-bit unsigned integers against their 8 byte 2s-complement representation, with the least significant byte first.</summary>
        public static IJar<UInt64> UInt64LittleEndian { get { return NumericJarUtil.MakeStandardNumericJar<UInt64>(Endianess.LittleEndian); } }
        /// <summary>Pickles 64-bit unsigned integers against their 8 byte 2s-complement representation, with the most significant byte first.</summary>
        public static IJar<UInt64> UInt64BigEndian { get { return NumericJarUtil.MakeStandardNumericJar<UInt64>(Endianess.BigEndian); } }

        /// <summary>Pickles IEEE 32-bit single-precision floating point numbers into/outof the standard 4 byte representation, with the least significant byte first.</summary>
        public static IJar<float> Float32LittleEndian { get { return NumericJarUtil.MakeStandardNumericJar<float>(Endianess.LittleEndian); } }
        /// <summary>Pickles IEEE 32-bit single-precision floating point numbers into/outof the standard 4 byte representation, with the most significant byte first.</summary>
        public static IJar<float> Float32BigEndian { get { return NumericJarUtil.MakeStandardNumericJar<float>(Endianess.BigEndian); } }
        /// <summary>Pickles IEEE 64-bit double-precision floating point numbers into/outof the standard 8 byte representation, with the least significant byte first.</summary>
        public static IJar<double> Float64LittleEndian { get { return NumericJarUtil.MakeStandardNumericJar<double>(Endianess.LittleEndian); } }
        /// <summary>Pickles IEEE 64-bit double-precision floating point numbers into/outof the standard 8 byte representation, with the most significant byte first.</summary>
        public static IJar<double> Float64BigEndian { get { return NumericJarUtil.MakeStandardNumericJar<double>(Endianess.BigEndian); } }

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
                canBeFollowed: true,
                isBlittable: false,
                optionalConstantSerializedLength: null,
                tryInlinedParserComponents: null);
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
                jar.CanBeFollowed,
                isBlittable: false,
                optionalConstantSerializedLength: jar.OptionalConstantSerializedLength(),
                tryInlinedParserComponents: null);
        }

        /// <summary>
        /// Augments a jar with a constraint.
        /// Parsing and packing fail when the involved value doesn't match the constraint.
        /// </summary>
        public static IJar<T> Where<T>(this IJar<T> jar, Func<T, bool> constraint) {
            if (jar == null) throw new ArgumentNullException("jar");
            if (constraint == null) throw new ArgumentNullException("constraint");
            return new AnonymousJar<T>(
                data => {
                    var v = jar.Parse(data);
                    if (!constraint(v.Value)) throw new InvalidOperationException("Data did not match Where constraint");
                    return v;
                },
                item => {
                    if (!constraint(item)) throw new InvalidOperationException("Data did not match Where constraint");
                    return jar.Pack(item);
                },
                jar.CanBeFollowed,
                isBlittable: false,
                optionalConstantSerializedLength: jar.OptionalConstantSerializedLength(),
                tryInlinedParserComponents: null);
        }

        /// <summary>
        /// Creates a jar that pickles values of type T into/outof the representations of the given members one after another.
        /// </summary>
        public static IJar<T> BuildJarForType<T>(this IEnumerable<IJarForMember> jarsForMembers) {
            if (jarsForMembers == null) throw new ArgumentNullException("jarsForMembers");
            var list = jarsForMembers.ToArray();
            return TypeJarBlit<T>.TryMake(list) ?? TypeJarCompiled.MakeBySequenceAndInject<T>(list);
        }

        public static IJar<T> BuildJarForType<T>(this IEnumerable<NamedJarList.Entry> namedJars) {
            return namedJars.Select(e => e.ToJarForMember()).BuildJarForType<T>();
        }

        public static IJar<IReadOnlyDictionary<string, object>> ToDictionaryJar(this IEnumerable<NamedJarList.Entry> keyedJars) {
            return keyedJars.Select(e => new KeyValuePair<string, IJar<object>>(e.Name, e.JarAsObjectJar())).ToDictionaryJar();
        }
        public static IJar<IReadOnlyDictionary<TKey, TValue>> ToDictionaryJar<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, IJar<TValue>>> keyedJars) {
            if (keyedJars == null) throw new ArgumentNullException("keyedJars");
            return new DictionaryJar<TKey, TValue>(keyedJars.Select(e => new KeyValueJar<TKey, TValue>(e.Key, e.Value)));
        }

        public static IJar<IReadOnlyList<T>> ToListJar<T>(this IEnumerable<IJar<T>> jars) {
            if (jars == null) throw new ArgumentNullException("jars");
            var jarsCached = jars.ToArray();
            if (jarsCached.Take(jarsCached.Length - 1).Any(e => !e.CanBeFollowed)) throw new ArgumentException("!jar.CanBeFollowed");
            return SequencedJarUtil.MakeSequencedJar(jarsCached);
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

        /// <summary>
        /// Creates an 'anonymous' jar, with methods implemented by the given delegates.
        /// </summary>
        /// <typeparam name="T">The type of values parsed by the created jar.</typeparam>
        /// <param name="parse">The parse method, which deserializes data into a value, of the created jar.</param>
        /// <param name="pack">The pack method, which serializes values, of the created jar.</param>
        /// <param name="canBeFollowed">
        /// True when the serialized data can still be parsed if it is followed by other serialized data.
        /// For example, should be false if the jar always consumes all data when parsing (since appended data would be consumed, affecting the serialized value and breaking round-tripping).
        /// </param>
        /// <returns>The created anonymous jar.</returns>
        public static IJar<T> Create<T>(Func<ArraySegment<byte>, ParsedValue<T>> parse, Func<T, byte[]> pack, bool canBeFollowed) {
            if (parse == null) throw new ArgumentNullException("parse");
            if (pack == null) throw new ArgumentNullException("pack");
            return new AnonymousJar<T>(parse, pack, canBeFollowed, isBlittable: false, optionalConstantSerializedLength: null, tryInlinedParserComponents: null);
        }
    }
}
