using System;

public interface IFieldParserOfUnknownType {
    string Name { get; }
    Type ParserValueType { get; }
    object Parser {get;}
    bool IsBlittable { get; }
    int? OptionalConstantSerializedLength { get; }
}
public sealed class FieldParserOfUnknownType<T> : IFieldParserOfUnknownType {
    public readonly IParser<T> Parser;
    public string Name { get; private set; }

    public Type ParserValueType { get { return typeof(T); } }
    object IFieldParserOfUnknownType.Parser { get { return Parser; } }
    public bool IsBlittable { get { return Parser.IsBlittable; } }
    public int? OptionalConstantSerializedLength { get { return Parser.OptionalConstantSerializedLength; } }
    
    public FieldParserOfUnknownType(IParser<T> parser, string name) {
        Parser = parser;
        Name = name;
    }
}
