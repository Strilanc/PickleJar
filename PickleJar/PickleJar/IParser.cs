using System;

namespace Strilanc.PickleJar {
    public interface IParser<T> {
        ParsedValue<T> Parse(ArraySegment<byte> data);
    }
}