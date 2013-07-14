using System;
using System.Linq.Expressions;

public sealed class SelectManyParser<T, M, R> : IParser<R> {
    public readonly IParser<T> SubParser;
    public readonly Expression<Func<T, IParser<M>>> Proj1;
    public readonly Expression<Func<T, M, R>> Proj2;
    public bool IsBlittable { get { return false; } }
    public int? OptionalConstantSerializedLength { get { return null; } }

    public SelectManyParser(IParser<T> subParser, Expression<Func<T, IParser<M>>> proj1, Expression<Func<T, M, R>> proj2) {
        this.SubParser = subParser;
        this.Proj1 = proj1;
        this.Proj2 = proj2;
    }
    public ParsedValue<R> Parse(ArraySegment<byte> data) {
        var sub = SubParser.Parse(data);
        var p = Proj1.Compile()(sub.Value);
        var sub2 = p.Parse(new ArraySegment<byte>(data.Array, data.Offset + sub.Consumed, data.Count - sub.Consumed));
        return new ParsedValue<R>(Proj2.Compile()(sub.Value, sub2.Value), sub.Consumed + sub2.Consumed);
    }
}