using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Strilanc.PickleJar.Internal.RuntimeSpecialization {
    /// <summary>
    /// Stores the variable expressions which must be in scope for the results of a parse to be accessed.
    /// The variable expressions ForBoth the value and consumed count must be in scope before the parse is performed.
    /// </summary>
    /// <remarks>
    /// This class is used to allow inlined parsers to be more efficient.
    /// For example, a 32-bit integer parser can avoid using any space at all to holds its consumed count because it is always 4.
    /// In particular, having custom storage removes unnecessary creation and unwrapping of instances of ParsedValue.
    /// </remarks>
    internal struct SpecializedParserResultStorageParts {
        private readonly ParameterExpression[] _forValue;
        private readonly ParameterExpression[] _forConsumedCount;

        /// <summary>
        /// The variable expressions used to hold the value that was parsed.
        /// May overlap with ForConsumedCount.
        /// </summary>
        public IReadOnlyList<ParameterExpression> ForValue { get { return _forValue ?? new ParameterExpression[0]; } }
        /// <summary>
        /// The variable expressions used to hold the number of bytes that were parsed.
        /// May overlap with ForValue.
        /// </summary>
        public IReadOnlyList<ParameterExpression> ForConsumedCount { get { return _forConsumedCount ?? new ParameterExpression[0]; } }
        /// <summary>
        /// The variables expressions needed, in addition to the ones used for holding the value, to hold the number of bytes that were parsed.
        /// </summary>
        public IReadOnlyList<ParameterExpression> ForConsumedCountIfValueAlreadyInScope { get { return ForConsumedCount.Except(ForValue).ToArray(); } }
        /// <summary>
        /// The variables expressions needed, in addition to the ones used for holding the number of bytes, to hold the value that was parsed.
        /// </summary>
        public IReadOnlyList<ParameterExpression> ForValueIfConsumedCountAlreadyInScope { get { return ForValue.Except(ForConsumedCount).ToArray(); } }
        /// <summary>
        /// The variable expressions needed to hold both the value and number of bytes that were parsed.
        /// </summary>
        public IReadOnlyList<ParameterExpression> ForBoth { get { return ForConsumedCount.Concat(ForValue).Distinct().ToArray(); } }

        public SpecializedParserResultStorageParts(IEnumerable<ParameterExpression> variablesNeededForValue,
                                  IEnumerable<ParameterExpression> variablesNeededForConsumedCount) {
            if (variablesNeededForValue == null) throw new ArgumentNullException("variablesNeededForValue");
            if (variablesNeededForConsumedCount == null) throw new ArgumentNullException("variablesNeededForConsumedCount");

            _forValue = variablesNeededForValue.ToArray();
            _forConsumedCount = variablesNeededForConsumedCount.ToArray();

            if (_forValue.HasNulls()) throw new ArgumentNullException("variablesNeededForValue", "variablesNeededForValue.HasNulls()");
            if (_forConsumedCount.HasNulls()) throw new ArgumentNullException("variablesNeededForConsumedCount", "variablesNeededForConsumedCount.HasNulls()");
        }
    }
}