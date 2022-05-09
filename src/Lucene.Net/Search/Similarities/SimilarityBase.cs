using Lucene.Net.Diagnostics;
using System;
using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
using BytesRef = Lucene.Net.Util.BytesRef;
using FieldInvertState = Lucene.Net.Index.FieldInvertState;
using NumericDocValues = Lucene.Net.Index.NumericDocValues;
using SmallSingle = Lucene.Net.Util.SmallSingle;

namespace Lucene.Net.Search.Similarities
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
    /// A subclass of <see cref="Similarity"/> that provides a simplified API for its
    /// descendants. Subclasses are only required to implement the <see cref="Score(BasicStats, float, float)"/>
    /// and <see cref="ToString()"/> methods. Implementing
    /// <see cref="Explain(Explanation, BasicStats, int, float, float)"/> is optional,
    /// inasmuch as <see cref="SimilarityBase"/> already provides a basic explanation of the score
    /// and the term frequency. However, implementers of a subclass are encouraged to
    /// include as much detail about the scoring method as possible.
    /// <para/>
    /// Note: multi-word queries such as phrase queries are scored in a different way
    /// than Lucene's default ranking algorithm: whereas it "fakes" an IDF value for
    /// the phrase as a whole (since it does not know it), this class instead scores
    /// phrases as a summation of the individual term scores.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public abstract class SimilarityBase : Similarity
    {
        /// <summary>
        /// For <see cref="Log2(double)"/>. Precomputed for efficiency reasons. </summary>
        private static readonly double LOG_2 = Math.Log(2);

        /// <summary>
        /// True if overlap tokens (tokens with a position of increment of zero) are
        /// discounted from the document's length.
        /// </summary>
        private bool discountOverlaps = true; // LUCENENET Specific: made private, since it can be get/set through property

        /// <summary>
        /// Sole constructor. (For invocation by subclass
        /// constructors, typically implicit.)
        /// </summary>
        protected SimilarityBase() // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
        {
        }

        /// <summary>
        /// Determines whether overlap tokens (Tokens with
        /// 0 position increment) are ignored when computing
        /// norm.  By default this is <c>true</c>, meaning overlap
        /// tokens do not count when computing norms.
        /// <para/>
        /// @lucene.experimental
        /// </summary>
        /// <seealso cref="ComputeNorm(FieldInvertState)"/>
        public virtual bool DiscountOverlaps
        {
            get => discountOverlaps;
            set => discountOverlaps = value;
        }

        public override sealed SimWeight ComputeWeight(float queryBoost, CollectionStatistics collectionStats, params TermStatistics[] termStats)
        {
            BasicStats[] stats = new BasicStats[termStats.Length];
            for (int i = 0; i < termStats.Length; i++)
            {
                stats[i] = NewStats(collectionStats.Field, queryBoost);
                FillBasicStats(stats[i], collectionStats, termStats[i]);
            }
            return stats.Length == 1 ? stats[0] : new MultiSimilarity.MultiStats(stats) as SimWeight;
        }

        /// <summary>
        /// Factory method to return a custom stats object </summary>
        protected internal virtual BasicStats NewStats(string field, float queryBoost)
        {
            return new BasicStats(field, queryBoost);
        }

        /// <summary>
        /// Fills all member fields defined in <see cref="BasicStats"/> in <paramref name="stats"/>.
        /// Subclasses can override this method to fill additional stats.
        /// </summary>
        protected internal virtual void FillBasicStats(BasicStats stats, CollectionStatistics collectionStats, TermStatistics termStats)
        {
            // #positions(field) must be >= #positions(term)
            if (Debugging.AssertsEnabled) Debugging.Assert(collectionStats.SumTotalTermFreq == -1 || collectionStats.SumTotalTermFreq >= termStats.TotalTermFreq);
            long numberOfDocuments = collectionStats.MaxDoc;

            long docFreq = termStats.DocFreq;
            long totalTermFreq = termStats.TotalTermFreq;

            // codec does not supply totalTermFreq: substitute docFreq
            if (totalTermFreq == -1)
            {
                totalTermFreq = docFreq;
            }

            long numberOfFieldTokens;
            float avgFieldLength;

            long sumTotalTermFreq = collectionStats.SumTotalTermFreq;

            if (sumTotalTermFreq <= 0)
            {
                // field does not exist;
                // We have to provide something if codec doesnt supply these measures,
                // or if someone omitted frequencies for the field... negative values cause
                // NaN/Inf for some scorers.
                numberOfFieldTokens = docFreq;
                avgFieldLength = 1;
            }
            else
            {
                numberOfFieldTokens = sumTotalTermFreq;
                avgFieldLength = (float)numberOfFieldTokens / numberOfDocuments;
            }

            // TODO: add sumDocFreq for field (numberOfFieldPostings)
            stats.NumberOfDocuments = numberOfDocuments;
            stats.NumberOfFieldTokens = numberOfFieldTokens;
            stats.AvgFieldLength = avgFieldLength;
            stats.DocFreq = docFreq;
            stats.TotalTermFreq = totalTermFreq;
        }

        /// <summary>
        /// Scores the document <c>doc</c>.
        /// <para>Subclasses must apply their scoring formula in this class.</para> </summary>
        /// <param name="stats"> the corpus level statistics. </param>
        /// <param name="freq"> the term frequency. </param>
        /// <param name="docLen"> the document length. </param>
        /// <returns> the score. </returns>
        public abstract float Score(BasicStats stats, float freq, float docLen);

        /// <summary>
        /// Subclasses should implement this method to explain the score. <paramref name="expl"/>
        /// already contains the score, the name of the class and the doc id, as well
        /// as the term frequency and its explanation; subclasses can add additional
        /// clauses to explain details of their scoring formulae.
        /// <para>The default implementation does nothing.</para>
        /// </summary>
        /// <param name="expl"> the explanation to extend with details. </param>
        /// <param name="stats"> the corpus level statistics. </param>
        /// <param name="doc"> the document id. </param>
        /// <param name="freq"> the term frequency. </param>
        /// <param name="docLen"> the document length. </param>
        protected internal virtual void Explain(Explanation expl, BasicStats stats, int doc, float freq, float docLen)
        {
        }

        /// <summary>
        /// Explains the score. The implementation here provides a basic explanation
        /// in the format <em>Score(name-of-similarity, doc=doc-id,
        /// freq=term-frequency), computed from:</em>, and
        /// attaches the score (computed via the <see cref="Score(BasicStats, float, float)"/>
        /// method) and the explanation for the term frequency. Subclasses content with
        /// this format may add additional details in
        /// <see cref="Explain(Explanation, BasicStats, int, float, float)"/>.
        /// </summary>
        /// <param name="stats"> the corpus level statistics. </param>
        /// <param name="doc"> the document id. </param>
        /// <param name="freq"> the term frequency and its explanation. </param>
        /// <param name="docLen"> the document length. </param>
        /// <returns> the explanation. </returns>
        public virtual Explanation Explain(BasicStats stats, int doc, Explanation freq, float docLen)
        {
            Explanation result = new Explanation();
            result.Value = Score(stats, freq.Value, docLen);
            result.Description = "score(" + this.GetType().Name + ", doc=" + doc + ", freq=" + freq.Value + "), computed from:";
            result.AddDetail(freq);

            Explain(result, stats, doc, freq.Value, docLen);

            return result;
        }

        public override SimScorer GetSimScorer(SimWeight stats, AtomicReaderContext context)
        {
            if (stats is MultiSimilarity.MultiStats multiStats)
            {
                // a multi term query (e.g. phrase). return the summation,
                // scoring almost as if it were boolean query
                SimWeight[] subStats = multiStats.subStats;
                SimScorer[] subScorers = new SimScorer[subStats.Length];
                for (int i = 0; i < subScorers.Length; i++)
                {
                    BasicStats basicstats = (BasicStats)subStats[i];
                    subScorers[i] = new BasicSimScorer(this, basicstats, context.AtomicReader.GetNormValues(basicstats.Field));
                }
                return new MultiSimilarity.MultiSimScorer(subScorers);
            }
            else
            {
                BasicStats basicstats = (BasicStats)stats;
                return new BasicSimScorer(this, basicstats, context.AtomicReader.GetNormValues(basicstats.Field));
            }
        }

        /// <summary>
        /// Subclasses must override this method to return the name of the <see cref="Similarity"/>
        /// and preferably the values of parameters (if any) as well.
        /// </summary>
        public override abstract string ToString();

        // ------------------------------ Norm handling ------------------------------

        /// <summary>
        /// Norm -> document length map. </summary>
        private static readonly float[] NORM_TABLE = LoadNormTable();

        private static float[] LoadNormTable() // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
        {
            float[] normTable = new float[256];
            for (int i = 0; i < 256; i++)
            {
                float floatNorm = SmallSingle.SByte315ToSingle((sbyte)i);
                normTable[i] = 1.0f / (floatNorm * floatNorm);
            }
            return normTable;
        }

        /// <summary>
        /// Encodes the document length in the same way as <see cref="TFIDFSimilarity"/>. </summary>
        public override long ComputeNorm(FieldInvertState state)
        {
            float numTerms;
            if (discountOverlaps)
            {
                numTerms = state.Length - state.NumOverlap;
            }
            else
            {
                numTerms = state.Length;
            }
            return EncodeNormValue(state.Boost, numTerms);
        }

        /// <summary>
        /// Decodes a normalization factor (document length) stored in an index. </summary>
        /// <see cref="EncodeNormValue(float,float)"/>
        protected internal virtual float DecodeNormValue(byte norm)
        {
            return NORM_TABLE[norm & 0xFF]; // & 0xFF maps negative bytes to positive above 127
        }

        /// <summary>
        /// Encodes the length to a byte via <see cref="SmallSingle"/>. </summary>
        protected internal virtual byte EncodeNormValue(float boost, float length)
        {
            return SmallSingle.SingleToByte315((boost / (float)Math.Sqrt(length)));
        }

        // ----------------------------- Static methods ------------------------------

        /// <summary>
        /// Returns the base two logarithm of <c>x</c>. </summary>
        public static double Log2(double x)
        {
            // Put this to a 'util' class if we need more of these.
            return Math.Log(x) / LOG_2;
        }

        // --------------------------------- Classes ---------------------------------

        /// <summary>
        /// Delegates the <see cref="Score(int, float)"/> and
        /// <see cref="Explain(int, Explanation)"/> methods to
        /// <see cref="SimilarityBase.Score(BasicStats, float, float)"/> and
        /// <see cref="SimilarityBase.Explain(BasicStats, int, Explanation, float)"/>,
        /// respectively.
        /// </summary>
        private class BasicSimScorer : SimScorer
        {
            private readonly SimilarityBase outerInstance;

            private readonly BasicStats stats;
            private readonly NumericDocValues norms;

            internal BasicSimScorer(SimilarityBase outerInstance, BasicStats stats, NumericDocValues norms)
            {
                this.outerInstance = outerInstance;
                this.stats = stats;
                this.norms = norms;
            }

            public override float Score(int doc, float freq)
            {
                // We have to supply something in case norms are omitted
                return outerInstance.Score(stats, freq, norms is null ? 1F : outerInstance.DecodeNormValue((byte)norms.Get(doc)));
            }

            public override Explanation Explain(int doc, Explanation freq)
            {
                return outerInstance.Explain(stats, doc, freq, norms is null ? 1F : outerInstance.DecodeNormValue((byte)norms.Get(doc)));
            }

            public override float ComputeSlopFactor(int distance)
            {
                return 1.0f / (distance + 1);
            }

            public override float ComputePayloadFactor(int doc, int start, int end, BytesRef payload)
            {
                return 1f;
            }
        }
    }
}