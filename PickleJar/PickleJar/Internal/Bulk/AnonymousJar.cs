using System;
using System.Diagnostics;
using System.Linq.Expressions;
using Strilanc.PickleJar.Internal.RuntimeSpecialization;

namespace Strilanc.PickleJar.Internal {
    [DebuggerDisplay("{ToString()}")]
    internal sealed class AnonymousJar<T> : IJar<T>, IJarMetadataInternal {
        private readonly Func<ArraySegment<byte>, ParsedValue<T>> _parse;
        private readonly Func<T, byte[]> _pack;
        public bool CanBeFollowed { get; private set; }
        public bool IsBlittable { get; private set; }
        public int? OptionalConstantSerializedLength { get; private set; }
        private readonly SpecializedParserMaker _tryInlinedParserComponents;
        private readonly Func<string> _desc;
        private readonly object _components;

        public AnonymousJar(Func<ArraySegment<byte>, ParsedValue<T>> parse,
                            Func<T, byte[]> pack,
                            bool canBeFollowed,
                            bool isBlittable,
                            int? optionalConstantSerializedLength,
                            SpecializedParserMaker tryInlinedParserComponents,
                            Func<string> desc = null,
                            object components = null) {
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
        public override string ToString() {
            return _desc == null ? base.ToString() : _desc();
        }
    }

    internal static class AnonymousJar {
        public static AnonymousJar<T> CreateSpecialized<T>(SpecializedParserMaker specializedParserMaker,
                                                           Func<T, byte[]> packer,
                                                           bool canBeFollowed,
                                                           bool isBlittable,
                                                           int? constLength,
                                                           Func<string> desc = null,
                                                           object components = null) {
            if (specializedParserMaker == null) throw new ArgumentNullException("specializedParserMaker");
            if (packer == null) throw new ArgumentNullException("packer");
            return new AnonymousJar<T>(SpecializedParserParts.MakeParser<T>(specializedParserMaker),
                                       packer,
                                       canBeFollowed,
                                       isBlittable,
                                       constLength,
                                       specializedParserMaker,
                                       desc,
                                       components);
        }
    }
}