using System.Collections.Generic;
using Strilanc.PickleJar;
using System.Linq;

namespace PickleJarSample {
    public partial class MainWindow {
        public MainWindow() {
            InitializeComponent();

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
