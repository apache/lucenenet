using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using NUnit.Framework;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;
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

    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using DefaultSimilarity = Lucene.Net.Search.Similarities.DefaultSimilarity;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using SlowCompositeReaderWrapper = Lucene.Net.Index.SlowCompositeReaderWrapper;
    using Term = Lucene.Net.Index.Term;

    [TestFixture]
    public class TestTermScorer : LuceneTestCase
    {
        protected internal Directory directory;
        private const string FIELD = "field";

        protected internal string[] values = new string[] { "all", "dogs dogs", "like", "playing", "fetch", "all" };
        protected internal IndexSearcher indexSearcher;
        protected internal IndexReader indexReader;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            directory = NewDirectory();

            RandomIndexWriter writer = new RandomIndexWriter(Random, directory, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMergePolicy(NewLogMergePolicy()).SetSimilarity(new DefaultSimilarity()));
            for (int i = 0; i < values.Length; i++)
            {
                Document doc = new Document();
                doc.Add(NewTextField(FIELD, values[i], Field.Store.YES));
                writer.AddDocument(doc);
            }
            indexReader = SlowCompositeReaderWrapper.Wrap(writer.GetReader());
            writer.Dispose();
            indexSearcher = NewSearcher(indexReader);
            indexSearcher.Similarity = new DefaultSimilarity();
        }

        [TearDown]
        public override void TearDown()
        {
            indexReader.Dispose();
            directory.Dispose();
            base.TearDown();
        }

        [Test]
        public virtual void Test()
        {
            Term allTerm = new Term(FIELD, "all");
            TermQuery termQuery = new TermQuery(allTerm);

            Weight weight = indexSearcher.CreateNormalizedWeight(termQuery);
            Assert.IsTrue(indexSearcher.TopReaderContext is AtomicReaderContext);
            AtomicReaderContext context = (AtomicReaderContext)indexSearcher.TopReaderContext;
            BulkScorer ts = weight.GetBulkScorer(context, true, (context.AtomicReader).LiveDocs);
            // we have 2 documents with the term all in them, one document for all the
            // other values
            IList<TestHit> docs = new JCG.List<TestHit>();
            // must call next first

            ts.Score(new CollectorAnonymousClass(this, context, docs));
            Assert.IsTrue(docs.Count == 2, "docs Size: " + docs.Count + " is not: " + 2);
            TestHit doc0 = docs[0];
            TestHit doc5 = docs[1];
            // The scores should be the same
            Assert.IsTrue(doc0.Score == doc5.Score, doc0.Score + " does not equal: " + doc5.Score);
            /*
             * Score should be (based on Default Sim.: All floats are approximate tf = 1
             * numDocs = 6 docFreq(all) = 2 idf = ln(6/3) + 1 = 1.693147 idf ^ 2 =
             * 2.8667 boost = 1 lengthNorm = 1 //there is 1 term in every document coord
             * = 1 sumOfSquaredWeights = (idf * boost) ^ 2 = 1.693147 ^ 2 = 2.8667
             * queryNorm = 1 / (sumOfSquaredWeights)^0.5 = 1 /(1.693147) = 0.590
             *
             * score = 1 * 2.8667 * 1 * 1 * 0.590 = 1.69
             */
            Assert.IsTrue(doc0.Score == 1.6931472f, doc0.Score + " does not equal: " + 1.6931472f);
        }

        private sealed class CollectorAnonymousClass : ICollector
        {
            private readonly TestTermScorer outerInstance;

            private AtomicReaderContext context;
            private readonly IList<TestHit> docs;

            public CollectorAnonymousClass(TestTermScorer outerInstance, AtomicReaderContext context, IList<TestHit> docs)
            {
                this.outerInstance = outerInstance;
                this.context = context;
                this.docs = docs;
                @base = 0;
            }

            private int @base;
            private Scorer scorer;

            public void SetScorer(Scorer scorer)
            {
                this.scorer = scorer;
            }

            public void Collect(int doc)
            {
                float score = scorer.GetScore();
                doc = doc + @base;
                docs.Add(new TestHit(outerInstance, doc, score));
                Assert.IsTrue(score > 0, "score " + score + " is not greater than 0");
                Assert.IsTrue(doc == 0 || doc == 5, "Doc: " + doc + " does not equal 0 or doc does not equal 5");
            }

            public void SetNextReader(AtomicReaderContext context)
            {
                @base = context.DocBase;
            }

            public bool AcceptsDocsOutOfOrder => true;
        }

        [Test]
        public virtual void TestNext()
        {
            Term allTerm = new Term(FIELD, "all");
            TermQuery termQuery = new TermQuery(allTerm);

            Weight weight = indexSearcher.CreateNormalizedWeight(termQuery);
            Assert.IsTrue(indexSearcher.TopReaderContext is AtomicReaderContext);
            AtomicReaderContext context = (AtomicReaderContext)indexSearcher.TopReaderContext;
            Scorer ts = weight.GetScorer(context, (context.AtomicReader).LiveDocs);
            Assert.IsTrue(ts.NextDoc() != DocIdSetIterator.NO_MORE_DOCS, "next did not return a doc");
            Assert.IsTrue(ts.GetScore() == 1.6931472f, "score is not correct");
            Assert.IsTrue(ts.NextDoc() != DocIdSetIterator.NO_MORE_DOCS, "next did not return a doc");
            Assert.IsTrue(ts.GetScore() == 1.6931472f, "score is not correct");
            Assert.IsTrue(ts.NextDoc() == DocIdSetIterator.NO_MORE_DOCS, "next returned a doc and it should not have");
        }

        [Test]
        public virtual void TestAdvance()
        {
            Term allTerm = new Term(FIELD, "all");
            TermQuery termQuery = new TermQuery(allTerm);

            Weight weight = indexSearcher.CreateNormalizedWeight(termQuery);
            Assert.IsTrue(indexSearcher.TopReaderContext is AtomicReaderContext);
            AtomicReaderContext context = (AtomicReaderContext)indexSearcher.TopReaderContext;
            Scorer ts = weight.GetScorer(context, (context.AtomicReader).LiveDocs);
            Assert.IsTrue(ts.Advance(3) != DocIdSetIterator.NO_MORE_DOCS, "Didn't skip");
            // The next doc should be doc 5
            Assert.IsTrue(ts.DocID == 5, "doc should be number 5");
        }

        private class TestHit
        {
            private readonly TestTermScorer outerInstance;

            public int Doc { get; }
            public float Score { get; }

            public TestHit(TestTermScorer outerInstance, int doc, float score)
            {
                this.outerInstance = outerInstance;
                this.Doc = doc;
                this.Score = score;
            }

            public override string ToString()
            {
                return "TestHit{" + "doc=" + Doc + ", score=" + Score + "}";
            }
        }
    }
}