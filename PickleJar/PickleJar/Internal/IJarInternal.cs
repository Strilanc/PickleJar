namespace Strilanc.PickleJar.Internal {
    internal interface IJarInternal<T> : IJar<T>, IParserInternal<T>, IPackerInternal<T> {}
}