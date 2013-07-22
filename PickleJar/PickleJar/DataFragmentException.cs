using System;
using System.Linq.Expressions;

namespace Strilanc.PickleJar {
    /// <summary>
    /// DataFragmentException is thrown when parsing fails due to the data ending prematurely.
    /// </summary>
    public class DataFragmentException : ArgumentException {
        public static readonly Expression CachedThrowExpression = Expression.Throw(Expression.Constant(new DataFragmentException()));
        public DataFragmentException() : base("Ran out of data in the middle of parsing a value.") {}
    }
}
