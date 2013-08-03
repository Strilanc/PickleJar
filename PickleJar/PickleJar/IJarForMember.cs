namespace Strilanc.PickleJar {
    /// <summary>
    /// A jar associated with a member of a type.
    /// </summary>
    public interface IJarForMember {        
        /// <summary>
        /// The jar that used to parse/pack the associated member's value.
        /// Must implement the IJar interface for the appropriate type.
        /// </summary>
        object Jar { get; }

        /// <summary>
        /// Determines which member's value the jar is parsing/packing.
        /// </summary>
        MemberMatchInfo MemberMatchInfo { get; }
    }
}