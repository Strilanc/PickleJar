using System;
using System.Linq.Expressions;

internal static class ParserUtil {
    public static Expression MakeParseFromDataExpression<T>(this IParser<T> parser, Expression array, Expression offset, Expression count) {
        return parser.TryMakeParseFromDataExpression(array, offset, count)
               ?? Expression.Call(
                   Expression.Constant(parser),
                   typeof(IParser<T>).GetMethod("Parse"),
                   new Expression[] { 
                       Expression.New(
                           typeof(ArraySegment<byte>).GetConstructor(new[] {typeof(byte[]), typeof(int), typeof(int)}), 
                           new[] {array, offset, count}) });
    }

    public static Expression MakeGetValueFromParsedExpression<T>(this IParser<T> parser, Expression parsed) {
        return parser.TryMakeGetValueFromParsedExpression(parsed)
               ?? Expression.MakeMemberAccess(parsed, typeof(ParsedValue<>).MakeGenericType(typeof(T)).GetField("Value"));
    }
    public static Expression MakeGetCountFromParsedExpression<T>(this IParser<T> parser, Expression parsed) {
        return parser.TryMakeGetCountFromParsedExpression(parsed)
               ?? Expression.MakeMemberAccess(parsed, typeof(ParsedValue<>).MakeGenericType(typeof(T)).GetField("Consumed"));
    }    
}