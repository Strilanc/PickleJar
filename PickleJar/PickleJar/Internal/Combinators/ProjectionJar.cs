using System;
using System.Linq.Expressions;
using Strilanc.PickleJar.Internal.Basic;
using Strilanc.PickleJar.Internal.RuntimeSpecialization;
using System.Linq;

namespace Strilanc.PickleJar.Internal.Structured {
    internal static class ProjectionJar {
        public static IJar<TExposed> Create<TInternal, TExposed>(IJar<TInternal> jar,
                                                                 Func<TInternal, TExposed> parseProjection,
                                                                 Func<TExposed, TInternal> packProjection) {
            if (jar == null) throw new ArgumentNullException("jar");
            if (parseProjection == null) throw new ArgumentNullException("parseProjection");
            if (packProjection == null) throw new ArgumentNullException("packProjection");
            return new AnonymousJar<TExposed>(
                data => jar.Parse(data).Select(parseProjection),
                e => jar.Pack(packProjection(e)),
                jar.CanBeFollowed,
                isBlittable: false,
                optionalConstantSerializedLength: jar.OptionalConstantSerializedLength(),
                tryInlinedParserComponents: null);
        }
        public static IJar<TExposed> CreateSpecialized<TInternal, TExposed>(IJar<TInternal> internalJar,
                                                                            Func<Expression, Expression> getParsedValueProjection,
                                                                            Func<Expression, Expression> packProjection,
                                                                            Func<string> desc = null,
                                                                            object components = null) {
            if (internalJar == null) throw new ArgumentNullException("internalJar");
            if (packProjection == null) throw new ArgumentNullException("packProjection");

            SpecializedParserMaker specializedParserMaker = (array, offset, count) => {
                var sub = internalJar.MakeInlinedParserComponents(array, offset, count);
                var resultVar = Expression.Variable(typeof(TExposed), "result_projected");
                return new SpecializedParserParts(
                    parseDoer: sub.ParseDoer.FollowedBy(resultVar.AssignTo(getParsedValueProjection(sub.ValueGetter))),
                    valueGetter: resultVar,
                    consumedCountGetter: sub.ConsumedCountGetter,
                    storage: new SpecializedParserResultStorageParts(
                        variablesNeededForValue: sub.Storage.ForValue.Concat(new[] {resultVar}),
                        variablesNeededForConsumedCount: sub.Storage.ForConsumedCount));
            };

            SpecializedPackerMaker packerMaker = value => {
                var sub = internalJar.MakeSpecializedPacker(packProjection(value));
                return new SpecializedPackerParts(
                    capacityComputer: sub.CapacityComputer,
                    capacityGetter: sub.CapacityGetter,
                    capacityStorage: sub.CapacityStorage,
                    packDoer: sub.PackDoer);
            };

            return AnonymousJar.CreateSpecialized<TExposed>(
                specializedParserMaker: specializedParserMaker,
                specializedPacker: packerMaker,
                canBeFollowed: internalJar.CanBeFollowed,
                isBlittable: false,
                constLength: internalJar.OptionalConstantSerializedLength(),
                desc: desc ?? (() => string.Format("{0} (projected)", internalJar)),
                components: components);
        }
    }
}
