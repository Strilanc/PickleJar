using System.Linq.Expressions;

namespace Strilanc.PickleJar.Internal.StructuredParsers {
    internal interface IFieldParserInternal : IFieldParser {
        bool AreMemoryAndSerializedRepresentationsOfValueGuaranteedToMatch { get; }
        int? OptionalConstantSerializedLength { get; }

        InlinedParserComponents TryMakeInlinedParserComponents(Expression array, Expression offset, Expression count);
    }
}