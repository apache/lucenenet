using Lucene.Net.Support;

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

    /// <summary>
    /// Implementation of <see cref="Similarity"/> with the Vector Space Model.
    /// <para/>
    /// Expert: Scoring API.
    /// <para/>TFIDFSimilarity defines the components of Lucene scoring.
    /// Overriding computation of these components is a convenient
    /// way to alter Lucene scoring.
    ///
    /// <para/>Suggested reading:
    /// <a href="http://nlp.stanford.edu/IR-book/html/htmledition/queries-as-vectors-1.html">
    /// Introduction To Information Retrieval, Chapter 6</a>.
    ///
    /// <para/>The following describes how Lucene scoring evolves from
    /// underlying information retrieval models to (efficient) implementation.
    /// We first brief on <i>VSM Score</i>,
    /// then derive from it <i>Lucene's Conceptual Scoring Formula</i>,
    /// from which, finally, evolves <i>Lucene's Practical Scoring Function</i>
    /// (the latter is connected directly with Lucene classes and methods).
    ///
    /// <para/>Lucene combines
    /// <a href="http://en.wikipedia.org/wiki/Standard_Boolean_model">
    /// Boolean model (BM) of Information Retrieval</a>
    /// with
    /// <a href="http://en.wikipedia.org/wiki/Vector_Space_Model">
    /// Vector Space Model (VSM) of Information Retrieval</a> -
    /// documents "approved" by BM are scored by VSM.
    ///
    /// <para/>In VSM, documents and queries are represented as
    /// weighted vectors in a multi-dimensional space,
    /// where each distinct index term is a dimension,
    /// and weights are
    /// <a href="http://en.wikipedia.org/wiki/Tfidf">Tf-idf</a> values.
    ///
    /// <para/>VSM does not require weights to be <i>Tf-idf</i> values,
    /// but <i>Tf-idf</i> values are believed to produce search results of high quality,
    /// and so Lucene is using <i>Tf-idf</i>.
    /// <i>Tf</i> and <i>Idf</i> are described in more detail below,
    /// but for now, for completion, let's just say that
    /// for given term <i>t</i> and document (or query) <i>x</i>,
    /// <i>Tf(t,x)</i> varies with the number of occurrences of term <i>t</i> in <i>x</i>
    /// (when one increases so does the other) and
    /// <i>idf(t)</i> similarly varies with the inverse of the
    /// number of index documents containing term <i>t</i>.
    ///
    /// <para/><i>VSM score</i> of document <i>d</i> for query <i>q</i> is the
    /// <a href="http://en.wikipedia.org/wiki/Cosine_similarity">
    /// Cosine Similarity</a>
    /// of the weighted query vectors <i>V(q)</i> and <i>V(d)</i>:
    /// <para/>
    /// <list type="table">
    ///     <item>
    ///         <term>
    ///             <list type="table">
    ///                 <item>
    ///                     <term>cosine-similarity(q,d) &#160; = &#160;</term>
    ///                     <term>
    ///                         <table>
    ///                             <item><term><small>V(q)&#160;&#183;&#160;V(d)</small></term></item>
    ///                             <item><term>&#8211;&#8211;&#8211;&#8211;&#8211;&#8211;&#8211;&#8211;&#8211;</term></item>
    ///                             <item><term><small>|V(q)|&#160;|V(d)|</small></term></item>
    ///                         </table>
    ///                     </term>
    ///                 </item>
    ///             </list>
    ///         </term>
    ///     </item>
    ///     <item>
    ///         <term>VSM Score</term>
    ///     </item>
    /// </list>
    /// <para/>
    /// 
    ///
    /// Where <i>V(q)</i> &#183; <i>V(d)</i> is the
    /// <a href="http://en.wikipedia.org/wiki/Dot_product">dot product</a>
    /// of the weighted vectors,
    /// and <i>|V(q)|</i> and <i>|V(d)|</i> are their
    /// <a href="http://en.wikipedia.org/wiki/Euclidean_norm#Euclidean_norm">Euclidean norms</a>.
    ///
    /// <para/>Note: the above equation can be viewed as the dot product of
    /// the normalized weighted vectors, in the sense that dividing
    /// <i>V(q)</i> by its euclidean norm is normalizing it to a unit vector.
    ///
    /// <para/>Lucene refines <i>VSM score</i> for both search quality and usability:
    /// <list type="bullet">
    ///  <item><description>Normalizing <i>V(d)</i> to the unit vector is known to be problematic in that
    ///  it removes all document length information.
    ///  For some documents removing this info is probably ok,
    ///  e.g. a document made by duplicating a certain paragraph <i>10</i> times,
    ///  especially if that paragraph is made of distinct terms.
    ///  But for a document which contains no duplicated paragraphs,
    ///  this might be wrong.
    ///  To avoid this problem, a different document length normalization
    ///  factor is used, which normalizes to a vector equal to or larger
    ///  than the unit vector: <i>doc-len-norm(d)</i>.
    ///  </description></item>
    ///
    ///  <item><description>At indexing, users can specify that certain documents are more
    ///  important than others, by assigning a document boost.
    ///  For this, the score of each document is also multiplied by its boost value
    ///  <i>doc-boost(d)</i>.
    ///  </description></item>
    ///
    ///  <item><description>Lucene is field based, hence each query term applies to a single
    ///  field, document length normalization is by the length of the certain field,
    ///  and in addition to document boost there are also document fields boosts.
    ///  </description></item>
    ///
    ///  <item><description>The same field can be added to a document during indexing several times,
    ///  and so the boost of that field is the multiplication of the boosts of
    ///  the separate additions (or parts) of that field within the document.
    ///  </description></item>
    ///
    ///  <item><description>At search time users can specify boosts to each query, sub-query, and
    ///  each query term, hence the contribution of a query term to the score of
    ///  a document is multiplied by the boost of that query term <i>query-boost(q)</i>.
    ///  </description></item>
    ///
    ///  <item><description>A document may match a multi term query without containing all
    ///  the terms of that query (this is correct for some of the queries),
    ///  and users can further reward documents matching more query terms
    ///  through a coordination factor, which is usually larger when
    ///  more terms are matched: <i>coord-factor(q,d)</i>.
    ///  </description></item>
    /// </list>
    ///
    /// <para/>Under the simplifying assumption of a single field in the index,
    /// we get <i>Lucene's Conceptual scoring formula</i>:
    /// 
    /// <para/>
    /// <list type="table">
    ///     <item>
    ///         <term>
    ///             <list type="table">
    ///                 <item>
    ///                     <term>
    ///                         score(q,d) &#160; = &#160;
    ///                         <font color="#FF9933">coord-factor(q,d)</font> &#183; &#160;
    ///                         <font color="#CCCC00">query-boost(q)</font> &#183; &#160;
    ///                     </term>
    ///                     <term>
    ///                         <list type="table">
    ///                             <item><term><small><font color="#993399">V(q)&#160;&#183;&#160;V(d)</font></small></term></item>
    ///                             <item><term>&#8211;&#8211;&#8211;&#8211;&#8211;&#8211;&#8211;&#8211;&#8211;</term></item>
    ///                             <item><term><small><font color="#FF33CC">|V(q)|</font></small></term></item>
    ///                         </list>
    ///                     </term>
    ///                     <term>
    ///                         &#160; &#183; &#160; <font color="#3399FF">doc-len-norm(d)</font>
    ///                         &#160; &#183; &#160; <font color="#3399FF">doc-boost(d)</font>
    ///                     </term>
    ///                 </item>
    ///             </list>
    ///         </term>
    ///     </item>
    ///     <item>
    ///         <term>Lucene Conceptual Scoring Formula</term>
    ///     </item>
    /// </list>
    /// <para/>
    ///
    ///
    /// <para/>The conceptual formula is a simplification in the sense that (1) terms and documents
    /// are fielded and (2) boosts are usually per query term rather than per query.
    ///
    /// <para/>We now describe how Lucene implements this conceptual scoring formula, and
    /// derive from it <i>Lucene's Practical Scoring Function</i>.
    ///
    /// <para/>For efficient score computation some scoring components
    /// are computed and aggregated in advance:
    ///
    /// <list type="bullet">
    ///  <item><description><i>Query-boost</i> for the query (actually for each query term)
    ///  is known when search starts.
    ///  </description></item>
    ///
    ///  <item><description>Query Euclidean norm <i>|V(q)|</i> can be computed when search starts,
    ///  as it is independent of the document being scored.
    ///  From search optimization perspective, it is a valid question
    ///  why bother to normalize the query at all, because all
    ///  scored documents will be multiplied by the same <i>|V(q)|</i>,
    ///  and hence documents ranks (their order by score) will not
    ///  be affected by this normalization.
    ///  There are two good reasons to keep this normalization:
    ///  <list type="bullet">
    ///   <item><description>Recall that
    ///   <a href="http://en.wikipedia.org/wiki/Cosine_similarity">
    ///   Cosine Similarity</a> can be used find how similar
    ///   two documents are. One can use Lucene for e.g.
    ///   clustering, and use a document as a query to compute
    ///   its similarity to other documents.
    ///   In this use case it is important that the score of document <i>d3</i>
    ///   for query <i>d1</i> is comparable to the score of document <i>d3</i>
    ///   for query <i>d2</i>. In other words, scores of a document for two
    ///   distinct queries should be comparable.
    ///   There are other applications that may require this.
    ///   And this is exactly what normalizing the query vector <i>V(q)</i>
    ///   provides: comparability (to a certain extent) of two or more queries.
    ///   </description></item>
    ///
    ///   <item><description>Applying query normalization on the scores helps to keep the
    ///   scores around the unit vector, hence preventing loss of score data
    ///   because of floating point precision limitations.
    ///   </description></item>
    ///  </list>
    ///  </description></item>
    ///
    ///  <item><description>Document length norm <i>doc-len-norm(d)</i> and document
    ///  boost <i>doc-boost(d)</i> are known at indexing time.
    ///  They are computed in advance and their multiplication
    ///  is saved as a single value in the index: <i>norm(d)</i>.
    ///  (In the equations below, <i>norm(t in d)</i> means <i>norm(field(t) in doc d)</i>
    ///  where <i>field(t)</i> is the field associated with term <i>t</i>.)
    ///  </description></item>
    /// </list>
    ///
    /// <para/><i>Lucene's Practical Scoring Function</i> is derived from the above.
    /// The color codes demonstrate how it relates
    /// to those of the <i>conceptual</i> formula:
    ///
    /// <para/>
    /// <list type="table">
    ///     <item>
    ///         <term>
    ///             <list type="table">
    ///                 <item>
    ///                     <term>
    ///                         score(q,d) &#160; = &#160;
    ///                         <a href="#formula_coord"><font color="#FF9933">coord(q,d)</font></a> &#160; &#183; &#160;
    ///                         <a href="#formula_queryNorm"><font color="#FF33CC">queryNorm(q)</font></a> &#160; &#183; &#160;
    ///                     </term>
    ///                     <term><big><big><big>&#8721;</big></big></big></term>
    ///                     <term>
    ///                         <big><big>(</big></big>
    ///                         <a href="#formula_tf"><font color="#993399">tf(t in d)</font></a> &#160; &#183; &#160;
    ///                         <a href="#formula_idf"><font color="#993399">idf(t)</font></a><sup>2</sup> &#160; &#183; &#160;
    ///                         <a href="#formula_termBoost"><font color="#CCCC00">t.Boost</font></a> &#160; &#183; &#160;
    ///                         <a href="#formula_norm"><font color="#3399FF">norm(t,d)</font></a>
    ///                         <big><big>)</big></big>
    ///                     </term>
    ///                 </item>
    ///                 <item>
    ///                     <term></term>
    ///                     <term><small>t in q</small></term>
    ///                     <term></term>
    ///                 </item>
    ///             </list>
    ///         </term>
    ///     </item>
    ///     <item>
    ///         <term>Lucene Practical Scoring Function</term>
    ///     </item>
    /// </list>
    ///
    /// <para/> where
    /// <list type="number">
    ///    <item><description>
    ///      <a name="formula_tf"></a>
    ///      <b><i>tf(t in d)</i></b>
    ///      correlates to the term's <i>frequency</i>,
    ///      defined as the number of times term <i>t</i> appears in the currently scored document <i>d</i>.
    ///      Documents that have more occurrences of a given term receive a higher score.
    ///      Note that <i>tf(t in q)</i> is assumed to be <i>1</i> and therefore it does not appear in this equation,
    ///      However if a query contains twice the same term, there will be
    ///      two term-queries with that same term and hence the computation would still be correct (although
    ///      not very efficient).
    ///      The default computation for <i>tf(t in d)</i> in
    ///      DefaultSimilarity (<see cref="Lucene.Net.Search.Similarities.DefaultSimilarity.Tf(float)"/>) is:
    ///
    ///         <para/>
    ///         <list type="table">
    ///             <item>
    ///                 <term>
    ///                     tf(t in d) &#160; = &#160;
    ///                 </term>
    ///                 <term>
    ///                     frequency<sup><big>&#189;</big></sup>
    ///                 </term>
    ///             </item>
    ///         </list>
    ///         <para/>
    ///         
    ///    </description></item>
    ///
    ///    <item><description>
    ///      <a name="formula_idf"></a>
    ///      <b><i>idf(t)</i></b> stands for Inverse Document Frequency. this value
    ///      correlates to the inverse of <i>DocFreq</i>
    ///      (the number of documents in which the term <i>t</i> appears).
    ///      this means rarer terms give higher contribution to the total score.
    ///      <i>idf(t)</i> appears for <i>t</i> in both the query and the document,
    ///      hence it is squared in the equation.
    ///      The default computation for <i>idf(t)</i> in
    ///      DefaultSimilarity (<see cref="Lucene.Net.Search.Similarities.DefaultSimilarity.Idf(long, long)"/>) is:
    ///
    ///         <para/>
    ///         <list type="table">
    ///             <item>
    ///                 <term>idf(t) &#160; = &#160;</term>
    ///                 <term>1 + log <big>(</big></term>
    ///                 <term>
    ///                     <list type="table">
    ///                         <item><term><small>NumDocs</small></term></item>
    ///                         <item><term>&#8211;&#8211;&#8211;&#8211;&#8211;&#8211;&#8211;&#8211;&#8211;</term></item>
    ///                         <item><term><small>DocFreq+1</small></term></item>
    ///                     </list>
    ///                 </term>
    ///                 <term><big>)</big></term>
    ///             </item>
    ///         </list>
    ///         <para/>
    /// 
    ///    </description></item>
    ///
    ///    <item><description>
    ///      <a name="formula_coord"></a>
    ///      <b><i>coord(q,d)</i></b>
    ///      is a score factor based on how many of the query terms are found in the specified document.
    ///      Typically, a document that contains more of the query's terms will receive a higher score
    ///      than another document with fewer query terms.
    ///      this is a search time factor computed in
    ///      coord(q,d) (<see cref="Coord(int, int)"/>)
    ///      by the Similarity in effect at search time.
    ///      <para/>
    ///    </description></item>
    ///
    ///    <item><description><b>
    ///      <a name="formula_queryNorm"></a>
    ///      <i>queryNorm(q)</i>
    ///      </b>
    ///      is a normalizing factor used to make scores between queries comparable.
    ///      this factor does not affect document ranking (since all ranked documents are multiplied by the same factor),
    ///      but rather just attempts to make scores from different queries (or even different indexes) comparable.
    ///      this is a search time factor computed by the Similarity in effect at search time.
    ///
    ///      The default computation in
    ///      DefaultSimilarity (<see cref="Lucene.Net.Search.Similarities.DefaultSimilarity.QueryNorm(float)"/>)
    ///      produces a <a href="http://en.wikipedia.org/wiki/Euclidean_norm#Euclidean_norm">Euclidean norm</a>:
    ///      
    ///      <para/>
    ///      <list type="table">
    ///         <item>
    ///             <term>
    ///                 queryNorm(q)  &#160; = &#160;
    ///                 queryNorm(sumOfSquaredWeights)
    ///                 &#160; = &#160;
    ///             </term>
    ///             <term>
    ///                 <list type="table">
    ///                     <item><term><big>1</big></term></item>
    ///                     <item><term><big>&#8211;&#8211;&#8211;&#8211;&#8211;&#8211;&#8211;&#8211;&#8211;&#8211;&#8211;&#8211;&#8211;&#8211;</big></term></item>
    ///                     <item><term>sumOfSquaredWeights<sup><big>&#189;</big></sup></term></item>
    ///                 </list>
    ///             </term>
    ///         </item>
    ///      </list>
    ///      <para/>
    ///
    ///      The sum of squared weights (of the query terms) is
    ///      computed by the query <see cref="Lucene.Net.Search.Weight"/> object.
    ///      For example, a <see cref="Lucene.Net.Search.BooleanQuery"/>
    ///      computes this value as:
    ///      
    ///      <para/>
    ///      <list type="table">
    ///         <item>
    ///             <term>
    ///                 sumOfSquaredWeights &#160; = &#160;
    ///                 q.Boost <sup><big>2</big></sup>
    ///                 &#160;&#183;&#160;
    ///             </term> 
    ///             <term><big><big><big>&#8721;</big></big></big></term>
    ///             <term>
    ///                 <big><big>(</big></big>
    ///                 <a href="#formula_idf">idf(t)</a> &#160;&#183;&#160;
    ///                 <a href="#formula_termBoost">t.Boost</a>
    ///                 <big><big>) <sup>2</sup> </big></big>
    ///             </term>
    ///         </item>
    ///         <item>
    ///             <term></term>
    ///             <term><small>t in q</small></term>
    ///             <term></term>
    ///         </item>
    ///      </list>
    ///      where sumOfSquaredWeights is <see cref="Weight.GetValueForNormalization()"/> and
    ///      q.Boost is <see cref="Query.Boost"/>
    ///      <para/>
    ///    </description></item>
    ///
    ///    <item><description>
    ///      <a name="formula_termBoost"></a>
    ///      <b><i>t.Boost</i></b>
    ///      is a search time boost of term <i>t</i> in the query <i>q</i> as
    ///      specified in the query text
    ///      (see <a href="{@docRoot}/../queryparser/org/apache/lucene/queryparser/classic/package-summary.html#Boosting_a_Term">query syntax</a>),
    ///      or as set by application calls to
    ///      <see cref="Lucene.Net.Search.Query.Boost"/>.
    ///      Notice that there is really no direct API for accessing a boost of one term in a multi term query,
    ///      but rather multi terms are represented in a query as multi
    ///      <see cref="Lucene.Net.Search.TermQuery"/> objects,
    ///      and so the boost of a term in the query is accessible by calling the sub-query
    ///      <see cref="Lucene.Net.Search.Query.Boost"/>.
    ///      <para/>
    ///    </description></item>
    ///
    ///    <item><description>
    ///      <a name="formula_norm"></a>
    ///      <b><i>norm(t,d)</i></b> encapsulates a few (indexing time) boost and length factors:
    ///
    ///      <list type="bullet">
    ///        <item><description><b>Field boost</b> - set
    ///        <see cref="Documents.Field.Boost"/>
    ///        before adding the field to a document.
    ///        </description></item>
    ///        <item><description><b>lengthNorm</b> - computed
    ///        when the document is added to the index in accordance with the number of tokens
    ///        of this field in the document, so that shorter fields contribute more to the score.
    ///        LengthNorm is computed by the <see cref="Similarity"/> class in effect at indexing.
    ///        </description></item>
    ///      </list>
    ///      The <see cref="ComputeNorm(FieldInvertState)"/> method is responsible for
    ///      combining all of these factors into a single <see cref="float"/>.
    ///
    ///      <para/>
    ///      When a document is added to the index, all the above factors are multiplied.
    ///      If the document has multiple fields with the same name, all their boosts are multiplied together:
    ///      
    ///      <para/>
    ///      <list type="table">
    ///         <item>
    ///             <term>
    ///                 norm(t,d) &#160; = &#160;
    ///                 lengthNorm
    ///                 &#160;&#183;&#160;
    ///             </term>
    ///             <term><big><big><big>&#8719;</big></big></big></term>
    ///             <term><see cref="Index.IIndexableField.Boost"/></term>
    ///         </item>
    ///         <item>
    ///             <term></term>
    ///             <term><small>field <i><b>f</b></i> in <i>d</i> named as <i><b>t</b></i></small></term>
    ///             <term></term>
    ///         </item>
    ///      </list>
    ///      Note that search time is too late to modify this <i>norm</i> part of scoring,
    ///      e.g. by using a different <see cref="Similarity"/> for search.
    ///    </description></item>
    /// </list>
    /// </summary>
    /// <seealso cref="Lucene.Net.Index.IndexWriterConfig.Similarity"/>
    /// <seealso cref="IndexSearcher.Similarity"/>
    public abstract class TFIDFSimilarity : Similarity
    {
        /// <summary>
        /// Sole constructor. (For invocation by subclass
        /// constructors, typically implicit.)
        /// </summary>
        protected TFIDFSimilarity() // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
        {
        }

        /// <summary>
        /// Computes a score factor based on the fraction of all query terms that a
        /// document contains.  this value is multiplied into scores.
        ///
        /// <para/>The presence of a large portion of the query terms indicates a better
        /// match with the query, so implementations of this method usually return
        /// larger values when the ratio between these parameters is large and smaller
        /// values when the ratio between them is small.
        /// </summary>
        /// <param name="overlap"> The number of query terms matched in the document </param>
        /// <param name="maxOverlap"> The total number of terms in the query </param>
        /// <returns> A score factor based on term overlap with the query </returns>
        public override abstract float Coord(int overlap, int maxOverlap);

        /// <summary>
        /// Computes the normalization value for a query given the sum of the squared
        /// weights of each of the query terms.  this value is multiplied into the
        /// weight of each query term. While the classic query normalization factor is
        /// computed as 1/sqrt(sumOfSquaredWeights), other implementations might
        /// completely ignore sumOfSquaredWeights (ie return 1).
        ///
        /// <para/>This does not affect ranking, but the default implementation does make scores
        /// from different queries more comparable than they would be by eliminating the
        /// magnitude of the <see cref="Query"/> vector as a factor in the score.
        /// </summary>
        /// <param name="sumOfSquaredWeights"> The sum of the squares of query term weights </param>
        /// <returns> A normalization factor for query weights </returns>
        public override abstract float QueryNorm(float sumOfSquaredWeights);

        /// <summary>
        /// Computes a score factor based on a term or phrase's frequency in a
        /// document.  This value is multiplied by the <see cref="Idf(long, long)"/>
        /// factor for each term in the query and these products are then summed to
        /// form the initial score for a document.
        ///
        /// <para/>Terms and phrases repeated in a document indicate the topic of the
        /// document, so implementations of this method usually return larger values
        /// when <paramref name="freq"/> is large, and smaller values when <paramref name="freq"/>
        /// is small.
        /// </summary>
        /// <param name="freq"> The frequency of a term within a document </param>
        /// <returns> A score factor based on a term's within-document frequency </returns>
        public abstract float Tf(float freq);

        /// <summary>
        /// Computes a score factor for a simple term and returns an explanation
        /// for that score factor.
        ///
        /// <para/>
        /// The default implementation uses:
        ///
        /// <code>
        /// Idf(docFreq, searcher.MaxDoc);
        /// </code>
        ///
        /// Note that <see cref="CollectionStatistics.MaxDoc"/> is used instead of
        /// <see cref="Lucene.Net.Index.IndexReader.NumDocs"/> because also
        /// <see cref="TermStatistics.DocFreq"/> is used, and when the latter
        /// is inaccurate, so is <see cref="CollectionStatistics.MaxDoc"/>, and in the same direction.
        /// In addition, <see cref="CollectionStatistics.MaxDoc"/> is more efficient to compute
        /// </summary>
        /// <param name="collectionStats"> Collection-level statistics </param>
        /// <param name="termStats"> Term-level statistics for the term </param>
        /// <returns> An Explain object that includes both an idf score factor
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
        /// <param name="collectionStats"> Collection-level statistics </param>
        /// <param name="termStats"> Term-level statistics for the terms in the phrase </param>
        /// <returns> An Explain object that includes both an idf
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

        /// <summary>
        /// Computes a score factor based on a term's document frequency (the number
        /// of documents which contain the term).  This value is multiplied by the
        /// <see cref="Tf(float)"/> factor for each term in the query and these products are
        /// then summed to form the initial score for a document.
        ///
        /// <para/>Terms that occur in fewer documents are better indicators of topic, so
        /// implementations of this method usually return larger values for rare terms,
        /// and smaller values for common terms.
        /// </summary>
        /// <param name="docFreq"> The number of documents which contain the term </param>
        /// <param name="numDocs"> The total number of documents in the collection </param>
        /// <returns> A score factor based on the term's document frequency </returns>
        public abstract float Idf(long docFreq, long numDocs);

        /// <summary>
        /// Compute an index-time normalization value for this field instance.
        /// <para/>
        /// This value will be stored in a single byte lossy representation by
        /// <see cref="EncodeNormValue(float)"/>.
        /// </summary>
        /// <param name="state"> Statistics of the current field (such as length, boost, etc) </param>
        /// <returns> An index-time normalization value </returns>
        public abstract float LengthNorm(FieldInvertState state);

        public override sealed long ComputeNorm(FieldInvertState state)
        {
            float normValue = LengthNorm(state);
            return EncodeNormValue(normValue);
        }

        /// <summary>
        /// Decodes a normalization factor stored in an index.
        /// </summary>
        /// <see cref="EncodeNormValue(float)"/>
        public abstract float DecodeNormValue(long norm);

        /// <summary>
        /// Encodes a normalization factor for storage in an index. </summary>
        public abstract long EncodeNormValue(float f);

        /// <summary>
        /// Computes the amount of a sloppy phrase match, based on an edit distance.
        /// this value is summed for each sloppy phrase match in a document to form
        /// the frequency to be used in scoring instead of the exact term count.
        ///
        /// <para/>A phrase match with a small edit distance to a document passage more
        /// closely matches the document, so implementations of this method usually
        /// return larger values when the edit distance is small and smaller values
        /// when it is large.
        /// </summary>
        /// <seealso cref="PhraseQuery.Slop"/>
        /// <param name="distance"> The edit distance of this sloppy phrase match </param>
        /// <returns> The frequency increment for this match </returns>
        public abstract float SloppyFreq(int distance);

        /// <summary>
        /// Calculate a scoring factor based on the data in the payload.  Implementations
        /// are responsible for interpreting what is in the payload.  Lucene makes no assumptions about
        /// what is in the byte array.
        /// </summary>
        /// <param name="doc"> The docId currently being scored. </param>
        /// <param name="start"> The start position of the payload </param>
        /// <param name="end"> The end position of the payload </param>
        /// <param name="payload"> The payload byte array to be scored </param>
        /// <returns> An implementation dependent float to be used as a scoring factor </returns>
        public abstract float ScorePayload(int doc, int start, int end, BytesRef payload);

        public override sealed SimWeight ComputeWeight(float queryBoost, CollectionStatistics collectionStats, params TermStatistics[] termStats)
        {
            Explanation idf = termStats.Length == 1 ? IdfExplain(collectionStats, termStats[0]) : IdfExplain(collectionStats, termStats);
            return new IDFStats(collectionStats.Field, idf, queryBoost);
        }

        public override sealed SimScorer GetSimScorer(SimWeight stats, AtomicReaderContext context)
        {
            IDFStats idfstats = (IDFStats)stats;
            return new TFIDFSimScorer(this, idfstats, context.AtomicReader.GetNormValues(idfstats.Field));
        }

        private sealed class TFIDFSimScorer : SimScorer
        {
            private readonly TFIDFSimilarity outerInstance;

            private readonly IDFStats stats;
            private readonly float weightValue;
            private readonly NumericDocValues norms;

            internal TFIDFSimScorer(TFIDFSimilarity outerInstance, IDFStats stats, NumericDocValues norms)
            {
                this.outerInstance = outerInstance;
                this.stats = stats;
                this.weightValue = stats.Value;
                this.norms = norms;
            }

            public override float Score(int doc, float freq)
            {
                float raw = outerInstance.Tf(freq) * weightValue; // compute tf(f)*weight

                return norms is null ? raw : raw * outerInstance.DecodeNormValue(norms.Get(doc)); // normalize for field
            }

            public override float ComputeSlopFactor(int distance)
            {
                return outerInstance.SloppyFreq(distance);
            }

            public override float ComputePayloadFactor(int doc, int start, int end, BytesRef payload)
            {
                return outerInstance.ScorePayload(doc, start, end, payload);
            }

            public override Explanation Explain(int doc, Explanation freq)
            {
                return outerInstance.ExplainScore(doc, freq, stats, norms);
            }
        }

        /// <summary>
        /// Collection statistics for the TF-IDF model. The only statistic of interest
        /// to this model is idf.
        /// </summary>
        [ExceptionToClassNameConvention]
        private class IDFStats : SimWeight
        {
            internal string Field { get; private set; }

            /// <summary>
            /// The idf and its explanation </summary>
            internal Explanation Idf { get; private set; }

            internal float QueryNorm { get; set; }
            internal float QueryWeight { get; set; }
            internal float QueryBoost { get; private set; }
            internal float Value { get; set; }

            public IDFStats(string field, Explanation idf, float queryBoost)
            {
                // TODO: Validate?
                this.Field = field;
                this.Idf = idf;
                this.QueryBoost = queryBoost;
                this.QueryWeight = idf.Value * queryBoost; // compute query weight
            }

            public override float GetValueForNormalization()
            {
                // TODO: (sorta LUCENE-1907) make non-static class and expose this squaring via a nice method to subclasses?
                return QueryWeight * QueryWeight; // sum of squared weights
            }

            public override void Normalize(float queryNorm, float topLevelBoost)
            {
                this.QueryNorm = queryNorm * topLevelBoost;
                QueryWeight *= this.QueryNorm; // normalize query weight
                Value = QueryWeight * Idf.Value; // idf for document
            }
        }

        private Explanation ExplainScore(int doc, Explanation freq, IDFStats stats, NumericDocValues norms)
        {
            Explanation result = new Explanation();
            // LUCENENET specific - using freq.Value is a change that was made in Lucene 5.0, but is included
            // in 4.8.0 to remove annoying newlines from the output.
            // See: https://github.com/apache/lucene-solr/commit/f0bfcbc7d8fbc5bb2791da60af559e8b0ad6eed6
            result.Description = "score(doc=" + doc + ",freq=" + freq.Value + "), product of:";

            // explain query weight
            Explanation queryExpl = new Explanation();
            queryExpl.Description = "queryWeight, product of:";

            Explanation boostExpl = new Explanation(stats.QueryBoost, "boost");
            if (stats.QueryBoost != 1.0f)
            {
                queryExpl.AddDetail(boostExpl);
            }
            queryExpl.AddDetail(stats.Idf);

            Explanation queryNormExpl = new Explanation(stats.QueryNorm, "queryNorm");
            queryExpl.AddDetail(queryNormExpl);

            queryExpl.Value = boostExpl.Value * stats.Idf.Value * queryNormExpl.Value;

            result.AddDetail(queryExpl);

            // explain field weight
            Explanation fieldExpl = new Explanation();
            fieldExpl.Description = "fieldWeight in " + doc + ", product of:";

            Explanation tfExplanation = new Explanation();
            tfExplanation.Value = Tf(freq.Value);
            tfExplanation.Description = "tf(freq=" + freq.Value + "), with freq of:";
            tfExplanation.AddDetail(freq);
            fieldExpl.AddDetail(tfExplanation);
            fieldExpl.AddDetail(stats.Idf);

            Explanation fieldNormExpl = new Explanation();
            float fieldNorm = norms != null ? DecodeNormValue(norms.Get(doc)) : 1.0f;
            fieldNormExpl.Value = fieldNorm;
            fieldNormExpl.Description = "fieldNorm(doc=" + doc + ")";
            fieldExpl.AddDetail(fieldNormExpl);

            fieldExpl.Value = tfExplanation.Value * stats.Idf.Value * fieldNormExpl.Value;

            result.AddDetail(fieldExpl);

            // combine them
            result.Value = queryExpl.Value * fieldExpl.Value;

            if (queryExpl.Value == 1.0f)
            {
                return fieldExpl;
            }

            return result;
        }
    }
}
