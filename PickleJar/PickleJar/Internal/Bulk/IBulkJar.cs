using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Strilanc.PickleJar.Internal.RuntimeSpecialization;

namespace Strilanc.PickleJar.Internal.Bulk {
    /// <summary>
    /// IBulkJar is implemented by types capable of efficiently parsing multiple contiguous values.
    /// </summary>
    internal interface IBulkJar<T> {
        IJar<T> ItemJar { get; }
        ParsedValue<IReadOnlyList<T>> Parse(ArraySegment<byte> data, int count);
        int? OptionalConstantSerializedValueLength { get; }
        byte[] Pack(IReadOnlyCollection<T> values);
        SpecializedParserParts MakeInlinedParserComponents(Expression array, Expression offset, Expression count, Expression itemCount);
    }
}