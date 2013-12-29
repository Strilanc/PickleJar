using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Linq;

namespace Strilanc.PickleJar.Internal.RuntimeSpecialization {
    internal struct SpecializedMultiValueParserParts {
        private readonly Expression _parseDoer;
        private readonly Expression[] _valueGetters;
        private readonly Expression _consumedCountGetter;
        private readonly SpecializedParserResultStorageParts _storage;

        public Expression ParseDoer { get { return _parseDoer ?? Expression.Empty(); } }
        public IReadOnlyList<Expression> ValueGetters { get { return _valueGetters ?? new Expression[0]; } }
        public Expression ConsumedCountGetter { get { return _consumedCountGetter ?? Expression.Constant(0); } }
        public SpecializedParserResultStorageParts Storage { get { return _storage; } }

        public SpecializedMultiValueParserParts(Expression parseDoer,
                                                  IEnumerable<Expression> valueGetters,
                                                  Expression consumedCountGetter,
                                                  SpecializedParserResultStorageParts storage) {
            if (parseDoer == null) throw new ArgumentNullException("parseDoer");
            if (valueGetters == null) throw new ArgumentNullException("valueGetters");
            if (consumedCountGetter == null) throw new ArgumentNullException("consumedCountGetter");

            _parseDoer = parseDoer;
            _valueGetters = valueGetters.ToArray();
            _consumedCountGetter = consumedCountGetter;
            _storage = storage;

            if (_valueGetters.HasNulls()) throw new ArgumentNullException("valueGetters", "valueGetters.HasNulls()");
        }

        public static SpecializedMultiValueParserParts BuildComponentsOfParsingSequence(IEnumerable<JarMeta> jars, Expression array, Expression offset, Expression count) {
            if (jars == null) throw new ArgumentNullException("jars");
            if (array == null) throw new ArgumentNullException("array");
            if (offset == null) throw new ArgumentNullException("offset");
            if (count == null) throw new ArgumentNullException("count");

            var varConsumed = Expression.Variable(typeof(int), "listConsumed");

            var initLocals = varConsumed.AssignTo(Expression.Constant(0));

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
                         varConsumed.PlusEqual(inlinedParseComponents.ConsumedCountGetter)
                     })
                 select new { jar, inlinedParse = inlinedParseComponents, parseStatement }
                ).ToArray();

            var fullParseStatement = Expression.Block(
                initLocals,
                jarParseComponents.Select(e => e.parseStatement).Block());

            var storage = new SpecializedParserResultStorageParts(
                variablesNeededForValue: jarParseComponents.SelectMany(e => e.inlinedParse.Storage.ForValue),
                variablesNeededForConsumedCount: new[] { varConsumed });

            var resultGetters = jarParseComponents.Select(e => e.inlinedParse.ValueGetter);

            return new SpecializedMultiValueParserParts(
                fullParseStatement,
                resultGetters,
                varConsumed,
                storage);
        }
    }
}
