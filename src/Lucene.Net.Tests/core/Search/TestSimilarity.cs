using Lucene.Net.Documents;

namespace Lucene.Net.Search
{
    using NUnit.Framework;
    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using DefaultSimilarity = Lucene.Net.Search.Similarities.DefaultSimilarity;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;

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

    using Field = Field;
    using FieldInvertState = Lucene.Net.Index.FieldInvertState;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using Term = Lucene.Net.Index.Term;

    /// <summary>
    /// Similarity unit test.
    ///
    ///
    /// </summary>
    [TestFixture]
    public class TestSimilarity : LuceneTestCase
    {
        public class SimpleSimilarity : DefaultSimilarity
        {
            public override float QueryNorm(float sumOfSquaredWeights)
            {
                return 1.0f;
            }

            public override float Coord(int overlap, int maxOverlap)
            {
                return 1.0f;
            }

            public override float LengthNorm(FieldInvertState state)
            {
                return state.Boost;
            }

            public override float Tf(float freq)
            {
                return freq;
            }

            public override float SloppyFreq(int distance)
            {
                return 2.0f;
            }

            public override float Idf(long docFreq, long numDocs)
            {
                return 1.0f;
            }

            public override Explanation IdfExplain(CollectionStatistics collectionStats, TermStatistics[] stats)
            {
                return new Explanation(1.0f, "Inexplicable");
            }
        }

        [Test]
        public virtual void TestSimilarity_Mem()
        {
            Directory store = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), store, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetSimilarity(new SimpleSimilarity()));

            Document d1 = new Document();
            d1.Add(NewTextField("field", "a c", Field.Store.YES));

            Document d2 = new Document();
            d2.Add(NewTextField("field", "a b c", Field.Store.YES));

            writer.AddDocument(d1);
            writer.AddDocument(d2);
            IndexReader reader = writer.Reader;
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(reader);
            searcher.Similarity = new SimpleSimilarity();

            Term a = new Term("field", "a");
            Term b = new Term("field", "b");
            Term c = new Term("field", "c");

            searcher.Search(new TermQuery(b), new CollectorAnonymousInnerClassHelper(this));

            BooleanQuery bq = new BooleanQuery();
            bq.Add(new TermQuery(a), Occur.SHOULD);
            bq.Add(new TermQuery(b), Occur.SHOULD);
            //System.out.println(bq.toString("field"));
            searcher.Search(bq, new CollectorAnonymousInnerClassHelper2(this));

            PhraseQuery pq = new PhraseQuery();
            pq.Add(a);
            pq.Add(c);
            //System.out.println(pq.toString("field"));
            searcher.Search(pq, new CollectorAnonymousInnerClassHelper3(this));

            pq.Slop = 2;
            //System.out.println(pq.toString("field"));
            searcher.Search(pq, new CollectorAnonymousInnerClassHelper4(this));

            reader.Dispose();
            store.Dispose();
        }

        private class CollectorAnonymousInnerClassHelper : Collector
        {
            private readonly TestSimilarity OuterInstance;

            public CollectorAnonymousInnerClassHelper(TestSimilarity outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            private Scorer scorer;

            public override void SetScorer(Scorer scorer)
            {
                this.scorer = scorer;
            }

            public override void Collect(int doc)
            {
                Assert.AreEqual(1.0f, scorer.Score(), 0);
            }

            public override AtomicReaderContext NextReader
            {
                set
                {
                }
            }

            public override bool AcceptsDocsOutOfOrder()
            {
                return true;
            }
        }

        private class CollectorAnonymousInnerClassHelper2 : Collector
        {
            private readonly TestSimilarity OuterInstance;

            public CollectorAnonymousInnerClassHelper2(TestSimilarity outerInstance)
            {
                this.OuterInstance = outerInstance;
                @base = 0;
            }

            private int @base;
            private Scorer scorer;

            public override void SetScorer(Scorer scorer)
            {
                this.scorer = scorer;
            }

            public override void Collect(int doc)
            {
                //System.out.println("Doc=" + doc + " score=" + score);
                Assert.AreEqual((float)doc + @base + 1, scorer.Score(), 0);
            }

            public override AtomicReaderContext NextReader
            {
                set
                {
                    @base = value.DocBase;
                }
            }

            public override bool AcceptsDocsOutOfOrder()
            {
                return true;
            }
        }

        private class CollectorAnonymousInnerClassHelper3 : Collector
        {
            private readonly TestSimilarity OuterInstance;

            public CollectorAnonymousInnerClassHelper3(TestSimilarity outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            private Scorer scorer;

            public override void SetScorer(Scorer scorer)
            {
                this.scorer = scorer;
            }

            public override void Collect(int doc)
            {
                //System.out.println("Doc=" + doc + " score=" + score);
                Assert.AreEqual(1.0f, scorer.Score(), 0);
            }

            public override AtomicReaderContext NextReader
            {
                set
                {
                }
            }

            public override bool AcceptsDocsOutOfOrder()
            {
                return true;
            }
        }

        private class CollectorAnonymousInnerClassHelper4 : Collector
        {
            private readonly TestSimilarity OuterInstance;

            public CollectorAnonymousInnerClassHelper4(TestSimilarity outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            private Scorer scorer;

            public override void SetScorer(Scorer scorer)
            {
                this.scorer = scorer;
            }

            public override void Collect(int doc)
            {
                //System.out.println("Doc=" + doc + " score=" + score);
                Assert.AreEqual(2.0f, scorer.Score(), 0);
            }

            public override AtomicReaderContext NextReader
            {
                set
                {
                }
            }

            public override bool AcceptsDocsOutOfOrder()
            {
                return true;
            }
        }
    }
}