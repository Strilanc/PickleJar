using System;

namespace Strilanc.Parsing.Internal {
    /// <summary>
    /// IBulkParser is implemented by types capable of efficiently parsing multiple contiguous values.
    /// </summary>
    internal interface IBulkParser<T> {
        ParsedValue<T[]> Parse(ArraySegment<byte> data, int count);
        int? OptionalConstantSerializedValueLength { get; }
    }
}