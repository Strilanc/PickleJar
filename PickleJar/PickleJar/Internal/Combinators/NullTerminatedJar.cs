using System;
using System.Linq.Expressions;
using Strilanc.PickleJar.Internal.Basic;
using Strilanc.PickleJar.Internal.RuntimeSpecialization;

namespace Strilanc.PickleJar.Internal.Combinators {
    internal sealed class NullTerminatedJar {
        public static IJar<T> Create<T>(IJar<T> itemJar) {
            if (itemJar == null) throw new ArgumentNullException("itemJar");

            ParseSpecializer parseSpecializer = (array, offset, count) => {
                var varLengthToTerminator = Expression.Variable(typeof(int), "lengthToTerminator");
                var findTerminator = Expression.Block(
                    varLengthToTerminator.AssignTo(0.ConstExpr()),
                    varLengthToTerminator.IsGreaterThanOrEqualTo(count)
                                         .IfThenDo(Expression.Throw(new InvalidOperationException("Null terminator not found.").ConstExpr()))
                                         .DoWhileTrueDo(
                                             loopCondition: array.AccessIndex(offset.Plus(varLengthToTerminator)).IsNotEqualTo(((byte)0).ConstExpr()),
                                             loopBodyEnd: varLengthToTerminator.PlusEqual(1)));

                var sub = itemJar.MakeInlinedParserComponents(array, offset, varLengthToTerminator);
                return new SpecializedParserParts(
                    parseDoer: Expression.Block(
                        sub.Storage.ForConsumedCountIfValueAlreadyInScope,
                        findTerminator,
                        // todo: check that all data was consumed
                        sub.ParseDoer),
                    valueGetter: sub.ValueGetter,
                    consumedCountGetter: varLengthToTerminator.Plus(1),
                    storage: new SpecializedParserStorageParts(
                        variablesNeededForValue: sub.Storage.ForValue,
                        variablesNeededForConsumedCount: new[] {varLengthToTerminator}));
            };

            PackSpecializer packSpecializer = value => {
                var sub = itemJar.MakeSpecializedPacker(value);
                return new SpecializedPackerParts(
                    sizePrecomputer: sub.SizePrecomputer,
                    precomputedSizeGetter: sub.PrecomputedSizeGetter.Plus(1),
                    precomputedSizeStorage: sub.PrecomputedSizeStorage,
                    packDoer: (array, offset) => Expression.Block(
                        sub.PackDoer(array, offset), // todo: check that no zeroes were in the output
                        array.AccessIndex(offset).AssignTo(((byte)0).ConstExpr()),
                        offset.PlusEqual(1)));
            };

            return AnonymousJar.CreateSpecialized<T>(
                parseSpecializer: parseSpecializer,
                packSpecializer: packSpecializer,
                canBeFollowed: true,
                constLength: itemJar.OptionalConstantSerializedLength() + 1,
                desc: () => string.Format("{0}.NullTerminated()", itemJar));
        }
    }
}
