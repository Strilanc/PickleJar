using System;
using System.Linq.Expressions;

public interface IFieldParserOfUnknownType {
    string Name { get; }
    Type ParserValueType { get; }
    object Parser {get;}
    bool IsBlittable { get; }
    int? OptionalConstantSerializedLength { get; }

    Expression MakeParseFromDataExpression(Expression array, Expression offset, Expression count);
    Expression MakeGetValueFromParsedExpression(Expression parsed);
    Expression MakeGetCountFromParsedExpression(Expression parsed);
}