namespace Strilanc.PickleJar.Internal.Misc {
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
}