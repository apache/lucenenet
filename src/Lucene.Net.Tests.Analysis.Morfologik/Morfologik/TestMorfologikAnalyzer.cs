// Lucene version compatibility level 8.2.0
using J2N.Text;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Morfologik.TokenAttributes;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Support;
using NUnit.Framework;
using System;
using System.IO;
using System.Text;
using JCG = J2N.Collections.Generic;
using SCG = System.Collections.Generic;

namespace Lucene.Net.Analysis.Morfologik
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// <summary>
    /// TODO: The tests below rely on the order of returned lemmas, which is probably not good. 
    /// </summary>
    public class TestMorfologikAnalyzer : BaseTokenStreamTestCase
    {
        private Analyzer getTestAnalyzer()
        {
            return new MorfologikAnalyzer(TEST_VERSION_CURRENT);
        }

        /** Test stemming of single tokens with Morfologik library. */
        [Test]
        public void TestSingleTokens()
        {
            Analyzer a = getTestAnalyzer();
            AssertAnalyzesTo(a, "a", new String[] { "a" });
            AssertAnalyzesTo(a, "liście", new String[] { "liście", "liść", "list", "lista" });
            AssertAnalyzesTo(a, "danych", new String[] { "dany", "dana", "dane", "dać" });
            AssertAnalyzesTo(a, "ęóąśłżźćń", new String[] { "ęóąśłżźćń" });
            a.Dispose();
        }

        /** Test stemming of multiple tokens and proper term metrics. */
        [Test]
        public void TestMultipleTokens()
        {
            Analyzer a = getTestAnalyzer();
            AssertAnalyzesTo(
                a,
                "liście danych",
                new String[] { "liście", "liść", "list", "lista", "dany", "dana", "dane", "dać" },
                new int[] { 0, 0, 0, 0, 7, 7, 7, 7 },
                new int[] { 6, 6, 6, 6, 13, 13, 13, 13 },
                new int[] { 1, 0, 0, 0, 1, 0, 0, 0 });

            AssertAnalyzesTo(
                a,
                "T. Gl\u00FCcksberg",
                new String[] { "tom", "tona", "Gl\u00FCcksberg" },
                new int[] { 0, 0, 3 },
                new int[] { 1, 1, 13 },
                new int[] { 1, 0, 1 });
            a.Dispose();
        }

        private void dumpTokens(String input)
        {
            using Analyzer a = getTestAnalyzer();
            using TokenStream ts = a.GetTokenStream("dummy", input);
            ts.Reset();

            IMorphosyntacticTagsAttribute attribute = ts.GetAttribute<IMorphosyntacticTagsAttribute>();
            ICharTermAttribute charTerm = ts.GetAttribute<ICharTermAttribute>();
            while (ts.IncrementToken())
            {
                Console.WriteLine(charTerm.ToString() + " => " + string.Format(StringFormatter.InvariantCulture, "{0}", attribute.Tags));
            }
            ts.End();
        }

        /** Test reuse of MorfologikFilter with leftover stems. */
        [Test]
        public void TestLeftoverStems()
        {
            Analyzer a = getTestAnalyzer();
            using (TokenStream ts_1 = a.GetTokenStream("dummy", "liście"))
            {
                ICharTermAttribute termAtt_1 = ts_1.GetAttribute<ICharTermAttribute>();
                ts_1.Reset();
                ts_1.IncrementToken();
                assertEquals("first stream", "liście", termAtt_1.ToString());
                ts_1.End();
            }

            using (TokenStream ts_2 = a.GetTokenStream("dummy", "danych"))
            {
                ICharTermAttribute termAtt_2 = ts_2.GetAttribute<ICharTermAttribute>();
                ts_2.Reset();
                ts_2.IncrementToken();
                assertEquals("second stream", "dany", termAtt_2.toString());
                ts_2.End();
            }
            a.Dispose();
        }

        /** Test stemming of mixed-case tokens. */
        [Test]
        public void TestCase()
        {
            Analyzer a = getTestAnalyzer();

            AssertAnalyzesTo(a, "AGD", new String[] { "AGD", "artykuły gospodarstwa domowego" });
            AssertAnalyzesTo(a, "agd", new String[] { "artykuły gospodarstwa domowego" });

            AssertAnalyzesTo(a, "Poznania", new String[] { "Poznań" });
            AssertAnalyzesTo(a, "poznania", new String[] { "poznanie", "poznać" });

            AssertAnalyzesTo(a, "Aarona", new String[] { "Aaron" });
            AssertAnalyzesTo(a, "aarona", new String[] { "aarona" });

            AssertAnalyzesTo(a, "Liście", new String[] { "liście", "liść", "list", "lista" });
            a.Dispose();
        }

        private void assertPOSToken(TokenStream ts, String term, params String[] tags)
        {
            ts.IncrementToken();
            assertEquals(term, ts.GetAttribute<ICharTermAttribute>().ToString());

            SCG.ISet<String> actual = new JCG.SortedSet<String>(StringComparer.Ordinal);
            SCG.ISet<String> expected = new JCG.SortedSet<String>(StringComparer.Ordinal);
            foreach (StringBuilder b in ts.GetAttribute<IMorphosyntacticTagsAttribute>().Tags)
            {
                actual.Add(b.ToString());
            }
            foreach (String s in tags)
            {
                expected.Add(s);
            }

            // LUCENENET: Commented out unnecessary extra check
            //if (!expected.Equals(actual))
            //{
            //    Console.WriteLine("Expected:\n" + expected);
            //    Console.WriteLine("Actual:\n" + actual);
                assertEquals(expected, actual, aggressive: false);
            //}
        }

        /** Test morphosyntactic annotations. */
        [Test]
        public void TestPOSAttribute()
        {
            using Analyzer a = getTestAnalyzer();
            using TokenStream ts = a.GetTokenStream("dummy", "liście");
            ts.Reset();
            assertPOSToken(ts, "liście",
              "subst:sg:acc:n2",
              "subst:sg:nom:n2",
              "subst:sg:voc:n2");

            assertPOSToken(ts, "liść",
              "subst:pl:acc:m3",
              "subst:pl:nom:m3",
              "subst:pl:voc:m3");

            assertPOSToken(ts, "list",
              "subst:sg:loc:m3",
              "subst:sg:voc:m3");

            assertPOSToken(ts, "lista",
              "subst:sg:dat:f",
              "subst:sg:loc:f");
            ts.End();
        }

        private class MockMorfologikAnalyzer : MorfologikAnalyzer
        {
            public MockMorfologikAnalyzer()
                : base(TEST_VERSION_CURRENT)
            { }

            protected override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                CharArraySet keywords = new CharArraySet(TEST_VERSION_CURRENT, 1, false);
                keywords.add("liście");

                Tokenizer src = new StandardTokenizer(TEST_VERSION_CURRENT, reader);
                TokenStream result = new SetKeywordMarkerFilter(src, keywords);
                result = new MorfologikFilter(result);

                return new TokenStreamComponents(src, result);
            }
        }

        /** */
        [Test]
        public void TestKeywordAttrTokens()
        {
            Analyzer a = new MockMorfologikAnalyzer();

            AssertAnalyzesTo(
              a,
                  "liście danych",
                  new String[] { "liście", "dany", "dana", "dane", "dać" },
                  new int[] { 0, 7, 7, 7, 7 },
                  new int[] { 6, 13, 13, 13, 13 },
                  new int[] { 1, 1, 0, 0, 0 });
            a.Dispose();
        }

        /** blast some random strings through the analyzer */
        [Test]
        public void TestRandom()
        {
            Analyzer a = getTestAnalyzer();
            CheckRandomData(Random, a, 1000 * RandomMultiplier);
            a.Dispose();
        }
    }
}
