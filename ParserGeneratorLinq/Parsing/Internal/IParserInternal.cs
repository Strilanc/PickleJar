using System.Linq.Expressions;

namespace Strilanc.Parsing.Internal {
    public interface IParserInternal<T> : IParser<T> {
        bool IsBlittable { get; }
        int? OptionalConstantSerializedLength { get; }

        Expression TryMakeParseFromDataExpression(Expression array, Expression offset, Expression count);
        Expression TryMakeGetValueFromParsedExpression(Expression parsed);
        Expression TryMakeGetCountFromParsedExpression(Expression parsed);
    }
}