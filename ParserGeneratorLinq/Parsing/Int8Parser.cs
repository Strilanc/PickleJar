using System;
using System.Linq.Expressions;

namespace ParserGenerator {
    internal struct Int8Parser : IParser<sbyte> {
        public bool IsBlittable { get { return true; } }
        public int? OptionalConstantSerializedLength { get { return 1; } }

        public ParsedValue<sbyte> Parse(ArraySegment<byte> data) {
            unchecked {
                var value = (sbyte)data.Array[data.Offset];
                return new ParsedValue<sbyte>(value, 1);
            }
        }

        public Expression TryMakeParseFromDataExpression(Expression array, Expression offset, Expression count) {
            return NumberParseBuilderUtil.MakeParseFromDataExpression<sbyte>(true, array, offset, count);
        }
        public Expression TryMakeGetValueFromParsedExpression(Expression parsed) {
            return NumberParseBuilderUtil.MakeGetValueFromParsedExpression(parsed);
        }
        public Expression TryMakeGetCountFromParsedExpression(Expression parsed) {
            return NumberParseBuilderUtil.MakeGetCountFromParsedExpression<sbyte>(parsed);
        }
    }
}
