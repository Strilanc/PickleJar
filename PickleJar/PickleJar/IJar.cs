using System;

namespace Strilanc.PickleJar {
    public interface IJar<T> {
        ParsedValue<T> Parse(ArraySegment<byte> data);
        byte[] Pack(T value);
    }
}