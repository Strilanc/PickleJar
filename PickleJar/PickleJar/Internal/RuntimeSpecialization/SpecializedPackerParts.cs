using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Linq;

namespace Strilanc.PickleJar.Internal.RuntimeSpecialization {
    internal delegate Expression PackDoer(Expression array, ParameterExpression offset);
    internal delegate SpecializedPackerParts PackSpecializer(Expression value);

    internal struct SpecializedPackerParts {
        private readonly Expression _sizePrecomputer;
        private readonly Expression _precomputedSizeGetter;
        private readonly ParameterExpression[] _capacityStorage;
        private readonly PackDoer _packDoer;
        public SpecializedPackerParts(Expression sizePrecomputer, Expression precomputedSizeGetter, IEnumerable<ParameterExpression> precomputedSizeStorage, PackDoer packDoer) {
            if (sizePrecomputer == null) throw new ArgumentNullException("sizePrecomputer");
            if (precomputedSizeGetter == null) throw new ArgumentNullException("precomputedSizeGetter");
            if (precomputedSizeStorage == null) throw new ArgumentNullException("precomputedSizeStorage");
            if (packDoer == null) throw new ArgumentNullException("packDoer");
            _sizePrecomputer = sizePrecomputer;
            _precomputedSizeGetter = precomputedSizeGetter;
            _capacityStorage = precomputedSizeStorage.ToArray();
            _packDoer = packDoer;
        }

        public Expression SizePrecomputer { get { return _sizePrecomputer ?? Expression.Empty(); } }
        public Expression PrecomputedSizeGetter { get { return _precomputedSizeGetter ?? Expression.Constant(0); } }
        public ParameterExpression[] PrecomputedSizeStorage { get { return _capacityStorage ?? new ParameterExpression[0]; } }
        public PackDoer PackDoer { get { return _packDoer ?? ((a, o) => Expression.Empty()); } }

        public static SpecializedPackerParts FromSequence(params SpecializedPackerParts[] packers) {
            if (packers == null) throw new ArgumentNullException("packers");
            if (packers.Length == 0) return default(SpecializedPackerParts);

            var capacityVar = Expression.Variable(typeof(int), "totalCapacity");
            var subCapacitySummation = packers.Select(e => e.PrecomputedSizeGetter).Aggregate(Expression.Add);
            var capacityComputer = Expression.Block(
                packers.Select(e => e.SizePrecomputer).Block(),
                capacityVar.AssignTo(subCapacitySummation));
            var capacityStorage = packers.SelectMany(e => e.PrecomputedSizeStorage).Concat(new[] { capacityVar });

            PackDoer doer = (array, offset) =>
                            packers
                                .Select(e => e._packDoer(array, offset).FollowedBy(offset.PlusEqual(e.PrecomputedSizeGetter)))
                                .Block();
            return new SpecializedPackerParts(capacityComputer, capacityVar, capacityStorage, doer);
        }

        public static Func<T, byte[]> MakePacker<T>(PackSpecializer maker) {
            var paramValue = Expression.Parameter(typeof(T), "value");
            var specialized = maker(paramValue);

            var varOffset = Expression.Variable(typeof(int), "offset");
            var varResult = Expression.Variable(typeof(byte[]), "result");

            var body = Expression.Block(
                specialized.PrecomputedSizeStorage.Concat(new[] { varOffset, varResult }),
                specialized.SizePrecomputer,
                varResult.AssignTo(Expression.NewArrayBounds(typeof(byte), specialized.PrecomputedSizeGetter)),
                specialized.PackDoer(varResult, varOffset),
                varResult);

            var method = Expression.Lambda<Func<T, byte[]>>(
                body,
                new[] { paramValue });

            return method.Compile();
        }
    }
}
