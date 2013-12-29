using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using Strilanc.PickleJar.Internal.RuntimeSpecialization;

namespace Strilanc.PickleJar.Internal.Bulk {
    [DebuggerDisplay("{ToString()}")]
    internal sealed class AnonymousBulkJar<T> : IBulkJar<T> {
        private readonly Func<ArraySegment<byte>, int, ParsedValue<IReadOnlyList<T>>> _parse;
        private readonly Func<IReadOnlyCollection<T>, byte[]> _pack;
        private readonly InlinerBulkMaker _makeInlinedParserComponents;
        private readonly Func<string> _desc;
        public IJar<T> ItemJar { get; private set; }
        private readonly object _components;
        private readonly PackSpecializer _makeSpecializedCollectionPacker; 

        public bool CanBeFollowed { get { return true; } }
        public bool IsBlittable { get { return false; } }
        public int? OptionalConstantSerializedValueLength { get { return ItemJar.OptionalConstantSerializedLength(); } }

        public AnonymousBulkJar(IJar<T> itemJar,
                                Func<ArraySegment<byte>, int, ParsedValue<IReadOnlyList<T>>> parse,
                                Func<IReadOnlyCollection<T>, byte[]> pack,
                                InlinerBulkMaker makeInlinedParserComponents,
                                PackSpecializer makeSpecializedCollectionPacker,
                                Func<string> desc = null,
                                object components = null) {
            if (itemJar == null) throw new ArgumentNullException("itemJar");
            if (parse == null) throw new ArgumentNullException("parse");
            if (pack == null) throw new ArgumentNullException("pack");
            if (makeInlinedParserComponents == null) throw new ArgumentNullException("makeInlinedParserComponents");
            if (makeSpecializedCollectionPacker == null) throw new ArgumentNullException("makeSpecializedCollectionPacker");
            _parse = parse;
            _pack = pack;
            ItemJar = itemJar;
            _makeInlinedParserComponents = makeInlinedParserComponents;
            _desc = desc;
            _components = components;
            _makeSpecializedCollectionPacker = makeSpecializedCollectionPacker;
        }

        public SpecializedParserParts MakeInlinedParserComponents(Expression array, Expression offset, Expression count, Expression itemCount) {
            return _makeInlinedParserComponents(array, offset, count, itemCount);
        }
        public SpecializedPackerParts MakeSpecializedCollectionPacker(Expression collection) {
            return _makeSpecializedCollectionPacker(collection);
        } 
        public override string ToString() {
            return _desc == null ? base.ToString() : _desc();
        }
        public ParsedValue<IReadOnlyList<T>> Parse(ArraySegment<byte> data, int count) {
            return _parse(data, count);
        }
        public byte[] Pack(IReadOnlyCollection<T> values) {
            return _pack(values);
        }
    }

    internal static class AnonymousBulkJar {
        public static AnonymousBulkJar<T> CreateSpecialized<T>(IJar<T> itemJar,
                                                               InlinerBulkMaker parser,
                                                               PackSpecializer collectionPacker,
                                                               Func<string> desc,
                                                               object components) {
            return new AnonymousBulkJar<T>(itemJar,
                                           SpecializedParserParts.MakeBulkParser<T>(parser),
                                           SpecializedPackerParts.MakePacker<IReadOnlyCollection<T>>(collectionPacker),
                                           parser,
                                           collectionPacker,
                                           desc,
                                           components);
        }
    }
}
