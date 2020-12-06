using System.IO;

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

    // javadoc
    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;

    // javadoc
    using BytesRef = Lucene.Net.Util.BytesRef;
    using FieldInvertState = Lucene.Net.Index.FieldInvertState;

    // javadoc

    /// <summary>
    /// Similarity defines the components of Lucene scoring.
    /// <para/>
    /// Expert: Scoring API.
    /// <para/>
    /// This is a low-level API, you should only extend this API if you want to implement
    /// an information retrieval <i>model</i>.  If you are instead looking for a convenient way
    /// to alter Lucene's scoring, consider extending a higher-level implementation
    /// such as <see cref="TFIDFSimilarity"/>, which implements the vector space model with this API, or
    /// just tweaking the default implementation: <see cref="DefaultSimilarity"/>.
    /// <para/>
    /// Similarity determines how Lucene weights terms, and Lucene interacts with
    /// this class at both <a href="#indextime">index-time</a> and
    /// <a href="#querytime">query-time</a>.
    /// <para/>
    /// <a name="indextime"/>
    /// At indexing time, the indexer calls <see cref="ComputeNorm(FieldInvertState)"/>, allowing
    /// the <see cref="Similarity"/> implementation to set a per-document value for the field that will
    /// be later accessible via <see cref="Index.AtomicReader.GetNormValues(string)"/>.  Lucene makes no assumption
    /// about what is in this norm, but it is most useful for encoding length normalization
    /// information.
    /// <para/>
    /// Implementations should carefully consider how the normalization is encoded: while
    /// Lucene's classical <see cref="TFIDFSimilarity"/> encodes a combination of index-time boost
    /// and length normalization information with <see cref="Util.SmallSingle"/> into a single byte, this
    /// might not be suitable for all purposes.
    /// <para/>
    /// Many formulas require the use of average document length, which can be computed via a
    /// combination of <see cref="CollectionStatistics.SumTotalTermFreq"/> and
    /// <see cref="CollectionStatistics.MaxDoc"/> or <see cref="CollectionStatistics.DocCount"/>,
    /// depending upon whether the average should reflect field sparsity.
    /// <para/>
    /// Additional scoring factors can be stored in named
    /// <see cref="Documents.NumericDocValuesField"/>s and accessed
    /// at query-time with <see cref="Index.AtomicReader.GetNumericDocValues(string)"/>.
    /// <para/>
    /// Finally, using index-time boosts (either via folding into the normalization byte or
    /// via <see cref="Index.DocValues"/>), is an inefficient way to boost the scores of different fields if the
    /// boost will be the same for every document, instead the Similarity can simply take a constant
    /// boost parameter <i>C</i>, and <see cref="PerFieldSimilarityWrapper"/> can return different
    /// instances with different boosts depending upon field name.
    /// <para/>
    /// <a name="querytime"/>
    /// At query-time, Queries interact with the Similarity via these steps:
    /// <list type="number">
    ///   <item><description>The <see cref="ComputeWeight(float, CollectionStatistics, TermStatistics[])"/> method is called a single time,
    ///       allowing the implementation to compute any statistics (such as IDF, average document length, etc)
    ///       across <i>the entire collection</i>. The <see cref="TermStatistics"/> and <see cref="CollectionStatistics"/> passed in
    ///       already contain all of the raw statistics involved, so a <see cref="Similarity"/> can freely use any combination
    ///       of statistics without causing any additional I/O. Lucene makes no assumption about what is
    ///       stored in the returned <see cref="Similarity.SimWeight"/> object.</description></item>
    ///   <item><description>The query normalization process occurs a single time: <see cref="Similarity.SimWeight.GetValueForNormalization()"/>
    ///       is called for each query leaf node, <see cref="Similarity.QueryNorm(float)"/> is called for the top-level
    ///       query, and finally <see cref="Similarity.SimWeight.Normalize(float, float)"/> passes down the normalization value
    ///       and any top-level boosts (e.g. from enclosing <see cref="BooleanQuery"/>s).</description></item>
    ///   <item><description>For each segment in the index, the <see cref="Query"/> creates a <see cref="GetSimScorer(SimWeight, AtomicReaderContext)"/>
    ///       The GetScore() method is called for each matching document.</description></item>
    /// </list>
    /// <para/>
    /// <a name="explaintime"/>
    /// When <see cref="IndexSearcher.Explain(Lucene.Net.Search.Query, int)"/> is called, queries consult the Similarity's DocScorer for an
    /// explanation of how it computed its score. The query passes in a the document id and an explanation of how the frequency
    /// was computed.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    /// <seealso cref="Lucene.Net.Index.IndexWriterConfig.Similarity"/>
    /// <seealso cref="IndexSearcher.Similarity"/>
    public abstract class Similarity
    {
        /// <summary>
        /// Sole constructor. (For invocation by subclass
        /// constructors, typically implicit.)
        /// </summary>
        protected Similarity() // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
        {
        }

        /// <summary>
        /// Hook to integrate coordinate-level matching.
        /// <para/>
        /// By default this is disabled (returns <c>1</c>), as with
        /// most modern models this will only skew performance, but some
        /// implementations such as <see cref="TFIDFSimilarity"/> override this.
        /// </summary>
        /// <param name="overlap"> the number of query terms matched in the document </param>
        /// <param name="maxOverlap"> the total number of terms in the query </param>
        /// <returns> a score factor based on term overlap with the query </returns>
        public virtual float Coord(int overlap, int maxOverlap)
        {
            return 1f;
        }

        /// <summary>
        /// Computes the normalization value for a query given the sum of the
        /// normalized weights <see cref="SimWeight.GetValueForNormalization()"/> of
        /// each of the query terms.  this value is passed back to the
        /// weight (<see cref="SimWeight.Normalize(float, float)"/> of each query
        /// term, to provide a hook to attempt to make scores from different
        /// queries comparable.
        /// <para/>
        /// By default this is disabled (returns <c>1</c>), but some
        /// implementations such as <see cref="TFIDFSimilarity"/> override this.
        /// </summary>
        /// <param name="valueForNormalization"> the sum of the term normalization values </param>
        /// <returns> a normalization factor for query weights </returns>
        public virtual float QueryNorm(float valueForNormalization)
        {
            return 1f;
        }

        /// <summary>
        /// Computes the normalization value for a field, given the accumulated
        /// state of term processing for this field (see <see cref="FieldInvertState"/>).
        ///
        /// <para/>Matches in longer fields are less precise, so implementations of this
        /// method usually set smaller values when <c>state.Length</c> is large,
        /// and larger values when <code>state.Length</code> is small.
        /// <para/>
        /// @lucene.experimental
        /// </summary>
        /// <param name="state"> current processing state for this field </param>
        /// <returns> computed norm value </returns>
        public abstract long ComputeNorm(FieldInvertState state);

        /// <summary>
        /// Compute any collection-level weight (e.g. IDF, average document length, etc) needed for scoring a query.
        /// </summary>
        /// <param name="queryBoost"> the query-time boost. </param>
        /// <param name="collectionStats"> collection-level statistics, such as the number of tokens in the collection. </param>
        /// <param name="termStats"> term-level statistics, such as the document frequency of a term across the collection. </param>
        /// <returns> <see cref="SimWeight"/> object with the information this <see cref="Similarity"/> needs to score a query. </returns>
        public abstract SimWeight ComputeWeight(float queryBoost, CollectionStatistics collectionStats, params TermStatistics[] termStats);

        /// <summary>
        /// Creates a new <see cref="Similarity.SimScorer"/> to score matching documents from a segment of the inverted index. </summary>
        /// <param name="weight"> collection information from <see cref="ComputeWeight(float, CollectionStatistics, TermStatistics[])"/> </param>
        /// <param name="context"> segment of the inverted index to be scored. </param>
        /// <returns> Sloppy <see cref="SimScorer"/> for scoring documents across <c>context</c> </returns>
        /// <exception cref="IOException"> if there is a low-level I/O error </exception>
        public abstract SimScorer GetSimScorer(SimWeight weight, AtomicReaderContext context);

        /// <summary>
        /// API for scoring "sloppy" queries such as <see cref="TermQuery"/>,
        /// <see cref="Spans.SpanQuery"/>, and <see cref="PhraseQuery"/>.
        /// <para/>
        /// Frequencies are floating-point values: an approximate
        /// within-document frequency adjusted for "sloppiness" by
        /// <see cref="SimScorer.ComputeSlopFactor(int)"/>.
        /// </summary>
        public abstract class SimScorer
        {
            /// <summary>
            /// Sole constructor. (For invocation by subclass
            /// constructors, typically implicit.)
            /// </summary>
            protected SimScorer() // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
            {
            }

            /// <summary>
            /// Score a single document </summary>
            /// <param name="doc"> document id within the inverted index segment </param>
            /// <param name="freq"> sloppy term frequency </param>
            /// <returns> document's score </returns>
            public abstract float Score(int doc, float freq);

            /// <summary>
            /// Computes the amount of a sloppy phrase match, based on an edit distance. </summary>
            public abstract float ComputeSlopFactor(int distance);

            /// <summary>
            /// Calculate a scoring factor based on the data in the payload. </summary>
            public abstract float ComputePayloadFactor(int doc, int start, int end, BytesRef payload);

            /// <summary>
            /// Explain the score for a single document </summary>
            /// <param name="doc"> document id within the inverted index segment </param>
            /// <param name="freq"> Explanation of how the sloppy term frequency was computed </param>
            /// <returns> document's score </returns>
            public virtual Explanation Explain(int doc, Explanation freq)
            {
                Explanation result = new Explanation(Score(doc, freq.Value), "score(doc=" + doc + ",freq=" + freq.Value + "), with freq of:");
                result.AddDetail(freq);
                return result;
            }
        }

        /// <summary>
        /// Stores the weight for a query across the indexed collection. this abstract
        /// implementation is empty; descendants of <see cref="Similarity"/> should
        /// subclass <see cref="SimWeight"/> and define the statistics they require in the
        /// subclass. Examples include idf, average field length, etc.
        /// </summary>
        public abstract class SimWeight
        {
            /// <summary>
            /// Sole constructor. (For invocation by subclass
            /// constructors, typically implicit.)
            /// </summary>
            protected SimWeight() // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
            {
            }

            /// <summary>
            /// The value for normalization of contained query clauses (e.g. sum of squared weights).
            /// <para/>
            /// NOTE: a <see cref="Similarity"/> implementation might not use any query normalization at all,
            /// its not required. However, if it wants to participate in query normalization,
            /// it can return a value here.
            /// </summary>
            public abstract float GetValueForNormalization();

            /// <summary>
            /// Assigns the query normalization factor and boost from parent queries to this.
            /// <para/>
            /// NOTE: a <see cref="Similarity"/> implementation might not use this normalized value at all,
            /// its not required. However, its usually a good idea to at least incorporate
            /// the <paramref name="topLevelBoost"/> (e.g. from an outer <see cref="BooleanQuery"/>) into its score.
            /// </summary>
            public abstract void Normalize(float queryNorm, float topLevelBoost);
        }
    }
}