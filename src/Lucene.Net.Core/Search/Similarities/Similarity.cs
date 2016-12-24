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
    /// <p>
    /// Expert: Scoring API.
    /// <p>
    /// this is a low-level API, you should only extend this API if you want to implement
    /// an information retrieval <i>model</i>.  If you are instead looking for a convenient way
    /// to alter Lucene's scoring, consider extending a higher-level implementation
    /// such as <seealso cref="TFIDFSimilarity"/>, which implements the vector space model with this API, or
    /// just tweaking the default implementation: <seealso cref="DefaultSimilarity"/>.
    /// <p>
    /// Similarity determines how Lucene weights terms, and Lucene interacts with
    /// this class at both <a href="#indextime">index-time</a> and
    /// <a href="#querytime">query-time</a>.
    /// <p>
    /// <a name="indextime"/>
    /// At indexing time, the indexer calls <seealso cref="#computeNorm(FieldInvertState)"/>, allowing
    /// the Similarity implementation to set a per-document value for the field that will
    /// be later accessible via <seealso cref="AtomicReader#getNormValues(String)"/>.  Lucene makes no assumption
    /// about what is in this norm, but it is most useful for encoding length normalization
    /// information.
    /// <p>
    /// Implementations should carefully consider how the normalization is encoded: while
    /// Lucene's classical <seealso cref="TFIDFSimilarity"/> encodes a combination of index-time boost
    /// and length normalization information with <seealso cref="SmallFloat"/> into a single byte, this
    /// might not be suitable for all purposes.
    /// <p>
    /// Many formulas require the use of average document length, which can be computed via a
    /// combination of <seealso cref="CollectionStatistics#sumTotalTermFreq()"/> and
    /// <seealso cref="CollectionStatistics#maxDoc()"/> or <seealso cref="CollectionStatistics#docCount()"/>,
    /// depending upon whether the average should reflect field sparsity.
    /// <p>
    /// Additional scoring factors can be stored in named
    /// <code>NumericDocValuesField</code>s and accessed
    /// at query-time with <seealso cref="AtomicReader#getNumericDocValues(String)"/>.
    /// <p>
    /// Finally, using index-time boosts (either via folding into the normalization byte or
    /// via DocValues), is an inefficient way to boost the scores of different fields if the
    /// boost will be the same for every document, instead the Similarity can simply take a constant
    /// boost parameter <i>C</i>, and <seealso cref="PerFieldSimilarityWrapper"/> can return different
    /// instances with different boosts depending upon field name.
    /// <p>
    /// <a name="querytime"/>
    /// At query-time, Queries interact with the Similarity via these steps:
    /// <ol>
    ///   <li>The <seealso cref="#computeWeight(float, CollectionStatistics, TermStatistics...)"/> method is called a single time,
    ///       allowing the implementation to compute any statistics (such as IDF, average document length, etc)
    ///       across <i>the entire collection</i>. The <seealso cref="TermStatistics"/> and <seealso cref="CollectionStatistics"/> passed in
    ///       already contain all of the raw statistics involved, so a Similarity can freely use any combination
    ///       of statistics without causing any additional I/O. Lucene makes no assumption about what is
    ///       stored in the returned <seealso cref="Similarity.SimWeight"/> object.
    ///   <li>The query normalization process occurs a single time: <seealso cref="Similarity.SimWeight#getValueForNormalization()"/>
    ///       is called for each query leaf node, <seealso cref="Similarity#queryNorm(float)"/> is called for the top-level
    ///       query, and finally <seealso cref="Similarity.SimWeight#normalize(float, float)"/> passes down the normalization value
    ///       and any top-level boosts (e.g. from enclosing <seealso cref="BooleanQuery"/>s).
    ///   <li>For each segment in the index, the Query creates a <seealso cref="#simScorer(SimWeight, AtomicReaderContext)"/>
    ///       The score() method is called for each matching document.
    /// </ol>
    /// <p>
    /// <a name="explaintime"/>
    /// When <seealso cref="IndexSearcher#explain(Lucene.Net.Search.Query, int)"/> is called, queries consult the Similarity's DocScorer for an
    /// explanation of how it computed its score. The query passes in a the document id and an explanation of how the frequency
    /// was computed.
    /// </summary>
    /// <seealso cref= Lucene.Net.Index.IndexWriterConfig#setSimilarity(Similarity) </seealso>
    /// <seealso cref= IndexSearcher#setSimilarity(Similarity)
    /// @lucene.experimental </seealso>
    public abstract class Similarity
    {
        /// <summary>
        /// Sole constructor. (For invocation by subclass
        /// constructors, typically implicit.)
        /// </summary>
        public Similarity()
        {
        }

        /// <summary>
        /// Hook to integrate coordinate-level matching.
        /// <p>
        /// By default this is disabled (returns <code>1</code>), as with
        /// most modern models this will only skew performance, but some
        /// implementations such as <seealso cref="TFIDFSimilarity"/> override this.
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
        /// normalized weights <seealso cref="SimWeight#getValueForNormalization()"/> of
        /// each of the query terms.  this value is passed back to the
        /// weight (<seealso cref="SimWeight#normalize(float, float)"/> of each query
        /// term, to provide a hook to attempt to make scores from different
        /// queries comparable.
        /// <p>
        /// By default this is disabled (returns <code>1</code>), but some
        /// implementations such as <seealso cref="TFIDFSimilarity"/> override this.
        /// </summary>
        /// <param name="valueForNormalization"> the sum of the term normalization values </param>
        /// <returns> a normalization factor for query weights </returns>
        public virtual float QueryNorm(float valueForNormalization)
        {
            return 1f;
        }

        /// <summary>
        /// Computes the normalization value for a field, given the accumulated
        /// state of term processing for this field (see <seealso cref="FieldInvertState"/>).
        ///
        /// <p>Matches in longer fields are less precise, so implementations of this
        /// method usually set smaller values when <code>state.getLength()</code> is large,
        /// and larger values when <code>state.getLength()</code> is small.
        ///
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
        /// <returns> SimWeight object with the information this Similarity needs to score a query. </returns>
        public abstract SimWeight ComputeWeight(float queryBoost, CollectionStatistics collectionStats, params TermStatistics[] termStats);

        /// <summary>
        /// Creates a new <seealso cref="Similarity.SimScorer"/> to score matching documents from a segment of the inverted index. </summary>
        /// <param name="weight"> collection information from <seealso cref="#computeWeight(float, CollectionStatistics, TermStatistics...)"/> </param>
        /// <param name="context"> segment of the inverted index to be scored. </param>
        /// <returns> SloppySimScorer for scoring documents across <code>context</code> </returns>
        /// <exception cref="IOException"> if there is a low-level I/O error </exception>
        public abstract SimScorer DoSimScorer(SimWeight weight, AtomicReaderContext context); // LUCENENET TODO: Rename SimScorer() (or GetSimScorer())

        /// <summary>
        /// API for scoring "sloppy" queries such as <seealso cref="TermQuery"/>,
        /// <seealso cref="SpanQuery"/>, and <seealso cref="PhraseQuery"/>.
        /// <p>
        /// Frequencies are floating-point values: an approximate
        /// within-document frequency adjusted for "sloppiness" by
        /// <seealso cref="SimScorer#computeSlopFactor(int)"/>.
        /// </summary>
        public abstract class SimScorer // LUCENENET TODO: de-nest from this class so we can name the above method SimScorer()
        {
            /// <summary>
            /// Sole constructor. (For invocation by subclass
            /// constructors, typically implicit.)
            /// </summary>
            public SimScorer()
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
        /// implementation is empty; descendants of {@code Similarity} should
        /// subclass {@code SimWeight} and define the statistics they require in the
        /// subclass. Examples include idf, average field length, etc.
        /// </summary>
        public abstract class SimWeight
        {
            /// <summary>
            /// Sole constructor. (For invocation by subclass
            /// constructors, typically implicit.)
            /// </summary>
            public SimWeight()
            {
            }

            /// <summary>
            /// The value for normalization of contained query clauses (e.g. sum of squared weights).
            /// <p>
            /// NOTE: a Similarity implementation might not use any query normalization at all,
            /// its not required. However, if it wants to participate in query normalization,
            /// it can return a value here.
            /// </summary>
            public abstract float GetValueForNormalization();

            /// <summary>
            /// Assigns the query normalization factor and boost from parent queries to this.
            /// <p>
            /// NOTE: a Similarity implementation might not use this normalized value at all,
            /// its not required. However, its usually a good idea to at least incorporate
            /// the topLevelBoost (e.g. from an outer BooleanQuery) into its score.
            /// </summary>
            public abstract void Normalize(float queryNorm, float topLevelBoost);
        }
    }
}