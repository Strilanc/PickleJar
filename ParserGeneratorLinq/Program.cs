using System;
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
    private static Pointy[] ParseFrom(ArraySegment<byte> data) {
        var r = new Pointy[5000];
        var j = data.Offset;
        for (var i = 0; i < 5000; i += 4) {
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
        var repeatBlitParser = 
            new ParseBuilder {
                {"y", Parse.Int32LittleEndian},
                {"x", Parse.Int32LittleEndian},
                {"m", Parse.Int16LittleEndian},
                {"n", Parse.Int16LittleEndian},
            }.BuildAsParserForType<Pointy>()
            .RepeatUntilEndOfData();

        var m = Enumerable.Repeat(new byte[] { 1, 0, 0, 0, 2, 0, 0, 0, 4, 0, 0, 0 }, 5000).SelectMany(e => e).ToArray();
        var m2 = new ArraySegment<byte>(m, 0, m.Length);

        for (var j = 0; j < 10; j++) {
            var s = new Stopwatch();
            s.Start();
            for (var i = 0; i < 20000; i++) {
                var y = ParseFrom(m2);
            }
            s.Stop();
            var t = s.Elapsed;
            s.Reset();
            s.Start();
            for (var i = 0; i < 20000; i++) {
                var z = repeatBlitParser.Parse(m2);
            }
            s.Stop();
            var t2 = s.Elapsed;
            var b = ParseFrom(m2).SequenceEqual(repeatBlitParser.Parse(m2).Value);
            Console.WriteLine(t.TotalSeconds);
            Console.WriteLine(t2.TotalSeconds);
            Console.WriteLine(b);
        }
        Console.ReadLine();
    }
    static string[] GetVarNames<T>(IParser<T> parser) {
        dynamic d = parser;
        if (parser.GetType().GetGenericTypeDefinition() == typeof (SelectManyParser<,,>)) {
            string lastName = d.Proj2.Parameters[1].Name;
            string firstName = d.Proj2.Parameters[0].Name;
            if (firstName.StartsWith("<>")) {
                return ((string[])GetVarNames(d.SubParser)).Concat(new[] {lastName}).ToArray();
            }
            return new[] {firstName, lastName};
        } else if (parser.GetType().GetGenericTypeDefinition() == typeof(SelectParser<,>)) {
            throw new NotImplementedException();
        } else {
            throw new NotImplementedException();
        }
    }
}