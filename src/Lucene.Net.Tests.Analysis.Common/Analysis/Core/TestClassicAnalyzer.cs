// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;
using System;

namespace Lucene.Net.Analysis.Core
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

    public class TestClassicAnalyzer : BaseTokenStreamTestCase
    {

        private Analyzer a = new ClassicAnalyzer(TEST_VERSION_CURRENT);

        [Test]
        public virtual void TestMaxTermLength()
        {
            ClassicAnalyzer sa = new ClassicAnalyzer(TEST_VERSION_CURRENT);
            sa.MaxTokenLength = 5;
            AssertAnalyzesTo(sa, "ab cd toolong xy z", new string[] { "ab", "cd", "xy", "z" });
        }

        [Test]
        public virtual void TestMaxTermLength2()
        {
            ClassicAnalyzer sa = new ClassicAnalyzer(TEST_VERSION_CURRENT);
            AssertAnalyzesTo(sa, "ab cd toolong xy z", new string[] { "ab", "cd", "toolong", "xy", "z" });
            sa.MaxTokenLength = 5;

            AssertAnalyzesTo(sa, "ab cd toolong xy z", new string[] { "ab", "cd", "xy", "z" }, new int[] { 1, 1, 2, 1 });
        }

        [Test]
        public virtual void TestMaxTermLength3()
        {
            char[] chars = new char[255];
            for (int i = 0; i < 255; i++)
            {
                chars[i] = 'a';
            }
            string longTerm = new string(chars, 0, 255);

            AssertAnalyzesTo(a, "ab cd " + longTerm + " xy z", new string[] { "ab", "cd", longTerm, "xy", "z" });
            AssertAnalyzesTo(a, "ab cd " + longTerm + "a xy z", new string[] { "ab", "cd", "xy", "z" });
        }

        [Test]
        public virtual void TestAlphanumeric()
        {
            // alphanumeric tokens
            AssertAnalyzesTo(a, "B2B", new string[] { "b2b" });
            AssertAnalyzesTo(a, "2B", new string[] { "2b" });
        }

        [Test]
        public virtual void TestUnderscores()
        {
            // underscores are delimiters, but not in email addresses (below)
            AssertAnalyzesTo(a, "word_having_underscore", new string[] { "word", "having", "underscore" });
            AssertAnalyzesTo(a, "word_with_underscore_and_stopwords", new string[] { "word", "underscore", "stopwords" });
        }

        [Test]
        public virtual void TestDelimiters()
        {
            // other delimiters: "-", "/", ","
            AssertAnalyzesTo(a, "some-dashed-phrase", new string[] { "some", "dashed", "phrase" });
            AssertAnalyzesTo(a, "dogs,chase,cats", new string[] { "dogs", "chase", "cats" });
            AssertAnalyzesTo(a, "ac/dc", new string[] { "ac", "dc" });
        }

        [Test]
        public virtual void TestApostrophes()
        {
            // internal apostrophes: O'Reilly, you're, O'Reilly's
            // possessives are actually removed by StardardFilter, not the tokenizer
            AssertAnalyzesTo(a, "O'Reilly", new string[] { "o'reilly" });
            AssertAnalyzesTo(a, "you're", new string[] { "you're" });
            AssertAnalyzesTo(a, "she's", new string[] { "she" });
            AssertAnalyzesTo(a, "Jim's", new string[] { "jim" });
            AssertAnalyzesTo(a, "don't", new string[] { "don't" });
            AssertAnalyzesTo(a, "O'Reilly's", new string[] { "o'reilly" });
        }

        [Test]
        public virtual void TestTSADash()
        {
            // t and s had been stopwords in Lucene <= 2.0, which made it impossible
            // to correctly search for these terms:
            AssertAnalyzesTo(a, "s-class", new string[] { "s", "class" });
            AssertAnalyzesTo(a, "t-com", new string[] { "t", "com" });
            // 'a' is still a stopword:
            AssertAnalyzesTo(a, "a-class", new string[] { "class" });
        }

        [Test]
        public virtual void TestCompanyNames()
        {
            // company names
            AssertAnalyzesTo(a, "AT&T", new string[] { "at&t" });
            AssertAnalyzesTo(a, "Excite@Home", new string[] { "excite@home" });
        }

        [Test]
        public virtual void TestLucene1140()
        {
            try
            {
                ClassicAnalyzer analyzer = new ClassicAnalyzer(TEST_VERSION_CURRENT);
                AssertAnalyzesTo(analyzer, "www.nutch.org.", new string[] { "www.nutch.org" }, new string[] { "<HOST>" });
            }
            catch (NullReferenceException)
            {
                fail("Should not throw an NPE and it did");
            }

        }

        [Test]
        public virtual void TestDomainNames()
        {
            // Current lucene should not show the bug
            ClassicAnalyzer a2 = new ClassicAnalyzer(TEST_VERSION_CURRENT);

            // domain names
            AssertAnalyzesTo(a2, "www.nutch.org", new string[] { "www.nutch.org" });
            //Notice the trailing .  See https://issues.apache.org/jira/browse/LUCENE-1068.
            // the following should be recognized as HOST:
            AssertAnalyzesTo(a2, "www.nutch.org.", new string[] { "www.nutch.org" }, new string[] { "<HOST>" });

            // 2.3 should show the bug. But, alas, it's obsolete, we don't support it.
            // a2 = new ClassicAnalyzer(org.apache.lucene.util.Version.LUCENE_23);
            // AssertAnalyzesTo(a2, "www.nutch.org.", new String[]{ "wwwnutchorg" }, new String[] { "<ACRONYM>" });

            // 2.4 should not show the bug. But, alas, it's also obsolete,
            // so we check latest released (Robert's gonna break this on 4.0 soon :) )
#pragma warning disable 612, 618
            a2 = new ClassicAnalyzer(LuceneVersion.LUCENE_31);
#pragma warning restore 612, 618
            AssertAnalyzesTo(a2, "www.nutch.org.", new string[] { "www.nutch.org" }, new string[] { "<HOST>" });
        }

        [Test]
        public virtual void TestEMailAddresses()
        {
            // email addresses, possibly with underscores, periods, etc
            AssertAnalyzesTo(a, "test@example.com", new string[] { "test@example.com" });
            AssertAnalyzesTo(a, "first.lastname@example.com", new string[] { "first.lastname@example.com" });
            AssertAnalyzesTo(a, "first_lastname@example.com", new string[] { "first_lastname@example.com" });
        }

        [Test]
        public virtual void TestNumeric()
        {
            // floating point, serial, model numbers, ip addresses, etc.
            // every other segment must have at least one digit
            AssertAnalyzesTo(a, "21.35", new string[] { "21.35" });
            AssertAnalyzesTo(a, "R2D2 C3PO", new string[] { "r2d2", "c3po" });
            AssertAnalyzesTo(a, "216.239.63.104", new string[] { "216.239.63.104" });
            AssertAnalyzesTo(a, "1-2-3", new string[] { "1-2-3" });
            AssertAnalyzesTo(a, "a1-b2-c3", new string[] { "a1-b2-c3" });
            AssertAnalyzesTo(a, "a1-b-c3", new string[] { "a1-b-c3" });
        }

        [Test]
        public virtual void TestTextWithNumbers()
        {
            // numbers
            AssertAnalyzesTo(a, "David has 5000 bones", new string[] { "david", "has", "5000", "bones" });
        }

        [Test]
        public virtual void TestVariousText()
        {
            // various
            AssertAnalyzesTo(a, "C embedded developers wanted", new string[] { "c", "embedded", "developers", "wanted" });
            AssertAnalyzesTo(a, "foo bar FOO BAR", new string[] { "foo", "bar", "foo", "bar" });
            AssertAnalyzesTo(a, "foo      bar .  FOO <> BAR", new string[] { "foo", "bar", "foo", "bar" });
            AssertAnalyzesTo(a, "\"QUOTED\" word", new string[] { "quoted", "word" });
        }

        [Test]
        public virtual void TestAcronyms()
        {
            // acronyms have their dots stripped
            AssertAnalyzesTo(a, "U.S.A.", new string[] { "usa" });
        }

        [Test]
        public virtual void TestCPlusPlusHash()
        {
            // It would be nice to change the grammar in StandardTokenizer.jj to make "C#" and "C++" end up as tokens.
            AssertAnalyzesTo(a, "C++", new string[] { "c" });
            AssertAnalyzesTo(a, "C#", new string[] { "c" });
        }

        [Test]
        public virtual void TestKorean()
        {
            // Korean words
            AssertAnalyzesTo(a, "안녕하세요 한글입니다", new string[] { "안녕하세요", "한글입니다" });
        }

        // Compliance with the "old" JavaCC-based analyzer, see:
        // https://issues.apache.org/jira/browse/LUCENE-966#action_12516752

        [Test]
        public virtual void TestComplianceFileName()
        {
            AssertAnalyzesTo(a, "2004.jpg", new string[] { "2004.jpg" }, new string[] { "<HOST>" });
        }

        [Test]
        public virtual void TestComplianceNumericIncorrect()
        {
            AssertAnalyzesTo(a, "62.46", new string[] { "62.46" }, new string[] { "<HOST>" });
        }

        [Test]
        public virtual void TestComplianceNumericLong()
        {
            AssertAnalyzesTo(a, "978-0-94045043-1", new string[] { "978-0-94045043-1" }, new string[] { "<NUM>" });
        }

        [Test]
        public virtual void TestComplianceNumericFile()
        {
            AssertAnalyzesTo(a, "78academyawards/rules/rule02.html", new string[] { "78academyawards/rules/rule02.html" }, new string[] { "<NUM>" });
        }

        [Test]
        public virtual void TestComplianceNumericWithUnderscores()
        {
            AssertAnalyzesTo(a, "2006-03-11t082958z_01_ban130523_rtridst_0_ozabs", new string[] { "2006-03-11t082958z_01_ban130523_rtridst_0_ozabs" }, new string[] { "<NUM>" });
        }

        [Test]
        public virtual void TestComplianceNumericWithDash()
        {
            AssertAnalyzesTo(a, "mid-20th", new string[] { "mid-20th" }, new string[] { "<NUM>" });
        }

        [Test]
        public virtual void TestComplianceManyTokens()
        {
            AssertAnalyzesTo(a, "/money.cnn.com/magazines/fortune/fortune_archive/2007/03/19/8402357/index.htm " + "safari-0-sheikh-zayed-grand-mosque.jpg", new string[] { "money.cnn.com", "magazines", "fortune", "fortune", "archive/2007/03/19/8402357", "index.htm", "safari-0-sheikh", "zayed", "grand", "mosque.jpg" }, new string[] { "<HOST>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<NUM>", "<HOST>", "<NUM>", "<ALPHANUM>", "<ALPHANUM>", "<HOST>" });
        }

        [Test]
        public virtual void TestJava14BWCompatibility()
        {
#pragma warning disable 612, 618
            ClassicAnalyzer sa = new ClassicAnalyzer(LuceneVersion.LUCENE_30);
#pragma warning restore 612, 618
            AssertAnalyzesTo(sa, "test\u02C6test", new string[] { "test", "test" });
        }

        /// <summary>
        /// Make sure we skip wicked long terms.
        /// </summary>
        [Test]
        public virtual void TestWickedLongTerm()
        {
            using RAMDirectory dir = new RAMDirectory();
            char[] chars = new char[IndexWriter.MAX_TERM_LENGTH];
            Arrays.Fill(chars, 'x');

            string bigTerm = new string(chars);
            Document doc = new Document();

            using (IndexWriter writer = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, new ClassicAnalyzer(TEST_VERSION_CURRENT))))
            {
                // This produces a too-long term:
                string contents = "abc xyz x" + bigTerm + " another term";
                doc.Add(new TextField("content", contents, Field.Store.NO));
                writer.AddDocument(doc);

                // Make sure we can add another normal document
                doc = new Document();
                doc.Add(new TextField("content", "abc bbb ccc", Field.Store.NO));
                writer.AddDocument(doc);
            }
#pragma warning disable 612, 618
            using (IndexReader reader = IndexReader.Open(dir))
#pragma warning restore 612, 618
            {

                // Make sure all terms < max size were indexed
                assertEquals(2, reader.DocFreq(new Term("content", "abc")));
                assertEquals(1, reader.DocFreq(new Term("content", "bbb")));
                assertEquals(1, reader.DocFreq(new Term("content", "term")));
                assertEquals(1, reader.DocFreq(new Term("content", "another")));

                // Make sure position is still incremented when
                // massive term is skipped:
                DocsAndPositionsEnum tps = MultiFields.GetTermPositionsEnum(reader, MultiFields.GetLiveDocs(reader), "content", new BytesRef("another"));
                assertTrue(tps.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
                assertEquals(1, tps.Freq);
                assertEquals(3, tps.NextPosition());

                // Make sure the doc that has the massive term is in
                // the index:
                assertEquals("document with wicked long term should is not in the index!", 2, reader.NumDocs);

            }

            // Make sure we can add a document with exactly the
            // maximum length term, and search on that term:
            doc = new Document();
            doc.Add(new TextField("content", bigTerm, Field.Store.NO));
            ClassicAnalyzer sa = new ClassicAnalyzer(TEST_VERSION_CURRENT);
            sa.MaxTokenLength = 100000;
            using (var writer = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, sa)))
            {
                writer.AddDocument(doc);
            }
#pragma warning disable 612, 618
            using (var reader = IndexReader.Open(dir))
#pragma warning restore 612, 618
            {
                assertEquals(1, reader.DocFreq(new Term("content", bigTerm)));
            }
        }

        /// <summary>
        /// blast some random strings through the analyzer </summary>
        [Test]
        public virtual void TestRandomStrings()
        {
            CheckRandomData(Random, new ClassicAnalyzer(TEST_VERSION_CURRENT), 1000 * RandomMultiplier);
        }

        /// <summary>
        /// blast some random large strings through the analyzer </summary>
        [Test]
        public virtual void TestRandomHugeStrings()
        {
            Random random = Random;
            CheckRandomData(random, new ClassicAnalyzer(TEST_VERSION_CURRENT), 100 * RandomMultiplier, 8192);
        }
    }
}