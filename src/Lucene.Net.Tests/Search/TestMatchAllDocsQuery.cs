using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
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

    using Analyzer = Lucene.Net.Analysis.Analyzer;
    using Directory = Lucene.Net.Store.Directory;
    using DirectoryReader = Lucene.Net.Index.DirectoryReader;
    using Document = Documents.Document;
    using Field = Field;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using IndexWriter = Lucene.Net.Index.IndexWriter;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using Term = Lucene.Net.Index.Term;

    /// <summary>
    /// Tests MatchAllDocsQuery.
    ///
    /// </summary>
    [TestFixture]
    public class TestMatchAllDocsQuery : LuceneTestCase
    {
        private Analyzer analyzer;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            analyzer = new MockAnalyzer(Random);
        }

        [Test]
        public virtual void TestQuery()
        {
            Directory dir = NewDirectory();
            IndexWriter iw = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer).SetMaxBufferedDocs(2).SetMergePolicy(NewLogMergePolicy()));
            AddDoc("one", iw, 1f);
            AddDoc("two", iw, 20f);
            AddDoc("three four", iw, 300f);
            IndexReader ir = DirectoryReader.Open(iw, true);

            IndexSearcher @is = NewSearcher(ir);
            ScoreDoc[] hits;

            hits = @is.Search(new MatchAllDocsQuery(), null, 1000).ScoreDocs;
            Assert.AreEqual(3, hits.Length);
            Assert.AreEqual("one", @is.Doc(hits[0].Doc).Get("key"));
            Assert.AreEqual("two", @is.Doc(hits[1].Doc).Get("key"));
            Assert.AreEqual("three four", @is.Doc(hits[2].Doc).Get("key"));

            // some artificial queries to trigger the use of skipTo():

            BooleanQuery bq = new BooleanQuery();
            bq.Add(new MatchAllDocsQuery(), Occur.MUST);
            bq.Add(new MatchAllDocsQuery(), Occur.MUST);
            hits = @is.Search(bq, null, 1000).ScoreDocs;
            Assert.AreEqual(3, hits.Length);

            bq = new BooleanQuery();
            bq.Add(new MatchAllDocsQuery(), Occur.MUST);
            bq.Add(new TermQuery(new Term("key", "three")), Occur.MUST);
            hits = @is.Search(bq, null, 1000).ScoreDocs;
            Assert.AreEqual(1, hits.Length);

            iw.DeleteDocuments(new Term("key", "one"));
            ir.Dispose();
            ir = DirectoryReader.Open(iw, true);
            @is = NewSearcher(ir);

            hits = @is.Search(new MatchAllDocsQuery(), null, 1000).ScoreDocs;
            Assert.AreEqual(2, hits.Length);

            iw.Dispose();
            ir.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestEquals()
        {
            Query q1 = new MatchAllDocsQuery();
            Query q2 = new MatchAllDocsQuery();
            Assert.IsTrue(q1.Equals(q2));
            q1.Boost = 1.5f;
            Assert.IsFalse(q1.Equals(q2));
        }

        private void AddDoc(string text, IndexWriter iw, float boost)
        {
            Document doc = new Document();
            Field f = NewTextField("key", text, Field.Store.YES);
            f.Boost = boost;
            doc.Add(f);
            iw.AddDocument(doc);
        }
    }
}