using System;
using System.Linq.Expressions;

namespace Strilanc.PickleJar.Internal.StructuredParsers {
    /// <summary>
    /// FieldParser names and exposes a generic parser with known type as a non-generic parser of unknown type.
    /// It is used as part of parsing types via reflection: each of the type's fields must be matched to a corresponding field parser.
    /// </summary>
    internal sealed class FieldParser<T> : IFieldParserInternal {
        public readonly IParser<T> Parser;
        public CanonicalizingMemberName CanonicalName { get; private set; }

        public Type ParserValueType { get { return typeof(T); } }
        object IFieldParser.Parser { get { return Parser; } }
        public bool AreMemoryAndSerializedRepresentationsOfValueGuaranteedToMatch { get { return Parser.AreMemoryAndSerializedRepresentationsOfValueGuaranteedToMatch(); } }
        public int? OptionalConstantSerializedLength { get { return Parser.OptionalConstantSerializedLength(); } }
        public InlinedParserComponents TryMakeInlinedParserComponents(Expression array, Expression offset, Expression count) {
            return Parser.MakeInlinedParserComponents(array, offset, count);
        }

        public FieldParser(IParser<T> parser, CanonicalizingMemberName name) {
            Parser = parser;
            CanonicalName = name;
        }
    }
}
