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
    using SmallSingle = Lucene.Net.Util.SmallSingle;

    /// <summary>
    /// BM25 Similarity. Introduced in Stephen E. Robertson, Steve Walker,
    /// Susan Jones, Micheline Hancock-Beaulieu, and Mike Gatford. Okapi at TREC-3.
    /// In Proceedings of the Third <b>T</b>ext <b>RE</b>trieval <b>C</b>onference (TREC 1994).
    /// Gaithersburg, USA, November 1994.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public class BM25Similarity : Similarity
    {
        private readonly float k1;
        private readonly float b;
        // TODO: should we add a delta like sifaka.cs.uiuc.edu/~ylv2/pub/sigir11-bm25l.pdf ?

        /// <summary>
        /// BM25 with the supplied parameter values. </summary>
        /// <param name="k1"> Controls non-linear term frequency normalization (saturation). </param>
        /// <param name="b"> Controls to what degree document length normalizes tf values. </param>
        public BM25Similarity(float k1, float b)
        {
            this.k1 = k1;
            this.b = b;
        }

        /// <summary>
        /// BM25 with these default values:
        /// <list type="bullet">
        ///   <item><description><c>k1 = 1.2</c>,</description></item>
        ///   <item><description><c>b = 0.75</c>.</description></item>
        /// </list>
        /// </summary>
        public BM25Similarity()
        {
            this.k1 = 1.2f;
            this.b = 0.75f;
        }

        /// <summary>
        /// Implemented as <c>log(1 + (numDocs - docFreq + 0.5)/(docFreq + 0.5))</c>. </summary>
        protected internal virtual float Idf(long docFreq, long numDocs)
        {
            return (float)Math.Log(1 + (numDocs - docFreq + 0.5D) / (docFreq + 0.5D));
        }

        /// <summary>
        /// Implemented as <c>1 / (distance + 1)</c>. </summary>
        protected internal virtual float SloppyFreq(int distance)
        {
            return 1.0f / (distance + 1);
        }

        /// <summary>
        /// The default implementation returns <c>1</c> </summary>
        protected internal virtual float ScorePayload(int doc, int start, int end, BytesRef payload)
        {
            return 1;
        }

        /// <summary>
        /// The default implementation computes the average as <c>sumTotalTermFreq / maxDoc</c>,
        /// or returns <c>1</c> if the index does not store sumTotalTermFreq (Lucene 3.x indexes
        /// or any field that omits frequency information).
        /// </summary>
        protected internal virtual float AvgFieldLength(CollectionStatistics collectionStats)
        {
            long sumTotalTermFreq = collectionStats.SumTotalTermFreq;
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
        /// The default implementation encodes <c>boost / sqrt(length)</c>
        /// with <see cref="SmallSingle.SingleToByte315(float)"/>.  This is compatible with
        /// Lucene's default implementation.  If you change this, then you should
        /// change <see cref="DecodeNormValue(byte)"/> to match.
        /// </summary>
        protected internal virtual byte EncodeNormValue(float boost, int fieldLength) 
        {
            return SmallSingle.SingleToByte315(boost / (float)Math.Sqrt(fieldLength));
        }

        /// <summary>
        /// The default implementation returns <c>1 / f<sup>2</sup></c>
        /// where <c>f</c> is <see cref="SmallSingle.Byte315ToSingle(byte)"/>.
        /// </summary>
        protected internal virtual float DecodeNormValue(byte b)
        {
            return NORM_TABLE[b & 0xFF];
        }

        /// <summary>
        /// True if overlap tokens (tokens with a position of increment of zero) are
        /// discounted from the document's length.
        /// </summary>
        private bool discountOverlaps = true; // LUCENENET specific: made private, since value can be set/get through propery

        /// <summary>
        /// Gets or Sets whether overlap tokens (Tokens with 0 position increment) are
        /// ignored when computing norm.  By default this is true, meaning overlap
        /// tokens do not count when computing norms.
        /// </summary>
        public virtual bool DiscountOverlaps
        {
            get => discountOverlaps;
            set => discountOverlaps = value;
        }

        /// <summary>
        /// Cache of decoded bytes. </summary>
        private static readonly float[] NORM_TABLE = LoadNormTable();

        private static float[] LoadNormTable() // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
        {
            float[] normTable = new float[256];
            for (int i = 0; i < 256; i++)
            {
                float f = SmallSingle.SByte315ToSingle((sbyte)i);
                normTable[i] = 1.0f / (f * f);
            }
            return normTable;
        }

        public override sealed long ComputeNorm(FieldInvertState state)
        {
            int numTerms = discountOverlaps ? state.Length - state.NumOverlap : state.Length;
            return EncodeNormValue(state.Boost, numTerms);
        }

        /// <summary>
        /// Computes a score factor for a simple term and returns an explanation
        /// for that score factor.
        ///
        /// <para/>
        /// The default implementation uses:
        ///
        /// <code>
        ///     Idf(docFreq, searcher.MaxDoc);
        /// </code>
        ///
        /// Note that <see cref="CollectionStatistics.MaxDoc"/> is used instead of
        /// <see cref="Lucene.Net.Index.IndexReader.NumDocs"/> because also
        /// <see cref="TermStatistics.DocFreq"/> is used, and when the latter
        /// is inaccurate, so is <see cref="CollectionStatistics.MaxDoc"/>, and in the same direction.
        /// In addition, <see cref="CollectionStatistics.MaxDoc"/> is more efficient to compute
        /// </summary>
        /// <param name="collectionStats"> collection-level statistics </param>
        /// <param name="termStats"> term-level statistics for the term </param>
        /// <returns> an <see cref="Explanation"/> object that includes both an idf score factor
        ///           and an explanation for the term. </returns>
        public virtual Explanation IdfExplain(CollectionStatistics collectionStats, TermStatistics termStats)
        {
            long df = termStats.DocFreq;
            long max = collectionStats.MaxDoc;
            float idf = Idf(df, max);
            return new Explanation(idf, "idf(docFreq=" + df + ", maxDocs=" + max + ")");
        }

        /// <summary>
        /// Computes a score factor for a phrase.
        ///
        /// <para/>
        /// The default implementation sums the idf factor for
        /// each term in the phrase.
        /// </summary>
        /// <param name="collectionStats"> collection-level statistics </param>
        /// <param name="termStats"> term-level statistics for the terms in the phrase </param>
        /// <returns> an <see cref="Explanation"/> object that includes both an idf
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
                long df = stat.DocFreq;
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
                cache[i] = k1 * ((1 - b) + b * DecodeNormValue((byte)i) / avgdl);
            }
            return new BM25Stats(collectionStats.Field, idf, queryBoost, avgdl, cache);
        }

        public override sealed SimScorer GetSimScorer(SimWeight stats, AtomicReaderContext context)
        {
            BM25Stats bm25stats = (BM25Stats)stats;
            return new BM25DocScorer(this, bm25stats, context.AtomicReader.GetNormValues(bm25stats.Field));
        }

        private class BM25DocScorer : SimScorer
        {
            private readonly BM25Similarity outerInstance;

            private readonly BM25Stats stats;
            private readonly float weightValue; // boost * idf * (k1 + 1)
            private readonly NumericDocValues norms;
            private readonly float[] cache;

            internal BM25DocScorer(BM25Similarity outerInstance, BM25Stats stats, NumericDocValues norms)
            {
                this.outerInstance = outerInstance;
                this.stats = stats;
                this.weightValue = stats.Weight * (outerInstance.k1 + 1);
                this.cache = stats.Cache;
                this.norms = norms;
            }

            public override float Score(int doc, float freq)
            {
                // if there are no norms, we act as if b=0
                float norm = norms is null ? outerInstance.k1 : cache[(sbyte)norms.Get(doc) & 0xFF];
                return weightValue * freq / (freq + norm);
            }

            public override Explanation Explain(int doc, Explanation freq)
            {
                return outerInstance.ExplainScore(doc, freq, stats, norms);
            }

            public override float ComputeSlopFactor(int distance)
            {
                return outerInstance.SloppyFreq(distance);
            }

            public override float ComputePayloadFactor(int doc, int start, int end, BytesRef payload)
            {
                return outerInstance.ScorePayload(doc, start, end, payload);
            }
        }

        /// <summary>
        /// Collection statistics for the BM25 model. </summary>
        private class BM25Stats : SimWeight
        {
            /// <summary>
            /// BM25's idf </summary>
            internal Explanation Idf { get; private set; }

            /// <summary>
            /// The average document length. </summary>
            internal float Avgdl { get; private set; }

            /// <summary>
            /// query's inner boost </summary>
            internal float QueryBoost { get; private set; }

            /// <summary>
            /// query's outer boost (only for explain) </summary>
            internal float TopLevelBoost { get; set; }

            /// <summary>
            /// weight (idf * boost) </summary>
            internal float Weight { get; set; }

            /// <summary>
            /// field name, for pulling norms </summary>
            internal string Field { get; private set; }

            /// <summary>
            /// precomputed norm[256] with k1 * ((1 - b) + b * dl / avgdl) </summary>
            internal float[] Cache { get; private set; }

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
            tfNormExpl.AddDetail(new Explanation(k1, "parameter k1"));
            if (norms is null)
            {
                tfNormExpl.AddDetail(new Explanation(0, "parameter b (norms omitted for field)"));
                tfNormExpl.Value = (freq.Value * (k1 + 1)) / (freq.Value + k1);
            }
            else
            {
                float doclen = DecodeNormValue((byte)norms.Get(doc));
                tfNormExpl.AddDetail(new Explanation(b, "parameter b"));
                tfNormExpl.AddDetail(new Explanation(stats.Avgdl, "avgFieldLength"));
                tfNormExpl.AddDetail(new Explanation(doclen, "fieldLength"));
                tfNormExpl.Value = (freq.Value * (k1 + 1)) / (freq.Value + k1 * (1 - b + b * doclen / stats.Avgdl));
            }
            result.AddDetail(tfNormExpl);
            result.Value = boostExpl.Value * stats.Idf.Value * tfNormExpl.Value;
            return result;
        }

        public override string ToString()
        {
            return "BM25(k1=" + k1 + ",b=" + b + ")";
        }

        /// <summary>
        /// Returns the <c>k1</c> parameter </summary>
        /// <seealso cref="BM25Similarity(float, float)"/>
        public virtual float K1 => k1;

        /// <summary>
        /// Returns the <c>b</c> parameter </summary>
        /// <seealso cref="BM25Similarity(float, float)"/>
        public virtual float B => b;
    }
}