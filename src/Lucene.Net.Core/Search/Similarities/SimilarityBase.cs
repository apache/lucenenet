using System;
using System.Diagnostics;
using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
using BytesRef = Lucene.Net.Util.BytesRef;
using FieldInvertState = Lucene.Net.Index.FieldInvertState;
using NumericDocValues = Lucene.Net.Index.NumericDocValues;
using SmallFloat = Lucene.Net.Util.SmallFloat;

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
    /// A subclass of {@code Similarity} that provides a simplified API for its
    /// descendants. Subclasses are only required to implement the <seealso cref="#score"/>
    /// and <seealso cref="#toString()"/> methods. Implementing
    /// <seealso cref="#explain(Explanation, BasicStats, int, float, float)"/> is optional,
    /// inasmuch as SimilarityBase already provides a basic explanation of the score
    /// and the term frequency. However, implementers of a subclass are encouraged to
    /// include as much detail about the scoring method as possible.
    /// <p>
    /// Note: multi-word queries such as phrase queries are scored in a different way
    /// than Lucene's default ranking algorithm: whereas it "fakes" an IDF value for
    /// the phrase as a whole (since it does not know it), this class instead scores
    /// phrases as a summation of the individual term scores.
    /// @lucene.experimental
    /// </summary>
    public abstract class SimilarityBase : Similarity
    {
        /// <summary>
        /// For <seealso cref="#log2(double)"/>. Precomputed for efficiency reasons. </summary>
        private static readonly double LOG_2 = Math.Log(2);

        /// <summary>
        /// True if overlap tokens (tokens with a position of increment of zero) are
        /// discounted from the document's length.
        /// </summary>
        private bool DiscountOverlaps_Renamed = true; // LUCENENET Specific: made private, since it can be get/set through property

        /// <summary>
        /// Sole constructor. (For invocation by subclass
        /// constructors, typically implicit.)
        /// </summary>
        public SimilarityBase()
        {
        }

        /// <summary>
        /// Determines whether overlap tokens (Tokens with
        ///  0 position increment) are ignored when computing
        ///  norm.  By default this is true, meaning overlap
        ///  tokens do not count when computing norms.
        ///
        ///  @lucene.experimental
        /// </summary>
        ///  <seealso cref= #computeNorm </seealso>
        public virtual bool DiscountOverlaps
        {
            set
            {
                DiscountOverlaps_Renamed = value;
            }
            get
            {
                return DiscountOverlaps_Renamed;
            }
        }

        public override sealed SimWeight ComputeWeight(float queryBoost, CollectionStatistics collectionStats, params TermStatistics[] termStats)
        {
            BasicStats[] stats = new BasicStats[termStats.Length];
            for (int i = 0; i < termStats.Length; i++)
            {
                stats[i] = NewStats(collectionStats.Field(), queryBoost);
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
        /// Fills all member fields defined in {@code BasicStats} in {@code stats}.
        ///  Subclasses can override this method to fill additional stats.
        /// </summary>
        protected internal virtual void FillBasicStats(BasicStats stats, CollectionStatistics collectionStats, TermStatistics termStats)
        {
            // #positions(field) must be >= #positions(term)
            Debug.Assert(collectionStats.SumTotalTermFreq() == -1 || collectionStats.SumTotalTermFreq() >= termStats.TotalTermFreq());
            long numberOfDocuments = collectionStats.MaxDoc;

            long docFreq = termStats.DocFreq();
            long totalTermFreq = termStats.TotalTermFreq();

            // codec does not supply totalTermFreq: substitute docFreq
            if (totalTermFreq == -1)
            {
                totalTermFreq = docFreq;
            }

            long numberOfFieldTokens;
            float avgFieldLength;

            long sumTotalTermFreq = collectionStats.SumTotalTermFreq();

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
        /// Scores the document {@code doc}.
        /// <p>Subclasses must apply their scoring formula in this class.</p> </summary>
        /// <param name="stats"> the corpus level statistics. </param>
        /// <param name="freq"> the term frequency. </param>
        /// <param name="docLen"> the document length. </param>
        /// <returns> the score. </returns>
        public abstract float Score(BasicStats stats, float freq, float docLen);

        /// <summary>
        /// Subclasses should implement this method to explain the score. {@code expl}
        /// already contains the score, the name of the class and the doc id, as well
        /// as the term frequency and its explanation; subclasses can add additional
        /// clauses to explain details of their scoring formulae.
        /// <p>The default implementation does nothing.</p>
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
        /// in the format <em>score(name-of-similarity, doc=doc-id,
        /// freq=term-frequency), computed from:</em>, and
        /// attaches the score (computed via the <seealso cref="#score(BasicStats, float, float)"/>
        /// method) and the explanation for the term frequency. Subclasses content with
        /// this format may add additional details in
        /// <seealso cref="#explain(Explanation, BasicStats, int, float, float)"/>.
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

        public override SimScorer DoSimScorer(SimWeight stats, AtomicReaderContext context)
        {
            if (stats is MultiSimilarity.MultiStats)
            {
                // a multi term query (e.g. phrase). return the summation,
                // scoring almost as if it were boolean query
                SimWeight[] subStats = ((MultiSimilarity.MultiStats)stats).subStats;
                SimScorer[] subScorers = new SimScorer[subStats.Length];
                for (int i = 0; i < subScorers.Length; i++)
                {
                    BasicStats basicstats = (BasicStats)subStats[i];
                    subScorers[i] = new BasicSimScorer(this, basicstats, context.AtomicReader.GetNormValues(basicstats.field));
                }
                return new MultiSimilarity.MultiSimScorer(subScorers);
            }
            else
            {
                BasicStats basicstats = (BasicStats)stats;
                return new BasicSimScorer(this, basicstats, context.AtomicReader.GetNormValues(basicstats.field));
            }
        }

        /// <summary>
        /// Subclasses must override this method to return the name of the Similarity
        /// and preferably the values of parameters (if any) as well.
        /// </summary>
        public override abstract string ToString();

        // ------------------------------ Norm handling ------------------------------

        /// <summary>
        /// Norm -> document length map. </summary>
        private static readonly float[] NORM_TABLE = new float[256];

        static SimilarityBase()
        {
            for (int i = 0; i < 256; i++)
            {
                float floatNorm = SmallFloat.Byte315ToFloat((sbyte)i);
                NORM_TABLE[i] = 1.0f / (floatNorm * floatNorm);
            }
        }

        /// <summary>
        /// Encodes the document length in the same way as <seealso cref="TFIDFSimilarity"/>. </summary>
        public override long ComputeNorm(FieldInvertState state)
        {
            float numTerms;
            if (DiscountOverlaps_Renamed)
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
        /// <seealso cref= #encodeNormValue(float,float) </seealso>
        protected internal virtual float DecodeNormValue(sbyte norm) // LUCENENET TODO: Can this be byte?
        {
            return NORM_TABLE[norm & 0xFF]; // & 0xFF maps negative bytes to positive above 127
        }

        /// <summary>
        /// Encodes the length to a byte via SmallFloat. </summary>
        protected internal virtual sbyte EncodeNormValue(float boost, float length) // LUCENENET TODO: Can this be byte?
        {
            return SmallFloat.FloatToByte315((boost / (float)Math.Sqrt(length)));
        }

        // ----------------------------- Static methods ------------------------------

        /// <summary>
        /// Returns the base two logarithm of {@code x}. </summary>
        public static double Log2(double x)
        {
            // Put this to a 'util' class if we need more of these.
            return Math.Log(x) / LOG_2;
        }

        // --------------------------------- Classes ---------------------------------

        /// <summary>
        /// Delegates the <seealso cref="#score(int, float)"/> and
        /// <seealso cref="#explain(int, Explanation)"/> methods to
        /// <seealso cref="SimilarityBase#score(BasicStats, float, float)"/> and
        /// <seealso cref="SimilarityBase#explain(BasicStats, int, Explanation, float)"/>,
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
                return outerInstance.Score(stats, freq, norms == null ? 1F : outerInstance.DecodeNormValue((sbyte)norms.Get(doc)));
            }

            public override Explanation Explain(int doc, Explanation freq)
            {
                return outerInstance.Explain(stats, doc, freq, norms == null ? 1F : outerInstance.DecodeNormValue((sbyte)norms.Get(doc)));
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