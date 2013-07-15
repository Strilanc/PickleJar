using System;

namespace Strilanc.Parsing.Internal.Misc {
    internal interface IArrayParser<T> {
        ParsedValue<T[]> Parse(ArraySegment<byte> data, int count);
        bool IsValueBlittable { get; }
        int? OptionalConstantSerializedValueLength { get; }
    }
}