using ParserGenerator;

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
}
