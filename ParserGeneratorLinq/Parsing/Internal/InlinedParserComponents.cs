using System;
using System.Linq.Expressions;

namespace Strilanc.Parsing.Internal {
    internal sealed class InlinedParserComponents {
        public readonly Expression PerformParse;
        public readonly ParameterExpression[] ResultStorage;
        public readonly Expression AfterParseValueGetter;
        public readonly Expression AfterParseConsumedGetter;
        public InlinedParserComponents(Expression performParse, Expression afterParseValueGetter, Expression afterParseConsumedGetter, params ParameterExpression[] resultStorage) {
            if (performParse == null) throw new ArgumentNullException("performParse");
            if (resultStorage == null) throw new ArgumentNullException("resultStorage");
            if (afterParseValueGetter == null) throw new ArgumentNullException("afterParseValueGetter");
            if (afterParseConsumedGetter == null) throw new ArgumentNullException("afterParseConsumedGetter");
            PerformParse = performParse;
            ResultStorage = resultStorage;
            AfterParseValueGetter = afterParseValueGetter;
            AfterParseConsumedGetter = afterParseConsumedGetter;
        }
    }
}