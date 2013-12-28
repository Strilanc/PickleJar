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
    }
}
