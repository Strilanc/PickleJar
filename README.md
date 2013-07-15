Fast Parser Combinators for C#
==============================

Example usage:

```CSharp
public sealed class Point {
    // (X can't be set externally, but the library will detect that it can use the constructor parameter 'x' to do it)
    public readonly int X;
    public readonly int Y;
    public Point(int x, int y) {
        X = x;
        Y = y;
    }
}

var pointParser = new Parse.Builder<Point> {
    // since y comes first in these entries, the serialized form will have y first
    {"y", Parse.Int32LittleEndian},
    {"x", Parse.Int32LittleEndian}
}.Build();

var p = pointParser.Parse(new byte[] {2,0,0,0, 3,0,0,0}).Value;
// p now contains a Point with X=3 and Y=2
```

Example optimizing usage:

```C#
// (the layout attribute forces a particular memory representation, which the library will notice and exploit)
[StructLayoutAttribute(LayoutKind.Sequential, Pack = 1)]
public struct Point3 {
    public int X;
    public int Y;
    public int Z;
}

var bulkPointParser = new Parse.Builder<Point3> {
    // notice that the fields come in the same order as they are declared
    {"x", Parse.Int32LittleEndian},
    {"y", Parse.Int32LittleEndian},
    {"z", Parse.Int32LittleEndian}
}.Build()
 .RepeatUntilEndOfData();

// because an array of Point3 has the same representation as the serialized data, a memcpy is  valid parser
// the library notices this, and makes an unsafe parser that beats the pants off safe hand-written C#

var singlePointData = new byte[] {1,0,0,0,2,0,0,0,3,0,0,0};
var tenMillion = 10000000;
var bulkPointData = Enumerable.Repeat(singlePointData, tenMillion).SelectMany(e => e).ToArray();
    
var points = bulkPointParser.Parse(bulkPointData).Value; // parses at about 1GB/s
```
    
