using System;
using System.Linq.Expressions;
using System.Reflection;
using Strilanc.PickleJar.Internal.RuntimeSpecialization;

namespace Strilanc.PickleJar.Internal.Basic {
    /// <summary>
    /// A jar that consumes no data, and always returns the same fixed value.
    /// </summary>
    internal static class ConstantJar {
        public static IJar<T> Create<T>(T constantValue) {
            var constValExp = constantValue.ConstExpr();
            return AnonymousJar.CreateSpecialized<T>(
                parseSpecializer: (array, offset, count) => new SpecializedParserParts(
                                                                parseDoer: Expression.Empty(),
                                                                valueGetter: constValExp,
                                                                consumedCountGetter: 0.ConstExpr(),
                                                                storage: default(SpecializedParserStorageParts)),
                packSpecializer: value => new SpecializedPackerParts(
                                              capacityComputer: Expression.Empty(),
                                              capacityGetter: 0.ConstExpr(),
                                              capacityStorage: new ParameterExpression[0],
                                              packDoer: (array, offset) =>
                                                        Expression.Call(typeof(Object).GetMethod("Equals", BindingFlags.Static | BindingFlags.Public),
                                                                        constValExp,
                                                                        value)
                                                                  .Not()
                                                                  .IfThenDo(Expression.Throw(
                                                                      new ArgumentException("!Equals(value, ConstantValue)").ConstExpr()))),
                canBeFollowed: true,
                constLength: 0,
                desc: () => string.Format("Constant[{0}]", constantValue),
                components: constantValue);
        }
    }
}
