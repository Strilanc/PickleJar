using System.Linq.Expressions;

namespace Strilanc.PickleJar.Internal {
    /// <summary>
    /// IParserInternal exposes the information used to optimize parsers.
    /// A parser that also implements IParserInternal can take advantage of optimizations such as being inlined inside other parsers.
    /// </summary>
    internal interface IParserInternal<T> : IParser<T> {
        /// <summary>
        /// Determines if this parser's value's serialized representation is guaranteed to be the same its memory representation.
        /// When true, it may be possible to replace the parser with one that simply does a memcpy.
        /// </summary>
        bool AreMemoryAndSerializedRepresentationsOfValueGuaranteedToMatch { get; }
        /// <summary>
        /// Determines if this parser is guaranteed to always consume the same number of bytes.
        /// If the result is non-null, it is the guaranteed constant number of bytes consumed in every parse.
        /// If the result is null, there is no guarantee that the number of bytes is constant or known.
        /// </summary>
        int? OptionalConstantSerializedLength { get; }

        /// <summary>
        /// Returns the components used to inline a parser, or else null if this parser doesn't support inlining.
        /// </summary>
        InlinedParserComponents TryMakeInlinedParserComponents(Expression array, Expression offset, Expression count);
    }
}