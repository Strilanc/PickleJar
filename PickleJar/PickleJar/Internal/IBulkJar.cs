using System.Collections.Generic;

namespace Strilanc.PickleJar.Internal {
    internal interface IBulkJar<T> : IBulkParser<T> {
        byte[] Pack(IReadOnlyList<T> values);
    }
}