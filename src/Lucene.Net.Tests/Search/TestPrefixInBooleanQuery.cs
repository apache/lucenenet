using Lucene.Net.Documents;
using NUnit.Framework;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Search
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

    using Directory = Lucene.Net.Store.Directory;
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
        private static Directory directory;
        private static IndexReader reader;
        private static IndexSearcher searcher;

        /// <summary>
        /// LUCENENET specific
        /// Is non-static because Similarity and TimeZone are not static.
        /// </summary>
        [OneTimeSetUp]
        public override void BeforeClass()
        {
            base.BeforeClass();

            directory = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, directory);

            Document doc = new Document();
            Field field = NewStringField(FIELD, "meaninglessnames", Field.Store.NO);
            doc.Add(field);

            for (int i = 0; i < 5137; ++i)
            {
                writer.AddDocument(doc);
            }

            field.SetStringValue("tangfulin");
            writer.AddDocument(doc);

            field.SetStringValue("meaninglessnames");
            for (int i = 5138; i < 11377; ++i)
            {
                writer.AddDocument(doc);
            }

            field.SetStringValue("tangfulin");
            writer.AddDocument(doc);

            reader = writer.GetReader();
            searcher = NewSearcher(reader);
            writer.Dispose();
        }

        [OneTimeTearDown]
        public override void AfterClass()
        {
            searcher = null;
            reader.Dispose();
            reader = null;
            directory.Dispose();
            directory = null;

            base.AfterClass();
        }

        [Test]
        public virtual void TestPrefixQuery()
        {
            Query query = new PrefixQuery(new Term(FIELD, "tang"));
            Assert.AreEqual(2, searcher.Search(query, null, 1000).TotalHits, "Number of matched documents");
        }

        [Test]
        public virtual void TestTermQuery()
        {
            Query query = new TermQuery(new Term(FIELD, "tangfulin"));
            Assert.AreEqual(2, searcher.Search(query, null, 1000).TotalHits, "Number of matched documents");
        }

        [Test]
        public virtual void TestTermBooleanQuery()
        {
            BooleanQuery query = new BooleanQuery();
            query.Add(new TermQuery(new Term(FIELD, "tangfulin")), Occur.SHOULD);
            query.Add(new TermQuery(new Term(FIELD, "notexistnames")), Occur.SHOULD);
            Assert.AreEqual(2, searcher.Search(query, null, 1000).TotalHits, "Number of matched documents");
        }

        [Test]
        public virtual void TestPrefixBooleanQuery()
        {
            BooleanQuery query = new BooleanQuery();
            query.Add(new PrefixQuery(new Term(FIELD, "tang")), Occur.SHOULD);
            query.Add(new TermQuery(new Term(FIELD, "notexistnames")), Occur.SHOULD);
            Assert.AreEqual(2, searcher.Search(query, null, 1000).TotalHits, "Number of matched documents");
        }
    }
}