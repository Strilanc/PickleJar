using System;

namespace Strilanc.PickleJar {
    public static class JarUtil {
        ///<summary>Parses a value from the data in an array.</summary>
        public static ParsedValue<T> Parse<T>(this IJar<T> parser, byte[] data) {
            return parser.Parse(new ArraySegment<byte>(data, 0, data.Length));
        }
    }
}
