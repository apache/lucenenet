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

namespace Lucene.Net.QueryParsers.Classic
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

    [TestFixture]
    public class TestMultiFieldQueryParser : LuceneTestCase
    {
        /// <summary>
        /// test stop words parsing for both the non static form, and for the
        /// corresponding static form (qtxt, fields[]).
        /// </summary>
        [Test]
        public virtual void TestStopwordsParsing()
        {
            AssertStopQueryEquals("one", "b:one t:one");
            AssertStopQueryEquals("one stop", "b:one t:one");
            AssertStopQueryEquals("one (stop)", "b:one t:one");
            AssertStopQueryEquals("one ((stop))", "b:one t:one");
            AssertStopQueryEquals("stop", "");
            AssertStopQueryEquals("(stop)", "");
            AssertStopQueryEquals("((stop))", "");
        }

        /// <summary>
        /// verify parsing of query using a stopping analyzer
        /// </summary>
        /// <param name="qtxt"></param>
        /// <param name="expectedRes"></param>
        private void AssertStopQueryEquals(string qtxt, string expectedRes)
        {
            string[] fields = { "b", "t" };
            Occur[] occur = new Occur[] { Occur.SHOULD, Occur.SHOULD };
            TestQueryParser.QPTestAnalyzer a = new TestQueryParser.QPTestAnalyzer();
            MultiFieldQueryParser mfqp = new MultiFieldQueryParser(TEST_VERSION_CURRENT, fields, a);

            Query q = mfqp.Parse(qtxt);
            assertEquals(expectedRes, q.toString());

            q = MultiFieldQueryParser.Parse(TEST_VERSION_CURRENT, qtxt, fields, occur, a);
            assertEquals(expectedRes, q.toString());
        }

        [Test]
        public virtual void TestSimple()
        {
            string[] fields = { "b", "t" };
            MultiFieldQueryParser mfqp = new MultiFieldQueryParser(TEST_VERSION_CURRENT, fields, new MockAnalyzer(Random));

            Query q = mfqp.Parse("one");
            assertEquals("b:one t:one", q.toString());

            q = mfqp.Parse("one two");
            assertEquals("(b:one t:one) (b:two t:two)", q.toString());

            q = mfqp.Parse("+one +two");
            assertEquals("+(b:one t:one) +(b:two t:two)", q.toString());

            q = mfqp.Parse("+one -two -three");
            assertEquals("+(b:one t:one) -(b:two t:two) -(b:three t:three)", q.toString());

            q = mfqp.Parse("one^2 two");
            assertEquals("((b:one t:one)^2.0) (b:two t:two)", q.toString());

            q = mfqp.Parse("one~ two");
            assertEquals("(b:one~2 t:one~2) (b:two t:two)", q.toString());

            q = mfqp.Parse("one~0.8 two^2");
            assertEquals("(b:one~0 t:one~0) ((b:two t:two)^2.0)", q.toString());

            q = mfqp.Parse("one* two*");
            assertEquals("(b:one* t:one*) (b:two* t:two*)", q.toString());

            q = mfqp.Parse("[a TO c] two");
            assertEquals("(b:[a TO c] t:[a TO c]) (b:two t:two)", q.toString());

            q = mfqp.Parse("w?ldcard");
            assertEquals("b:w?ldcard t:w?ldcard", q.toString());

            q = mfqp.Parse("\"foo bar\"");
            assertEquals("b:\"foo bar\" t:\"foo bar\"", q.toString());

            q = mfqp.Parse("\"aa bb cc\" \"dd ee\"");
            assertEquals("(b:\"aa bb cc\" t:\"aa bb cc\") (b:\"dd ee\" t:\"dd ee\")", q.toString());

            q = mfqp.Parse("\"foo bar\"~4");
            assertEquals("b:\"foo bar\"~4 t:\"foo bar\"~4", q.toString());

            // LUCENE-1213: MultiFieldQueryParser was ignoring slop when phrase had a field.
            q = mfqp.Parse("b:\"foo bar\"~4");
            assertEquals("b:\"foo bar\"~4", q.toString());

            // make sure that terms which have a field are not touched:
            q = mfqp.Parse("one f:two");
            assertEquals("(b:one t:one) f:two", q.toString());

            // AND mode:
            mfqp.DefaultOperator = QueryParserBase.AND_OPERATOR;
            q = mfqp.Parse("one two");
            assertEquals("+(b:one t:one) +(b:two t:two)", q.toString());
            q = mfqp.Parse("\"aa bb cc\" \"dd ee\"");
            assertEquals("+(b:\"aa bb cc\" t:\"aa bb cc\") +(b:\"dd ee\" t:\"dd ee\")", q.toString());
        }

        [Test]
        public virtual void TestBoostsSimple()
        {
            IDictionary<string, float> boosts = new Dictionary<string, float>();
            boosts["b"] = (float)5;
            boosts["t"] = (float)10;
            string[] fields = { "b", "t" };
            MultiFieldQueryParser mfqp = new MultiFieldQueryParser(TEST_VERSION_CURRENT, fields, new MockAnalyzer(Random), boosts);


            //Check for simple
            Query q = mfqp.Parse("one");
            assertEquals("b:one^5.0 t:one^10.0", q.toString());

            //Check for AND
            q = mfqp.Parse("one AND two");
            assertEquals("+(b:one^5.0 t:one^10.0) +(b:two^5.0 t:two^10.0)", q.toString());

            //Check for OR
            q = mfqp.Parse("one OR two");
            assertEquals("(b:one^5.0 t:one^10.0) (b:two^5.0 t:two^10.0)", q.toString());

            //Check for AND and a field
            q = mfqp.Parse("one AND two AND foo:test");
            assertEquals("+(b:one^5.0 t:one^10.0) +(b:two^5.0 t:two^10.0) +foo:test", q.toString());

            q = mfqp.Parse("one^3 AND two^4");
            assertEquals("+((b:one^5.0 t:one^10.0)^3.0) +((b:two^5.0 t:two^10.0)^4.0)", q.toString());
        }

        [Test]
        public virtual void TestStaticMethod1()
        {
            string[] fields = { "b", "t" };
            string[] queries = { "one", "two" };
            Query q = MultiFieldQueryParser.Parse(TEST_VERSION_CURRENT, queries, fields, new MockAnalyzer(Random));
            assertEquals("b:one t:two", q.toString());

            string[] queries2 = { "+one", "+two" };
            q = MultiFieldQueryParser.Parse(TEST_VERSION_CURRENT, queries2, fields, new MockAnalyzer(Random));
            assertEquals("(+b:one) (+t:two)", q.toString());

            string[] queries3 = { "one", "+two" };
            q = MultiFieldQueryParser.Parse(TEST_VERSION_CURRENT, queries3, fields, new MockAnalyzer(Random));
            assertEquals("b:one (+t:two)", q.toString());

            string[] queries4 = { "one +more", "+two" };
            q = MultiFieldQueryParser.Parse(TEST_VERSION_CURRENT, queries4, fields, new MockAnalyzer(Random));
            assertEquals("(b:one +b:more) (+t:two)", q.toString());

            string[] queries5 = { "blah" };
            try
            {
                q = MultiFieldQueryParser.Parse(TEST_VERSION_CURRENT, queries5, fields, new MockAnalyzer(Random));
                fail();
            }
            catch (Exception e) when (e.IsIllegalArgumentException())
            {
                // expected exception, array length differs
            }

            // check also with stop words for this static form (qtxts[], fields[]).
            TestQueryParser.QPTestAnalyzer stopA = new TestQueryParser.QPTestAnalyzer();

            string[] queries6 = { "((+stop))", "+((stop))" };
            q = MultiFieldQueryParser.Parse(TEST_VERSION_CURRENT, queries6, fields, stopA);
            assertEquals("", q.toString());

            string[] queries7 = { "one ((+stop)) +more", "+((stop)) +two" };
            q = MultiFieldQueryParser.Parse(TEST_VERSION_CURRENT, queries7, fields, stopA);
            assertEquals("(b:one +b:more) (+t:two)", q.toString());
        }

        [Test]
        public virtual void TestStaticMethod2()
        {
            string[] fields = { "b", "t" };
            Occur[] flags = { Occur.MUST, Occur.MUST_NOT };
            Query q = MultiFieldQueryParser.Parse(TEST_VERSION_CURRENT, "one", fields, flags, new MockAnalyzer(Random));
            assertEquals("+b:one -t:one", q.toString());

            q = MultiFieldQueryParser.Parse(TEST_VERSION_CURRENT, "one two", fields, flags, new MockAnalyzer(Random));
            assertEquals("+(b:one b:two) -(t:one t:two)", q.toString());

            try
            {
                Occur[] flags2 = { Occur.MUST };
                q = MultiFieldQueryParser.Parse(TEST_VERSION_CURRENT, "blah", fields, flags2, new MockAnalyzer(Random));
                fail();
            }
            catch (Exception e) when (e.IsIllegalArgumentException())
            {
                // expected exception, array length differs
            }
        }

        [Test]
        public virtual void TestStaticMethod2Old()
        {
            string[] fields = { "b", "t" };
            //int[] flags = {MultiFieldQueryParser.REQUIRED_FIELD, MultiFieldQueryParser.PROHIBITED_FIELD};
            Occur[] flags = { Occur.MUST, Occur.MUST_NOT };

            Query q = MultiFieldQueryParser.Parse(TEST_VERSION_CURRENT, "one", fields, flags, new MockAnalyzer(Random));//, fields, flags, new MockAnalyzer(random));
            assertEquals("+b:one -t:one", q.toString());

            q = MultiFieldQueryParser.Parse(TEST_VERSION_CURRENT, "one two", fields, flags, new MockAnalyzer(Random));
            assertEquals("+(b:one b:two) -(t:one t:two)", q.toString());

            try
            {
                Occur[] flags2 = { Occur.MUST };
                q = MultiFieldQueryParser.Parse(TEST_VERSION_CURRENT, "blah", fields, flags2, new MockAnalyzer(Random));
                fail();
            }
            catch (Exception e) when (e.IsIllegalArgumentException())
            {
                // expected exception, array length differs
            }
        }

        [Test]
        public virtual void TestStaticMethod3()
        {
            string[] queries = { "one", "two", "three" };
            string[] fields = { "f1", "f2", "f3" };
            Occur[] flags = {Occur.MUST,
                Occur.MUST_NOT, Occur.SHOULD};
            Query q = MultiFieldQueryParser.Parse(TEST_VERSION_CURRENT, queries, fields, flags, new MockAnalyzer(Random));
            assertEquals("+f1:one -f2:two f3:three", q.toString());

            try
            {
                Occur[] flags2 = { Occur.MUST };
                q = MultiFieldQueryParser.Parse(TEST_VERSION_CURRENT, queries, fields, flags2, new MockAnalyzer(Random));
                fail();
            }
            catch (Exception e) when (e.IsIllegalArgumentException())
            {
                // expected exception, array length differs
            }
        }

        [Test]
        public virtual void TestStaticMethod3Old()
        {
            string[] queries = { "one", "two" };
            string[] fields = { "b", "t" };
            Occur[] flags = { Occur.MUST, Occur.MUST_NOT };
            Query q = MultiFieldQueryParser.Parse(TEST_VERSION_CURRENT, queries, fields, flags, new MockAnalyzer(Random));
            assertEquals("+b:one -t:two", q.toString());

            try
            {
                Occur[] flags2 = { Occur.MUST };
                q = MultiFieldQueryParser.Parse(TEST_VERSION_CURRENT, queries, fields, flags2, new MockAnalyzer(Random));
                fail();
            }
            catch (Exception e) when (e.IsIllegalArgumentException())
            {
                // expected exception, array length differs
            }
        }

        [Test]
        public void TestAnalyzerReturningNull()
        {
            string[] fields = new string[] { "f1", "f2", "f3" };
            MultiFieldQueryParser parser = new MultiFieldQueryParser(TEST_VERSION_CURRENT, fields, new AnalyzerReturningNull());
            Query q = parser.Parse("bla AND blo");
            assertEquals("+(f2:bla f3:bla) +(f2:blo f3:blo)", q.toString());
            // the following queries are not affected as their terms are not analyzed anyway:
            q = parser.Parse("bla*");
            assertEquals("f1:bla* f2:bla* f3:bla*", q.toString());
            q = parser.Parse("bla~");
            assertEquals("f1:bla~2 f2:bla~2 f3:bla~2", q.toString());
            q = parser.Parse("[a TO c]");
            assertEquals("f1:[a TO c] f2:[a TO c] f3:[a TO c]", q.toString());
        }

        [Test]
        public virtual void TestStopWordSearching()
        {
            Analyzer analyzer = new MockAnalyzer(Random);
            using var ramDir = NewDirectory();
            using (IndexWriter iw = new IndexWriter(ramDir, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer)))
            {
                Document doc = new Document();
                doc.Add(NewTextField("body", "blah the footest blah", Field.Store.NO));
                iw.AddDocument(doc);
            }

            MultiFieldQueryParser mfqp =
              new MultiFieldQueryParser(TEST_VERSION_CURRENT, new string[] { "body" }, analyzer);
            mfqp.DefaultOperator = Operator.AND;
            Query q = mfqp.Parse("the footest");
            using IndexReader ir = DirectoryReader.Open(ramDir);
            IndexSearcher @is = NewSearcher(ir);
            ScoreDoc[] hits = @is.Search(q, null, 1000).ScoreDocs;
            assertEquals(1, hits.Length);
        }

        private class AnalyzerReturningNull : Analyzer
        {
            MockAnalyzer stdAnalyzer = new MockAnalyzer(Random);

            public AnalyzerReturningNull()
                : base(PER_FIELD_REUSE_STRATEGY)
            { }

            protected internal override TextReader InitReader(string fieldName, TextReader reader)
            {
                if ("f1".Equals(fieldName, StringComparison.Ordinal))
                {
                    // we don't use the reader, so close it:
                    IOUtils.DisposeWhileHandlingException(reader);
                    // return empty reader, so MockTokenizer returns no tokens:
                    return new StringReader("");
                }
                else
                {
                    return base.InitReader(fieldName, reader);
                }
            }

            protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                return stdAnalyzer.CreateComponents(fieldName, reader);
            }

            // LUCENENET specific
            protected override void Dispose(bool disposing)
            {
                try
                {
                    if (disposing)
                    {
                        stdAnalyzer?.Dispose(); // LUCENENET specific - dispose stdAnalyzer and set to null
                        stdAnalyzer = null;
                    }
                }
                finally
                {
                    base.Dispose(disposing);
                }
            }
        }

        [Test]
        public virtual void TestSimpleRegex()
        {
            string[] fields = new string[] { "a", "b" };
            MultiFieldQueryParser mfqp = new MultiFieldQueryParser(TEST_VERSION_CURRENT, fields, new MockAnalyzer(Random));

            BooleanQuery bq = new BooleanQuery(true);
            bq.Add(new RegexpQuery(new Term("a", "[a-z][123]")), Occur.SHOULD);
            bq.Add(new RegexpQuery(new Term("b", "[a-z][123]")), Occur.SHOULD);
            assertEquals(bq, mfqp.Parse("/[a-z][123]/"));
        }

        [Test]
        [LuceneNetSpecific] // LUCENENET specific - Issue #1157
        public virtual void TestFieldBoostsWithPartialBoostMap()
        {
            string[] fields = { "title", "keyword", "description" };
            MockAnalyzer analyzer = new MockAnalyzer(Random);

            // Create a boost map that only contains boosts for some fields, not all
            // This tests that the TryGetValue fix prevents KeyNotFoundException
            var boosts = new Dictionary<string, float>
            {
                { "title", 2.0f },
                // Intentionally omitting "keyword" and "description" from boost map
            };

            MultiFieldQueryParser parser = new MultiFieldQueryParser(TEST_VERSION_CURRENT, fields, analyzer, boosts);
            Query q = parser.Parse("test");

            // The query should successfully parse without throwing KeyNotFoundException
            string queryString = q.toString();
            assertTrue("Query should contain boosted title field", queryString.Contains("title:test^2.0"));
            assertTrue("Query should contain keyword field without boost", queryString.Contains("keyword:test"));
            assertTrue("Query should contain description field without boost", queryString.Contains("description:test"));

            // Ensure no boost notation for fields not in the boost map
            assertFalse("Keyword should not have boost notation", queryString.Contains("keyword:test^"));
            assertFalse("Description should not have boost notation", queryString.Contains("description:test^"));
        }

        [Test]
        [LuceneNetSpecific] // LUCENENET specific - Issue #1157
        public virtual void TestFieldBoosts()
        {
            string[] fields = { "title", "keyword" };
            MockAnalyzer analyzer = new MockAnalyzer(Random);

            // Test 1: Verify boosts are applied to the query string representation
            var boosts = new Dictionary<string, float>
            {
                { "title", 2.0f },
                { "keyword", 1.0f }
            };

            MultiFieldQueryParser parser = new MultiFieldQueryParser(TEST_VERSION_CURRENT, fields, analyzer, boosts);
            Query q = parser.Parse("ldqk");

            // The query should have different boosts for each field
            string queryString = q.toString();
            assertTrue("Query should contain boosted title field", queryString.Contains("title:ldqk^2.0"));
            assertFalse("Keyword field should not have boost notation when boost is 1.0", queryString.Contains("keyword:ldqk^"));

            // Test 2: Different boost configuration
            var boosts2 = new Dictionary<string, float>
            {
                { "title", 1.0f },
                { "keyword", 2.0f }
            };

            MultiFieldQueryParser parser2 = new MultiFieldQueryParser(TEST_VERSION_CURRENT, fields, analyzer, boosts2);
            Query q2 = parser2.Parse("ldqk");

            string queryString2 = q2.toString();
            assertFalse("Title field should not have boost notation when boost is 1.0", queryString2.Contains("title:ldqk^"));
            assertTrue("Query should contain boosted keyword field", queryString2.Contains("keyword:ldqk^2.0"));

            // Test 3: Verify that boosts actually affect document scoring
            using var ramDir = NewDirectory();
            using (IndexWriter iw = new IndexWriter(ramDir, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer)))
            {
                // Doc 0: "ldqk" only in title
                Document doc0 = new Document();
                doc0.Add(NewTextField("title", "ldqk", Field.Store.YES));
                doc0.Add(NewTextField("keyword", "other", Field.Store.YES));
                iw.AddDocument(doc0);

                // Doc 1: "ldqk" only in keyword
                Document doc1 = new Document();
                doc1.Add(NewTextField("title", "other", Field.Store.YES));
                doc1.Add(NewTextField("keyword", "ldqk", Field.Store.YES));
                iw.AddDocument(doc1);
            }

            using (IndexReader ir = DirectoryReader.Open(ramDir))
            {
                IndexSearcher searcher = NewSearcher(ir);

                // Test with equal boosts first (baseline)
                var equalBoosts = new Dictionary<string, float>
                {
                    { "title", 1.0f },
                    { "keyword", 1.0f }
                };
                MultiFieldQueryParser equalParser = new MultiFieldQueryParser(TEST_VERSION_CURRENT, fields, analyzer, equalBoosts);
                Query equalQuery = equalParser.Parse("ldqk");
                TopDocs equalDocs = searcher.Search(equalQuery, 10);

                // Get baseline scores
                float doc0BaseScore = 0, doc1BaseScore = 0;
                foreach (var scoreDoc in equalDocs.ScoreDocs)
                {
                    if (scoreDoc.Doc == 0) doc0BaseScore = scoreDoc.Score;
                    if (scoreDoc.Doc == 1) doc1BaseScore = scoreDoc.Score;
                }

                // Search with title boosted 2.0
                var titleBoosts = new Dictionary<string, float>
                {
                    { "title", 2.0f },
                    { "keyword", 1.0f }
                };
                MultiFieldQueryParser titleParser = new MultiFieldQueryParser(TEST_VERSION_CURRENT, fields, analyzer, titleBoosts);
                Query titleQuery = titleParser.Parse("ldqk");
                TopDocs titleDocs = searcher.Search(titleQuery, 10);

                // Get scores with title boost
                float doc0TitleBoostScore = 0, doc1TitleBoostScore = 0;
                foreach (var scoreDoc in titleDocs.ScoreDocs)
                {
                    if (scoreDoc.Doc == 0) doc0TitleBoostScore = scoreDoc.Score;
                    if (scoreDoc.Doc == 1) doc1TitleBoostScore = scoreDoc.Score;
                }

                // Search with keyword boosted 2.0
                var keywordBoosts = new Dictionary<string, float>
                {
                    { "title", 1.0f },
                    { "keyword", 2.0f }
                };
                MultiFieldQueryParser keywordParser = new MultiFieldQueryParser(TEST_VERSION_CURRENT, fields, analyzer, keywordBoosts);
                Query keywordQuery = keywordParser.Parse("ldqk");
                TopDocs keywordDocs = searcher.Search(keywordQuery, 10);

                // Get scores with keyword boost
                float doc0KeywordBoostScore = 0, doc1KeywordBoostScore = 0;
                foreach (var scoreDoc in keywordDocs.ScoreDocs)
                {
                    if (scoreDoc.Doc == 0) doc0KeywordBoostScore = scoreDoc.Score;
                    if (scoreDoc.Doc == 1) doc1KeywordBoostScore = scoreDoc.Score;
                }

                // Assertions:
                // When title is boosted, doc0 (title match) should score higher than baseline
                assertTrue("Doc0 with title match should score higher when title is boosted compared to equal boosts",
                          doc0TitleBoostScore > doc0BaseScore);

                // When keyword is boosted, doc1 (keyword match) should score higher than baseline
                assertTrue("Doc1 with keyword match should score higher when keyword is boosted compared to equal boosts",
                          doc1KeywordBoostScore > doc1BaseScore);

                // Doc0 should score higher with title boost than with keyword boost
                assertTrue("Doc0 (title match) should score higher with title boost than keyword boost",
                          doc0TitleBoostScore > doc0KeywordBoostScore);

                // Doc1 should score higher with keyword boost than with title boost
                assertTrue("Doc1 (keyword match) should score higher with keyword boost than title boost",
                          doc1KeywordBoostScore > doc1TitleBoostScore);
            }
        }
    }
}
