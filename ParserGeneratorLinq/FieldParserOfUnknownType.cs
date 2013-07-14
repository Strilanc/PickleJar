using System;
using System.Linq.Expressions;

public interface IFieldParserOfUnknownType {
    string Name { get; }
    Type ParserValueType { get; }
    object Parser {get;}
    bool IsBlittable { get; }
    int? OptionalConstantSerializedLength { get; }
    Expression TryParseInline(Expression array, Expression offset, Expression count);
}
public sealed class FieldParserOfUnknownType<T> : IFieldParserOfUnknownType {
    public readonly IParser<T> Parser;
    public string Name { get; private set; }

    public Type ParserValueType { get { return typeof(T); } }
    object IFieldParserOfUnknownType.Parser { get { return Parser; } }
    public bool IsBlittable { get { return Parser.IsBlittable; } }
    public int? OptionalConstantSerializedLength { get { return Parser.OptionalConstantSerializedLength; } }
    public Expression TryParseInline(Expression array, Expression offset, Expression count) {
        return Parser.TryParseInline(array, offset, count);
    }

    public FieldParserOfUnknownType(IParser<T> parser, string name) {
        Parser = parser;
        Name = name;
    }
}
