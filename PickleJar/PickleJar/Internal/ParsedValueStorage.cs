using System.Linq;
using System.Linq.Expressions;

namespace Strilanc.PickleJar.Internal {
    internal struct ParsedValueStorage {
        public readonly ParameterExpression[] ForValue;
        public readonly ParameterExpression[] ForConsumedCount;

        public ParameterExpression[] ForConsumedCountIfValueAlreadyInScope {
            get { return ForConsumedCount.Except(ForValue).ToArray(); }
        }
        public ParameterExpression[] ForValueIfConsumedCountAlreadyInScope {
            get { return ForValue.Except(ForConsumedCount).ToArray(); }
        }
        public ParameterExpression[] ForBoth {
            get { return ForConsumedCount.Concat(ForValue).Distinct().ToArray(); }
        }

        public ParsedValueStorage(ParameterExpression[] variablesNeededForValue, ParameterExpression[] variablesNeededForConsumedCount) {
            ForValue = variablesNeededForValue;
            ForConsumedCount = variablesNeededForConsumedCount;
        }
    }
}