using System;
using System.Linq.Expressions;

namespace Strilanc.PickleJar {
    /// <summary>
    /// DataFragmentException is thrown when parsing fails due to the data ending prematurely.
    /// </summary>
    public class DataFragmentException : ArgumentException {
        internal static readonly Expression CachedThrowExpression = Expression.Throw(Expression.Constant(new DataFragmentException()));
        /// <summary>Initializes a new instance of the DataFragmentException class with a default error message.</summary>
        public DataFragmentException() : base("Ran out of data in the middle of parsing a value.") { }
    }
}
