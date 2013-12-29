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
        private readonly SpecializedParserMaker _tryInlinedParserComponents;
        private readonly Func<Expression, SpecializedPackerParts?> _tryMakeSpecializedPackerParts;
        private readonly Func<string> _desc;
        private readonly object _components;

        public AnonymousJar(Func<ArraySegment<byte>, ParsedValue<T>> parse,
                            Func<T, byte[]> pack,
                            bool canBeFollowed,
                            bool isBlittable,
                            int? optionalConstantSerializedLength,
                            SpecializedParserMaker tryInlinedParserComponents,
                            Func<string> desc = null,
                            object components = null,
                            Func<Expression, SpecializedPackerParts?> tryMakeSpecializedPackerParts = null) {
            if (parse == null) throw new ArgumentNullException("parse");
            if (pack == null) throw new ArgumentNullException("pack");
            _parse = parse;
            _pack = pack;
            CanBeFollowed = canBeFollowed;
            IsBlittable = isBlittable;
            OptionalConstantSerializedLength = optionalConstantSerializedLength;
            _tryInlinedParserComponents = tryInlinedParserComponents;
            _desc = desc;
            _components = components;
            _tryMakeSpecializedPackerParts = tryMakeSpecializedPackerParts;
        }

        public ParsedValue<T> Parse(ArraySegment<byte> data) {
            return _parse(data);
        }
        public byte[] Pack(T value) {
            return _pack(value);
        }
        public SpecializedParserParts TryMakeInlinedParserComponents(Expression array, Expression offset, Expression count) {
            if (_tryInlinedParserComponents == null) return null;
            return _tryInlinedParserComponents(array, offset, count);
        }
        public SpecializedPackerParts? TryMakeSpecializedPackerParts(Expression value) {
            if (_tryMakeSpecializedPackerParts == null) return null;
            return _tryMakeSpecializedPackerParts(value);
        }
        public override string ToString() {
            return _desc == null ? base.ToString() : _desc();
        }
    }

    internal static class AnonymousJar {
        public static AnonymousJar<T> CreateSpecialized<T>(SpecializedParserMaker specializedParserMaker,
                                                           SpecializedPackerMaker specializedPacker,
                                                           bool canBeFollowed,
                                                           bool isBlittable,
                                                           int? constLength,
                                                           Func<string> desc = null,
                                                           object components = null) {
            if (specializedParserMaker == null) throw new ArgumentNullException("specializedParserMaker");
            if (specializedPacker == null) throw new ArgumentNullException("specializedPacker");
            return new AnonymousJar<T>(SpecializedParserParts.MakeParser<T>(specializedParserMaker),
                                       SpecializedPackerParts.MakePacker<T>(specializedPacker),
                                       canBeFollowed,
                                       isBlittable,
                                       constLength,
                                       specializedParserMaker,
                                       desc,
                                       components,
                                       e => specializedPacker(e));
        }
    }
}