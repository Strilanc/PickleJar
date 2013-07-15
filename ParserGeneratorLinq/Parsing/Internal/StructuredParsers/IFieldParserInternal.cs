using System.Linq.Expressions;

namespace Strilanc.Parsing.Internal.StructuredParsers {
    internal interface IFieldParserInternal : IFieldParser {
        bool AreMemoryAndSerializedRepresentationsOfValueGuaranteedToMatch { get; }
        int? OptionalConstantSerializedLength { get; }

        Expression TryMakeParseFromDataExpression(Expression array, Expression offset, Expression count);
        Expression TryMakeGetValueFromParsedExpression(Expression parsed);
        Expression TryMakeGetConsumedFromParsedExpression(Expression parsed);
    }
}