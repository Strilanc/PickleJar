using System;

namespace Strilanc.PickleJar {
    public interface IJar<T> {
        ParsedValue<T> Parse(ArraySegment<byte> data);
        byte[] Pack(T value);

        /// <summary>
        /// Determines if it's meaningful to have a jar after this jar.
        /// For example, a jar that always consumes all data can't be followed because the next jar would have no data to consume.
        /// Basically, if the jar is bounded by the end of the data instead of the data itself, then it can't be followed.
        /// </summary>
        bool CanBeFollowed { get; }
    }
}