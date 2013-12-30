using System.Collections.Generic;
using Strilanc.PickleJar;
using System.Linq;

namespace PickleJarSample {
    public partial class MainWindow {
        public MainWindow() {
            InitializeComponent();
            this.Loaded += (sender, arg) => Test();
        }
        public void Test() {
            var xx = Jar.Int16LittleEndian.RepeatUntilEndOfData().NullTerminated().DataSizePrefixed(Jar.Int32LittleEndian, includePrefixInSize: false);

            var r = 
                    new[] {
                        Jar.UInt8.RepeatNTimes(3),
                        Jar.UInt8.RepeatNTimes(2),
                        Jar.UInt8.RepeatCountPrefixTimes(Jar.Int32LittleEndian),
                        Jar.UInt8.RepeatUntilEndOfData()
                    }.ToListJar();
            var r2 = r.Parse(new byte[] {1, 2, 3, 1, 2, 4, 0, 0, 0, 1, 2, 3, 4, 1, 2, 3, 4, 5, 6, 7, 8});

            var xxx = new Jar.NamedJarList {
                {"test", Jar.Int32LittleEndian},
                {"test2", Jar.Int32BigEndian},
            };
            var dicParser = xxx.ToDictionaryJar().RepeatCountPrefixTimes(Jar.Int32LittleEndian);
            var x23 = dicParser.Parse(new byte[] { 1,0,0,0, 1, 0, 0, 0, 1, 0, 0, 0 });
            var zz = x23.Value.First()["test2"];

            var xParser =
                new Jar.Builder {
                {"x", Jar.Int32LittleEndian},
                {"y", Jar.Float32LittleEndian},
                {"z", Jar.Int32LittleEndian}
            }.BuildJarForAnonymousTypeByExample(new {
                X = default(int), 
                Y = default(float), 
                Z = default(int)
            });
            var y = xParser.Parse(new byte[] {1, 0, 0, 0, 0, 0, 0, 0, 5, 0, 0, 0});
        }
    }
    public static class XXXXXX {
        public static IJar<T> BuildJarForAnonymousTypeByExample<T>(this IEnumerable<IJarForMember> builder, T anon) {
            return builder.BuildJarForType<T>();
        }
    }
}
