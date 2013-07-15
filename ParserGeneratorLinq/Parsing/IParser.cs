using System;

namespace Strilanc.Parsing {
    public interface IParser<T> {
        ParsedValue<T> Parse(ArraySegment<byte> data);
    }
}