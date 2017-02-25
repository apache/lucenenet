using Lucene.Net.Documents;

namespace Lucene.Net.Search
{
    using NUnit.Framework;

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
    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using FieldInvertState = Lucene.Net.Index.FieldInvertState;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using Similarity = Lucene.Net.Search.Similarities.Similarity;
    using Store = Field.Store;
    using StringField = StringField;
    using Term = Lucene.Net.Index.Term;
    using TextField = TextField;

    [TestFixture]
    public class TestConjunctions : LuceneTestCase
    {
        internal Analyzer Analyzer;
        internal Directory Dir;
        internal IndexReader Reader;
        internal IndexSearcher Searcher;

        internal const string F1 = "title";
        internal const string F2 = "body";

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            Analyzer = new MockAnalyzer(Random());
            Dir = NewDirectory();
            IndexWriterConfig config = NewIndexWriterConfig(TEST_VERSION_CURRENT, Analyzer);
            config.SetMergePolicy(NewLogMergePolicy()); // we will use docids to validate
            RandomIndexWriter writer = new RandomIndexWriter(Random(), Dir, config);
            writer.AddDocument(Doc("lucene", "lucene is a very popular search engine library"));
            writer.AddDocument(Doc("solr", "solr is a very popular search server and is using lucene"));
            writer.AddDocument(Doc("nutch", "nutch is an internet search engine with web crawler and is using lucene and hadoop"));
            Reader = writer.Reader;
            writer.Dispose();
            Searcher = NewSearcher(Reader);
            Searcher.Similarity = new TFSimilarity();
        }

        internal static Document Doc(string v1, string v2)
        {
            Document doc = new Document();
            doc.Add(new StringField(F1, v1, Store.YES));
            doc.Add(new TextField(F2, v2, Store.YES));
            return doc;
        }

        [Test]
        public virtual void TestTermConjunctionsWithOmitTF()
        {
            BooleanQuery bq = new BooleanQuery();
            bq.Add(new TermQuery(new Term(F1, "nutch")), Occur.MUST);
            bq.Add(new TermQuery(new Term(F2, "is")), Occur.MUST);
            TopDocs td = Searcher.Search(bq, 3);
            Assert.AreEqual(1, td.TotalHits);
            Assert.AreEqual(3F, td.ScoreDocs[0].Score, 0.001F); // f1:nutch + f2:is + f2:is
        }

        [TearDown]
        public override void TearDown()
        {
            Reader.Dispose();
            Dir.Dispose();
            base.TearDown();
        }

        // Similarity that returns the TF as score
        private class TFSimilarity : Similarity
        {
            public override long ComputeNorm(FieldInvertState state)
            {
                return 1; // we dont care
            }

            public override SimWeight ComputeWeight(float queryBoost, CollectionStatistics collectionStats, params TermStatistics[] termStats)
            {
                return new SimWeightAnonymousInnerClassHelper(this);
            }

            private class SimWeightAnonymousInnerClassHelper : SimWeight
            {
                private readonly TFSimilarity OuterInstance;

                public SimWeightAnonymousInnerClassHelper(TFSimilarity outerInstance)
                {
                    this.OuterInstance = outerInstance;
                }

                public override float GetValueForNormalization()
                {
                    return 1; // we don't care
                }

                public override void Normalize(float queryNorm, float topLevelBoost)
                {
                    // we don't care
                }
            }

            public override SimScorer GetSimScorer(SimWeight weight, AtomicReaderContext context)
            {
                return new SimScorerAnonymousInnerClassHelper(this);
            }

            private class SimScorerAnonymousInnerClassHelper : SimScorer
            {
                private readonly TFSimilarity OuterInstance;

                public SimScorerAnonymousInnerClassHelper(TFSimilarity outerInstance)
                {
                    this.OuterInstance = outerInstance;
                }

                public override float Score(int doc, float freq)
                {
                    return freq;
                }

                public override float ComputeSlopFactor(int distance)
                {
                    return 1F;
                }

                public override float ComputePayloadFactor(int doc, int start, int end, BytesRef payload)
                {
                    return 1F;
                }
            }
        }
    }
}