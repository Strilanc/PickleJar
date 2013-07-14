using System;
using System.Linq.Expressions;

public interface IParser<T> {
    ParsedValue<T> Parse(ArraySegment<byte> data);
    bool IsBlittable { get; }
    int? OptionalConstantSerializedLength { get; }

    Expression TryMakeParseFromDataExpression(Expression array, Expression offset, Expression count);
    Expression TryMakeGetValueFromParsedExpression(Expression parsed);
    Expression TryMakeGetCountFromParsedExpression(Expression parsed);
}