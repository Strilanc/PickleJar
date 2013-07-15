using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;

namespace Strilanc.Parsing.Internal.UnsafeParsers {
    internal sealed class BlittableStructParser<T> : IParserInternal<T> {
        private readonly int _length;
        private readonly UnsafeBlitUtil.UnsafeValueBlitParser<T> _parser;
        private BlittableStructParser(IEnumerable<IFieldParserOfUnknownType> fieldParsers) {
            var len = fieldParsers.Aggregate((int?)0, (a, e) => a + e.OptionalConstantSerializedLength);
            if (!len.HasValue) throw new ArgumentException();
            _parser = UnsafeBlitUtil.MakeUnsafeValueBlitParser<T>();
            _length = len.Value;
        }

        public ParsedValue<T> Parse(ArraySegment<byte> data) {
            if (data.Count < _length) throw new InvalidOperationException("Fragment");
            var value = _parser(data.Array, data.Offset, _length);
            return new ParsedValue<T>(value, _length);
        }
        public bool IsBlittable { get { return true; } }
        public int? OptionalConstantSerializedLength { get { return _length; } }

        public static BlittableStructParser<T> TryMake(IReadOnlyList<IFieldParserOfUnknownType> fieldParsers) {
            if (!CanBlitParseWith(fieldParsers)) return null;
            return new BlittableStructParser<T>(fieldParsers);
        }

        private static bool CanBlitParseWith(IReadOnlyList<IFieldParserOfUnknownType> fieldParsers) {
            if (fieldParsers == null) throw new ArgumentNullException("fieldParsers");

            // type has blittable representation?
            if (!Util.IsBlittable<T>()) return false;

            // all parsers have same constant length representation as value in memory?
            if (fieldParsers.Any(e => !e.IsBlittable)) return false;
            if (fieldParsers.Any(e => !e.OptionalConstantSerializedLength.HasValue)) return false;

            // type has no padding?
            var structLayout = typeof(T).StructLayoutAttribute;
            if (structLayout == null) return false;
            if (structLayout.Value != LayoutKind.Sequential) return false;
            if (structLayout.Pack != 1) return false;

            // parsers and struct fields have matching canonical names?
            var serialNames = fieldParsers.Select(e => e.CanonicalName());
            var fieldNames = typeof(T).GetFields().Select(e => e.CanonicalName());
            if (!serialNames.HasSameSetOfItemsAs(fieldNames)) return false;

            // offsets implied by parser ordering matches offsets of the struct's fields?
            var memoryOffsets =
                typeof(T).GetFields().ToDictionary(
                    e => e.CanonicalName(),
                    e => (int?)typeof(T).FieldOffsetOf(e));
            var serialOffsets =
                fieldParsers
                    .StreamZip((int?)0, (a, e) => a + e.OptionalConstantSerializedLength)
                    .ToDictionary(e => e.Item1.CanonicalName(), e => e.Item2 - e.Item1.OptionalConstantSerializedLength);
            if (!serialOffsets.HasSameKeyValuesAs(memoryOffsets)) return false;

            return true;
        }

        public Expression TryMakeParseFromDataExpression(Expression array, Expression offset, Expression count) {
            return null;
        }
        public Expression TryMakeGetValueFromParsedExpression(Expression parsed) {
            return null;
        }
        public Expression TryMakeGetCountFromParsedExpression(Expression parsed) {
            return null;
        }
    }
}