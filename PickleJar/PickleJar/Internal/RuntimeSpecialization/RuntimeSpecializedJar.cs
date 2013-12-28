using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Strilanc.PickleJar.Internal.Structured;

namespace Strilanc.PickleJar.Internal.RuntimeSpecialization {
    /// <summary>
    /// RuntimeSpecializedJar parses a value by using reflection to match up named parsers with fields/properties/constructor-parameters of that type.
    /// Creates a method, dynamically optimized at runtime, that runs the field parsers and initializes the type with their results.
    /// Attempts to inline the expressions used to parse fields, in order to avoid intermediate values to increase efficiency.
    /// </summary>
    internal static class RuntimeSpecializedJar {
        private static IReadOnlyDictionary<MemberMatchInfo, MemberInfo> GetMutableMemberMap<T>() {
            var mutableFields = typeof(T).GetFields()
                                         .Where(e => e.IsPublic)
                                         .Where(e => !e.IsInitOnly)
                                         .Select(e => new { value = (MemberInfo)e, key = e.MatchInfo() });
            var mutableProperties = typeof(T).GetProperties()
                                             .Where(e => e.CanWrite)
                                             .Where(e => e.SetMethod.IsPublic)
                                             .Select(e => new { value = (MemberInfo)e, key = e.MatchInfo() });
            return mutableFields.Concat(mutableProperties)
                                .ToDictionary(e => e.key, e => e.value);
        }
        private static ConstructorInfo ChooseCompatibleConstructor<T>(IEnumerable<MemberMatchInfo> mutableMembers, IEnumerable<MemberMatchInfo> parsers) {
            var possibleConstructors = (from c in typeof(T).GetConstructors()
                                        where c.IsPublic
                                        let parameterNames = c.GetParameters().Select(e => e.MatchInfo()).ToArray()
                                        where parameterNames.IsSameOrSubsetOf(parsers)
                                        where parsers.IsSameOrSubsetOf(parameterNames.Concat(mutableMembers))
                                        select c
                                       ).ToArray();
            if (possibleConstructors.Length == 0) {
                if (typeof(T).IsValueType && parsers.IsSameOrSubsetOf(mutableMembers)) 
                    return null;
                throw new ArgumentException("No constructor with a parameter for each readonly parsed values (with no extra non-parsed-value parameters).");
            }
            return possibleConstructors.MaxBy(e => e.GetParameters().Count());
        }

        public static IJar<T> MakeBySequenceAndInject<T>(IEnumerable<IJarForMember> memberJars) {
            var jarsCopy = memberJars.ToArray();
            var canBeFollowed = jarsCopy.Length == 0 || jarsCopy.Last().CanBeFollowed();

            return AnonymousJar.CreateSpecialized(
                specializedParserMaker: (array, offset, count) => MakeInlinedParserComponents<T>(jarsCopy, array, offset, count),
                packer: MakePacker<T>(jarsCopy),
                canBeFollowed: canBeFollowed,
                isBlittable: false,
                constLength: jarsCopy.Select(e => e.OptionalConstantSerializedLength()).Sum(),
                desc: () => string.Format("{0}.BuildForType<{1}>()", jarsCopy.StringJoinList("[", ", ", "]"), typeof(T)),
                components: memberJars);
        }
        public static SpecializedParserParts MakeInlinedParserComponents<T>(IJarForMember[] memberJars, Expression array, Expression offset, Expression count) {
            if (memberJars == null) throw new ArgumentNullException("memberJars");
            if (array == null) throw new ArgumentNullException("array");
            if (offset == null) throw new ArgumentNullException("offset");
            if (count == null) throw new ArgumentNullException("count");

            var varResultValue = Expression.Variable(typeof(T));
            var memberJarMap = memberJars.KeyedBy(e => e.MemberMatchInfo);
            var mutableMemberMap = GetMutableMemberMap<T>();

            var chosenConstructor = ChooseCompatibleConstructor<T>(mutableMemberMap.Keys, memberJarMap.Keys);
            var parameterMap = (chosenConstructor == null ? new ParameterInfo[0] : chosenConstructor.GetParameters())
                .KeyedBy(e => e.MatchInfo());
            var unmatched = memberJars
                .Where(e => !mutableMemberMap.ContainsKey(e.MemberMatchInfo))
                .Where(e => !parameterMap.ContainsKey(e.MemberMatchInfo))
                .Select(e => e.MemberMatchInfo);
            var mismatched = mutableMemberMap
                .Where(e => !parameterMap.ContainsKey(e.Key))
                .Where(e => memberJarMap.ContainsKey(e.Key))
                .Where(e => memberJarMap[e.Key].MemberMatchInfo.MemberType != e.Value.GetMemberSettableType())
                .Select(e => e.Key);
            var unmatchedValue = unmatched
                .Concat(mismatched)
                .FirstOrNull();
            if (unmatchedValue.HasValue) {
                throw new ArgumentException(string.Format(
                    "Failed to find a member matching {0} on type {1}",
                    unmatchedValue.Value,
                    typeof(T)));
            }

            var parseSequence = SequencedJarUtil.BuildComponentsOfParsingSequence(
                memberJars.Select(e => new JarMeta(e.Jar, e.MemberMatchInfo.MemberType)), 
                array, 
                offset, 
                count);

            var memberValueGetters = parseSequence.ValueGetters.Zip(memberJars, Tuple.Create).ToDictionary(e => e.Item2.MemberMatchInfo, e => e.Item1);
            var valueConstructedFromParsedValues = 
                chosenConstructor == null 
                ? (Expression)Expression.Default(typeof(T))
                : Expression.New(chosenConstructor,
                                 chosenConstructor.GetParameters().Select(e => memberValueGetters[e.MatchInfo()]));

            var assignMutableMembersBlock =
                memberJarMap
                .Where(e => !parameterMap.ContainsKey(e.Key))
                .Select(e => Expression.Assign(
                    Expression.MakeMemberAccess(varResultValue, mutableMemberMap[e.Key]),
                    memberValueGetters[e.Key]))
                .Block();

            var parseDoer = Expression.Block(
                parseSequence.Storage.ForValueIfConsumedCountAlreadyInScope,
                new[] {
                    parseSequence.ParseDoer,
                    Expression.Assign(varResultValue, valueConstructedFromParsedValues),
                    assignMutableMembersBlock
                });

            return new SpecializedParserParts(
                parseDoer: parseDoer,
                valueGetter: varResultValue,
                consumedCountGetter: parseSequence.ConsumedCountGetter,
                storage: new SpecializedParserResultStorageParts(new[] {varResultValue}, parseSequence.Storage.ForConsumedCount));
        }

        private static Func<T, byte[]> MakePacker<T>(IJarForMember[] memberJars) {
            var memberMap = memberJars.ToDictionary(
                e => e.MemberMatchInfo,
                e => new { memberJar = e, memberGetter = e.PickMatchingMemberGetterForType(typeof (T))});

            var param = Expression.Parameter(typeof (T), "value");
            var resVar = Expression.Variable(typeof (List<byte[]>), "res");
            var statements = Expression.Block(
                (from memberJar in memberJars
                 let packMethod = typeof(IJar<>).MakeGenericType(memberJar.MemberMatchInfo.MemberType).GetMethod("Pack")
                 let packAccess = memberMap[memberJar.MemberMatchInfo].memberGetter(param)
                 let packCall = Expression.Call(Expression.Constant(memberJar.Jar), packMethod, new[] {packAccess})
                 select Expression.Call(resVar, typeof(List<byte[]>).GetMethod("Add"), new Expression[] { packCall })).Block(),
                Expression.Assign(resVar, Expression.New(typeof (List<byte[]>).GetConstructor(new Type[0]).NotNull())));

            // todo: inlining

            var flattened = Expression.Call(typeof (CollectionUtil).GetMethod("Flatten"), new Expression[] {resVar});
            var body = Expression.Block(
                new[] {resVar},
                statements,
                flattened);
            var method = Expression.Lambda<Func<T, byte[]>>(
                body,
                new[] { param });

            return method.Compile();
        }
    }
}