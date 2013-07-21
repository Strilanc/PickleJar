using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Linq;
using MoreLinq;
using Strilanc.PickleJar.Internal.Repeated;
using Strilanc.PickleJar.Internal.Structured;
using Strilanc.PickleJar.Internal.Unsafe;

namespace Strilanc.PickleJar.Internal {
    /// <summary>
    /// ParserUtil contains internal utility and convenience methods related to parsing and optimizing parsers.
    /// </summary>
    internal static class ParserUtil {
        /// <summary>
        /// Returns an appropriate parser capable of bulk parsing operations, based on the given item parser.
        /// </summary>
        public static IBulkParser<T> Bulk<T>(this IParser<T> itemParser) {
            if (itemParser == null) throw new ArgumentNullException("itemParser");

            return (IBulkParser<T>)BlittableBulkParser<T>.TryMake(itemParser)
                   ?? new CompiledBulkParser<T>(itemParser);
        }
        public static ParsedValue<T> AsParsed<T>(this T value, int consumed) {
            return new ParsedValue<T>(value, consumed);
        }
        public static ParsedValue<TOut> Select<TIn, TOut>(this ParsedValue<TIn> value, Func<TIn, TOut> projection) {
            return new ParsedValue<TOut>(projection(value.Value), value.Consumed);
        }

        public static bool AreMemoryAndSerializedRepresentationsOfValueGuaranteedToMatch<T>(this IParser<T> parser) {
            var r = parser as IParserInternal<T>;
            return r != null && r.AreMemoryAndSerializedRepresentationsOfValueGuaranteedToMatch;
        }
        public static bool AreMemoryAndSerializedRepresentationsOfValueGuaranteedToMatch(this IFieldParser parser) {
            var r = parser as IFieldParserInternal;
            return r != null && r.AreMemoryAndSerializedRepresentationsOfValueGuaranteedToMatch;
        }

        public static int? OptionalConstantSerializedLength<T>(this IParser<T> parser) {
            var r = parser as IParserInternal<T>;
            return r == null ? null : r.OptionalConstantSerializedLength;
        }
        public static int? OptionalConstantSerializedLength(this IFieldParser parser) {
            var r = parser as IFieldParserInternal;
            return r == null ? null : r.OptionalConstantSerializedLength;
        }

        private static InlinedParserComponents MakeDefaultInlinedParserComponents(object parser, Type valueType, Expression array, Expression offset, Expression count) {
            var resultVar = Expression.Variable(valueType, "parsed");
            var parse = Expression.Call(
                Expression.Constant(parser),
                typeof(IParser<>).MakeGenericType(valueType).GetMethod("Parse"),
                new Expression[] {
                    Expression.New(
                        typeof (ArraySegment<byte>).GetConstructor(new[] {typeof (byte[]), typeof (int), typeof (int)}).NotNull(),
                        new[] {array, offset, count})
                });

            return new InlinedParserComponents(
                performParse: Expression.Assign(resultVar, parse),
                afterParseValueGetter: Expression.MakeMemberAccess(resultVar, typeof(ParsedValue<>).MakeGenericType(valueType).GetField("Value")),
                afterParseConsumedGetter: Expression.MakeMemberAccess(resultVar, typeof(ParsedValue<>).MakeGenericType(valueType).GetField("Consumed")),
                resultStorage: new[] {resultVar});
        }
        public static InlinedParserComponents MakeInlinedParserComponents<T>(this IParser<T> parser, Expression array, Expression offset, Expression count) {
            var r = parser as IParserInternal<T>;
            return (r == null ? null : r.TryMakeInlinedParserComponents(array, offset, count))
                   ?? MakeDefaultInlinedParserComponents(parser, typeof(T), array, offset, count);
        }
        public static InlinedParserComponents MakeInlinedParserComponents(this IFieldParser fieldParser, Expression array, Expression offset, Expression count) {
            var r = fieldParser as IFieldParserInternal;
            return (r == null ? null : r.TryMakeInlinedParserComponents(array, offset, count))
                   ?? MakeDefaultInlinedParserComponents(fieldParser.Parser, fieldParser.ParserValueType, array, offset, count);
        }

        public static InlinedParserComponents MakeInlinedNumberParserComponents<T>(bool isSystemEndian, Expression array, Expression offset, Expression count) {
            var varParsedNumber = Expression.Parameter(typeof(T));
            return new InlinedParserComponents(
                performParse: Expression.Assign(varParsedNumber, MakeInlinedNumberParserExpression<T>(isSystemEndian, array, offset, count)),
                afterParseValueGetter: varParsedNumber,
                afterParseConsumedGetter: Expression.Constant(Marshal.SizeOf(typeof(T))),
                resultStorage: new[] {varParsedNumber});
        }
        private static Expression MakeInlinedNumberParserExpression<T>(bool isSystemEndian, Expression array, Expression offset, Expression count) {
            var numberTypes = new[] {
                typeof (byte), typeof (short), typeof (int), typeof (long),
                typeof (sbyte), typeof (ushort), typeof (uint), typeof (ulong),
                typeof(float), typeof(double)
            };
            if (!numberTypes.Contains(typeof(T))) throw new ArgumentException("Unrecognized number type.");

            if (typeof(T) == typeof(byte) || typeof(T) == typeof(sbyte)) {
                var v = (Expression)Expression.ArrayIndex(array, offset);
                v = typeof (T) == typeof (byte) ? v : Expression.Convert(Expression.ArrayIndex(array, offset), typeof (sbyte));
                return v;
            }

            var boundsCheck = Expression.IfThen(Expression.LessThan(count, Expression.Constant(Marshal.SizeOf(typeof(T)))), DataFragmentException.CachedThrowExpression);
            var value = Expression.Call(typeof(BitConverter).GetMethod("To" + typeof(T).Name), array, offset);
            var result = isSystemEndian
                       ? value 
                       : Expression.Call(typeof(TwiddleUtil).GetMethod("ReverseBytes", new[] { typeof(T) }), value);
            return Expression.Block(boundsCheck, result);
        }

        public static T NotNull<T>(this T value) where T : class {
            if (value == null) throw new NullReferenceException();
            return value;
        }


        public static int FieldOffsetOf(this Type type, FieldInfo field) {
            return (int)Marshal.OffsetOf(type, field.Name);
        }
        public static bool IsBlittable<T>() {
            return IsBlittable(typeof(T));
        }
        public static bool IsBlittable(this Type type) {
            var blittablePrimitives = new[] {
            typeof(byte),
            typeof(sbyte),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong)
        };
            return blittablePrimitives.Contains(type)
                   || (type.IsArray && type.GetElementType().IsValueType && type.GetElementType().IsBlittable())
                   || type.GetFields().All(e => e.FieldType.IsBlittable());
        }
        public static Expression Block(this IEnumerable<Expression> expressions) {
            var exp = expressions.ToArray();
            if (exp.Length == 0) return Expression.Empty();
            return Expression.Block(exp);
        }
        public static CanonicalizingMemberName CanonicalName(this MemberInfo member) {
            return new CanonicalizingMemberName(member.Name);
        }
        public static CanonicalizingMemberName CanonicalName(this ParameterInfo parameter) {
            return new CanonicalizingMemberName(parameter.Name);
        }
        public static IFieldParser ForField<T>(this IParser<T> parser, string fieldName) {
            return new FieldParser<T>(parser, fieldName);
        }
    }
}
