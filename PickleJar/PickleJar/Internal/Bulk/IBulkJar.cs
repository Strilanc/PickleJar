using System.Collections.Generic;

namespace Strilanc.PickleJar.Internal.Bulk {
    internal interface IBulkJar<T> : IBulkParser<T> {
        byte[] Pack(IReadOnlyList<T> values);
    }
}