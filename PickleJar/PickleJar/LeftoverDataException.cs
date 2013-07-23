using System;

namespace Strilanc.PickleJar {
    /// <summary>
    /// LeftoverDataException is thrown when parsing fails due to not all data being consumed (when that is required).
    /// </summary>
    public class LeftoverDataException : ArgumentException {
        public LeftoverDataException() : base("Not all data was consumed by parsing as expected.") { }
    }
}
