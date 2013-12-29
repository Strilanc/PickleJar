using System;
using Strilanc.PickleJar.Internal.Basic;

namespace Strilanc.PickleJar.Internal.Combinators {
    internal static class ConstraintJar {
        public static IJar<T> Create<T>(IJar<T> jar,
                                        Func<T, bool> constraint) {
            if (jar == null) throw new ArgumentNullException("jar");
            if (constraint == null) throw new ArgumentNullException("constraint");
            return new AnonymousJar<T>(
                data => {
                    var v = jar.Parse(data);
                    if (!constraint(v.Value)) throw new InvalidOperationException("Data did not match Where constraint");
                    return v;
                },
                item => {
                    if (!constraint(item)) throw new InvalidOperationException("Data did not match Where constraint");
                    return jar.Pack(item);
                },
                jar.CanBeFollowed,
                isBlittable: false,
                optionalConstantSerializedLength: jar.OptionalConstantSerializedLength(),
                tryInlinedParserComponents: null);
        }
    }
}
