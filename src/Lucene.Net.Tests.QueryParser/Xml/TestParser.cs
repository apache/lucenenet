using Lucene.Net.Analysis;
using Lucene.Net.Attributes;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;

namespace Lucene.Net.QueryParsers.Xml
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

    public class TestParser : LuceneTestCase
    {
        private static CoreParser builder;
        private static Store.Directory dir;
        private static IndexReader reader;
        private static IndexSearcher searcher;

        [OneTimeSetUp]
        public override void OneTimeSetUp()
        {
            base.OneTimeSetUp();

            // TODO: rewrite test (this needs to set QueryParser.enablePositionIncrements, too, for work with CURRENT):
            Analyzer analyzer = new MockAnalyzer(Random, MockTokenizer.WHITESPACE, true, MockTokenFilter.ENGLISH_STOPSET);
            //initialize the parser
            builder = new CorePlusExtensionsParser("contents", analyzer);

            TextReader d = new StreamReader(
                typeof(TestParser).getResourceAsStream("reuters21578.txt"), Encoding.ASCII);
            dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer));
            String line = d.ReadLine();
            while (line != null)
            {
                int endOfDate = line.IndexOf('\t');
                String date = line.Substring(0, endOfDate).Trim();
                String content = line.Substring(endOfDate).Trim();
                Document doc = new Document();
                doc.Add(NewTextField("date", date, Field.Store.YES));
                doc.Add(NewTextField("contents", content, Field.Store.YES));
                doc.Add(new Int32Field("date2", Convert.ToInt32(date), Field.Store.NO));
                writer.AddDocument(doc);
                line = d.ReadLine();
            }
            d.Dispose();
            writer.Dispose();
            reader = DirectoryReader.Open(dir);
            searcher = NewSearcher(reader);
        }

        [OneTimeTearDown]
        public override void OneTimeTearDown()
        {
            reader.Dispose();
            dir.Dispose();
            reader = null;
            searcher = null;
            dir = null;
            builder = null;
            base.OneTimeTearDown();
        }

        [Test]
        public void TestSimpleXML()
        {
            Query q = Parse("TermQuery.xml");
            DumpResults("TermQuery", q, 5);
        }

        // LUCENENET: Ported from Lucene 7.1 (SOLR-11477)
        [Test]
        public void Test_DOCTYPE_TermQueryXML()
        {
            ParserException pe = Assert.Throws<ParserException>(() => Parse("DOCTYPE_TermQuery.xml"));
            AssertExternalEntityResolvingUnsupported(pe);
        }

        // LUCENENET: Ported from Lucene 7.1 (SOLR-11477)
        [Test]
        public void Test_ENTITY_TermQueryXML()
        {
            ParserException pe = Assert.Throws<ParserException>(() => Parse("ENTITY_TermQuery.xml"));
            AssertExternalEntityResolvingUnsupported(pe);
        }

        [Test]
        [LuceneNetSpecific]
        public void TestInternalEntityIsExpanded()
        {
            const string xml = "<?xml version=\"1.0\"?><!DOCTYPE TermQuery [<!ENTITY internalTerm \"sumitomo\">]>"
                + "<TermQuery fieldName=\"contents\">&internalTerm;</TermQuery>";
            Query q = builder.Parse(new StringReader(xml));
            Assert.IsInstanceOf<TermQuery>(q);
            Assert.AreEqual(new Term("contents", "sumitomo"), ((TermQuery)q).Term);
        }

        [Test]
        [LuceneNetSpecific]
        public void TestEntityExpansionIsLimited()
        {
            string xml = CreateEntityExpansionXml("<TermQuery fieldName=\"contents\">&lol5;</TermQuery>");

            using (Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(xml)))
            {
                ParserException pe = Assert.Throws<ParserException>(() => builder.Parse(stream));
                Assert.IsInstanceOf<XmlException>(pe.InnerException);
            }

            ParserException pe2 = Assert.Throws<ParserException>(() => builder.Parse(new StringReader(xml)));
            Assert.IsInstanceOf<XmlException>(pe2.InnerException);
        }

        [Test]
        [LuceneNetSpecific]
        public void TestDOMUtilsEntityExpansionIsLimited()
        {
            string xml = CreateEntityExpansionXml("<TermQuery fieldName=\"contents\">&lol5;</TermQuery>");

            using (Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(xml)))
            {
                Exception e = Assert.Catch(() => DOMUtils.LoadXML(stream));
                Assert.IsTrue(e.IsRuntimeException());
                Assert.IsInstanceOf<XmlException>(e.InnerException);
            }

            Exception e2 = Assert.Catch(() => DOMUtils.LoadXML(new StringReader(xml)));
            Assert.IsTrue(e2.IsRuntimeException());
            Assert.IsInstanceOf<XmlException>(e2.InnerException);
        }

        [Test]
        [LuceneNetSpecific]
        public void TestDOMUtilsExternalEntityIsNotResolved()
        {
            using Stream xmlStream = typeof(TestParser).getResourceAsStream("ENTITY_TermQuery.xml");
            Exception e = Assert.Catch(() => DOMUtils.LoadXML(xmlStream));
            Assert.IsTrue(e.IsRuntimeException());
            AssertExternalEntityResolvingUnsupported(e);
        }

        [Test]
        [LuceneNetSpecific]
        public void TestBoostingTermQueryEntityExpansionIsLimited() // #1531
        {
            string xml = CreateEntityExpansionXml("<BoostingTermQuery fieldName=\"contents\">&lol5;</BoostingTermQuery>");

            using Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
            ParserException pe = Assert.Throws<ParserException>(() => builder.Parse(stream));
            Assert.IsInstanceOf<XmlException>(pe.InnerException);
        }

        [Test]
        [LuceneNetSpecific]
        public void TestExternalFileEntityIsNotResolved() // #1531
        {
            const string secret = "lucenenet-secret-1531";
            FileInfo secretFile = CreateTempFile("secret", ".txt");
            File.WriteAllText(secretFile.FullName, secret);
            string xml = "<?xml version=\"1.0\"?>"
                + "<!DOCTYPE TermQuery [<!ENTITY secret SYSTEM \"" + new Uri(secretFile.FullName).AbsoluteUri + "\">]>"
                + "<TermQuery fieldName=\"contents\">&secret;</TermQuery>";

            using (Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(xml)))
            {
                ParserException pe = Assert.Throws<ParserException>(() => builder.Parse(stream));
                AssertExternalEntityResolvingUnsupported(pe);
                StringAssert.DoesNotContain(secret, pe.ToString());
            }

            ParserException pe2 = Assert.Throws<ParserException>(() => builder.Parse(new StringReader(xml)));
            AssertExternalEntityResolvingUnsupported(pe2);
            StringAssert.DoesNotContain(secret, pe2.ToString());
        }

        [Test]
        public void TestSimpleTermsQueryXML()
        {
            Query q = Parse("TermsQuery.xml");
            DumpResults("TermsQuery", q, 5);
        }

        [Test]
        public void TestBooleanQueryXML()
        {
            Query q = Parse("BooleanQuery.xml");
            DumpResults("BooleanQuery", q, 5);
        }

        [Test]
        public void TestDisjunctionMaxQueryXML()
        {
            Query q = Parse("DisjunctionMaxQuery.xml");
            assertTrue(q is DisjunctionMaxQuery);
            DisjunctionMaxQuery d = (DisjunctionMaxQuery)q;
            assertEquals(0.0f, d.TieBreakerMultiplier, 0.0001f);
            assertEquals(2, d.Disjuncts.size());
            DisjunctionMaxQuery ndq = (DisjunctionMaxQuery)d.Disjuncts[1];
            assertEquals(1.2f, ndq.TieBreakerMultiplier, 0.0001f);
            assertEquals(1, ndq.Disjuncts.size());
        }

        [Test]
        public void TestRangeFilterQueryXML()
        {
            Query q = Parse("RangeFilterQuery.xml");
            DumpResults("RangeFilter", q, 5);
        }

        [Test]
        public void TestUserQueryXML()
        {
            Query q = Parse("UserInputQuery.xml");
            DumpResults("UserInput with Filter", q, 5);
        }

        [Test]
        public void TestCustomFieldUserQueryXML()
        {
            Query q = Parse("UserInputQueryCustomField.xml");
            int h = searcher.Search(q, null, 1000).TotalHits;
            assertEquals("UserInputQueryCustomField should produce 0 result ", 0, h);
        }

        [Test]
        public void TestLikeThisQueryXML()
        {
            Query q = Parse("LikeThisQuery.xml");
            DumpResults("like this", q, 5);
        }

        [Test]
        public void TestBoostingQueryXML()
        {
            Query q = Parse("BoostingQuery.xml");
            DumpResults("boosting ", q, 5);
        }

        [Test]
        public void TestFuzzyLikeThisQueryXML()
        {
            Query q = Parse("FuzzyLikeThisQuery.xml");
            //show rewritten fuzzyLikeThisQuery - see what is being matched on
            if (Verbose)
            {
                Console.WriteLine(q.Rewrite(reader));
            }
            DumpResults("FuzzyLikeThis", q, 5);
        }

        [Test]
        public void TestTermsFilterXML()
        {
            Query q = Parse("TermsFilterQuery.xml");
            DumpResults("Terms Filter", q, 5);
        }

        [Test]
        public void TestBoostingTermQueryXML()
        {
            Query q = Parse("BoostingTermQuery.xml");
            DumpResults("BoostingTermQuery", q, 5);
        }

        [Test]
        public void TestSpanTermXML()
        {
            Query q = Parse("SpanQuery.xml");
            DumpResults("Span Query", q, 5);
        }

        [Test]
        public void TestConstantScoreQueryXML()
        {
            Query q = Parse("ConstantScoreQuery.xml");
            DumpResults("ConstantScoreQuery", q, 5);
        }

        [Test]
        public void TestMatchAllDocsPlusFilterXML()
        {
            Query q = Parse("MatchAllDocsQuery.xml");
            DumpResults("MatchAllDocsQuery with range filter", q, 5);
        }

        [Test]
        public void TestBooleanFilterXML()
        {
            Query q = Parse("BooleanFilter.xml");
            DumpResults("Boolean filter", q, 5);
        }

        [Test]
        public void TestNestedBooleanQuery()
        {
            Query q = Parse("NestedBooleanQuery.xml");
            DumpResults("Nested Boolean query", q, 5);
        }

        [Test]
        public void TestCachedFilterXML()
        {
            Query q = Parse("CachedFilter.xml");
            DumpResults("Cached filter", q, 5);
        }

        [Test]
        public void TestDuplicateFilterQueryXML()
        {
            IList<AtomicReaderContext> leaves = searcher.TopReaderContext.Leaves;
            //AssumeTrue("", leaves.size() == 1); // LUCENENET NOTE: Not sure why this is here - the test is skipped
            Query q = Parse("DuplicateFilterQuery.xml");
            int h = searcher.Search(q, null, 1000).TotalHits;
            assertEquals("DuplicateFilterQuery should produce 1 result ", 1, h);
        }

        [Test]
        public void TestNumericRangeFilterQueryXML()
        {
            Query q = Parse("NumericRangeFilterQuery.xml");
            DumpResults("NumericRangeFilter", q, 5);
        }

        [Test]
        public void TestNumericRangeQueryQueryXML()
        {
            Query q = Parse("NumericRangeQueryQuery.xml");
            DumpResults("NumericRangeQuery", q, 5);
        }

        //================= Helper methods ===================================

        private Query Parse(String xmlFileName)
        {
            using Stream xmlStream = typeof(TestParser).getResourceAsStream(xmlFileName);
            Query result = builder.Parse(xmlStream);
            return result;
        }

        // LUCENENET specific - builds an XML document with exponentially expanding internal entities
        // (10^5 = 100,000 "lol"s, 300,000 characters), which exceeds the entity expansion limit
        private static string CreateEntityExpansionXml(string body)
        {
            return "<?xml version=\"1.0\"?>"
                + "<!DOCTYPE TermQuery ["
                + "<!ENTITY lol \"lol\">"
                + "<!ENTITY lol1 \"&lol;&lol;&lol;&lol;&lol;&lol;&lol;&lol;&lol;&lol;\">"
                + "<!ENTITY lol2 \"&lol1;&lol1;&lol1;&lol1;&lol1;&lol1;&lol1;&lol1;&lol1;&lol1;\">"
                + "<!ENTITY lol3 \"&lol2;&lol2;&lol2;&lol2;&lol2;&lol2;&lol2;&lol2;&lol2;&lol2;\">"
                + "<!ENTITY lol4 \"&lol3;&lol3;&lol3;&lol3;&lol3;&lol3;&lol3;&lol3;&lol3;&lol3;\">"
                + "<!ENTITY lol5 \"&lol4;&lol4;&lol4;&lol4;&lol4;&lol4;&lol4;&lol4;&lol4;&lol4;\">"
                + "]>"
                + body;
        }

        // LUCENENET specific - the "External Entity resolving unsupported" exception may be wrapped by XmlReader
        internal static void AssertExternalEntityResolvingUnsupported(Exception e)
        {
            for (Exception cause = e; cause != null; cause = cause.InnerException)
            {
                if (cause.Message.StartsWith("External Entity resolving unsupported:", StringComparison.Ordinal))
                {
                    return;
                }
            }
            Assert.Fail("Expected an 'External Entity resolving unsupported' exception, but got: " + e);
        }

        private void DumpResults(String qType, Query q, int numDocs)
        {
            if (Verbose)
            {
                Console.WriteLine("TEST: query=" + q);
            }
            TopDocs hits = searcher.Search(q, null, numDocs);
            assertTrue(qType + " should produce results ", hits.TotalHits > 0);
            if (Verbose)
            {
                Console.WriteLine("=========" + qType + "============");
                ScoreDoc[] scoreDocs = hits.ScoreDocs;
                for (int i = 0; i < Math.Min(numDocs, hits.TotalHits); i++)
                {
                    Document ldoc = searcher.Doc(scoreDocs[i].Doc);
                    Console.WriteLine("[" + ldoc.Get("date") + "]" + ldoc.Get("contents"));
                }
                Console.WriteLine();
            }
        }
    }
}
