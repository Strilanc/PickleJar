using System;
using System.Linq.Expressions;

namespace Strilanc.Parsing.Internal.Misc {
    public sealed class SelectParser<T, R> : IParser<R> {
        public readonly IParser<T> SubParser;
        public readonly Expression<Func<T, R>> Proj;
        public bool IsBlittable { get { return false; } }
        public int? OptionalConstantSerializedLength { get { return null; } }

        public SelectParser(IParser<T> subParser, Expression<Func<T, R>> proj) {
            this.SubParser = subParser;
            this.Proj = proj;
        }
        public ParsedValue<R> Parse(ArraySegment<byte> data) {
            var sub = SubParser.Parse(data);
            return new ParsedValue<R>(Proj.Compile()(sub.Value), sub.Consumed);
        }
        public Expression TryMakeParseFromDataExpression(Expression array, Expression offset, Expression count) {
            return null;
        }
        public Expression TryMakeGetValueFromParsedExpression(Expression parsed) {
            return null;
        }
        public Expression TryMakeGetCountFromParsedExpression(Expression parsed) {
            return null;
        }
    }
}