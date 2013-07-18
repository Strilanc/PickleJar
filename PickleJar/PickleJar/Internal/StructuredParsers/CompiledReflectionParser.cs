using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MoreLinq;

namespace Strilanc.PickleJar.Internal.StructuredParsers {
    /// <summary>
    /// CompiledReflectionParser parses a value by using reflection to match up named parsers with fields/properties/constructor-parameters of that type.
    /// Creates a method, dynamically optimized at runtime, that runs the field parsers and initializes the type with their results.
    /// Attempts to inline the expressions used to parse fields, in order to avoid intermediate values to increase efficiency.
    /// </summary>
    internal sealed class CompiledReflectionParser<T> : IParserInternal<T> {
        private readonly IReadOnlyList<IFieldParser> _fieldParsers;
        private readonly Func<ArraySegment<byte>, ParsedValue<T>> _parser;

        public CompiledReflectionParser(IReadOnlyList<IFieldParser> fieldParsers) {
            _fieldParsers = fieldParsers;
            _parser = MakeParser();
        }

        private static IReadOnlyDictionary<CanonicalizingMemberName, MemberInfo> GetMutableMemberMap() {
            var mutableFields = typeof(T).GetFields()
                                         .Where(e => e.IsPublic)
                                         .Where(e => !e.IsInitOnly);
            var mutableProperties = typeof(T).GetProperties()
                                             .Where(e => e.CanWrite)
                                             .Where(e => e.SetMethod.IsPublic);
            return mutableFields.Cast<MemberInfo>()
                                .Concat(mutableProperties)
                                .KeyedBy(e => e.CanonicalName());
        }
        private static ConstructorInfo ChooseCompatibleConstructor(IEnumerable<CanonicalizingMemberName> mutableMembers, IEnumerable<CanonicalizingMemberName> parsers) {
            var possibleConstructors = (from c in typeof(T).GetConstructors()
                                        where c.IsPublic
                                        let parameterNames = c.GetParameters().Select(e => e.CanonicalName()).ToArray()
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
            var parserMap = _fieldParsers.KeyedBy(e => e.CanonicalName);
            var mutableMemberMap = GetMutableMemberMap();

            var unmatchedReadOnlyField = typeof(T).GetFields().FirstOrDefault(e => e.IsInitOnly && !parserMap.ContainsKey(e.CanonicalName()));
            if (unmatchedReadOnlyField != null)
                throw new ArgumentException(string.Format("A readonly field named '{0}' of type {1} doesn't have a corresponding fieldParser.", unmatchedReadOnlyField.Name, typeof(T)));

            var chosenConstructor = ChooseCompatibleConstructor(mutableMemberMap.Keys, parserMap.Keys);
            var parameterMap = (chosenConstructor == null ? new ParameterInfo[0] : chosenConstructor.GetParameters())
                .KeyedBy(e => e.CanonicalName());

            var initLocals = Expression.Assign(varTotal, Expression.Constant(0));

            var fieldParsings = (from fieldParser in _fieldParsers
                                 let inlinedParseComponents = fieldParser.MakeInlinedParserComponents(
                                     array,
                                     Expression.Add(offset, varTotal),
                                     Expression.Subtract(count, varTotal))
                                 select new { fieldParser, inlinedParseComponents }
                                 ).ToArray();

            var parseFieldsAndStoreResultsBlock = fieldParsings.Select(e => Expression.Block(
                e.inlinedParseComponents.PerformParse,
                Expression.AddAssign(varTotal, e.inlinedParseComponents.AfterParseConsumedGetter))).Block();

            var parseValMap = fieldParsings.KeyedBy(e => e.fieldParser.CanonicalName);
            var valueConstructedFromParsedValues = 
                chosenConstructor == null 
                ? (Expression)Expression.Default(typeof(T))
                : Expression.New(chosenConstructor,
                                 chosenConstructor.GetParameters().Select(e => parseValMap[e.CanonicalName()].inlinedParseComponents.AfterParseValueGetter));

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
        public int? OptionalConstantSerializedLength { get { return _fieldParsers.Aggregate((int?)0, (a,e) => a + e.OptionalConstantSerializedLength()); } }
    }
}