using System;

namespace Strilanc.PickleJar {
    public interface IMemberAndJar {
        /// <summary>
        /// The name of the field, ignoring underscores and casing.
        /// </summary>
        CanonicalMemberName CanonicalName { get; }

        /// <summary>
        /// The type of value stored in the field.
        /// Equivalently, the type of value parsed and packed by the jar returned by the Jar property.
        /// </summary>
        Type FieldType { get; }

        /// <summary>
        /// The jar that should be used to parse the field.
        /// The returned object must implement the IJar interface for the type returned by the FieldType property.
        /// </summary>
        object Jar { get; }
    }
}