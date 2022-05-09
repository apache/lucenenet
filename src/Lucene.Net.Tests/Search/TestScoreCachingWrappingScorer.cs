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
            private int idx = 0;
            private int doc = -1;

            public SimpleScorer(Weight weight)
                : base(weight)
            {
            }

            public override float GetScore()
            {
                // advance idx on purpose, so that consecutive calls to score will get
                // different results. this is to emulate computation of a score. If
                // ScoreCachingWrappingScorer is used, this should not be called more than
                // once per document.
                return idx == scores.Length ? float.NaN : scores[idx++];
            }

            public override int Freq => 1;

            public override int DocID => doc;

            public override int NextDoc()
            {
                return ++doc < scores.Length ? doc : NO_MORE_DOCS;
            }

            public override int Advance(int target)
            {
                doc = target;
                return doc < scores.Length ? doc : NO_MORE_DOCS;
            }

            public override long GetCost()
            {
                return scores.Length;
            }
        }

        private sealed class ScoreCachingCollector : ICollector
        {
            private int idx = 0;
            private Scorer scorer;
            public readonly float[] mscores;

            public ScoreCachingCollector(int numToCollect)
            {
                mscores = new float[numToCollect];
            }

            public void Collect(int doc)
            {
                // just a sanity check to avoid IOOB.
                if (idx == mscores.Length)
                {
                    return;
                }

                // just call score() a couple of times and record the score.
                mscores[idx] = scorer.GetScore();
                mscores[idx] = scorer.GetScore();
                mscores[idx] = scorer.GetScore();
                ++idx;
            }

            public void SetNextReader(AtomicReaderContext context)
            {
            }

            public void SetScorer(Scorer scorer)
            {
                this.scorer = new ScoreCachingWrappingScorer(scorer);
            }

            public bool AcceptsDocsOutOfOrder => true;
        }

        private static readonly float[] scores = new float[] { 0.7767749f, 1.7839992f, 8.9925785f, 7.9608946f, 0.07948637f, 2.6356435f, 7.4950366f, 7.1490803f, 8.108544f, 4.961808f, 2.2423935f, 7.285586f, 4.6699767f };

        [Test]
        public virtual void TestGetScores()
        {
            Directory directory = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, directory);
            writer.Commit();
            IndexReader ir = writer.GetReader();
            writer.Dispose();
            IndexSearcher searcher = NewSearcher(ir);
            Weight fake = (new TermQuery(new Term("fake", "weight"))).CreateWeight(searcher);
            Scorer s = new SimpleScorer(fake);
            ScoreCachingCollector scc = new ScoreCachingCollector(scores.Length);
            scc.SetScorer(s);

            // We need to iterate on the scorer so that its doc() advances.
            int doc;
            while ((doc = s.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
            {
                scc.Collect(doc);
            }

            for (int i = 0; i < scores.Length; i++)
            {
                Assert.AreEqual(scores[i], scc.mscores[i], 0f);
            }
            ir.Dispose();
            directory.Dispose();
        }
    }
}