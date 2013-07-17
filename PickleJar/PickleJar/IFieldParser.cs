using System;

namespace Strilanc.PickleJar {
    public interface IFieldParser {
        CanonicalizingMemberName CanonicalName { get; }
        Type ParserValueType { get; }
        object Parser {get;}
    }
}