public struct ParsedValue<T> {
    public readonly T Value;
    public readonly int Consumed;
    public ParsedValue(T value, int consumed) {
        this.Value = value;
        this.Consumed = consumed;
    }
}
