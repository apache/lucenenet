using Lucene.Net.Analysis;
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
using Console = Lucene.Net.Util.SystemConsole;

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
        public override void BeforeClass()
        {
            base.BeforeClass();

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
        public override void AfterClass()
        {
            reader.Dispose();
            dir.Dispose();
            reader = null;
            searcher = null;
            dir = null;
            builder = null;
            base.AfterClass();
        }

        [Test]
        public void TestSimpleXML()
        {
            Query q = Parse("TermQuery.xml");
            DumpResults("TermQuery", q, 5);
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
