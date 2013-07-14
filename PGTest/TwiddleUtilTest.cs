using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Strilanc.Parsing;
using Strilanc.Parsing.Misc;

[TestClass]
public class TwiddleUtilTest {
    [TestMethod]
    public void TestReverseBytes() {
        ((short)0x1234).ReverseBytes().AssertEquals((short)0x3412);
        ((ushort)0x1234).ReverseBytes().AssertEquals((ushort)0x3412);

        0x12345678.ReverseBytes().AssertEquals(0x78563412);
        0x12345678u.ReverseBytes().AssertEquals(0x78563412u);

        0x123456789ABCDE0FL.ReverseBytes().AssertEquals(0x0FDEBC9A78563412L);
        0x123456789ABCDEF0UL.ReverseBytes().AssertEquals(0xF0DEBC9A78563412UL);

        (-1).ReverseBytes().AssertEquals(-1);
        ((short)-1).ReverseBytes().AssertEquals((short)-1);
        (-1L).ReverseBytes().AssertEquals(-1L);

        (0).ReverseBytes().AssertEquals(0);
        ((short)0).ReverseBytes().AssertEquals((short)0);
        (0L).ReverseBytes().AssertEquals(0L);
    }
    [TestMethod]
    public void TestBitsOfReverseBytes() {
        for (var i = 0; i < 64; i += 8) {
            for (var j = 0; j < 8; j++) {
                if (i < 16) {
                    ((short)(1 << (i + j))).ReverseBytes().AssertEquals((short)(1 << (16 - i - 8 + j)));
                    ((ushort)(1 << (i + j))).ReverseBytes().AssertEquals((ushort)(1 << (16 - i - 8 + j)));
                }

                if (i < 32) {
                    (1u << (i + j)).ReverseBytes().AssertEquals(1u << (32 - i - 8 + j));
                    (1 << (i + j)).ReverseBytes().AssertEquals(1 << (32 - i - 8 + j));
                }

                (1UL << (i + j)).ReverseBytes().AssertEquals(1UL << (64 - i - 8 + j));
                (1L << (i + j)).ReverseBytes().AssertEquals(1L << (64 - i - 8 + j));
            }
        }
    }
}
