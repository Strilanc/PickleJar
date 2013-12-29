using System;
using System.Diagnostics;
using System.Linq.Expressions;
using Strilanc.PickleJar.Internal.RuntimeSpecialization;

namespace Strilanc.PickleJar.Internal.Basic {
    /// <summary>
    /// Uses delegates and values given to its constructor to implement a jar.
    /// </summary>
    [DebuggerDisplay("{ToString()}")]
    internal sealed class AnonymousJar<T> : IJar<T>, IJarMetadataInternal {
        private readonly Func<ArraySegment<byte>, ParsedValue<T>> _parse;
        private readonly Func<T, byte[]> _pack;
        public bool CanBeFollowed { get; private set; }
        public bool IsBlittable { get; private set; }
        public int? OptionalConstantSerializedLength { get; private set; }
        private readonly ParseSpecializer _trySpecializeParser;
        private readonly PackSpecializer _specializePacker;
        private readonly Func<string> _desc;
        private readonly object _components;

        public AnonymousJar(Func<ArraySegment<byte>, ParsedValue<T>> parse,
                            Func<T, byte[]> pack,
                            bool canBeFollowed,
                            bool isBlittable = false,
                            int? optionalConstantSerializedLength = null,
                            ParseSpecializer trySpecializeParser = null,
                            PackSpecializer specializePacker = null,
                            Func<string> desc = null,
                            object components = null) {
            _parse = parse;
            _pack = pack;
            CanBeFollowed = canBeFollowed;
            IsBlittable = isBlittable;
            OptionalConstantSerializedLength = optionalConstantSerializedLength;
            _trySpecializeParser = trySpecializeParser;
            _desc = desc;
            _components = components;
            _specializePacker = specializePacker;
        }

        public ParsedValue<T> Parse(ArraySegment<byte> data) {
            return _parse(data);
        }
        public byte[] Pack(T value) {
            return _pack(value);
        }
        public SpecializedParserParts TrySpecializeParser(Expression array, Expression offset, Expression count) {
            if (array == null) throw new ArgumentNullException("array");
            if (offset == null) throw new ArgumentNullException("offset");
            if (count == null) throw new ArgumentNullException("count");
            if (_trySpecializeParser == null) return null;
            return _trySpecializeParser(array, offset, count);
        }
        public SpecializedPackerParts? TrySpecializePacker(Expression value) {
            if (value == null) throw new ArgumentNullException("value");
            if (_specializePacker == null) return null;
            return _specializePacker(value);
        }
        public override string ToString() {
            return _desc == null ? base.ToString() : _desc();
        }
    }

    internal static class AnonymousJar {
        public static AnonymousJar<T> CreateSpecialized<T>(ParseSpecializer parseSpecializer,
                                                           PackSpecializer packSpecializer,
                                                           bool canBeFollowed,
                                                           bool isBlittable = false,
                                                           int? constLength = null,
                                                           Func<string> desc = null,
                                                           object components = null) {
            if (parseSpecializer == null) throw new ArgumentNullException("parseSpecializer");
            if (packSpecializer == null) throw new ArgumentNullException("packSpecializer");
            return new AnonymousJar<T>(SpecializedParserParts.MakeParser<T>(parseSpecializer),
                                       SpecializedPackerParts.MakePacker<T>(packSpecializer),
                                       canBeFollowed,
                                       isBlittable,
                                       constLength,
                                       parseSpecializer,
                                       packSpecializer,
                                       desc,
                                       components);
        }
    }
}