using System;
using System.Linq.Expressions;
using Strilanc.PickleJar.Internal.RuntimeSpecialization;

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
                                                                            Func<TExposed, TInternal> packProjection,
                                                                            Func<string> desc = null,
                                                                            object components = null) {
            if (internalJar == null) throw new ArgumentNullException("internalJar");
            if (packProjection == null) throw new ArgumentNullException("packProjection");

            SpecializedParserMaker specializedParserMaker = (array, offset, count) => {
                var sub = internalJar.MakeInlinedParserComponents(array, offset, count);
                return new SpecializedParserParts(
                    parseDoer: sub.ParseDoer,
                    valueGetter: getParsedValueProjection(sub.ValueGetter),
                    consumedCountGetter: sub.ConsumedCountGetter,
                    storage: sub.Storage);
            };

            // todo: specialize at runtime
            Func<TExposed, byte[]> packer = exposedValue => internalJar.Pack(packProjection(exposedValue));

            return AnonymousJar.CreateSpecialized(
                specializedParserMaker: specializedParserMaker,
                packer: packer,
                canBeFollowed: internalJar.CanBeFollowed,
                isBlittable: false,
                constLength: internalJar.OptionalConstantSerializedLength(),
                desc: desc ?? (() => string.Format("{0} (projected)", internalJar)),
                components: components);
        }
    }
}
