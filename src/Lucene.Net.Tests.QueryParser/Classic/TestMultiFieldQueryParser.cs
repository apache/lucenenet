using Lucene.Net.Analysis;
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
            BooleanClause.Occur[] occur = new BooleanClause.Occur[] { BooleanClause.Occur.SHOULD, BooleanClause.Occur.SHOULD };
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
            MultiFieldQueryParser mfqp = new MultiFieldQueryParser(TEST_VERSION_CURRENT, fields, new MockAnalyzer(Random()));

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
            MultiFieldQueryParser mfqp = new MultiFieldQueryParser(TEST_VERSION_CURRENT, fields, new MockAnalyzer(Random()), boosts);


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
            Query q = MultiFieldQueryParser.Parse(TEST_VERSION_CURRENT, queries, fields, new MockAnalyzer(Random()));
            assertEquals("b:one t:two", q.toString());

            string[] queries2 = { "+one", "+two" };
            q = MultiFieldQueryParser.Parse(TEST_VERSION_CURRENT, queries2, fields, new MockAnalyzer(Random()));
            assertEquals("(+b:one) (+t:two)", q.toString());

            string[] queries3 = { "one", "+two" };
            q = MultiFieldQueryParser.Parse(TEST_VERSION_CURRENT, queries3, fields, new MockAnalyzer(Random()));
            assertEquals("b:one (+t:two)", q.toString());

            string[] queries4 = { "one +more", "+two" };
            q = MultiFieldQueryParser.Parse(TEST_VERSION_CURRENT, queries4, fields, new MockAnalyzer(Random()));
            assertEquals("(b:one +b:more) (+t:two)", q.toString());

            string[] queries5 = { "blah" };
            try
            {
                q = MultiFieldQueryParser.Parse(TEST_VERSION_CURRENT, queries5, fields, new MockAnalyzer(Random()));
                fail();
            }
            catch (ArgumentException /*e*/)
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
            BooleanClause.Occur[] flags = { BooleanClause.Occur.MUST, BooleanClause.Occur.MUST_NOT };
            Query q = MultiFieldQueryParser.Parse(TEST_VERSION_CURRENT, "one", fields, flags, new MockAnalyzer(Random()));
            assertEquals("+b:one -t:one", q.toString());

            q = MultiFieldQueryParser.Parse(TEST_VERSION_CURRENT, "one two", fields, flags, new MockAnalyzer(Random()));
            assertEquals("+(b:one b:two) -(t:one t:two)", q.toString());

            try
            {
                BooleanClause.Occur[] flags2 = { BooleanClause.Occur.MUST };
                q = MultiFieldQueryParser.Parse(TEST_VERSION_CURRENT, "blah", fields, flags2, new MockAnalyzer(Random()));
                fail();
            }
            catch (ArgumentException /*e*/)
            {
                // expected exception, array length differs
            }
        }

        [Test]
        public virtual void TestStaticMethod2Old()
        {
            string[] fields = { "b", "t" };
            //int[] flags = {MultiFieldQueryParser.REQUIRED_FIELD, MultiFieldQueryParser.PROHIBITED_FIELD};
            BooleanClause.Occur[] flags = { BooleanClause.Occur.MUST, BooleanClause.Occur.MUST_NOT };

            Query q = MultiFieldQueryParser.Parse(TEST_VERSION_CURRENT, "one", fields, flags, new MockAnalyzer(Random()));//, fields, flags, new MockAnalyzer(random));
            assertEquals("+b:one -t:one", q.toString());

            q = MultiFieldQueryParser.Parse(TEST_VERSION_CURRENT, "one two", fields, flags, new MockAnalyzer(Random()));
            assertEquals("+(b:one b:two) -(t:one t:two)", q.toString());

            try
            {
                BooleanClause.Occur[] flags2 = { BooleanClause.Occur.MUST };
                q = MultiFieldQueryParser.Parse(TEST_VERSION_CURRENT, "blah", fields, flags2, new MockAnalyzer(Random()));
                fail();
            }
            catch (ArgumentException /*e*/)
            {
                // expected exception, array length differs
            }
        }

        [Test]
        public virtual void TestStaticMethod3()
        {
            string[] queries = { "one", "two", "three" };
            string[] fields = { "f1", "f2", "f3" };
            BooleanClause.Occur[] flags = {BooleanClause.Occur.MUST,
                BooleanClause.Occur.MUST_NOT, BooleanClause.Occur.SHOULD};
            Query q = MultiFieldQueryParser.Parse(TEST_VERSION_CURRENT, queries, fields, flags, new MockAnalyzer(Random()));
            assertEquals("+f1:one -f2:two f3:three", q.toString());

            try
            {
                BooleanClause.Occur[] flags2 = { BooleanClause.Occur.MUST };
                q = MultiFieldQueryParser.Parse(TEST_VERSION_CURRENT, queries, fields, flags2, new MockAnalyzer(Random()));
                fail();
            }
            catch (ArgumentException /*e*/)
            {
                // expected exception, array length differs
            }
        }

        [Test]
        public virtual void TestStaticMethod3Old()
        {
            string[] queries = { "one", "two" };
            string[] fields = { "b", "t" };
            BooleanClause.Occur[] flags = { BooleanClause.Occur.MUST, BooleanClause.Occur.MUST_NOT };
            Query q = MultiFieldQueryParser.Parse(TEST_VERSION_CURRENT, queries, fields, flags, new MockAnalyzer(Random()));
            assertEquals("+b:one -t:two", q.toString());

            try
            {
                BooleanClause.Occur[] flags2 = { BooleanClause.Occur.MUST };
                q = MultiFieldQueryParser.Parse(TEST_VERSION_CURRENT, queries, fields, flags2, new MockAnalyzer(Random()));
                fail();
            }
            catch (ArgumentException /*e*/)
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
            Analyzer analyzer = new MockAnalyzer(Random());
            using (var ramDir = NewDirectory())
            {
                using (IndexWriter iw = new IndexWriter(ramDir, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer)))
                {
                    Document doc = new Document();
                    doc.Add(NewTextField("body", "blah the footest blah", Field.Store.NO));
                    iw.AddDocument(doc);
                }

                MultiFieldQueryParser mfqp =
                  new MultiFieldQueryParser(TEST_VERSION_CURRENT, new string[] { "body" }, analyzer);
                mfqp.DefaultOperator = QueryParser.Operator.AND;
                Query q = mfqp.Parse("the footest");
                using (IndexReader ir = DirectoryReader.Open(ramDir))
                {
                    IndexSearcher @is = NewSearcher(ir);
                    ScoreDoc[] hits = @is.Search(q, null, 1000).ScoreDocs;
                    assertEquals(1, hits.Length);
                }
            }
        }

        private class AnalyzerReturningNull : Analyzer
        {
            MockAnalyzer stdAnalyzer = new MockAnalyzer(Random());

            public AnalyzerReturningNull()
                : base(PER_FIELD_REUSE_STRATEGY)
            { }

            public override System.IO.TextReader InitReader(string fieldName, TextReader reader)
            {
                if ("f1".equals(fieldName))
                {
                    // we don't use the reader, so close it:
                    IOUtils.CloseWhileHandlingException(reader);
                    // return empty reader, so MockTokenizer returns no tokens:
                    return new StringReader("");
                }
                else
                {
                    return base.InitReader(fieldName, reader);
                }
            }

            public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                return stdAnalyzer.CreateComponents(fieldName, reader);
            }
        }

        [Test]
        public virtual void TestSimpleRegex()
        {
            string[] fields = new string[] { "a", "b" };
            MultiFieldQueryParser mfqp = new MultiFieldQueryParser(TEST_VERSION_CURRENT, fields, new MockAnalyzer(Random()));

            BooleanQuery bq = new BooleanQuery(true);
            bq.Add(new RegexpQuery(new Term("a", "[a-z][123]")), BooleanClause.Occur.SHOULD);
            bq.Add(new RegexpQuery(new Term("b", "[a-z][123]")), BooleanClause.Occur.SHOULD);
            assertEquals(bq, mfqp.Parse("/[a-z][123]/"));
        }
    }
}
