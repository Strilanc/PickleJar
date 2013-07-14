using System;

public enum Endianess {
    BigEndian,
    LittleEndian
}

public static class TwiddleUtil {
    public static UInt64 ReverseBytes(this UInt64 value) {
        var r32 = (value >> 32) | (value << 32);
        var r16 = ((r32 >> 16) & 0x0000FFFF0000FFFFUL) | ((r32 << 16) & 0xFFFF0000FFFF0000UL);
        return ((r16 >>  8) & 0x00FF00FF00FF00FFUL) | ((r16 <<  8) & 0xFF00FF00FF00FF00UL);
    }
    public static UInt32 ReverseBytes(this UInt32 value) {
        var r16 = (value >> 16) | (value << 16);
        return ((r16 >> 8) & 0x00FF00FFU) | ((r16 << 8) & 0xFF00FF00U);
    }
    public static UInt16 ReverseBytes(this UInt16 value) {
        unchecked {
            return (UInt16)((value >> 8) | (value << 8));
        }
    }
    public static Int64 ReverseBytes(this Int64 value) {
        unchecked {
            return (Int64)((UInt64)value).ReverseBytes();
        }
    }
    public static Int32 ReverseBytes(this Int32 value) {
        unchecked {
            return (Int32)((UInt32)value).ReverseBytes();
        }
    }
    public static Int16 ReverseBytes(this Int16 value) {
        unchecked {
            return (Int16)((UInt16)value).ReverseBytes();
        }
    }
}