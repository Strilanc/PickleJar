using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Linq;

namespace Strilanc.PickleJar.Internal.RuntimeSpecialization {
    internal delegate Expression PackDoer(Expression array, ParameterExpression offset);
    internal delegate SpecializedPackerParts SpecializedPackerMaker(Expression value);

    internal struct SpecializedPackerParts {
        private readonly Expression _capacityComputer;
        private readonly Expression _capacityGetter;
        private readonly ParameterExpression[] _capacityStorage;
        private readonly PackDoer _packDoer;
        public SpecializedPackerParts(Expression capacityComputer, Expression capacityGetter, IEnumerable<ParameterExpression> capacityStorage, PackDoer packDoer) {
            if (capacityComputer == null) throw new ArgumentNullException("capacityComputer");
            if (capacityGetter == null) throw new ArgumentNullException("capacityGetter");
            if (capacityStorage == null) throw new ArgumentNullException("capacityStorage");
            if (packDoer == null) throw new ArgumentNullException("packDoer");
            _capacityComputer = capacityComputer;
            _capacityGetter = capacityGetter;
            _capacityStorage = capacityStorage.ToArray();
            _packDoer = packDoer;
        }

        public Expression CapacityComputer { get { return _capacityComputer ?? Expression.Empty(); } }
        public PackDoer PackDoer { get { return _packDoer ?? ((a,o) => Expression.Empty()); } }
        public Expression CapacityGetter { get { return _capacityGetter ?? Expression.Constant(0); } }
        public ParameterExpression[] CapacityStorage { get { return _capacityStorage ?? new ParameterExpression[0]; } }

        public static SpecializedPackerParts FromSequence(params SpecializedPackerParts[] packers) {
            if (packers == null) throw new ArgumentNullException("packers");
            if (packers.Length == 0) return default(SpecializedPackerParts);

            var capacityVar = Expression.Variable(typeof(int), "totalCapacity");
            var subCapacitySummation = packers.Select(e => e.CapacityGetter).Aggregate(Expression.Add);
            var capacityComputer = Expression.Block(
                packers.Select(e => e.CapacityComputer).Block(),
                capacityVar.AssignTo(subCapacitySummation));
            var capacityStorage = packers.SelectMany(e => e.CapacityStorage).Concat(new[] { capacityVar });

            PackDoer doer = (array, offset) =>
                            packers
                                .Select(e => e._packDoer(array, offset).FollowedBy(offset.PlusEqual(e.CapacityGetter)))
                                .Block();
            return new SpecializedPackerParts(capacityComputer, capacityVar, capacityStorage, doer);
        }

        public static Func<T, byte[]> MakePacker<T>(SpecializedPackerMaker maker) {
            var paramValue = Expression.Parameter(typeof(T), "value");
            var specialized = maker(paramValue);

            var varOffset = Expression.Variable(typeof(int), "offset");
            var varResult = Expression.Variable(typeof(byte[]), "result");

            var body = Expression.Block(
                specialized.CapacityStorage.Concat(new[] { varOffset, varResult }),
                specialized.CapacityComputer,
                varResult.AssignTo(Expression.NewArrayBounds(typeof(byte), specialized.CapacityGetter)),
                specialized.PackDoer(varResult, varOffset),
                varResult);

            var method = Expression.Lambda<Func<T, byte[]>>(
                body,
                new[] { paramValue });

            return method.Compile();
        }
    }
}
