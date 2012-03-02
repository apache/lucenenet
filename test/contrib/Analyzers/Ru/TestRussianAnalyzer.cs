using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Ru;
using Lucene.Net.Analysis.Tokenattributes;
using NUnit.Framework;
using Version=Lucene.Net.Util.Version;

namespace Lucene.Net.Analyzers.Ru
{
    /**
     * Test case for RussianAnalyzer.
     */
    [TestFixture]
    public class TestRussianAnalyzer : BaseTokenStreamTestCase
    {
        private StreamReader inWords;

        private StreamReader sampleUnicode;

        protected void setUp()
        {
            base.SetUp();
        }

        [Test]
        public void TestUnicode()
        {
            RussianAnalyzer ra = new RussianAnalyzer(Version.LUCENE_CURRENT);

            using (inWords = new StreamReader(@"ru\testUTF8.txt", Encoding.UTF8))
            using (sampleUnicode = new StreamReader(@"ru\resUTF8.htm", Encoding.UTF8))
            {

                TokenStream _in = ra.TokenStream("all", inWords);

                RussianLetterTokenizer sample =
                    new RussianLetterTokenizer(
                        sampleUnicode);

                TermAttribute text = _in.GetAttribute<TermAttribute>();
                TermAttribute sampleText = sample.GetAttribute<TermAttribute>();

                for (; ; )
                {
                    if (_in.IncrementToken() == false)
                        break;

                    bool nextSampleToken = sample.IncrementToken();
                    Assert.AreEqual(text.Term(), nextSampleToken == false ? null : sampleText.Term(), "Unicode");
                }
            }
        }

        [Test]
        public void TestDigitsInRussianCharset()
        {
            TextReader reader = new StringReader("text 1000");
            RussianAnalyzer ra = new RussianAnalyzer(Version.LUCENE_CURRENT);
            TokenStream stream = ra.TokenStream("", reader);

            TermAttribute termText = stream.GetAttribute<TermAttribute>();
            try
            {
                Assert.True(stream.IncrementToken());
                Assert.AreEqual("text", termText.Term());
                Assert.True(stream.IncrementToken());
                Assert.AreEqual("1000", termText.Term(), "RussianAnalyzer's tokenizer skips numbers from input text");
                Assert.False(stream.IncrementToken());
            }
            catch (IOException e)
            {
                Assert.Fail("unexpected IOException");
            }
        }

        [Test]
        public void TestReusableTokenStream()
        {
            Analyzer a = new RussianAnalyzer(Version.LUCENE_CURRENT);
            AssertAnalyzesToReuse(a, "Вместе с тем о силе электромагнитной энергии имели представление еще",
                                  new String[] {"вмест", "сил", "электромагнитн", "энерг", "имел", "представлен"});
            AssertAnalyzesToReuse(a, "Но знание это хранилось в тайне",
                                  new String[] {"знан", "хран", "тайн"});
        }
    }
}
