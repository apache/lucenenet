using System;

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

    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using FieldInvertState = Lucene.Net.Index.FieldInvertState;
    using NumericDocValues = Lucene.Net.Index.NumericDocValues;
    using SmallFloat = Lucene.Net.Util.SmallFloat;

    /// <summary>
    /// BM25 Similarity. Introduced in Stephen E. Robertson, Steve Walker,
    /// Susan Jones, Micheline Hancock-Beaulieu, and Mike Gatford. Okapi at TREC-3.
    /// In Proceedings of the Third <b>T</b>ext <b>RE</b>trieval <b>C</b>onference (TREC 1994).
    /// Gaithersburg, USA, November 1994.
    /// @lucene.experimental
    /// </summary>
    public class BM25Similarity : Similarity
    {
        private readonly float K1_Renamed;
        private readonly float b;
        // TODO: should we add a delta like sifaka.cs.uiuc.edu/~ylv2/pub/sigir11-bm25l.pdf ?

        /// <summary>
        /// BM25 with the supplied parameter values. </summary>
        /// <param name="k1"> Controls non-linear term frequency normalization (saturation). </param>
        /// <param name="b"> Controls to what degree document length normalizes tf values. </param>
        public BM25Similarity(float k1, float b)
        {
            this.K1_Renamed = k1;
            this.b = b;
        }

        /// <summary>
        /// BM25 with these default values:
        /// <ul>
        ///   <li>{@code k1 = 1.2},
        ///   <li>{@code b = 0.75}.</li>
        /// </ul>
        /// </summary>
        public BM25Similarity()
        {
            this.K1_Renamed = 1.2f;
            this.b = 0.75f;
        }

        /// <summary>
        /// Implemented as <code>log(1 + (numDocs - docFreq + 0.5)/(docFreq + 0.5))</code>. </summary>
        protected internal virtual float Idf(long docFreq, long numDocs)
        {
            return (float)Math.Log(1 + (numDocs - docFreq + 0.5D) / (docFreq + 0.5D));
        }

        /// <summary>
        /// Implemented as <code>1 / (distance + 1)</code>. </summary>
        protected internal virtual float SloppyFreq(int distance)
        {
            return 1.0f / (distance + 1);
        }

        /// <summary>
        /// The default implementation returns <code>1</code> </summary>
        protected internal virtual float ScorePayload(int doc, int start, int end, BytesRef payload)
        {
            return 1;
        }

        /// <summary>
        /// The default implementation computes the average as <code>sumTotalTermFreq / maxDoc</code>,
        /// or returns <code>1</code> if the index does not store sumTotalTermFreq (Lucene 3.x indexes
        /// or any field that omits frequency information).
        /// </summary>
        protected internal virtual float AvgFieldLength(CollectionStatistics collectionStats)
        {
            long sumTotalTermFreq = collectionStats.SumTotalTermFreq();
            if (sumTotalTermFreq <= 0)
            {
                return 1f; // field does not exist, or stat is unsupported
            }
            else
            {
                return (float)(sumTotalTermFreq / (double)collectionStats.MaxDoc);
            }
        }

        /// <summary>
        /// The default implementation encodes <code>boost / sqrt(length)</code>
        /// with <seealso cref="SmallFloat#floatToByte315(float)"/>.  this is compatible with
        /// Lucene's default implementation.  If you change this, then you should
        /// change <seealso cref="#decodeNormValue(byte)"/> to match.
        /// </summary>
        protected internal virtual sbyte EncodeNormValue(float boost, int fieldLength) // LUCENENET TODO: Can we use byte?
        {
            return SmallFloat.FloatToByte315(boost / (float)Math.Sqrt(fieldLength));
        }

        /// <summary>
        /// The default implementation returns <code>1 / f<sup>2</sup></code>
        /// where <code>f</code> is <seealso cref="SmallFloat#byte315ToFloat(byte)"/>.
        /// </summary>
        protected internal virtual float DecodeNormValue(sbyte b) // LUCENENET TODO: Can we use byte?
        {
            return NORM_TABLE[b & 0xFF];
        }

        /// <summary>
        /// True if overlap tokens (tokens with a position of increment of zero) are
        /// discounted from the document's length.
        /// </summary>
        private bool DiscountOverlaps_Renamed = true; // LUCENENET specific: made private, since value can be set/get through propery

        /// <summary>
        /// Sets whether overlap tokens (Tokens with 0 position increment) are
        ///  ignored when computing norm.  By default this is true, meaning overlap
        ///  tokens do not count when computing norms.
        /// </summary>
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

        /// <summary>
        /// Cache of decoded bytes. </summary>
        private static readonly float[] NORM_TABLE = new float[256];

        static BM25Similarity()
        {
            for (int i = 0; i < 256; i++)
            {
                float f = SmallFloat.Byte315ToFloat((sbyte)i);
                NORM_TABLE[i] = 1.0f / (f * f);
            }
        }

        public override sealed long ComputeNorm(FieldInvertState state)
        {
            int numTerms = DiscountOverlaps_Renamed ? state.Length - state.NumOverlap : state.Length;
            return EncodeNormValue(state.Boost, numTerms);
        }

        /// <summary>
        /// Computes a score factor for a simple term and returns an explanation
        /// for that score factor.
        ///
        /// <p>
        /// The default implementation uses:
        ///
        /// <pre class="prettyprint">
        /// idf(docFreq, searcher.maxDoc());
        /// </pre>
        ///
        /// Note that <seealso cref="CollectionStatistics#maxDoc()"/> is used instead of
        /// <seealso cref="Lucene.Net.Index.IndexReader#numDocs() IndexReader#numDocs()"/> because also
        /// <seealso cref="TermStatistics#docFreq()"/> is used, and when the latter
        /// is inaccurate, so is <seealso cref="CollectionStatistics#maxDoc()"/>, and in the same direction.
        /// In addition, <seealso cref="CollectionStatistics#maxDoc()"/> is more efficient to compute
        /// </summary>
        /// <param name="collectionStats"> collection-level statistics </param>
        /// <param name="termStats"> term-level statistics for the term </param>
        /// <returns> an Explain object that includes both an idf score factor
        ///           and an explanation for the term. </returns>
        public virtual Explanation IdfExplain(CollectionStatistics collectionStats, TermStatistics termStats)
        {
            long df = termStats.DocFreq();
            long max = collectionStats.MaxDoc;
            float idf = Idf(df, max);
            return new Explanation(idf, "idf(docFreq=" + df + ", maxDocs=" + max + ")");
        }

        /// <summary>
        /// Computes a score factor for a phrase.
        ///
        /// <p>
        /// The default implementation sums the idf factor for
        /// each term in the phrase.
        /// </summary>
        /// <param name="collectionStats"> collection-level statistics </param>
        /// <param name="termStats"> term-level statistics for the terms in the phrase </param>
        /// <returns> an Explain object that includes both an idf
        ///         score factor for the phrase and an explanation
        ///         for each term. </returns>
        public virtual Explanation IdfExplain(CollectionStatistics collectionStats, TermStatistics[] termStats)
        {
            long max = collectionStats.MaxDoc;
            float idf = 0.0f;
            Explanation exp = new Explanation();
            exp.Description = "idf(), sum of:";
            foreach (TermStatistics stat in termStats)
            {
                long df = stat.DocFreq();
                float termIdf = Idf(df, max);
                exp.AddDetail(new Explanation(termIdf, "idf(docFreq=" + df + ", maxDocs=" + max + ")"));
                idf += termIdf;
            }
            exp.Value = idf;
            return exp;
        }

        public override sealed SimWeight ComputeWeight(float queryBoost, CollectionStatistics collectionStats, params TermStatistics[] termStats)
        {
            Explanation idf = termStats.Length == 1 ? IdfExplain(collectionStats, termStats[0]) : IdfExplain(collectionStats, termStats);

            float avgdl = AvgFieldLength(collectionStats);

            // compute freq-independent part of bm25 equation across all norm values
            float[] cache = new float[256];
            for (int i = 0; i < cache.Length; i++)
            {
                cache[i] = K1_Renamed * ((1 - b) + b * DecodeNormValue((sbyte)i) / avgdl);
            }
            return new BM25Stats(collectionStats.Field(), idf, queryBoost, avgdl, cache);
        }

        public override sealed SimScorer DoSimScorer(SimWeight stats, AtomicReaderContext context)
        {
            BM25Stats bm25stats = (BM25Stats)stats;
            return new BM25DocScorer(this, bm25stats, context.AtomicReader.GetNormValues(bm25stats.Field));
        }

        private class BM25DocScorer : SimScorer
        {
            private readonly BM25Similarity OuterInstance;

            private readonly BM25Stats Stats;
            private readonly float WeightValue; // boost * idf * (k1 + 1)
            private readonly NumericDocValues Norms;
            private readonly float[] Cache;

            internal BM25DocScorer(BM25Similarity outerInstance, BM25Stats stats, NumericDocValues norms)
            {
                this.OuterInstance = outerInstance;
                this.Stats = stats;
                this.WeightValue = stats.Weight * (outerInstance.K1_Renamed + 1);
                this.Cache = stats.Cache;
                this.Norms = norms;
            }

            public override float Score(int doc, float freq)
            {
                // if there are no norms, we act as if b=0
                float norm = Norms == null ? OuterInstance.K1_Renamed : Cache[(sbyte)Norms.Get(doc) & 0xFF];
                return WeightValue * freq / (freq + norm);
            }

            public override Explanation Explain(int doc, Explanation freq)
            {
                return OuterInstance.ExplainScore(doc, freq, Stats, Norms);
            }

            public override float ComputeSlopFactor(int distance)
            {
                return OuterInstance.SloppyFreq(distance);
            }

            public override float ComputePayloadFactor(int doc, int start, int end, BytesRef payload)
            {
                return OuterInstance.ScorePayload(doc, start, end, payload);
            }
        }

        /// <summary>
        /// Collection statistics for the BM25 model. </summary>
        private class BM25Stats : SimWeight
        {
            /// <summary>
            /// BM25's idf </summary>
            internal readonly Explanation Idf; // LUCENENET TODO: Make property

            /// <summary>
            /// The average document length. </summary>
            internal readonly float Avgdl; // LUCENENET TODO: Make property

            /// <summary>
            /// query's inner boost </summary>
            internal readonly float QueryBoost; // LUCENENET TODO: Make property

            /// <summary>
            /// query's outer boost (only for explain) </summary>
            internal float TopLevelBoost; // LUCENENET TODO: Make property

            /// <summary>
            /// weight (idf * boost) </summary>
            internal float Weight; // LUCENENET TODO: Make property

            /// <summary>
            /// field name, for pulling norms </summary>
            internal readonly string Field; // LUCENENET TODO: Make property

            /// <summary>
            /// precomputed norm[256] with k1 * ((1 - b) + b * dl / avgdl) </summary>
            internal readonly float[] Cache; // LUCENENET TODO: Make property

            internal BM25Stats(string field, Explanation idf, float queryBoost, float avgdl, float[] cache)
            {
                this.Field = field;
                this.Idf = idf;
                this.QueryBoost = queryBoost;
                this.Avgdl = avgdl;
                this.Cache = cache;
            }

            public override float GetValueForNormalization()
            {
                // we return a TF-IDF like normalization to be nice, but we don't actually normalize ourselves.
                float queryWeight = Idf.Value * QueryBoost;
                return queryWeight * queryWeight;
            }

            public override void Normalize(float queryNorm, float topLevelBoost)
            {
                // we don't normalize with queryNorm at all, we just capture the top-level boost
                this.TopLevelBoost = topLevelBoost;
                this.Weight = Idf.Value * QueryBoost * topLevelBoost;
            }
        }

        private Explanation ExplainScore(int doc, Explanation freq, BM25Stats stats, NumericDocValues norms)
        {
            Explanation result = new Explanation();
            result.Description = "score(doc=" + doc + ",freq=" + freq + "), product of:";

            Explanation boostExpl = new Explanation(stats.QueryBoost * stats.TopLevelBoost, "boost");
            if (boostExpl.Value != 1.0f)
            {
                result.AddDetail(boostExpl);
            }

            result.AddDetail(stats.Idf);

            Explanation tfNormExpl = new Explanation();
            tfNormExpl.Description = "tfNorm, computed from:";
            tfNormExpl.AddDetail(freq);
            tfNormExpl.AddDetail(new Explanation(K1_Renamed, "parameter k1"));
            if (norms == null)
            {
                tfNormExpl.AddDetail(new Explanation(0, "parameter b (norms omitted for field)"));
                tfNormExpl.Value = (freq.Value * (K1_Renamed + 1)) / (freq.Value + K1_Renamed);
            }
            else
            {
                float doclen = DecodeNormValue((sbyte)norms.Get(doc));
                tfNormExpl.AddDetail(new Explanation(b, "parameter b"));
                tfNormExpl.AddDetail(new Explanation(stats.Avgdl, "avgFieldLength"));
                tfNormExpl.AddDetail(new Explanation(doclen, "fieldLength"));
                tfNormExpl.Value = (freq.Value * (K1_Renamed + 1)) / (freq.Value + K1_Renamed * (1 - b + b * doclen / stats.Avgdl));
            }
            result.AddDetail(tfNormExpl);
            result.Value = boostExpl.Value * stats.Idf.Value * tfNormExpl.Value;
            return result;
        }

        public override string ToString()
        {
            return "BM25(k1=" + K1_Renamed + ",b=" + b + ")";
        }

        /// <summary>
        /// Returns the <code>k1</code> parameter </summary>
        /// <seealso cref= #BM25Similarity(float, float)  </seealso>
        public virtual float K1
        {
            get
            {
                return K1_Renamed;
            }
        }

        /// <summary>
        /// Returns the <code>b</code> parameter </summary>
        /// <seealso cref= #BM25Similarity(float, float)  </seealso>
        public virtual float B
        {
            get
            {
                return b;
            }
        }
    }
}