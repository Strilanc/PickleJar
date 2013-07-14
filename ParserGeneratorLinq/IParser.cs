using System;
using System.Linq.Expressions;

public interface IArrayParser<T> {
    ParsedValue<T[]> Parse(ArraySegment<byte> data, int count);
    bool IsValueBlittable { get; }
    int? OptionalConstantSerializedValueLength { get; }
}

public interface IParser<T> {
    ParsedValue<T> Parse(ArraySegment<byte> data);
    bool IsBlittable { get; }
    int? OptionalConstantSerializedLength { get; }
    Expression TryParseInline(Expression array, Expression offset, Expression count);
}