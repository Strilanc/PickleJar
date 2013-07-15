using System;
using System.Linq.Expressions;

namespace Strilanc.Parsing.Internal.StructuredParsers {
    /// <summary>
    /// FieldParserOfUnknownType names and exposes a generic parser with known type as a non-generic parser of unknown type.
    /// It is used as part of parsing types via reflection: each of the type's fields must be matched to a corresponding field parser.
    /// </summary>
    internal sealed class FieldParserOfUnknownType<T> : IFieldParserOfUnknownType {
        public readonly IParser<T> Parser;
        public string Name { get; private set; }

        public Type ParserValueType { get { return typeof(T); } }
        object IFieldParserOfUnknownType.Parser { get { return Parser; } }
        public bool IsBlittable { get { return Parser.IsMemoryRepresentationGuaranteedToMatch(); } }
        public int? OptionalConstantSerializedLength { get { return Parser.OptionalConstantSerializedLength(); } }
        public Expression MakeParseFromDataExpression(Expression array, Expression offset, Expression count) {
            return Parser.MakeParseFromDataExpression(array, offset, count);
        }
        public Expression MakeGetValueFromParsedExpression(Expression parsed) {
            return Parser.MakeGetValueFromParsedExpression(parsed);
        }
        public Expression MakeGetCountFromParsedExpression(Expression parsed) {
            return Parser.MakeGetConsumedFromParsedExpression(parsed);
        }

        public FieldParserOfUnknownType(IParser<T> parser, string name) {
            Parser = parser;
            Name = name;
        }
    }
}
