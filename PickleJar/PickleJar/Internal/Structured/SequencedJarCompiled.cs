using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Strilanc.PickleJar.Internal.RuntimeSpecialization;

namespace Strilanc.PickleJar.Internal.Structured {
    internal static class SequencedJarUtil {
        public static IJar<IReadOnlyList<T>> MakeSequencedJar<T>(IEnumerable<IJar<T>> jars) {
            if (jars == null) throw new ArgumentNullException("jars");

            var jarsCopy = jars.ToArray();
            if (jarsCopy.Any(jar => jar == null)) throw new ArgumentException("jars.Any(jar => jar == null)");
            if (jarsCopy.SkipLast(1).Any(jar => !jar.CanBeFollowed)) throw new ArgumentException("jars.SkipLast(1).Any(jar => !jar.CanBeFollowed)");

            return AnonymousJar.CreateFrom<IReadOnlyList<T>>(
                parser: (array, offset, count) => MakeInlinedParserComponentsForJarSequence(jarsCopy, array, offset, count),
                packer: v => { throw new NotImplementedException(); },
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

            var r = BuildComponentsOfParsingSequence(jars.Select(e => new JarMeta(e, typeof(T))), array, offset, count);

            var resultArray = Expression.Variable(typeof(T[]), "resultArray");
            var cap = Expression.Constant(jars.Length);

            var parserDoer = Expression.Block(
                r.Storage.ForValueIfConsumedCountAlreadyInScope,
                new[] {
                    r.ParseDoer,
                    Expression.Assign(resultArray, Expression.NewArrayBounds(typeof(T), cap)),
                    Enumerable.Range(0, jars.Length).Select(i => Expression.Assign(Expression.ArrayAccess(resultArray, Expression.Constant(i)), r.ValueGetters[i])).Block(),
                });

            var storage = new SpecializedParserResultStorageParts(r.Storage.ForConsumedCount, new[] { resultArray });
            return new SpecializedParserParts(
                parserDoer,
                resultArray,
                r.ConsumedCountGetter,
                storage);
        }

        public static SpecializedMultiValueParserParts BuildComponentsOfParsingSequence(IEnumerable<JarMeta> jars, Expression array, Expression offset, Expression count) {
            if (jars == null) throw new ArgumentNullException("jars");
            if (array == null) throw new ArgumentNullException("array");
            if (offset == null) throw new ArgumentNullException("offset");
            if (count == null) throw new ArgumentNullException("count");

            var varConsumed = Expression.Variable(typeof(int), "listConsumed");

            var initLocals = Expression.Assign(varConsumed, Expression.Constant(0));

            var jarParseComponents =
                (from jar in jars
                 let inlinedParseComponents = jar.MakeInlinedParserComponents(
                     array,
                     Expression.Add(offset, varConsumed),
                     Expression.Subtract(count, varConsumed))
                 let parseStatement = Expression.Block(
                     inlinedParseComponents.Storage.ForConsumedCountIfValueAlreadyInScope,
                     new[] {
                         inlinedParseComponents.ParseDoer,
                         Expression.AddAssign(varConsumed, inlinedParseComponents.ConsumedCountGetter)
                     })
                 select new { jar, inlinedParse = inlinedParseComponents, parseStatement}
                ).ToArray();

            var fullParseStatement = Expression.Block(
                initLocals,
                jarParseComponents.Select(e => e.parseStatement).Block());

            var storage = new SpecializedParserResultStorageParts(
                variablesNeededForValue: jarParseComponents.SelectMany(e => e.inlinedParse.Storage.ForValue),
                variablesNeededForConsumedCount: new[] {varConsumed});

            var resultGetters = jarParseComponents.Select(e => e.inlinedParse.ValueGetter);

            return new SpecializedMultiValueParserParts(
                fullParseStatement, 
                resultGetters, 
                varConsumed,
                storage);
        }
    }
}