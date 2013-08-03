using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MoreLinq;

namespace Strilanc.PickleJar.Internal.Structured {
    /// <summary>
    /// TypeJarCompiled parses a value by using reflection to match up named parsers with fields/properties/constructor-parameters of that type.
    /// Creates a method, dynamically optimized at runtime, that runs the field parsers and initializes the type with their results.
    /// Attempts to inline the expressions used to parse fields, in order to avoid intermediate values to increase efficiency.
    /// </summary>
    internal sealed class TypeJarCompiled<T> : IJarMetadataInternal, IJar<T> {
        private readonly IReadOnlyList<IJarForMember> _memberJars;
        private readonly Func<ArraySegment<byte>, ParsedValue<T>> _parser;
        private readonly Func<T, byte[]> _packer;

        public TypeJarCompiled(IReadOnlyList<IJarForMember> memberJars) {
            if (memberJars == null) throw new ArgumentNullException("memberJars");
            _memberJars = memberJars;
            _parser = MakeParser();
            _packer = MakePacker();
        }

        private static IReadOnlyDictionary<MemberMatchInfo, MemberInfo> GetMutableMemberMap() {
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
        private static ConstructorInfo ChooseCompatibleConstructor(IEnumerable<MemberMatchInfo> mutableMembers, IEnumerable<MemberMatchInfo> parsers) {
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

        private Func<ArraySegment<byte>, ParsedValue<T>> MakeParser() {
            var paramData = Expression.Parameter(typeof(ArraySegment<byte>), "data");
            var paramDataArray = Expression.MakeMemberAccess(paramData, typeof(ArraySegment<byte>).GetProperty("Array"));
            var paramDataOffset = Expression.MakeMemberAccess(paramData, typeof(ArraySegment<byte>).GetProperty("Offset"));
            var paramDataCount = Expression.MakeMemberAccess(paramData, typeof(ArraySegment<byte>).GetProperty("Count"));

            var bodyAndVars = TryMakeInlinedParserComponents(paramDataArray, paramDataOffset, paramDataCount);
    
            var method = Expression.Lambda<Func<ArraySegment<byte>, ParsedValue<T>>>(
                Expression.Block(
                    bodyAndVars.ResultStorage,
                    new[] {
                        bodyAndVars.PerformParse,
                        Expression.New(typeof (ParsedValue<T>).GetConstructor(new[] {typeof (T), typeof (int)}).NotNull(),
                                       bodyAndVars.AfterParseValueGetter,
                                       bodyAndVars.AfterParseConsumedGetter)
                    }),
                new[] {paramData});

            return method.Compile();
        }

        public ParsedValue<T> Parse(ArraySegment<byte> data) {
            return _parser(data);
        }
        public InlinedParserComponents TryMakeInlinedParserComponents(Expression array, Expression offset, Expression count) {
            var varResultValue = Expression.Variable(typeof(T));
            var varTotal = Expression.Variable(typeof(int));
            var parserMap = _memberJars.KeyedBy(e => e.MemberMatchInfo);
            var mutableMemberMap = GetMutableMemberMap();

            var chosenConstructor = ChooseCompatibleConstructor(mutableMemberMap.Keys, parserMap.Keys);
            var parameterMap = (chosenConstructor == null ? new ParameterInfo[0] : chosenConstructor.GetParameters())
                .KeyedBy(e => e.MatchInfo());

            var initLocals = Expression.Assign(varTotal, Expression.Constant(0));

            var fieldParsings = (from fieldParser in _memberJars
                                 let inlinedParseComponents = fieldParser.MakeInlinedParserComponents(
                                     array,
                                     Expression.Add(offset, varTotal),
                                     Expression.Subtract(count, varTotal))
                                 select new { fieldParser, inlinedParseComponents }
                                 ).ToArray();

            var parseFieldsAndStoreResultsBlock = fieldParsings.Select(e => Expression.Block(
                e.inlinedParseComponents.PerformParse,
                Expression.AddAssign(varTotal, e.inlinedParseComponents.AfterParseConsumedGetter))).Block();

            var parseValMap = fieldParsings.KeyedBy(e => e.fieldParser.MemberMatchInfo);
            var valueConstructedFromParsedValues = 
                chosenConstructor == null 
                ? (Expression)Expression.Default(typeof(T))
                : Expression.New(chosenConstructor,
                                 chosenConstructor.GetParameters().Select(e => parseValMap[e.MatchInfo()].inlinedParseComponents.AfterParseValueGetter));

            var assignMutableMembersBlock =
                parserMap
                    .Where(e => !parameterMap.ContainsKey(e.Key))
                    .Select(e => Expression.Assign(
                        Expression.MakeMemberAccess(varResultValue, mutableMemberMap[e.Key]),
                        parseValMap[e.Key].inlinedParseComponents.AfterParseValueGetter))
                    .Block();

            var locals = fieldParsings.SelectMany(e => e.inlinedParseComponents.ResultStorage);
            var statements = new[] {
                initLocals,
                parseFieldsAndStoreResultsBlock,
                Expression.Assign(varResultValue, valueConstructedFromParsedValues),
                assignMutableMembersBlock
            };

            return new InlinedParserComponents(
                performParse: Expression.Block(locals, statements),
                afterParseValueGetter: varResultValue,
                afterParseConsumedGetter: varTotal,
                resultStorage: new[] {varTotal, varResultValue});
        }

        public bool AreMemoryAndSerializedRepresentationsOfValueGuaranteedToMatch { get { return false; } }
        public int? OptionalConstantSerializedLength { get { return _memberJars.Aggregate((int?)0, (a,e) => a + e.OptionalConstantSerializedLength()); } }
        public byte[] Pack(T value) {
            return _packer(value);
        }

        private Func<T, byte[]> MakePacker() {
            var memberMap = _memberJars.ToDictionary(
                e => e.MemberMatchInfo,
                e => new { memberJar = e, memberGetter = e.PickMatchingMemberGetterForType(typeof (T))});

            var param = Expression.Parameter(typeof (T), "value");
            var resVar = Expression.Variable(typeof (List<byte[]>), "res");
            var statements = Expression.Block(
                (from memberJar in _memberJars
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
        public override string ToString() {
            return string.Format(
                "{0}[compiled]",
                typeof(T));
        }
    }
}