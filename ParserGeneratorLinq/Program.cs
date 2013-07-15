using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Strilanc.Parsing;
using Strilanc.Parsing.Internal.Misc;

public class Program {
    public struct Pointy2 {
        public Pointy P1;
        public Pointy P2;
    }

    [StructLayoutAttribute(LayoutKind.Sequential, Pack = 1)]
    public struct Pointy {
        public readonly int X;
        public readonly int Y;
        public readonly Int16 M;
        public readonly Int16 N;
        public Pointy(int x, int y, Int16 m, Int16 n) {
            X = x;
            Y = y;
            M = m;
            N = n;
        }

    }
    private static Pointy[] ParseFrom(ArraySegment<byte> data, int n) {
        var r = new Pointy[n];
        var j = data.Offset;
        for (var i = 0; i < n; i += 4) {
            r[i] = new Pointy(
                BitConverter.ToInt32(data.Array, j + 4),
                BitConverter.ToInt32(data.Array, j + 0),
                BitConverter.ToInt16(data.Array, j + 8),
                BitConverter.ToInt16(data.Array, j + 10));
            r[i+1] = new Pointy(
                BitConverter.ToInt32(data.Array, j + 16),
                BitConverter.ToInt32(data.Array, j + 12),
                BitConverter.ToInt16(data.Array, j + 20),
                BitConverter.ToInt16(data.Array, j + 22));
            r[i + 2] = new Pointy(
                BitConverter.ToInt32(data.Array, j + 28),
                BitConverter.ToInt32(data.Array, j + 24),
                BitConverter.ToInt16(data.Array, j + 32),
                BitConverter.ToInt16(data.Array, j + 34));
            r[i + 3] = new Pointy(
                BitConverter.ToInt32(data.Array, j + 40),
                BitConverter.ToInt32(data.Array, j + 36),
                BitConverter.ToInt16(data.Array, j + 44),
                BitConverter.ToInt16(data.Array, j + 46));
            j += 48;
        }
        return r;
    }
    static void Main() {
        const int DataRepeatCount = 10000;

        var dynamicParser = 
            (from y in Parse.Int32LittleEndian
             from x in Parse.Int32LittleEndian
             from m in Parse.Int16LittleEndian
             from n in Parse.Int16LittleEndian
             select new Pointy(x, y, m, n)
            ).RepeatNTimes(DataRepeatCount);

        var blitParser =
            new Parse.Builder<Pointy> {
                {"x", Parse.Int32LittleEndian},
                {"y", Parse.Int32LittleEndian},
                {"m", Parse.Int16LittleEndian},
                {"n", Parse.Int16LittleEndian}}.Build()
            .RepeatUntilEndOfData();

        var compiledParser =
            new Parse.Builder<Pointy> {
                {"y", Parse.Int32LittleEndian},
                {"x", Parse.Int32LittleEndian},
                {"m", Parse.Int16LittleEndian},
                {"n", Parse.Int16LittleEndian}}.Build()
            .RepeatUntilEndOfData();

        var handrolledParser = new AnonymousParser<Pointy[]>(e => new ParsedValue<Pointy[]>(ParseFrom(e, DataRepeatCount), DataRepeatCount*12));

        var data = new ArraySegment<byte>(Enumerable.Repeat(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 }, DataRepeatCount).SelectMany(e => e).ToArray());

        var parsers = new Dictionary<string, IParser<Pointy[]>> {
            {"handrolled", handrolledParser},
            {"compiled", compiledParser},
            {"blit", blitParser},
            {"dynamic", dynamicParser}
        };
        var s = new Stopwatch();
        const int Repetitions = 1000;
        for (var j = 0; j < 10; j++) {
            foreach (var parser in parsers) {
                var p = parser.Value;
                s.Reset();
                s.Start();
                for (var i = 0; i < Repetitions; i++) {
                    var _ = p.Parse(data);
                }
                s.Stop();
                Console.WriteLine("{0}: {1}", parser.Key.ToString().PadLeft(10), s.Elapsed.TotalSeconds);
            }
            Console.WriteLine();
        }
        Console.ReadLine();
    }
}