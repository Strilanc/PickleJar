using System;
using ParserGenerator;
using ParserGenerator.Blittable;

public static class Parse {
    public static readonly Int8Parser Int8 = new Int8Parser();

    public static readonly Int16Parser Int16LittleEndian = new Int16Parser(Endianess.LittleEndian);
    public static readonly Int16Parser Int16BigEndian = new Int16Parser(Endianess.BigEndian);

    public static readonly Int32Parser Int32LittleEndian = new Int32Parser(Endianess.LittleEndian);
    public static readonly Int32Parser Int32BigEndian = new Int32Parser(Endianess.BigEndian);

    public static readonly Int64Parser Int64LittleEndian = new Int64Parser(Endianess.LittleEndian);
    public static readonly Int64Parser Int64BigEndian = new Int64Parser(Endianess.BigEndian);

    public static readonly UInt8Parser UInt8 = new UInt8Parser();

    public static readonly UInt16Parser UInt16LittleEndian = new UInt16Parser(Endianess.LittleEndian);
    public static readonly UInt16Parser UInt16BigEndian = new UInt16Parser(Endianess.BigEndian);

    public static readonly UInt32Parser UInt32LittleEndian = new UInt32Parser(Endianess.LittleEndian);
    public static readonly UInt32Parser UInt32BigEndian = new UInt32Parser(Endianess.BigEndian);

    public static readonly UInt64Parser UInt64LittleEndian = new UInt64Parser(Endianess.LittleEndian);
    public static readonly UInt64Parser UInt64BigEndian = new UInt64Parser(Endianess.BigEndian);

    public static IArrayParser<T> Array<T>(this IParser<T> itemParser) {
        if (itemParser == null) throw new ArgumentNullException("itemParser");

        return (IArrayParser<T>)BlittableArrayParser<T>.TryMake(itemParser)
            ?? new ExpressionArrayParser<T>(itemParser);
    }

    public static IParser<T[]> ConstantRepeat<T>(this IParser<T> itemParser, int constantRepeatCount) {
        return new FixedRepeatParser<T>(itemParser.Array(), constantRepeatCount);
    }
    public static IParser<T[]> CountPrefixedRepeat<T>(this IParser<T> itemParser, IParser<int> countPrefixParser) {
        return new CountPrefixedRepeatParser<T>(countPrefixParser, itemParser.Array());
    }
    public static IParser<T[]> GreedyRepeat<T>(this IParser<T> itemParser) {
        if (!itemParser.OptionalConstantSerializedLength.HasValue) {
            return new GreedyRepeatParser<T>(itemParser);
        }

        var itemLength = itemParser.OptionalConstantSerializedLength.Value;
        var counter = new AnonymousParser<int>(e => {
            if (e.Count % itemLength != 0) throw new InvalidOperationException("Fragment");
            return new ParsedValue<int>(e.Count/itemLength, 0);
        });
        return new CountPrefixedRepeatParser<T>(
            counter,
            itemParser.Array());
    }
}
