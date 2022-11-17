using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Flexible.Standard.Config;
using Lucene.Net.Search;
using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;

namespace Lucene.Net.QueryParsers.Flexible.Standard
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
    /// This test case is a copy of the core Lucene query parser test, it was adapted
    /// to use new QueryParserHelper instead of the old query parser.
    /// 
    /// Tests QueryParser.
    /// </summary>
    public class TestMultiFieldQPHelper : LuceneTestCase
    {
        /**
        * test stop words parsing for both the non static form, and for the
        * corresponding static form (qtxt, fields[]).
        */
        [Test]
        public void TestStopwordsParsing()
        {
            assertStopQueryEquals("one", "b:one t:one");
            assertStopQueryEquals("one stop", "b:one t:one");
            assertStopQueryEquals("one (stop)", "b:one t:one");
            assertStopQueryEquals("one ((stop))", "b:one t:one");
            assertStopQueryEquals("stop", "");
            assertStopQueryEquals("(stop)", "");
            assertStopQueryEquals("((stop))", "");
        }

        // verify parsing of query using a stopping analyzer
        private void assertStopQueryEquals(String qtxt, String expectedRes)
        {
            String[]
            fields = { "b", "t" };
            Occur[] occur = { Occur.SHOULD, Occur.SHOULD };
            TestQPHelper.QPTestAnalyzer a = new TestQPHelper.QPTestAnalyzer();
            StandardQueryParser mfqp = new StandardQueryParser();
            mfqp.SetMultiFields(fields);
            mfqp.Analyzer = (a);

            Query q = mfqp.Parse(qtxt, null);
            assertEquals(expectedRes, q.toString());

            q = QueryParserUtil.Parse(qtxt, fields, occur, a);
            assertEquals(expectedRes, q.toString());
        }

        [Test]
        public void TestSimple()
        {
            String[]
            fields = { "b", "t" };
            StandardQueryParser mfqp = new StandardQueryParser();
            mfqp.SetMultiFields(fields);
            mfqp.Analyzer = (new MockAnalyzer(Random));

            Query q = mfqp.Parse("one", null);
            assertEquals("b:one t:one", q.toString());

            q = mfqp.Parse("one two", null);
            assertEquals("(b:one t:one) (b:two t:two)", q.toString());

            q = mfqp.Parse("+one +two", null);
            assertEquals("+(b:one t:one) +(b:two t:two)", q.toString());

            q = mfqp.Parse("+one -two -three", null);
            assertEquals("+(b:one t:one) -(b:two t:two) -(b:three t:three)", q
                .toString());

            q = mfqp.Parse("one^2 two", null);
            assertEquals("((b:one t:one)^2.0) (b:two t:two)", q.toString());

            q = mfqp.Parse("one~ two", null);
            assertEquals("(b:one~2 t:one~2) (b:two t:two)", q.toString());

            q = mfqp.Parse("one~0.8 two^2", null);
            assertEquals("(b:one~0 t:one~0) ((b:two t:two)^2.0)", q.toString());

            q = mfqp.Parse("one* two*", null);
            assertEquals("(b:one* t:one*) (b:two* t:two*)", q.toString());

            q = mfqp.Parse("[a TO c] two", null);
            assertEquals("(b:[a TO c] t:[a TO c]) (b:two t:two)", q.toString());

            q = mfqp.Parse("w?ldcard", null);
            assertEquals("b:w?ldcard t:w?ldcard", q.toString());

            q = mfqp.Parse("\"foo bar\"", null);
            assertEquals("b:\"foo bar\" t:\"foo bar\"", q.toString());

            q = mfqp.Parse("\"aa bb cc\" \"dd ee\"", null);
            assertEquals("(b:\"aa bb cc\" t:\"aa bb cc\") (b:\"dd ee\" t:\"dd ee\")", q
                .toString());

            q = mfqp.Parse("\"foo bar\"~4", null);
            assertEquals("b:\"foo bar\"~4 t:\"foo bar\"~4", q.toString());

            // LUCENE-1213: QueryParser was ignoring slop when phrase
            // had a field.
            q = mfqp.Parse("b:\"foo bar\"~4", null);
            assertEquals("b:\"foo bar\"~4", q.toString());

            // make sure that terms which have a field are not touched:
            q = mfqp.Parse("one f:two", null);
            assertEquals("(b:one t:one) f:two", q.toString());

            // AND mode:
            mfqp.DefaultOperator = (StandardQueryConfigHandler.Operator.AND);
            q = mfqp.Parse("one two", null);
            assertEquals("+(b:one t:one) +(b:two t:two)", q.toString());
            q = mfqp.Parse("\"aa bb cc\" \"dd ee\"", null);
            assertEquals("+(b:\"aa bb cc\" t:\"aa bb cc\") +(b:\"dd ee\" t:\"dd ee\")",
                q.toString());

        }

        [Test]
        public void TestBoostsSimple()
        {
            IDictionary<String, float> boosts = new Dictionary<String, float>();
            boosts["b"] = 5;
            boosts["t"] = 10;
            String[] fields = { "b", "t" };
            StandardQueryParser mfqp = new StandardQueryParser();
            mfqp.SetMultiFields(fields);
            mfqp.FieldsBoost = (boosts);
            mfqp.Analyzer = (new MockAnalyzer(Random));

            // Check for simple
            Query q = mfqp.Parse("one", null);
            assertEquals("b:one^5.0 t:one^10.0", q.toString());

            // Check for AND
            q = mfqp.Parse("one AND two", null);
            assertEquals("+(b:one^5.0 t:one^10.0) +(b:two^5.0 t:two^10.0)", q
                .toString());

            // Check for OR
            q = mfqp.Parse("one OR two", null);
            assertEquals("(b:one^5.0 t:one^10.0) (b:two^5.0 t:two^10.0)", q.toString());

            // Check for AND and a field
            q = mfqp.Parse("one AND two AND foo:test", null);
            assertEquals("+(b:one^5.0 t:one^10.0) +(b:two^5.0 t:two^10.0) +foo:test", q
                .toString());

            q = mfqp.Parse("one^3 AND two^4", null);
            assertEquals("+((b:one^5.0 t:one^10.0)^3.0) +((b:two^5.0 t:two^10.0)^4.0)",
                q.toString());
        }

        [Test]
        public void TestStaticMethod1()
        {
            String[]
            fields = { "b", "t" };
            String[]
            queries = { "one", "two" };
            Query q = QueryParserUtil.Parse(queries, fields, new MockAnalyzer(Random));
            assertEquals("b:one t:two", q.toString());

            String[] queries2 = { "+one", "+two" };
            q = QueryParserUtil.Parse(queries2, fields, new MockAnalyzer(Random));
            assertEquals("b:one t:two", q.toString());

            String[] queries3 = { "one", "+two" };
            q = QueryParserUtil.Parse(queries3, fields, new MockAnalyzer(Random));
            assertEquals("b:one t:two", q.toString());

            String[] queries4 = { "one +more", "+two" };
            q = QueryParserUtil.Parse(queries4, fields, new MockAnalyzer(Random));
            assertEquals("(b:one +b:more) t:two", q.toString());

            String[] queries5 = { "blah" };
            try
            {
                q = QueryParserUtil.Parse(queries5, fields, new MockAnalyzer(Random));
                fail();
            }
            catch (Exception e) when (e.IsIllegalArgumentException())
            {
                // expected exception, array length differs
            }

            // check also with stop words for this static form (qtxts[], fields[]).
            TestQPHelper.QPTestAnalyzer stopA = new TestQPHelper.QPTestAnalyzer();

            String[] queries6 = { "((+stop))", "+((stop))" };
            q = QueryParserUtil.Parse(queries6, fields, stopA);
            assertEquals("", q.toString());

            String[] queries7 = { "one ((+stop)) +more", "+((stop)) +two" };
            q = QueryParserUtil.Parse(queries7, fields, stopA);
            assertEquals("(b:one +b:more) (+t:two)", q.toString());

        }

        [Test]
        public void TestStaticMethod2()
        {
            String[]
            fields = { "b", "t" };
            Occur[]
            flags = {
                Occur.MUST,
                Occur.MUST_NOT };
            Query q = QueryParserUtil.Parse("one", fields, flags,
                new MockAnalyzer(Random));
            assertEquals("+b:one -t:one", q.toString());

            q = QueryParserUtil.Parse("one two", fields, flags, new MockAnalyzer(Random));
            assertEquals("+(b:one b:two) -(t:one t:two)", q.toString());

            try
            {
                Occur[] flags2 = { Occur.MUST };
                q = QueryParserUtil.Parse("blah", fields, flags2, new MockAnalyzer(Random));
                fail();
            }
            catch (Exception e) when (e.IsIllegalArgumentException())
            {
                // expected exception, array length differs
            }
        }

        [Test]
        public void TestStaticMethod2Old()
        {
            String[]
            fields = { "b", "t" };
            Occur[]
            flags = {
        Occur.MUST,
        Occur.MUST_NOT };
            StandardQueryParser parser = new StandardQueryParser();
            parser.SetMultiFields(fields);
            parser.Analyzer = (new MockAnalyzer(Random));

            Query q = QueryParserUtil.Parse("one", fields, flags,
                new MockAnalyzer(Random));// , fields, flags, new
                                            // MockAnalyzer());
            assertEquals("+b:one -t:one", q.toString());

            q = QueryParserUtil.Parse("one two", fields, flags, new MockAnalyzer(Random));
            assertEquals("+(b:one b:two) -(t:one t:two)", q.toString());

            try
            {
                Occur[] flags2 = { Occur.MUST };
                q = QueryParserUtil.Parse("blah", fields, flags2, new MockAnalyzer(Random));
                fail();
            }
            catch (Exception e) when (e.IsIllegalArgumentException())
            {
                // expected exception, array length differs
            }
        }

        [Test]
        public void TestStaticMethod3()
        {
            String[]
            queries = { "one", "two", "three" };
            String[]
            fields = { "f1", "f2", "f3" };
            Occur[]
            flags = {
                Occur.MUST,
                Occur.MUST_NOT, Occur.SHOULD };
            Query q = QueryParserUtil.Parse(queries, fields, flags,
                new MockAnalyzer(Random));
            assertEquals("+f1:one -f2:two f3:three", q.toString());

            try
            {
                Occur[] flags2 = { Occur.MUST };
                q = QueryParserUtil
                    .Parse(queries, fields, flags2, new MockAnalyzer(Random));
                fail();
            }
            catch (Exception e) when (e.IsIllegalArgumentException())
            {
                // expected exception, array length differs
            }
        }

        [Test]
        public void TestStaticMethod3Old()
        {
            String[]
            queries = { "one", "two" };
            String[]
            fields = { "b", "t" };
            Occur[]
            flags = {
                Occur.MUST,
                Occur.MUST_NOT };
            Query q = QueryParserUtil.Parse(queries, fields, flags,
                new MockAnalyzer(Random));
            assertEquals("+b:one -t:two", q.toString());

            try
            {
                Occur[] flags2 = { Occur.MUST };
                q = QueryParserUtil
                    .Parse(queries, fields, flags2, new MockAnalyzer(Random));
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
            String[]
            fields = new String[] { "f1", "f2", "f3" };
            StandardQueryParser parser = new StandardQueryParser();
            parser.SetMultiFields(fields);
            parser.Analyzer = (new AnalyzerReturningNull());

            Query q = parser.Parse("bla AND blo", null);
            assertEquals("+(f2:bla f3:bla) +(f2:blo f3:blo)", q.toString());
            // the following queries are not affected as their terms are not
            // analyzed anyway:
            q = parser.Parse("bla*", null);
            assertEquals("f1:bla* f2:bla* f3:bla*", q.toString());
            q = parser.Parse("bla~", null);
            assertEquals("f1:bla~2 f2:bla~2 f3:bla~2", q.toString());
            q = parser.Parse("[a TO c]", null);
            assertEquals("f1:[a TO c] f2:[a TO c] f3:[a TO c]", q.toString());
        }

        [Test]
        public void TestStopWordSearching()
        {
            Analyzer analyzer = new MockAnalyzer(Random);
            Store.Directory ramDir = NewDirectory();
            IndexWriter iw = new IndexWriter(ramDir, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer));
            Document doc = new Document();
            doc.Add(NewTextField("body", "blah the footest blah", Field.Store.NO));
            iw.AddDocument(doc);
            iw.Dispose();

            StandardQueryParser mfqp = new StandardQueryParser();

            mfqp.SetMultiFields(new String[] { "body" });
            mfqp.Analyzer = (analyzer);
            mfqp.DefaultOperator = (StandardQueryConfigHandler.Operator.AND);
            Query q = mfqp.Parse("the footest", null);
            IndexReader ir = DirectoryReader.Open(ramDir);
            IndexSearcher @is = NewSearcher(ir);
            ScoreDoc[] hits = @is.Search(q, null, 1000).ScoreDocs;
            assertEquals(1, hits.Length);
            ir.Dispose();
            ramDir.Dispose();
        }

        /**
         * Return no tokens for field "f1".
         */
        private class AnalyzerReturningNull : Analyzer
        {
            MockAnalyzer stdAnalyzer = new MockAnalyzer(Random);

            public AnalyzerReturningNull()
                        : base(PER_FIELD_REUSE_STRATEGY)
            {
            }


            protected internal override TextReader InitReader(String fieldName, TextReader reader)
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


            protected internal override TokenStreamComponents CreateComponents(String fieldName, TextReader reader)
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
    }
}
