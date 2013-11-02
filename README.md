PickleJar
=========

PickleJar is a library for describing simple binary formats, and using that description to parse and pack values.

You parse and pack values (a.k.a. 'pickling') using a 'Jar'.
PickleJar includes basic jars, like `Jar.Int32LittleEndian` and `Jar.Utf8`, as well as methods to augment and combine jars, like `Jar.RepeatNTimes` and `Jar.NullTerminated`.
Code for the combined and augmented jars is generated, optimized, and compiled at runtime (in cases where this has been implemented).

See [this blog post](http://twistedoakstudios.com/blog/Post4708_optimizing-a-parser-combinator-into-a-memcpy) for more discussion.

============
Current Work
============

I am currently working on:

- Adding more optimization cases (e.g. inlining bits of RepeatNTimes, packing by blitting).
- Adding support for building anonymous type parsers, or using a dictionary instead of a type.
- Testing error cases.

============
Installation
============

**NuGet Package**

- Add the [Strilanc.PickleJar NuGet package](https://www.nuget.org/packages/Strilanc.PickleJar/) to your project's references.
- Namespace: `using Strilanc.PickleJar;`

=====
Usage
=====

All functionality for making jars is present on the static `Strilanc.PickleJar.Jar` class.
The quickest way to see what's available is to "dot around" `Jar` with intellisense, or browse through the [NuDoq documentation](http://www.nudoq.org/#!/Packages/Strilanc.PickleJar/PickleJar/Jar).

**Example #1: Pickling a string**

```CSharp
// if the string is UTF8-encoded and null-terminated:
var stringJar1 = Jar.Utf8.NullTerminated();

// if the string is ASCII-encoded and prefixed by the byte length of the encoded characters:
var stringJar2 = Jar.Ascii.DataSizePrefixed(Jar.Int32LittleEndian, includePrefixInSize: false);

// packing:
// returns new byte[] { 116,101,115,116,226,156,147,0 }
byte[] data = stringJar1.Pack("test✓");

// parsing:
// returns value="aca", consumed=7
ParsedValue<string> = stringJar2.Parse(new byte[] {3,0,0,0, 97,99,97,255,255,255});
```

**Example #2: Parsing a custom type**

Parsing two contiguous floats into a point class.

```CSharp
// The type (the library is smart enough to realize it must pass X and Y to the constructor instead of setting them):
public sealed class Point {
    public readonly float X;
    public readonly float Y;
    public Point(float x, float y) {
        X = x;
        Y = y;
    }
}

// The jar, keying the component jars by name and then building the custom type jar:
var pointJar = new Jar.Builder {
    {"x", Jar.Float32LittleEndian},
    {"y", Jar.Float32LittleEndian}
}.BuildJarForType<Point>();

// parsing:
// returns value={X=0.0f, Y=1.0f}, consumed=8
var p = pointJar.Parse(new byte[]{0,0,0,0, 0,0,128,63});
```

**Example #3: Using StructLayoutAttribute to allow memcpy optimizations**

```CSharp
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
    
