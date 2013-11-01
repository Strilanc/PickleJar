using System;
using System.Linq.Expressions;

namespace Strilanc.PickleJar.Internal {
    internal sealed class InlinedMultiParserComponents {
        public readonly Expression ParseDoer;
        public readonly ParsedValueStorage Storage;
        public readonly Expression[] ValueGetters;
        public readonly Expression ConsumedCountGetter;

        public InlinedMultiParserComponents(Expression parseDoer, Expression[] valueGetters, Expression consumedCountGetter, ParsedValueStorage storage) {
            if (parseDoer == null) throw new ArgumentNullException("parseDoer");
            if (valueGetters == null) throw new ArgumentNullException("valueGetters");
            if (consumedCountGetter == null) throw new ArgumentNullException("consumedCountGetter");
            ParseDoer = parseDoer;
            ValueGetters = valueGetters;
            ConsumedCountGetter = consumedCountGetter;
            Storage = storage;
        }
    }
}