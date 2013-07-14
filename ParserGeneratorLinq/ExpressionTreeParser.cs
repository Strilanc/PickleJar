using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using MoreLinq;
using ParserGenerator.Blittable;

public sealed class ExpressionTreeParser<T> : IParser<T> {
    private readonly IReadOnlyList<IFieldParserOfUnknownType> _fieldParsers;
    private readonly Func<ArraySegment<byte>, ParsedValue<T>> _parser;
    private readonly Lazy<bool> _isBlittable;

    public ExpressionTreeParser(IReadOnlyList<IFieldParserOfUnknownType> fieldParsers) {
        _fieldParsers = fieldParsers;
        _parser = MakeParser(fieldParsers);
        _isBlittable = new Lazy<bool>(() => BlittableStructParser<T>.IsBlitParsableBy(fieldParsers));
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

    private static Func<ArraySegment<byte>, ParsedValue<T>> MakeParser(IEnumerable<IFieldParserOfUnknownType> fieldParsers) {
        var parserMap = fieldParsers.KeyedBy(e => e.CanonicalName());
        var mutableMemberMap = GetMutableMemberMap();

        var unmatchedReadOnlyField = typeof (T).GetFields().FirstOrDefault(e => e.IsInitOnly && !parserMap.ContainsKey(e.CanonicalName()));
        if (unmatchedReadOnlyField != null) 
            throw new ArgumentException(string.Format("A readonly field named '{0}' of type {1} doesn't have a corresponding parser.", unmatchedReadOnlyField.Name, typeof(T)));

        var chosenConstructor = ChooseCompatibleConstructor(mutableMemberMap.Keys, parserMap.Keys);
        var parameterMap = (chosenConstructor == null ? new ParameterInfo[0] : chosenConstructor.GetParameters())
            .KeyedBy(e => e.CanonicalName());

        var dataArg = Expression.Parameter(typeof (ArraySegment<byte>), "data");
        var variableForResultValue = Expression.Variable(typeof(T), "result");

        var fieldParsings = (from fieldParser in fieldParsers
                             let parsedValueType = typeof(ParsedValue<>).MakeGenericType(fieldParser.ParserValueType)
                             let variableForResultOfParsing = Expression.Variable(parsedValueType, fieldParser.Name)
                             let parsingValue = Expression.MakeMemberAccess(variableForResultOfParsing, parsedValueType.GetField("Value"))
                             let parsingConsumed = Expression.MakeMemberAccess(variableForResultOfParsing, parsedValueType.GetField("Consumed"))
                             select new { fieldParser, parsingValue, parsingConsumed, variableForResultOfParsing }
                             ).ToArray();

        var parseFieldsAndStoreResultsBlock = fieldParsings.Select(e => {
            var invokeParse = Expression.Call(
                Expression.Constant(e.fieldParser.Parser),
                typeof(IParser<>).MakeGenericType(e.fieldParser.ParserValueType).GetMethod("Parse"),
                new Expression[] { dataArg });

            var nextDataOffset = Expression.Add(
                Expression.MakeMemberAccess(dataArg, typeof(ArraySegment<byte>).GetProperty("Offset")),
                e.parsingConsumed);
            var remainingDataCount = Expression.Subtract(
                Expression.MakeMemberAccess(dataArg, typeof(ArraySegment<byte>).GetProperty("Count")),
                e.parsingConsumed);
            var nextData = Expression.New(
                typeof(ArraySegment<byte>).GetConstructor(new[] { typeof(byte[]), typeof(int), typeof(int) }),
                Expression.MakeMemberAccess(dataArg, typeof(ArraySegment<byte>).GetProperty("Array")),
                nextDataOffset,
                remainingDataCount);

            return Expression.Block(
                Expression.Assign(e.variableForResultOfParsing, invokeParse),
                Expression.Assign(dataArg, nextData));
        }).Block();

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
                Expression.MakeMemberAccess(variableForResultValue, mutableMemberMap[e.Key]),
                parseValMap[e.Key].parsingValue))
            .Block();

        var totalConsumed = fieldParsings.Select(e => (Expression)e.parsingConsumed).Aggregate(Expression.Add);
        var returned = Expression.New(
            typeof(ParsedValue<T>).GetConstructor(new[] { typeof(T), typeof(int) }),
            variableForResultValue,
            totalConsumed);

        var locals = fieldParsings.Select(e => e.variableForResultOfParsing).Concat(new[] { variableForResultValue });
        var statements = new[] {
            parseFieldsAndStoreResultsBlock,
            Expression.Assign(variableForResultValue, valueConstructedFromParsedValues),
            assignMutableMembersBlock,
            returned
        };

        var method = Expression.Lambda<Func<ArraySegment<byte>, ParsedValue<T>>>(
            Expression.Block(
                locals, 
                statements),
            new[] {dataArg});

        return method.Compile();
    }
    public ParsedValue<T> Parse(ArraySegment<byte> data) {
        return _parser(data);
    }

    public bool IsBlittable { get { return _isBlittable.Value; } }
    public int? OptionalConstantSerializedLength {
        get {
            return _isBlittable.Value
                       ? (int?)Marshal.SizeOf(typeof (T))
                       : null;
        }
    }
}