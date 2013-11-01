using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Strilanc.PickleJar.Internal.Structured {
    internal class SequencedJarCompiled<T> : IJar<IReadOnlyList<T>>, IJarMetadataInternal {
        private readonly IJar<T>[] _jars;
        private readonly Func<ArraySegment<byte>, ParsedValue<IReadOnlyList<T>>> _parser;

        public SequencedJarCompiled(IJar<T>[] jars) {
            this._jars = jars.ToArray();
            _parser = InlinedParserComponents.MakeParser<IReadOnlyList<T>>(this.TryMakeInlinedParserComponents);
        }

        public ParsedValue<IReadOnlyList<T>> Parse(ArraySegment<byte> data) {
            return _parser(data);
        }
        public byte[] Pack(IReadOnlyList<T> value) {
            throw new NotImplementedException();
        }
        public bool CanBeFollowed { get { return _jars.Length == 0 || _jars.Last().CanBeFollowed; } }
        public bool IsBlittable { get { return _jars.All(e => e is IJarMetadataInternal && ((IJarMetadataInternal)e).IsBlittable); } }
        public int? OptionalConstantSerializedLength { get { return null; } }
        
        public InlinedParserComponents TryMakeInlinedParserComponents(Expression array, Expression offset, Expression count) {
            var r = SequencedJarUtil.BuildSequenceParsing(_jars.Select(e => new JarMeta(e, typeof (T))), array, offset, count);

            var resultArray = Expression.Variable(typeof (T[]), "resultArray");
            var cap = Expression.Constant(_jars.Length);

            var parserDoer = Expression.Block(
                r.Storage.ForValueIfConsumedCountAlreadyInScope,
                new[] {
                    r.ParseDoer,
                    Expression.Assign(resultArray, Expression.NewArrayBounds(typeof(T), cap)),
                    Enumerable.Range(0, _jars.Length).Select(i => Expression.Assign(Expression.ArrayAccess(resultArray, Expression.Constant(i)), r.ValueGetters[i])).Block(),
                });

            var storage = new ParsedValueStorage(r.Storage.ForConsumedCount, new[] {resultArray});
            return new InlinedParserComponents(
                parserDoer, 
                resultArray, 
                r.ConsumedCountGetter, 
                storage);
        }
    }
    internal static class SequencedJarUtil {
        public static InlinedMultiParserComponents BuildSequenceParsing(IEnumerable<JarMeta> jars, Expression array, Expression offset, Expression count) {
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

            var storage = new ParsedValueStorage(
                variablesNeededForValue: jarParseComponents.SelectMany(e => e.inlinedParse.Storage.ForValue).ToArray(),
                variablesNeededForConsumedCount: new[] {varConsumed});

            var resultGetters = jarParseComponents.Select(e => e.inlinedParse.ValueGetter).ToArray();

            return new InlinedMultiParserComponents(
                fullParseStatement, 
                resultGetters, 
                varConsumed,
                storage);
        }
    }
}