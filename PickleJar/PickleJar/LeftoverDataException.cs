using System;

namespace Strilanc.PickleJar {
    /// <summary>
    /// LeftoverDataException is thrown when parsing fails due to not all data being consumed (when that is required).
    /// </summary>
    public class LeftoverDataException : ArgumentException {
        /// <summary>Initializes a new instance of the LeftoverDataException class with a default error message.</summary>
        public LeftoverDataException() : base("Not all data was consumed by parsing as expected.") { }
    }
}
