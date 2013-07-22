using System;
using System.Collections.Generic;

namespace Strilanc.PickleJar.Internal.Bulk {
    /// <summary>
    /// IBulkJar is implemented by types capable of efficiently parsing multiple contiguous values.
    /// </summary>
    internal interface IBulkJar<T> {
        IJar<T> ItemJar { get; }
        ParsedValue<IReadOnlyList<T>> Parse(ArraySegment<byte> data, int count);
        int? OptionalConstantSerializedValueLength { get; }
        byte[] Pack(IReadOnlyCollection<T> values);
    }
}