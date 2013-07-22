using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Strilanc.PickleJar;

public class Program {
    [StructLayoutAttribute(LayoutKind.Sequential, Pack = 1)]
    public struct Point3 {
        public readonly int X;
        public readonly int Y;
        public readonly int Z;
        public Point3(int x, int y, int z) {
            X = x;
            Y = y;
            Z = z;
        }
    }
    private static ParsedValue<IReadOnlyList<Point3>> HandrolledParse(ArraySegment<byte> data) {
        if (data.Count % 12 != 0) throw new ArgumentException();
        var count = data.Count/12;
        var r = new Point3[count];
        var j = data.Offset;
        var a = data.Array;
        for (var i = 0; i < count; i++) {
            r[i] = new Point3(
                BitConverter.ToInt32(a, j + 0),
                BitConverter.ToInt32(a, j + 4),
                BitConverter.ToInt32(a, j + 8));
            j += 12;
        }
        return new ParsedValue<IReadOnlyList<Point3>>(r, data.Count);
    }
    static void Main2() {
        const int DataRepeatCount = 10000;

        var handrolledParser = new AnonymousJar<IReadOnlyList<Point3>>(
            HandrolledParse, 
            x => { throw new NotImplementedException(); });

        var blitParser =
            new Jar.Builder<Point3> {
                {"x", Jar.Int32LittleEndian},
                {"y", Jar.Int32LittleEndian},
                {"z", Jar.Int32LittleEndian}}.Build()
            .RepeatUntilEndOfData();

        //var dynamicParser =
        //    (from y in Jar.Int32LittleEndian
        //     from x in (y == 0 ? Jar.Int32LittleEndian : Jar.Int32BigEndian)
        //     from z in Jar.Int32LittleEndian
        //     select new Point3(x, y, z)
        //    ).RepeatUntilEndOfData();

        var compiledParser =
            new Jar.Builder<Point3> {
                {"y", Jar.Int32LittleEndian},
                {"x", Jar.Int32LittleEndian},
                {"z", Jar.Int32LittleEndian}}.Build()
            .RepeatUntilEndOfData();

        var data = new ArraySegment<byte>(Enumerable.Repeat(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 }, DataRepeatCount).SelectMany(e => e).ToArray());

        var parsers = new Dictionary<string, IJar<IReadOnlyList<Point3>>> {
            {"handrolled", handrolledParser},
            {"compiled", compiledParser},
            {"blit", blitParser},
            //{"dynamic", dynamicParser},
        };
        var s = new Stopwatch();
        const long Repetitions = 1000;
        for (var j = 0; j < 10; j++) {
            Console.WriteLine("Parsing {0} {1}-byte items {2} times", DataRepeatCount, Marshal.SizeOf(typeof(Point3)), Repetitions);
            foreach (var parser in parsers) {
                var p = parser.Value;
                s.Reset();
                s.Start();
                for (var i = 0; i < Repetitions; i++) {
                    p.Parse(data);
                }
                s.Stop();

                Console.WriteLine("{0}: {1:0.00}s, ~{2}", parser.Key.PadLeft(10), s.Elapsed.TotalSeconds, AsNiceBps(Repetitions * data.LongCount() / s.Elapsed.TotalSeconds));
            }
            Console.WriteLine();
        }
        Console.ReadLine();
    }
    private static string AsNiceBps(double d) {
        var prefixes = new[] {"B/s", "KB/s", "MB/s", "GB/s", "TB/s"};
        var i = 0;
        while (d > 100) {
            d /= 1000;
            i += 1;
        }
        if (d < 1) return String.Format("{0:0.0}{1}", d, prefixes[i]);
        return String.Format("{0:0}{1}", d, prefixes[i]);
    }
}