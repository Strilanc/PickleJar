using System;

namespace Strilanc.Parsing.Internal {
    internal interface IArrayParser<T> {
        ParsedValue<T[]> Parse(ArraySegment<byte> data, int count);
        bool IsValueBlittable { get; }
        int? OptionalConstantSerializedValueLength { get; }
    }
}