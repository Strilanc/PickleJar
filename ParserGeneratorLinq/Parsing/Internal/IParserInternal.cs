using System.Linq.Expressions;

namespace Strilanc.Parsing.Internal {
    /// <summary>
    /// IParserInternal exposes the information used to optimize parsers.
    /// A parser that also implements IParserInternal can take advantage of optimizations such as being inlined inside other parsers.
    /// </summary>
    public interface IParserInternal<T> : IParser<T> {
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
        /// Creates an expression tree which represents parsing a value.
        /// The result of the expression may be of any type, as long as using TryMakeGetValue/Count on it gives the components of the parsed result.
        /// For example, the expression may have type ParsedResult(int) and use the Value and Consumed members to get the components.
        /// Alternatively, the expression may have type Int32 and directly use that value and a constant 4 as the components.
        /// </summary>
        Expression TryMakeParseFromDataExpression(Expression array, Expression offset, Expression count);
        /// <summary>
        /// Extracts the Value component of what was parsed by TryMakeParseFromDataExpression.
        /// </summary>
        Expression TryMakeGetValueFromParsedExpression(Expression parsed);
        /// <summary>
        /// Extracts the Consumed component of what was parsed by TryMakeParseFromDataExpression.
        /// </summary>
        Expression TryMakeGetConsumedFromParsedExpression(Expression parsed);
    }
}