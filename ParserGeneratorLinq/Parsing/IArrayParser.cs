using System;

internal interface IArrayParser<T> {
    ParsedValue<T[]> Parse(ArraySegment<byte> data, int count);
    bool IsValueBlittable { get; }
    int? OptionalConstantSerializedValueLength { get; }
}