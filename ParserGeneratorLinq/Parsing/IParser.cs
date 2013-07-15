using System;
using System.Linq.Expressions;

namespace Strilanc.Parsing {
    public interface IParser<T> {
        ParsedValue<T> Parse(ArraySegment<byte> data);
    }
    public interface IParserInternal<T> : IParser<T> {
        bool IsBlittable { get; }
        int? OptionalConstantSerializedLength { get; }

        Expression TryMakeParseFromDataExpression(Expression array, Expression offset, Expression count);
        Expression TryMakeGetValueFromParsedExpression(Expression parsed);
        Expression TryMakeGetCountFromParsedExpression(Expression parsed);
    }
}