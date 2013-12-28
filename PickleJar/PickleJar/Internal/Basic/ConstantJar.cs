using System;
using System.Linq.Expressions;
using Strilanc.PickleJar.Internal.RuntimeSpecialization;

namespace Strilanc.PickleJar.Internal.Basic {
    /// <summary>
    /// A jar that consumes no data, and always returns the same fixed value.
    /// </summary>
    internal struct ConstantJar<T> : IJarMetadataInternal, IJar<T> {
        public bool IsBlittable { get { return false; } }
        public int? OptionalConstantSerializedLength { get { return 0; } }
        public bool CanBeFollowed { get { return true; } }
        public readonly T ConstantValue;
        public ConstantJar(T constantValue) {
            ConstantValue = constantValue;
        }

        public ParsedValue<T> Parse(ArraySegment<byte> data) {
            return new ParsedValue<T>(ConstantValue, 0);
        }
        public byte[] Pack(T value) {
            if (!Equals(value, ConstantValue)) throw new ArgumentException("!Equals(value, ConstantValue)");
            return new byte[0];
        }
        public SpecializedParserParts TryMakeInlinedParserComponents(Expression array, Expression offset, Expression count) {
            return new SpecializedParserParts(
                Expression.Empty(),
                Expression.Constant(ConstantValue),
                Expression.Constant(0),
                new SpecializedParserResultStorageParts(new ParameterExpression[0], new ParameterExpression[0]));
        }
        public override string ToString() {
            return string.Format(
                "Constant[{0}]",
                ConstantValue);
        }
    }
}
