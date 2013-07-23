PickleJar
=========

**PickleJar is not yet ready for usage.**

PickleJar is a library for describing the binary serialized formats, and using that description to pack and parse values into and out of said format.

PickleJar performs optimization and compilation of parsers/packers at runtime, to avoid the overhead of interpreting the descriptions anew each time.

(The name comes from python, which refers to serialization as 'pickling', and the process of actually making pickles, which uses actual jars.)

========
Examples
========

Parsing a 2d point, serialized as two contiguous floats:

```CSharp
public sealed class Point {
    public readonly float X;
    public readonly float Y;
    public Point(float x, float y) {
        X = x;
        Y = y;
    }
}

var pointJar = new Jar.Builder<Point> {
    {"x", Parse.Float32},
    {"y", Parse.Float32}
}.Build();

var p = pointParser.Parse(new byte[]{0,0,0,0, 0,0,128,63}).Value;
// p now contains a Point with X=0.0f and Y=1.0f
```

Using StructLayoutAttribute to allow memcpy optimizations:

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
    
