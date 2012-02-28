using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.De;
using NUnit.Framework;
using Version=Lucene.Net.Util.Version;

namespace Lucene.Net.Analyzers.De
{
    /**
     * Test the German stemmer. The stemming algorithm is known to work less 
     * than perfect, as it doesn't use any word lists with exceptions. We 
     * also check some of the cases where the algorithm is wrong.
     *
     */
    [TestFixture]
    public class TestGermanStemFilter : BaseTokenStreamTestCase
    {
        [Test]
        public void TestStemming()
        {
            // read test cases from external file:
            string testFile = @"De\data.txt";
            using (FileStream fis = new FileStream(testFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (StreamReader breader = new StreamReader(fis, Encoding.GetEncoding("iso-8859-1")))
            {
                while (true)
                {
                    String line = breader.ReadLine();
                    if (line == null)
                        break;
                    line = line.Trim();
                    if (line.StartsWith("#") || string.IsNullOrEmpty(line))
                        continue; // ignore comments and empty lines
                    String[] parts = line.Split(';');
                    //System.out.println(parts[0] + " -- " + parts[1]);
                    Check(parts[0], parts[1]);
                }
            }
        }

        [Test]
        public void TestReusableTokenStream()
        {
            Analyzer a = new GermanAnalyzer(Version.LUCENE_CURRENT);
            CheckReuse(a, "Tisch", "tisch");
            CheckReuse(a, "Tische", "tisch");
            CheckReuse(a, "Tischen", "tisch");
        }

        /**
         * subclass that acts just like whitespace analyzer for testing
         */
        private class GermanSubclassAnalyzer : GermanAnalyzer
        {
            public GermanSubclassAnalyzer(Version matchVersion)
                : base(matchVersion)
            {
            }

            public override TokenStream TokenStream(String fieldName, TextReader reader)
            {
                return new WhitespaceTokenizer(reader);
            }
        }

        [Test]
        public void TestLucene1678BwComp()
        {
            CheckReuse(new GermanSubclassAnalyzer(Version.LUCENE_CURRENT), "Tischen", "Tischen");
        }

        /* 
         * Test that changes to the exclusion table are applied immediately
         * when using reusable token streams.
         */
        [Test]
        public void TestExclusionTableReuse()
        {
            GermanAnalyzer a = new GermanAnalyzer(Version.LUCENE_CURRENT);
            CheckReuse(a, "tischen", "tisch");
            a.SetStemExclusionTable(new String[] { "tischen" });
            CheckReuse(a, "tischen", "tischen");
        }

        private void Check(String input, String expected)
        {
            CheckOneTerm(new GermanAnalyzer(Version.LUCENE_CURRENT), input, expected);
        }

        private void CheckReuse(Analyzer a, String input, String expected)
        {
            CheckOneTermReuse(a, input, expected);
        }
    }
}
