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
public sealed class FieldParserOfUnknownType<T> : IFieldParserOfUnknownType {
    public readonly IParser<T> Parser;
    public string Name { get; private set; }

    public Type ParserValueType { get { return typeof(T); } }
    object IFieldParserOfUnknownType.Parser { get { return Parser; } }
    public bool IsBlittable { get { return Parser.IsBlittable; } }
    public int? OptionalConstantSerializedLength { get { return Parser.OptionalConstantSerializedLength; } }
    public Expression MakeParseFromDataExpression(Expression array, Expression offset, Expression count) {
        return Parser.TryMakeParseFromDataExpression(array, offset, count)
            ?? Expression.Call(
                    Expression.Constant(Parser),
                    Parser.GetType().GetMethod("Parse"),
                    new Expression[] { 
                        Expression.New(
                            typeof(ArraySegment<byte>).GetConstructor(new[] {typeof(byte[]), typeof(int), typeof(int)}), 
                            new[] {array, offset, count}) });
    }
    public Expression MakeGetValueFromParsedExpression(Expression parsed) {
        return Parser.TryMakeGetValueFromParsedExpression(parsed)
            ?? Expression.MakeMemberAccess(parsed, typeof(ParsedValue<>).MakeGenericType(typeof(T)).GetField("Value"));
    }
    public Expression MakeGetCountFromParsedExpression(Expression parsed) {
        return Parser.TryMakeGetCountFromParsedExpression(parsed)
            ?? Expression.MakeMemberAccess(parsed, typeof(ParsedValue<>).MakeGenericType(typeof(T)).GetField("Consumed"));
    }

    public FieldParserOfUnknownType(IParser<T> parser, string name) {
        Parser = parser;
        Name = name;
    }
}
