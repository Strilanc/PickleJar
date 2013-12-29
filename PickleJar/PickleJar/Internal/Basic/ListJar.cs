using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Strilanc.PickleJar.Internal.RuntimeSpecialization;

namespace Strilanc.PickleJar.Internal.Basic {
    internal static class ListJar {
        public static IJar<IReadOnlyList<T>> Create<T>(IEnumerable<IJar<T>> itemJars) {
            if (itemJars == null) throw new ArgumentNullException("itemJars");

            var jarsCopy = itemJars.ToArray();
            if (jarsCopy.Any(jar => jar == null)) throw new ArgumentException("itemJars.Any(jar => jar == null)");
            if (jarsCopy.SkipLast(1).Any(jar => !jar.CanBeFollowed)) throw new ArgumentException("itemJars.SkipLast(1).Any(jar => !jar.CanBeFollowed)");

            return AnonymousJar.CreateSpecialized<IReadOnlyList<T>>(
                specializedParserMaker: (array, offset, count) => MakeInlinedParserComponentsForJarSequence(jarsCopy, array, offset, count),
                specializedPacker: v => MakePackerComponents(jarsCopy, v),
                canBeFollowed: jarsCopy.Length == 0 || jarsCopy.Last().CanBeFollowed,
                isBlittable: jarsCopy.All(jar => jar is IJarMetadataInternal && ((IJarMetadataInternal)jar).IsBlittable),
                constLength: jarsCopy.Select(jar => jar.OptionalConstantSerializedLength()).Sum(),
                desc: () => jarsCopy.StringJoinList("[", ", ", "].ToListJar()"),
                components: jarsCopy);
        }

        public static SpecializedParserParts MakeInlinedParserComponentsForJarSequence<T>(IJar<T>[] jars, Expression array, Expression offset, Expression count) {
            if (array == null) throw new ArgumentNullException("array");
            if (offset == null) throw new ArgumentNullException("offset");
            if (count == null) throw new ArgumentNullException("count");

            var r = SpecializedMultiValueParserParts.BuildComponentsOfParsingSequence(jars.Select(e => new JarMeta(e, typeof(T))), array, offset, count);

            var resultArray = Expression.Variable(typeof(T[]), "resultArray");
            var cap = Expression.Constant(jars.Length);

            var parserDoer = Expression.Block(
                r.Storage.ForValueIfConsumedCountAlreadyInScope,
                new[] {
                    r.ParseDoer,
                    resultArray.AssignTo(Expression.NewArrayBounds(typeof(T), cap)),
                    Enumerable.Range(0, jars.Length)
                              .Select(i => resultArray.AccessIndex(i).AssignTo(r.ValueGetters[i]))
                              .Block(),
                });

            var storage = new SpecializedParserResultStorageParts(r.Storage.ForConsumedCount, new[] {resultArray});
            return new SpecializedParserParts(
                parserDoer,
                resultArray,
                r.ConsumedCountGetter,
                storage);
        }

        private static SpecializedPackerParts MakePackerComponents<T>(IEnumerable<IJar<T>> jars, Expression value) {
            if (value == null) throw new ArgumentNullException("value");

            return SpecializedPackerParts.FromSequence(
                jars.Select((e, i) => e.MakeSpecializedPacker(Expression.MakeIndex(value,
                                                                                   typeof(IReadOnlyList<T>).GetProperty("Item"),
                                                                                   new[] {i.ConstExpr()})))
                    .ToArray());
        }
    }
}
