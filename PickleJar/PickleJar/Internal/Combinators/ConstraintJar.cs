using System;
using System.Linq.Expressions;
using Strilanc.PickleJar.Internal.Basic;
using Strilanc.PickleJar.Internal.RuntimeSpecialization;

namespace Strilanc.PickleJar.Internal.Combinators {
    internal static class ConstraintJar {
        public static IJar<T> Create<T>(IJar<T> jar,
                                        Func<T, bool> constraint) {
            if (jar == null) throw new ArgumentNullException("jar");
            if (constraint == null) throw new ArgumentNullException("constraint");
            return new AnonymousJar<T>(
                data => {
                    var v = jar.Parse(data);
                    if (!constraint(v.Value)) throw new InvalidOperationException("Parsed value did not match constraint.");
                    return v;
                },
                item => {
                    if (!constraint(item)) throw new InvalidOperationException("Value to pack did not match constraint.");
                    return jar.Pack(item);
                },
                jar.CanBeFollowed,
                isBlittable: false,
                optionalConstantSerializedLength: jar.OptionalConstantSerializedLength(),
                tryInlinedParserComponents: null);
        }

        public static IJar<T> CreateSpecialized<T>(IJar<T> jar,
                                                   Expression<Func<T, bool>> constraint) {
            if (jar == null) throw new ArgumentNullException("jar");
            if (constraint == null) throw new ArgumentNullException("constraint");

            SpecializedParserMaker parser = (array, offset, count) => {
                var sub = jar.MakeInlinedParserComponents(array, offset, count);
                return new SpecializedParserParts(
                    parseDoer: sub.ParseDoer.FollowedBy(
                        Expression.Invoke(constraint, sub.ValueGetter)
                                  .IfThenDo(Expression.Throw(new InvalidOperationException("Parsed value did not match constraint.").ConstExpr()))),
                    valueGetter: sub.ValueGetter,
                    consumedCountGetter: sub.ConsumedCountGetter,
                    storage: sub.Storage);
            };
            SpecializedPackerMaker packer = value => {
                var sub = jar.MakeSpecializedPacker(value);
                return new SpecializedPackerParts(
                    capacityComputer: sub.CapacityComputer,
                    capacityGetter: sub.CapacityGetter,
                    capacityStorage: sub.CapacityStorage,
                    packDoer: (array, offset) => sub.PackDoer(array, offset).FollowedBy(
                        Expression.Invoke(constraint, value)
                                  .IfThenDo(Expression.Throw(new InvalidOperationException("Value to pack did not match constraint.").ConstExpr()))));
            };

            return AnonymousJar.CreateSpecialized<T>(
                specializedParserMaker: parser,
                specializedPacker: packer,
                canBeFollowed: jar.CanBeFollowed,
                isBlittable: false,
                constLength: jar.OptionalConstantSerializedLength(),
                desc: () => string.Format("{0}.Where({1})", jar, constraint),
                components: null);
        }
    }
}
