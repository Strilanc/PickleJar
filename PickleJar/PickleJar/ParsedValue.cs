namespace Strilanc.PickleJar {
    /// <summary>
    /// A value that has been parsed out of some data, as well as how many bytes of data the value took up within the data.
    /// </summary>
    /// <typeparam name="T">The type of value that was parsed.</typeparam>
    public struct ParsedValue<T> {
        /// <summary>The value that was parsed out of data.</summary>
        public readonly T Value;
        /// <summary>The number of bytes consumed while parsing the value.</summary>
        public readonly int Consumed;
        /// <summary>Constructs a parsed value with the given value and number of consumed bytes.</summary>
        public ParsedValue(T value, int consumed) {
            this.Value = value;
            this.Consumed = consumed;
        }
    }
}
