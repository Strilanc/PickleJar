using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using Strilanc.PickleJar.Internal;
using Strilanc.PickleJar.Internal.Basic;
using Strilanc.PickleJar.Internal.Combinators;
using Strilanc.PickleJar.Internal.Misc;
using Strilanc.PickleJar.Internal.RuntimeSpecialization;
using Strilanc.PickleJar.Internal.Unsafe;
using Strilanc.PickleJar.Internal.Structured;
using System.Linq;

namespace Strilanc.PickleJar {
    /// <summary>
    /// The Jar class exposes utilities for accessing, creating, and combining itemJars for pickling data (i.e. serialization and deserialization).
    /// </summary>
    public static partial class Jar {
        /// <summary>Pickles 8-bit signed integers against their 1 byte 2s-complement representation.</summary>
        public static IJar<sbyte> Int8 { get { return NumericJar.CreateForType<sbyte>(0); } }
        /// <summary>Pickles 8-bit unsigned integers against their 1 byte 2s-complement representation.</summary>
        public static IJar<byte> UInt8 { get { return NumericJar.CreateForType<byte>(0); } }

        /// <summary>Pickles 16-bit signed integers against their 2 byte 2s-complement representation, with the least significant byte first.</summary>
        public static IJar<Int16> Int16LittleEndian { get { return NumericJar.CreateForType<Int16>(Endianess.LittleEndian); } }
        /// <summary>Pickles 16-bit signed integers against their 2 byte 2s-complement representation, with the most significant byte first.</summary>
        public static IJar<Int16> Int16BigEndian { get { return NumericJar.CreateForType<Int16>(Endianess.BigEndian); } }
        /// <summary>Pickles 16-bit unsigned integers against their 2 byte 2s-complement representation, with the least significant byte first.</summary>
        public static IJar<UInt16> UInt16LittleEndian { get { return NumericJar.CreateForType<UInt16>(Endianess.LittleEndian); } }
        /// <summary>Pickles 16-bit unsigned integers against their 2 byte 2s-complement representation, with the most significant byte first.</summary>
        public static IJar<UInt16> UInt16BigEndian { get { return NumericJar.CreateForType<UInt16>(Endianess.BigEndian); } }

        /// <summary>Pickles 32-bit signed integers against their 4 byte 2s-complement representation, with the least significant byte first.</summary>
        public static IJar<Int32> Int32LittleEndian { get { return NumericJar.CreateForType<Int32>(Endianess.LittleEndian); } }
        /// <summary>Pickles 32-bit signed integers against their 4 byte 2s-complement representation, with the most significant byte first.</summary>
        public static IJar<Int32> Int32BigEndian { get { return NumericJar.CreateForType<Int32>(Endianess.BigEndian); } }
        /// <summary>Pickles 32-bit unsigned integers against their 4 byte 2s-complement representation, with the least significant byte first.</summary>
        public static IJar<UInt32> UInt32LittleEndian { get { return NumericJar.CreateForType<UInt32>(Endianess.LittleEndian); } }
        /// <summary>Pickles 32-bit unsigned integers against their 4 byte 2s-complement representation, with the most significant byte first.</summary>
        public static IJar<UInt32> UInt32BigEndian { get { return NumericJar.CreateForType<UInt32>(Endianess.BigEndian); } }

        /// <summary>Pickles 64-bit signed integers against their 8 byte 2s-complement representation, with the least significant byte first.</summary>
        public static IJar<Int64> Int64LittleEndian { get { return NumericJar.CreateForType<Int64>(Endianess.LittleEndian); } }
        /// <summary>Pickles 64-bit signed integers against their 8 byte 2s-complement representation, with the most significant byte first.</summary>
        public static IJar<Int64> Int64BigEndian { get { return NumericJar.CreateForType<Int64>(Endianess.BigEndian); } }
        /// <summary>Pickles 64-bit unsigned integers against their 8 byte 2s-complement representation, with the least significant byte first.</summary>
        public static IJar<UInt64> UInt64LittleEndian { get { return NumericJar.CreateForType<UInt64>(Endianess.LittleEndian); } }
        /// <summary>Pickles 64-bit unsigned integers against their 8 byte 2s-complement representation, with the most significant byte first.</summary>
        public static IJar<UInt64> UInt64BigEndian { get { return NumericJar.CreateForType<UInt64>(Endianess.BigEndian); } }

        /// <summary>Pickles IEEE 32-bit single-precision floating point numbers into/outof the standard 4 byte representation, with the least significant byte first.</summary>
        public static IJar<float> Float32LittleEndian { get { return NumericJar.CreateForType<float>(Endianess.LittleEndian); } }
        /// <summary>Pickles IEEE 32-bit single-precision floating point numbers into/outof the standard 4 byte representation, with the most significant byte first.</summary>
        public static IJar<float> Float32BigEndian { get { return NumericJar.CreateForType<float>(Endianess.BigEndian); } }
        /// <summary>Pickles IEEE 64-bit double-precision floating point numbers into/outof the standard 8 byte representation, with the least significant byte first.</summary>
        public static IJar<double> Float64LittleEndian { get { return NumericJar.CreateForType<double>(Endianess.LittleEndian); } }
        /// <summary>Pickles IEEE 64-bit double-precision floating point numbers into/outof the standard 8 byte representation, with the most significant byte first.</summary>
        public static IJar<double> Float64BigEndian { get { return NumericJar.CreateForType<double>(Endianess.BigEndian); } }

        /// <summary>
        /// Pickles exactly one value into/outof no data.
        /// Parsing always consumes no data and returns the constant value.
        /// Packing fails if given a value besides the constant value, and otherwise succeeds with empty data.
        /// </summary>
        public static IJar<T> Constant<T>(T constantValue) {
            return ConstantJar.Create(constantValue);
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
            return NullTerminatedJar.Create(itemJar);
        }

        /// <summary>
        /// Augments a jar to pickle values into/outof a representation prefixed by its size.</summary>
        public static IJar<T> DataSizePrefixed<T>(this IJar<T> itemJar, IJar<int> dataSizePrefixJar, bool includePrefixInSize) {
            return DataSizePrefixedJar.Create(dataSizePrefixJar, itemJar, includePrefixInSize);
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
            return RepeatUntilEndOfDataJarUtil.MakeRepeatUntilEndOfDataJar(bulkItemJar);
        }

        /// <summary>
        /// Wraps a jar to transform values before pickling them.
        /// When parsing, the given jar parses out an internal value which is transformed into a value of the exposed type.
        /// When packing, the exposed value is transformed into a value of the internal type and then packed by the given jar.
        /// </summary>
        public static IJar<TExposed> Select<TInternal, TExposed>(this IJar<TInternal> jar, Func<TInternal, TExposed> parseProjection, Func<TExposed, TInternal> packProjection) {
            return ProjectionJar.Create(jar, parseProjection, packProjection);
        }

        /// <summary>
        /// Augments a jar with a constraint.
        /// Parsing and packing fail when the involved value doesn't match the constraint.
        /// </summary>
        public static IJar<T> Where<T>(this IJar<T> jar, Func<T, bool> constraint) {
            return ConstraintJar.Create(jar, constraint);
        }

        public static IJar<T> WhereExpression<T>(this IJar<T> jar, Expression<Func<T, bool>> constraint) {
            return ConstraintJar.CreateSpecialized(jar, constraint);
        }

        /// <summary>
        /// Creates a jar that pickles values of type T into/outof the representations of the given members one after another.
        /// </summary>
        public static IJar<T> BuildJarForType<T>(this IEnumerable<IJarForMember> jarsForMembers) {
            if (jarsForMembers == null) throw new ArgumentNullException("jarsForMembers");
            var list = jarsForMembers.ToArray();
            return BlitJar<T>.TryMake(list) ?? RuntimeSpecializedJar.MakeBySequenceAndInject<T>(list);
        }

        public static IJar<T> BuildJarForType<T>(this IEnumerable<NamedJarList.Entry> namedJars) {
            return namedJars.Select(e => e.ToJarForMember()).BuildJarForType<T>();
        }

        public static IJar<IReadOnlyDictionary<string, object>> ToDictionaryJar(this IEnumerable<NamedJarList.Entry> keyedJars) {
            return keyedJars.Select(e => new KeyValuePair<string, IJar<object>>(e.Name, e.JarAsObjectJar())).ToDictionaryJar();
        }
        public static IJar<IReadOnlyDictionary<TKey, TValue>> ToDictionaryJar<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, IJar<TValue>>> keyedJars) {
            return DictionaryJar.Create(keyedJars);
        }

        public static IJar<IReadOnlyList<T>> ToListJar<T>(this IEnumerable<IJar<T>> jars) {
            return ListJar.Create(jars);
        }

        /// <summary>
        /// Creates a jar that pickles two values into/outof the representations of the given itemJars one after another.
        /// </summary>
        public static IJar<Tuple<T1, T2>> Then<T1, T2>(this IJar<T1> jar1, IJar<T2> jar2) {
            return TupleJar.Create(jar1, jar2);
        }

        /// <summary>
        /// Creates a jar that pickles three values into/outof the representations of the given itemJars one after another.
        /// </summary>
        public static IJar<Tuple<T1, T2, T3>> Then<T1, T2, T3>(this IJar<T1> jar1, IJar<T2> jar2, IJar<T3> jar3) {
            return TupleJar.Create(jar1, jar2, jar3);
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
        public static IJar<T> Create<T>(Func<ArraySegment<byte>, ParsedValue<T>> parse,
                                        Func<T, byte[]> pack,
                                        bool canBeFollowed) {
            if (parse == null) throw new ArgumentNullException("parse");
            if (pack == null) throw new ArgumentNullException("pack");
            return new AnonymousJar<T>(
                parse: parse,
                pack: pack,
                canBeFollowed: canBeFollowed,
                isBlittable: false,
                optionalConstantSerializedLength: null,
                trySpecializeParser: null);
        }
    }
}
