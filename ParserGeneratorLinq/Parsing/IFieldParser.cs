using System;

namespace Strilanc.Parsing {
    public interface IFieldParser {
        CanonicalizingMemberName CanonicalName { get; }
        Type ParserValueType { get; }
        object Parser {get;}
    }
}