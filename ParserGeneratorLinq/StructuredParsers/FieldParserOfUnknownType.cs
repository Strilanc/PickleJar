using System;
using System.Linq.Expressions;
using Strilanc.Parsing.Misc;

namespace Strilanc.Parsing.StructuredParsers {
    internal sealed class FieldParserOfUnknownType<T> : IFieldParserOfUnknownType {
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
}
