using System;

namespace Strilanc.PickleJar.Internal.Basic {
    internal static class TupleJar {
        public static IJar<Tuple<T1, T2>> Create<T1, T2>(IJar<T1> itemJar1, IJar<T2> itemJar2) {
            if (itemJar1 == null) throw new ArgumentNullException("itemJar1");
            if (itemJar2 == null) throw new ArgumentNullException("itemJar2");
            if (!itemJar1.CanBeFollowed) throw new ArgumentException("!itemJar1.CanBeFollowed", "itemJar1");

            return new Jar.NamedJarList {
                {"Item1", itemJar1},
                {"Item2", itemJar2}
            }.BuildJarForType<Tuple<T1, T2>>();
        }
        public static IJar<Tuple<T1, T2, T3>> Create<T1, T2, T3>(IJar<T1> itemJar1, IJar<T2> itemJar2, IJar<T3> itemJar3) {
            if (itemJar1 == null) throw new ArgumentNullException("itemJar1");
            if (itemJar2 == null) throw new ArgumentNullException("itemJar2");
            if (itemJar3 == null) throw new ArgumentNullException("itemJar3");
            if (!itemJar1.CanBeFollowed) throw new ArgumentException("!itemJar1.CanBeFollowed", "itemJar1");
            if (!itemJar2.CanBeFollowed) throw new ArgumentException("!jar2.CanBeFollowed", "itemJar2");

            return new Jar.NamedJarList {
                {"Item1", itemJar1},
                {"Item2", itemJar2},
                {"Item3", itemJar3}
            }.BuildJarForType<Tuple<T1, T2, T3>>();
        }
    }
}
