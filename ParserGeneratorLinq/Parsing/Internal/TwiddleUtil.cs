using System;

namespace Strilanc.Parsing.Internal {
    /// <summary>
    /// The endianess of a value determines the ordering of the bytes used to represent it.
    /// </summary>
    public enum Endianess {
        /// <summary>
        /// Big End First.
        /// The first byte is most significant, and the last byte is the least significant (1s).
        /// </summary>
        BigEndian,
        /// <summary>
        /// Little End First.
        /// The first byte is least significant (1s), and the last byte is the most significant.
        /// </summary>
        LittleEndian
    }

    /// <summary>
    /// TwiddleUtil contains utility methods for twiddling bits.
    /// http://graphics.stanford.edu/~seander/bithacks.html
    /// </summary>
    public static class TwiddleUtil {
        /// <summary>
        /// Reverses the byte ordering of a ulong.
        /// Used to swap between big endian and little endian.
        /// </summary>
        public static UInt64 ReverseBytes(this UInt64 value) {
            var r32 = (value >> 32) | (value << 32);
            var r16 = ((r32 >> 16) & 0x0000FFFF0000FFFFUL) | ((r32 << 16) & 0xFFFF0000FFFF0000UL);
            return ((r16 >>  8) & 0x00FF00FF00FF00FFUL) | ((r16 <<  8) & 0xFF00FF00FF00FF00UL);
        }
        /// <summary>
        /// Reverses the byte ordering of a uint.
        /// Used to swap between big endian and little endian.
        /// </summary>
        public static UInt32 ReverseBytes(this UInt32 value) {
            var r16 = (value >> 16) | (value << 16);
            return ((r16 >> 8) & 0x00FF00FFU) | ((r16 << 8) & 0xFF00FF00U);
        }
        /// <summary>
        /// Reverses the byte ordering of a ushort.
        /// Used to swap between big endian and little endian.
        /// </summary>
        public static UInt16 ReverseBytes(this UInt16 value) {
            unchecked {
                return (UInt16)((value >> 8) | (value << 8));
            }
        }
        /// <summary>
        /// Reverses the byte ordering of a long.
        /// Used to swap between big endian and little endian.
        /// </summary>
        public static Int64 ReverseBytes(this Int64 value) {
            unchecked {
                return (Int64)((UInt64)value).ReverseBytes();
            }
        }
        /// <summary>
        /// Reverses the byte ordering of an int.
        /// Used to swap between big endian and little endian.
        /// </summary>
        public static Int32 ReverseBytes(this Int32 value) {
            unchecked {
                return (Int32)((UInt32)value).ReverseBytes();
            }
        }
        /// <summary>
        /// Reverses the byte ordering of a short.
        /// Used to swap between big endian and little endian.
        /// </summary>
        public static Int16 ReverseBytes(this Int16 value) {
            unchecked {
                return (Int16)((UInt16)value).ReverseBytes();
            }
        }
    }
}