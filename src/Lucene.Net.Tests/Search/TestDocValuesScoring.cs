using Lucene.Net.Documents;
using NUnit.Framework;
using System;
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
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using FieldInvertState = Lucene.Net.Index.FieldInvertState;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using PerFieldSimilarityWrapper = Lucene.Net.Search.Similarities.PerFieldSimilarityWrapper;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using Similarity = Lucene.Net.Search.Similarities.Similarity;
    using SingleDocValuesField = SingleDocValuesField;
    using Term = Lucene.Net.Index.Term;

    /// <summary>
    /// Tests the use of indexdocvalues in scoring.
    ///
    /// In the example, a docvalues field is used as a per-document boost (separate from the norm)
    /// @lucene.experimental
    /// </summary>
    [SuppressCodecs("Lucene3x")]
    [TestFixture]
    public class TestDocValuesScoring : LuceneTestCase
    {
        private const float SCORE_EPSILON = 0.001f; // for comparing floats

        [Test]
        public virtual void TestSimple()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            Field field = NewTextField("foo", "", Field.Store.NO);
            doc.Add(field);
            Field dvField = new SingleDocValuesField("foo_boost", 0.0F);
            doc.Add(dvField);
            Field field2 = NewTextField("bar", "", Field.Store.NO);
            doc.Add(field2);

            field.SetStringValue("quick brown fox");
            field2.SetStringValue("quick brown fox");
            dvField.SetSingleValue(2f); // boost x2
            iw.AddDocument(doc);
            field.SetStringValue("jumps over lazy brown dog");
            field2.SetStringValue("jumps over lazy brown dog");
            dvField.SetSingleValue(4f); // boost x4
            iw.AddDocument(doc);
            IndexReader ir = iw.GetReader();
            iw.Dispose();

            // no boosting
            IndexSearcher searcher1 = NewSearcher(ir, false);
            Similarity @base = searcher1.Similarity;
            // boosting
            IndexSearcher searcher2 = NewSearcher(ir, false);
            searcher2.Similarity = new PerFieldSimilarityWrapperAnonymousClass(this, field, @base);

            // in this case, we searched on field "foo". first document should have 2x the score.
            TermQuery tq = new TermQuery(new Term("foo", "quick"));
            QueryUtils.Check(Random, tq, searcher1);
            QueryUtils.Check(Random, tq, searcher2);

            TopDocs noboost = searcher1.Search(tq, 10);
            TopDocs boost = searcher2.Search(tq, 10);
            Assert.AreEqual(1, noboost.TotalHits);
            Assert.AreEqual(1, boost.TotalHits);

            //System.out.println(searcher2.Explain(tq, boost.ScoreDocs[0].Doc));
            Assert.AreEqual(boost.ScoreDocs[0].Score, noboost.ScoreDocs[0].Score * 2f, SCORE_EPSILON);

            // this query matches only the second document, which should have 4x the score.
            tq = new TermQuery(new Term("foo", "jumps"));
            QueryUtils.Check(Random, tq, searcher1);
            QueryUtils.Check(Random, tq, searcher2);

            noboost = searcher1.Search(tq, 10);
            boost = searcher2.Search(tq, 10);
            Assert.AreEqual(1, noboost.TotalHits);
            Assert.AreEqual(1, boost.TotalHits);

            Assert.AreEqual(boost.ScoreDocs[0].Score, noboost.ScoreDocs[0].Score * 4f, SCORE_EPSILON);

            // search on on field bar just for kicks, nothing should happen, since we setup
            // our sim provider to only use foo_boost for field foo.
            tq = new TermQuery(new Term("bar", "quick"));
            QueryUtils.Check(Random, tq, searcher1);
            QueryUtils.Check(Random, tq, searcher2);

            noboost = searcher1.Search(tq, 10);
            boost = searcher2.Search(tq, 10);
            Assert.AreEqual(1, noboost.TotalHits);
            Assert.AreEqual(1, boost.TotalHits);

            Assert.AreEqual(boost.ScoreDocs[0].Score, noboost.ScoreDocs[0].Score, SCORE_EPSILON);

            ir.Dispose();
            dir.Dispose();
        }

        private sealed class PerFieldSimilarityWrapperAnonymousClass : PerFieldSimilarityWrapper
        {
            private readonly TestDocValuesScoring outerInstance;

            private Field field;
            private Similarity @base;

            public PerFieldSimilarityWrapperAnonymousClass(TestDocValuesScoring outerInstance, Field field, Similarity @base)
            {
                this.outerInstance = outerInstance;
                this.field = field;
                this.@base = @base;
                fooSim = new BoostingSimilarity(@base, "foo_boost");
            }

            private readonly Similarity fooSim;

            public override Similarity Get(string field)
            {
                return "foo".Equals(field, StringComparison.Ordinal) ? fooSim : @base;
            }

            public override float Coord(int overlap, int maxOverlap)
            {
                return @base.Coord(overlap, maxOverlap);
            }

            public override float QueryNorm(float sumOfSquaredWeights)
            {
                return @base.QueryNorm(sumOfSquaredWeights);
            }
        }

        /// <summary>
        /// Similarity that wraps another similarity and boosts the final score
        /// according to whats in a docvalues field.
        ///
        /// @lucene.experimental
        /// </summary>
        internal class BoostingSimilarity : Similarity
        {
            private readonly Similarity sim;
            private readonly string boostField;

            public BoostingSimilarity(Similarity sim, string boostField)
            {
                this.sim = sim;
                this.boostField = boostField;
            }

            public override long ComputeNorm(FieldInvertState state)
            {
                return sim.ComputeNorm(state);
            }

            public override SimWeight ComputeWeight(float queryBoost, CollectionStatistics collectionStats, params TermStatistics[] termStats)
            {
                return sim.ComputeWeight(queryBoost, collectionStats, termStats);
            }

            public override SimScorer GetSimScorer(SimWeight stats, AtomicReaderContext context)
            {
                SimScorer sub = sim.GetSimScorer(stats, context);
                FieldCache.Singles values = FieldCache.DEFAULT.GetSingles(context.AtomicReader, boostField, false);

                return new SimScorerAnonymousClass(this, sub, values);
            }

            private sealed class SimScorerAnonymousClass : SimScorer
            {
                private readonly BoostingSimilarity outerInstance;

                private readonly SimScorer sub;
                private readonly FieldCache.Singles values;

                public SimScorerAnonymousClass(BoostingSimilarity outerInstance, SimScorer sub, FieldCache.Singles values)
                {
                    this.outerInstance = outerInstance;
                    this.sub = sub;
                    this.values = values;
                }

                public override float Score(int doc, float freq)
                {
                    return values.Get(doc) * sub.Score(doc, freq);
                }

                public override float ComputeSlopFactor(int distance)
                {
                    return sub.ComputeSlopFactor(distance);
                }

                public override float ComputePayloadFactor(int doc, int start, int end, BytesRef payload)
                {
                    return sub.ComputePayloadFactor(doc, start, end, payload);
                }

                public override Explanation Explain(int doc, Explanation freq)
                {
                    Explanation boostExplanation = new Explanation(values.Get(doc), "indexDocValue(" + outerInstance.boostField + ")");
                    Explanation simExplanation = sub.Explain(doc, freq);
                    Explanation expl = new Explanation(boostExplanation.Value * simExplanation.Value, "product of:");
                    expl.AddDetail(boostExplanation);
                    expl.AddDetail(simExplanation);
                    return expl;
                }
            }
        }
    }
}