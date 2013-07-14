using System;
using System.Linq.Expressions;

namespace Strilanc.Parsing.NumberParsers {
    internal struct UInt8Parser : IParser<byte> {
        public bool IsBlittable { get { return true; } }
        public int? OptionalConstantSerializedLength { get { return 1; } }
        public ParsedValue<byte> Parse(ArraySegment<byte> data) {
            var value = data.Array[data.Offset];
            return new ParsedValue<byte>(value, 1);
        }

        public Expression TryMakeParseFromDataExpression(Expression array, Expression offset, Expression count) {
            return NumberParseBuilderUtil.MakeParseFromDataExpression<byte>(true, array, offset, count);
        }
        public Expression TryMakeGetValueFromParsedExpression(Expression parsed) {
            return NumberParseBuilderUtil.MakeGetValueFromParsedExpression(parsed);
        }
        public Expression TryMakeGetCountFromParsedExpression(Expression parsed) {
            return NumberParseBuilderUtil.MakeGetCountFromParsedExpression<byte>(parsed);
        }
    }
}
