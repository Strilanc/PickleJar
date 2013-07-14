using System;

public interface IArrayParser<T> {
    ParsedValue<T[]> Parse(ArraySegment<byte> data, int count);
    bool IsValueBlittable { get; }
    int? OptionalConstantSerializedValueLength { get; }
}

public interface IParser<T> {
    ParsedValue<T> Parse(ArraySegment<byte> data);
    bool IsBlittable { get; }
    int? OptionalConstantSerializedLength { get; }
}