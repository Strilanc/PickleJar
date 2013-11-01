PickleJar
=========

PickleJar is a library for describing the binary serialized formats, and using that description to pack and parse values into and out of said format.

PickleJar performs optimization and compilation of parsers/packers at runtime, to avoid the overhead of interpreting the descriptions anew each time.

(The name comes from python, which refers to serialization as 'pickling', and the process of actually making pickles, which uses actual jars.)

See [this blog post](http://twistedoakstudios.com/blog/Post4708_optimizing-a-parser-combinator-into-a-memcpy) for more discussion.

========
Examples
========

Parsing two contiguous floats into a point class. The library is smart enough to realize it must pass X and Y to the constructor:

```CSharp
public sealed class Point {
    public readonly float X;
    public readonly float Y;
    public Point(float x, float y) {
        X = x;
        Y = y;
    }
}

var pointJar = new Jar.Builder {
    {"x", Jar.Float32LittleEndian},
    {"y", Jar.Float32LittleEndian}
}.BuildJarForType<Point>();

var p = pointJar.Parse(new byte[]{0,0,0,0, 0,0,128,63}).Value;
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

var bulkPointParser = new Jar.Builder {
    // notice that the fields come in the same order as they are declared
    {"x", Jar.Int32LittleEndian},
    {"y", Jar.Int32LittleEndian},
    {"z", Jar.Int32LittleEndian}
}.BuildJarForType<Point3>()
 .RepeatUntilEndOfData();

// because an array of Point3 has the same representation as the serialized data, a memcpy is  valid parser
// the library notices this, and makes an unsafe parser that beats the pants off safe hand-written C#

var singlePointData = new byte[] {1,0,0,0,2,0,0,0,3,0,0,0};
var tenMillion = 10000000;
var bulkPointData = Enumerable.Repeat(singlePointData, tenMillion).SelectMany(e => e).ToArray();
    
var points = bulkPointParser.Parse(bulkPointData).Value; // parses at about 1GB/s
```
    
