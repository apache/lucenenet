using Lucene.Net.Documents;

namespace Lucene.Net.Search
{
    using NUnit.Framework;
    using Directory = Lucene.Net.Store.Directory;

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

    using Document = Documents.Document;
    using Field = Field;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using Term = Lucene.Net.Index.Term;

    /// <summary>
    /// https://issues.apache.org/jira/browse/LUCENE-1974
    ///
    /// represent the bug of
    ///
    ///    BooleanScorer.Score(Collector collector, int max, int firstDocID)
    ///
    /// Line 273, end=8192, subScorerDocID=11378, then more got false?
    /// </summary>
    [TestFixture]
    public class TestPrefixInBooleanQuery : LuceneTestCase
    {
        private const string FIELD = "name";
        private static Directory Directory;
        private static IndexReader Reader;
        private static IndexSearcher Searcher;

        /// <summary>
        /// LUCENENET specific
        /// Is non-static because Similarity and TimeZone are not static.
        /// </summary>
        [OneTimeSetUp]
        public void BeforeClass()
        {
            Directory = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), Directory, Similarity, TimeZone);

            Document doc = new Document();
            Field field = NewStringField(FIELD, "meaninglessnames", Field.Store.NO);
            doc.Add(field);

            for (int i = 0; i < 5137; ++i)
            {
                writer.AddDocument(doc);
            }

            field.StringValue = "tangfulin";
            writer.AddDocument(doc);

            field.StringValue = "meaninglessnames";
            for (int i = 5138; i < 11377; ++i)
            {
                writer.AddDocument(doc);
            }

            field.StringValue = "tangfulin";
            writer.AddDocument(doc);

            Reader = writer.Reader;
            Searcher = NewSearcher(Reader);
            writer.Dispose();
        }

        [OneTimeTearDown]
        public static void AfterClass()
        {
            Searcher = null;
            Reader.Dispose();
            Reader = null;
            Directory.Dispose();
            Directory = null;
        }

        [Test]
        public virtual void TestPrefixQuery()
        {
            Query query = new PrefixQuery(new Term(FIELD, "tang"));
            Assert.AreEqual(2, Searcher.Search(query, null, 1000).TotalHits, "Number of matched documents");
        }

        [Test]
        public virtual void TestTermQuery()
        {
            Query query = new TermQuery(new Term(FIELD, "tangfulin"));
            Assert.AreEqual(2, Searcher.Search(query, null, 1000).TotalHits, "Number of matched documents");
        }

        [Test]
        public virtual void TestTermBooleanQuery()
        {
            BooleanQuery query = new BooleanQuery();
            query.Add(new TermQuery(new Term(FIELD, "tangfulin")), BooleanClause.Occur.SHOULD);
            query.Add(new TermQuery(new Term(FIELD, "notexistnames")), BooleanClause.Occur.SHOULD);
            Assert.AreEqual(2, Searcher.Search(query, null, 1000).TotalHits, "Number of matched documents");
        }

        [Test]
        public virtual void TestPrefixBooleanQuery()
        {
            BooleanQuery query = new BooleanQuery();
            query.Add(new PrefixQuery(new Term(FIELD, "tang")), BooleanClause.Occur.SHOULD);
            query.Add(new TermQuery(new Term(FIELD, "notexistnames")), BooleanClause.Occur.SHOULD);
            Assert.AreEqual(2, Searcher.Search(query, null, 1000).TotalHits, "Number of matched documents");
        }
    }
}