using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MoreLinq;

internal sealed class ExpressionTreeParser<T> : IParser<T> {
    private readonly IReadOnlyList<IFieldParserOfUnknownType> _fieldParsers;
    private readonly Func<ArraySegment<byte>, ParsedValue<T>> _parser;

    public ExpressionTreeParser(IReadOnlyList<IFieldParserOfUnknownType> fieldParsers) {
        _fieldParsers = fieldParsers;
        _parser = MakeParser();
    }

    private static Dictionary<CanonicalizingMemberName, MemberInfo> GetMutableMemberMap() {
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

    private static readonly ParameterExpression VariableForResultValue = Expression.Variable(typeof(T), "result");
    private static readonly ParameterExpression VarTotal = Expression.Variable(typeof(int), "total");
    private static readonly ParameterExpression ParamData = Expression.Parameter(typeof(ArraySegment<byte>), "data");

    private Func<ArraySegment<byte>, ParsedValue<T>> MakeParser() {
        var paramDataArray = Expression.MakeMemberAccess(ParamData, typeof(ArraySegment<byte>).GetProperty("Array"));
        var paramDataOffset = Expression.MakeMemberAccess(ParamData, typeof(ArraySegment<byte>).GetProperty("Offset"));
        var paramDataCount = Expression.MakeMemberAccess(ParamData, typeof(ArraySegment<byte>).GetProperty("Count"));

        var body = TryMakeParseFromDataExpression(paramDataArray, paramDataOffset, paramDataCount);
    
        var method = Expression.Lambda<Func<ArraySegment<byte>, ParsedValue<T>>>(
            body,
            new[] {ParamData});

        return method.Compile();
    }

    public ParsedValue<T> Parse(ArraySegment<byte> data) {
        return _parser(data);
    }
    public Expression TryMakeParseFromDataExpression(Expression array, Expression offset, Expression count) {
        var parserMap = _fieldParsers.KeyedBy(e => e.CanonicalName());
        var mutableMemberMap = GetMutableMemberMap();

        var unmatchedReadOnlyField = typeof(T).GetFields().FirstOrDefault(e => e.IsInitOnly && !parserMap.ContainsKey(e.CanonicalName()));
        if (unmatchedReadOnlyField != null)
            throw new ArgumentException(string.Format("A readonly field named '{0}' of type {1} doesn't have a corresponding parser.", unmatchedReadOnlyField.Name, typeof(T)));

        var chosenConstructor = ChooseCompatibleConstructor(mutableMemberMap.Keys, parserMap.Keys);
        var parameterMap = (chosenConstructor == null ? new ParameterInfo[0] : chosenConstructor.GetParameters())
            .KeyedBy(e => e.CanonicalName());

        var initLocals = Expression.Assign(VarTotal, Expression.Constant(0));

        var fieldParsings = (from fieldParser in _fieldParsers
                             let invokeParse = fieldParser.MakeParseFromDataExpression(
                                 array,
                                 Expression.Add(offset, VarTotal),
                                 Expression.Subtract(count, VarTotal))
                             let variableForResultOfParsing = Expression.Variable(invokeParse.Type, fieldParser.Name)
                             let parsingValue = fieldParser.MakeGetValueFromParsedExpression(variableForResultOfParsing)
                             let parsingConsumed = fieldParser.MakeGetCountFromParsedExpression(variableForResultOfParsing)
                             select new { fieldParser, parsingValue, parsingConsumed, variableForResultOfParsing, invokeParse }
                             ).ToArray();

        var parseFieldsAndStoreResultsBlock = fieldParsings.Select(e => Expression.Block(
            Expression.Assign(e.variableForResultOfParsing, e.invokeParse),
            Expression.AddAssign(VarTotal, e.parsingConsumed))).Block();

        var parseValMap = fieldParsings.KeyedBy(e => e.fieldParser.CanonicalName());
        var valueConstructedFromParsedValues = chosenConstructor == null
            ? (Expression)Expression.Default(typeof(T))
            : Expression.New(
                chosenConstructor,
                chosenConstructor.GetParameters().Select(e => parseValMap[e.CanonicalName()].parsingValue));

        var assignMutableMembersBlock =
            parserMap
            .Where(e => !parameterMap.ContainsKey(e.Key))
            .Select(e => Expression.Assign(
                Expression.MakeMemberAccess(VariableForResultValue, mutableMemberMap[e.Key]),
                parseValMap[e.Key].parsingValue))
            .Block();

        var returned = Expression.New(
            typeof(ParsedValue<T>).GetConstructor(new[] { typeof(T), typeof(int) }),
            VariableForResultValue,
            VarTotal);

        var locals = fieldParsings.Select(e => e.variableForResultOfParsing)
            .Concat(new[] { VarTotal, VariableForResultValue });
        var statements = new[] {
            initLocals,
            parseFieldsAndStoreResultsBlock,
            Expression.Assign(VariableForResultValue, valueConstructedFromParsedValues),
            assignMutableMembersBlock,
            returned
        };

        return Expression.Block(
            locals,
            statements);
    }
    public Expression TryMakeGetValueFromParsedExpression(Expression parsed) {
        return Expression.MakeMemberAccess(parsed, typeof(ParsedValue<T>).GetMember("Value").Single());
    }
    public Expression TryMakeGetCountFromParsedExpression(Expression parsed) {
        return Expression.MakeMemberAccess(parsed, typeof(ParsedValue<T>).GetMember("Consumed").Single());
    }

    public bool IsBlittable { get { return false; } }
    public int? OptionalConstantSerializedLength { get { return _fieldParsers.Aggregate((int?)0, (a,e) => a + e.OptionalConstantSerializedLength); } }
}