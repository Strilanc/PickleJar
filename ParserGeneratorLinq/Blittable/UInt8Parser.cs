using System;
using System.Linq;
using System.Linq.Expressions;

namespace ParserGenerator {
    public struct UInt8Parser : IParser<byte> {
        public bool IsBlittable { get { return true; } }
        public int? OptionalConstantSerializedLength { get { return 1; } }
        public Expression TryParseInline(Expression array, Expression offset, Expression count) {
            return Expression.New(typeof(ParsedValue<byte>).GetConstructors().Single(),
                Expression.ArrayAccess(array, offset),
                Expression.Constant(1));
        }
        public ParsedValue<byte> Parse(ArraySegment<byte> data) {
            var value = data.Array[data.Offset];
            return new ParsedValue<byte>(value, 1);
        }
    }
}
