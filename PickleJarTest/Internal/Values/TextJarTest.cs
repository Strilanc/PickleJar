using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Strilanc.PickleJar.Internal.Basic;

[TestClass]
public class TextJarTest {
    [TestMethod]
    public void Utf8Test() {
        var jar = new TextJar(Encoding.GetEncoding(Encoding.UTF8.WebName,
                                                   new EncoderExceptionFallback(),
                                                   new DecoderExceptionFallback()));
        jar.AssertPickles("", new byte[0], ignoresExtraData: false, failsOnLessData: false);
        jar.AssertPickles("\0", new byte[1], ignoresExtraData: false, failsOnLessData: false);
        jar.AssertPickles("abc", new byte[] { 97, 98, 99 }, ignoresExtraData: false, failsOnLessData: false);
        jar.AssertPickles("∠", new byte[] { 0xE2, 0x88, 0xA0 }, ignoresExtraData: false, failsOnLessData: false);

        jar.AssertCantParse(0x80);
        jar.AssertCantParse(0xFF);
    }

    [TestMethod]
    public void Utf8ReplaceOnFailTest() {
        var jar = new TextJar(Encoding.GetEncoding(Encoding.UTF8.WebName,
                                                   new EncoderReplacementFallback(),
                                                   new DecoderReplacementFallback()));
        jar.AssertPickles("", new byte[0], ignoresExtraData: false, failsOnLessData: false);
        jar.AssertPickles("\0", new byte[1], ignoresExtraData: false, failsOnLessData: false);
        jar.AssertPickles("abc", new byte[] { 97, 98, 99 }, ignoresExtraData: false, failsOnLessData: false);
        jar.AssertPickles("∠", new byte[] { 0xE2, 0x88, 0xA0 }, ignoresExtraData: false, failsOnLessData: false);

        jar.AssertCanParse(0x80);
        jar.AssertCanParse(0xFF);
    }

    [TestMethod]
    public void AsciiTest() {
        var jar = new TextJar(Encoding.GetEncoding(Encoding.ASCII.WebName,
                                                   new EncoderExceptionFallback(),
                                                   new DecoderExceptionFallback()));
        jar.AssertPickles("", new byte[0], ignoresExtraData: false, failsOnLessData: false);
        jar.AssertPickles("\0", new byte[1], ignoresExtraData: false, failsOnLessData: false);
        jar.AssertPickles("abc", new byte[] { 97, 98, 99 }, ignoresExtraData: false, failsOnLessData: false);
        
        jar.AssertCantPack("∠");
        jar.AssertCantParse(0x80);
        jar.AssertCantParse(0xFF);
    }

    [TestMethod]
    public void AsciiReplaceOnFailTest() {
        var jar = new TextJar(Encoding.GetEncoding(Encoding.ASCII.WebName,
                                                   new EncoderReplacementFallback(),
                                                   new DecoderReplacementFallback()));
        jar.AssertPickles("", new byte[0], ignoresExtraData: false, failsOnLessData: false);
        jar.AssertPickles("\0", new byte[1], ignoresExtraData: false, failsOnLessData: false);
        jar.AssertPickles("abc", new byte[] { 97, 98, 99 }, ignoresExtraData: false, failsOnLessData: false);
        
        jar.AssertCanPack("∠");
        jar.AssertCanParse(0x80);
        jar.AssertCanParse(0xFF);
    }
}
