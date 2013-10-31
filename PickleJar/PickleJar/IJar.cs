using System;

namespace Strilanc.PickleJar {
    /// <summary>
    /// Pickes values: serializes and deserializes values of type <see cref="T" /> into and out of a binary form.
    /// </summary>
    /// <typeparam name="T">The type of value serialized and deserialized by this jar.</typeparam>
    public interface IJar<T> {
        /// <summary>
        /// Deserializes a value out of the given data, and returns that value as well as how many bytes of the data were used.
        /// It is understood that parsing will use and consume data from the start of the segment, as opposed to the end or the middle or something else.
        /// </summary>
        /// <param name="data">The array segment of bytes to parse. The parser should not depend on data from the underlying array that is not within the segment.</param>
        /// <returns>The parsed value, as well as how many bytes of the data were used.</returns>
        ParsedValue<T> Parse(ArraySegment<byte> data);

        /// <summary>
        /// Serializes a value.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        /// <returns>The serialized form of the given value.</returns>
        byte[] Pack(T value);

        /// <summary>
        /// Determines if it's meaningful to have a jar that consumes data after the data consumed by this jar.
        /// For example, a jar that always consumes all data can't be followed because the next jar would have no data to consume.
        /// Basically, if the jar is bounded by the length of the data instead of the data itself, then it can't be followed.
        /// </summary>
        bool CanBeFollowed { get; }
    }
}