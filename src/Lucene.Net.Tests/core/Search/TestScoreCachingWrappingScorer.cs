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

    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using Directory = Lucene.Net.Store.Directory;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using Term = Lucene.Net.Index.Term;

    [TestFixture]
    public class TestScoreCachingWrappingScorer : LuceneTestCase
    {
        private sealed class SimpleScorer : Scorer
        {
            internal int Idx = 0;
            internal int Doc = -1;

            public SimpleScorer(Weight weight)
                : base(weight)
            {
            }

            public override float Score()
            {
                // advance idx on purpose, so that consecutive calls to score will get
                // different results. this is to emulate computation of a score. If
                // ScoreCachingWrappingScorer is used, this should not be called more than
                // once per document.
                return Idx == Scores.Length ? float.NaN : Scores[Idx++];
            }

            public override int Freq
            {
                get { return 1; }
            }

            public override int DocID()
            {
                return Doc;
            }

            public override int NextDoc()
            {
                return ++Doc < Scores.Length ? Doc : NO_MORE_DOCS;
            }

            public override int Advance(int target)
            {
                Doc = target;
                return Doc < Scores.Length ? Doc : NO_MORE_DOCS;
            }

            public override long Cost()
            {
                return Scores.Length;
            }
        }

        private sealed class ScoreCachingCollector : Collector
        {
            internal int Idx = 0;
            internal Scorer Scorer_Renamed;
            internal float[] Mscores;

            public ScoreCachingCollector(int numToCollect)
            {
                Mscores = new float[numToCollect];
            }

            public override void Collect(int doc)
            {
                // just a sanity check to avoid IOOB.
                if (Idx == Mscores.Length)
                {
                    return;
                }

                // just call score() a couple of times and record the score.
                Mscores[Idx] = Scorer_Renamed.Score();
                Mscores[Idx] = Scorer_Renamed.Score();
                Mscores[Idx] = Scorer_Renamed.Score();
                ++Idx;
            }

            public override void SetNextReader(AtomicReaderContext context)
            {
            }

            public override void SetScorer(Scorer scorer)
            {
                this.Scorer_Renamed = new ScoreCachingWrappingScorer(scorer);
            }

            public override bool AcceptsDocsOutOfOrder
            {
                get { return true; }
            }
        }

        private static readonly float[] Scores = new float[] { 0.7767749f, 1.7839992f, 8.9925785f, 7.9608946f, 0.07948637f, 2.6356435f, 7.4950366f, 7.1490803f, 8.108544f, 4.961808f, 2.2423935f, 7.285586f, 4.6699767f };

        [Test]
        public virtual void TestGetScores()
        {
            Directory directory = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), directory, Similarity, TimeZone);
            writer.Commit();
            IndexReader ir = writer.Reader;
            writer.Dispose();
            IndexSearcher searcher = NewSearcher(ir);
            Weight fake = (new TermQuery(new Term("fake", "weight"))).CreateWeight(searcher);
            Scorer s = new SimpleScorer(fake);
            ScoreCachingCollector scc = new ScoreCachingCollector(Scores.Length);
            scc.SetScorer(s);

            // We need to iterate on the scorer so that its doc() advances.
            int doc;
            while ((doc = s.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
            {
                scc.Collect(doc);
            }

            for (int i = 0; i < Scores.Length; i++)
            {
                Assert.AreEqual(Scores[i], scc.Mscores[i], 0f);
            }
            ir.Dispose();
            directory.Dispose();
        }
    }
}