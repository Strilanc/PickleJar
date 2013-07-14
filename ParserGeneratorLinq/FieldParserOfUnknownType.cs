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
public static class ParserUtil {
    public static Expression MakeParseFromDataExpression<T>(this IParser<T> parser, Expression array, Expression offset, Expression count) {
        return parser.TryMakeParseFromDataExpression(array, offset, count)
            ?? Expression.Call(
                    Expression.Constant(parser),
                    typeof(IParser<T>).GetMethod("Parse"),
                    new Expression[] { 
                        Expression.New(
                            typeof(ArraySegment<byte>).GetConstructor(new[] {typeof(byte[]), typeof(int), typeof(int)}), 
                            new[] {array, offset, count}) });
    }

    public static Expression MakeGetValueFromParsedExpression<T>(this IParser<T> parser, Expression parsed) {
        return parser.TryMakeGetValueFromParsedExpression(parsed)
            ?? Expression.MakeMemberAccess(parsed, typeof(ParsedValue<>).MakeGenericType(typeof(T)).GetField("Value"));
    }
    public static Expression MakeGetCountFromParsedExpression<T>(this IParser<T> parser, Expression parsed) {
        return parser.TryMakeGetCountFromParsedExpression(parsed)
            ?? Expression.MakeMemberAccess(parsed, typeof(ParsedValue<>).MakeGenericType(typeof(T)).GetField("Consumed"));
    }    
}
public sealed class FieldParserOfUnknownType<T> : IFieldParserOfUnknownType {
    public readonly IParser<T> Parser;
    public string Name { get; private set; }

    public Type ParserValueType { get { return typeof(T); } }
    object IFieldParserOfUnknownType.Parser { get { return Parser; } }
    public bool IsBlittable { get { return Parser.IsBlittable; } }
    public int? OptionalConstantSerializedLength { get { return Parser.OptionalConstantSerializedLength; } }
    public Expression MakeParseFromDataExpression(Expression array, Expression offset, Expression count) {
        return Parser.MakeParseFromDataExpression(array, offset, count);
    }
    public Expression MakeGetValueFromParsedExpression(Expression parsed) {
        return Parser.MakeGetValueFromParsedExpression(parsed);
    }
    public Expression MakeGetCountFromParsedExpression(Expression parsed) {
        return Parser.MakeGetCountFromParsedExpression(parsed);
    }

    public FieldParserOfUnknownType(IParser<T> parser, string name) {
        Parser = parser;
        Name = name;
    }
}
