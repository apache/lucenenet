using J2N.Collections.Generic.Extensions;
using Lucene.Net.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using JCG = J2N.Collections.Generic;

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
    using IBits = Lucene.Net.Util.IBits;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using Term = Lucene.Net.Index.Term;

    /// <summary>
    /// A query that generates the union of documents produced by its subqueries, and that scores each document with the maximum
    /// score for that document as produced by any subquery, plus a tie breaking increment for any additional matching subqueries.
    /// This is useful when searching for a word in multiple fields with different boost factors (so that the fields cannot be
    /// combined equivalently into a single search field).  We want the primary score to be the one associated with the highest boost,
    /// not the sum of the field scores (as <see cref="BooleanQuery"/> would give).
    /// <para/>
    /// If the query is "albino elephant" this ensures that "albino" matching one field and "elephant" matching
    /// another gets a higher score than "albino" matching both fields.
    /// <para/>
    /// To get this result, use both <see cref="BooleanQuery"/> and <see cref="DisjunctionMaxQuery"/>:  for each term a <see cref="DisjunctionMaxQuery"/> searches for it in
    /// each field, while the set of these <see cref="DisjunctionMaxQuery"/>'s is combined into a <see cref="BooleanQuery"/>.
    /// The tie breaker capability allows results that include the same term in multiple fields to be judged better than results that
    /// include this term in only the best of those multiple fields, without confusing this with the better case of two different terms
    /// in the multiple fields.
    /// <para/>
    /// Collection initializer note: To create and populate a <see cref="DisjunctionMaxQuery"/>
    /// in a single statement, you can use the following example as a guide:
    /// 
    /// <code>
    /// var disjunctionMaxQuery = new DisjunctionMaxQuery(0.1f) {
    ///     new TermQuery(new Term("field1", "albino")), 
    ///     new TermQuery(new Term("field2", "elephant"))
    /// };
    /// </code>
    /// </summary>
    public class DisjunctionMaxQuery : Query, IEnumerable<Query>
    {
        /// <summary>
        /// The subqueries
        /// </summary>
        private IList<Query> disjuncts = new JCG.List<Query>();

        /// <summary>
        /// Multiple of the non-max disjunct scores added into our final score.  Non-zero values support tie-breaking.
        /// </summary>
        private readonly float tieBreakerMultiplier = 0.0f;

        /// <summary>
        /// Creates a new empty <see cref="DisjunctionMaxQuery"/>.  Use <see cref="Add(Query)"/> to add the subqueries. </summary>
        /// <param name="tieBreakerMultiplier"> The score of each non-maximum disjunct for a document is multiplied by this weight
        ///        and added into the final score.  If non-zero, the value should be small, on the order of 0.1, which says that
        ///        10 occurrences of word in a lower-scored field that is also in a higher scored field is just as good as a unique
        ///        word in the lower scored field (i.e., one that is not in any higher scored field). </param>
        public DisjunctionMaxQuery(float tieBreakerMultiplier)
        {
            this.tieBreakerMultiplier = tieBreakerMultiplier;
        }

        /// <summary>
        /// Creates a new <see cref="DisjunctionMaxQuery"/> </summary>
        /// <param name="disjuncts"> A <see cref="T:ICollection{Query}"/> of all the disjuncts to add </param>
        /// <param name="tieBreakerMultiplier"> The weight to give to each matching non-maximum disjunct </param>
        public DisjunctionMaxQuery(ICollection<Query> disjuncts, float tieBreakerMultiplier)
        {
            this.tieBreakerMultiplier = tieBreakerMultiplier;
            AddInternal(disjuncts); // LUCENENET specific - calling private instead of virtual
        }

        /// <summary>
        /// Add a subquery to this disjunction </summary>
        /// <param name="query"> The disjunct added </param>
        public virtual void Add(Query query)
        {
            disjuncts.Add(query);
        }

        /// <summary>
        /// Add a collection of disjuncts to this disjunction
        /// via <see cref="T:IEnumerable{Query}"/> 
        ///
        /// NOTE: When overriding this method, be aware that the constructor of this class calls 
        /// a private method and not this virtual method. So if you need to override
        /// the behavior during the initialization, call your own private method from the constructor
        /// with whatever custom behavior you need.
        /// </summary>
        /// <param name="disjuncts"> A collection of queries to add as disjuncts. </param>
        public virtual void Add(ICollection<Query> disjuncts) =>
            AddInternal(disjuncts);

        // LUCENENET specific - S1699 - introduced private AddInternal that
        // is called from virtual Add and constructor
        private void AddInternal(ICollection<Query> disjuncts) =>
            this.disjuncts.AddRange(disjuncts);

        /// <returns> An <see cref="T:IEnumerator{Query}"/> over the disjuncts </returns>
        public virtual IEnumerator<Query> GetEnumerator()
        {
            return disjuncts.GetEnumerator();
        }

        // LUCENENET specific
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <returns> The disjuncts. </returns>
        public virtual IList<Query> Disjuncts => disjuncts;

        /// <returns> Tie breaker value for multiple matches. </returns>
        public virtual float TieBreakerMultiplier => tieBreakerMultiplier;

        /// <summary>
        /// Expert: the Weight for DisjunctionMaxQuery, used to
        /// normalize, score and explain these queries.
        ///
        /// <para>NOTE: this API and implementation is subject to
        /// change suddenly in the next release.</para>
        /// </summary>
        protected class DisjunctionMaxWeight : Weight
        {
            private readonly DisjunctionMaxQuery outerInstance;

            /// <summary>
            /// The <see cref="Weight"/>s for our subqueries, in 1-1 correspondence with disjuncts </summary>
            protected IList<Weight> m_weights = new JCG.List<Weight>(); // The Weight's for our subqueries, in 1-1 correspondence with disjuncts

            /// <summary>
            /// Construct the <see cref="Weight"/> for this <see cref="Search.Query"/> searched by <paramref name="searcher"/>.  Recursively construct subquery weights. </summary>
            public DisjunctionMaxWeight(DisjunctionMaxQuery outerInstance, IndexSearcher searcher)
            {
                this.outerInstance = outerInstance;
                foreach (Query disjunctQuery in outerInstance.disjuncts)
                {
                    m_weights.Add(disjunctQuery.CreateWeight(searcher));
                }
            }

            /// <summary>
            /// Return our associated <see cref="DisjunctionMaxQuery"/> </summary>
            public override Query Query => outerInstance;

            /// <summary>
            /// Compute the sub of squared weights of us applied to our subqueries.  Used for normalization. </summary>
            public override float GetValueForNormalization()
            {
                float max = 0.0f, sum = 0.0f;
                foreach (Weight currentWeight in m_weights)
                {
                    float sub = currentWeight.GetValueForNormalization();
                    sum += sub;
                    max = Math.Max(max, sub);
                }
                float boost = outerInstance.Boost;
                return (((sum - max) * outerInstance.tieBreakerMultiplier * outerInstance.tieBreakerMultiplier) + max) * boost * boost;
            }

            /// <summary>
            /// Apply the computed normalization factor to our subqueries </summary>
            public override void Normalize(float norm, float topLevelBoost)
            {
                topLevelBoost *= outerInstance.Boost; // Incorporate our boost
                foreach (Weight wt in m_weights)
                {
                    wt.Normalize(norm, topLevelBoost);
                }
            }

            /// <summary>
            /// Create the scorer used to score our associated <see cref="DisjunctionMaxQuery"/> </summary>
            public override Scorer GetScorer(AtomicReaderContext context, IBits acceptDocs)
            {
                IList<Scorer> scorers = new JCG.List<Scorer>();
                foreach (Weight w in m_weights)
                {
                    // we will advance() subscorers
                    Scorer subScorer = w.GetScorer(context, acceptDocs);
                    if (subScorer != null)
                    {
                        scorers.Add(subScorer);
                    }
                }
                if (scorers.Count == 0)
                {
                    // no sub-scorers had any documents
                    return null;
                }
                DisjunctionMaxScorer result = new DisjunctionMaxScorer(this, outerInstance.tieBreakerMultiplier, scorers.ToArray());
                return result;
            }

            /// <summary>
            /// Explain the score we computed for doc </summary>
            public override Explanation Explain(AtomicReaderContext context, int doc)
            {
                if (outerInstance.disjuncts.Count == 1)
                {
                    return m_weights[0].Explain(context, doc);
                }
                ComplexExplanation result = new ComplexExplanation();
                float max = 0.0f, sum = 0.0f;
                result.Description = outerInstance.tieBreakerMultiplier == 0.0f ? "max of:" : "max plus " + outerInstance.tieBreakerMultiplier + " times others of:";
                foreach (Weight wt in m_weights)
                {
                    Explanation e = wt.Explain(context, doc);
                    if (e.IsMatch)
                    {
                        result.Match = true;
                        result.AddDetail(e);
                        sum += e.Value;
                        max = Math.Max(max, e.Value);
                    }
                }
                result.Value = max + (sum - max) * outerInstance.tieBreakerMultiplier;
                return result;
            }
        } // end of DisjunctionMaxWeight inner class

        /// <summary>
        /// Create the <see cref="Weight"/> used to score us </summary>
        public override Weight CreateWeight(IndexSearcher searcher)
        {
            return new DisjunctionMaxWeight(this, searcher);
        }

        /// <summary>
        /// Optimize our representation and our subqueries representations </summary>
        /// <param name="reader"> The <see cref="IndexReader"/> we query </param>
        /// <returns> An optimized copy of us (which may not be a copy if there is nothing to optimize)  </returns>
        public override Query Rewrite(IndexReader reader)
        {
            int numDisjunctions = disjuncts.Count;
            if (numDisjunctions == 1)
            {
                Query singleton = disjuncts[0];
                Query result = singleton.Rewrite(reader);
                if (Boost != 1.0f)
                {
                    if (result == singleton)
                    {
                        result = (Query)result.Clone();
                    }
                    result.Boost = Boost * result.Boost;
                }
                return result;
            }
            DisjunctionMaxQuery clone = null;
            for (int i = 0; i < numDisjunctions; i++)
            {
                Query clause = disjuncts[i];
                Query rewrite = clause.Rewrite(reader);
                if (rewrite != clause)
                {
                    if (clone is null)
                    {
                        clone = (DisjunctionMaxQuery)this.Clone();
                    }
                    clone.disjuncts[i] = rewrite;
                }
            }
            if (clone != null)
            {
                return clone;
            }
            else
            {
                return this;
            }
        }

        /// <summary>
        /// Create a shallow copy of us -- used in rewriting if necessary </summary>
        /// <returns> A copy of us (but reuse, don't copy, our subqueries)  </returns>
        public override object Clone()
        {
            DisjunctionMaxQuery clone = (DisjunctionMaxQuery)base.Clone();
            clone.disjuncts = new JCG.List<Query>(this.disjuncts);
            return clone;
        }

        /// <summary>
        /// Expert: adds all terms occurring in this query to the terms set. Only
        /// works if this query is in its rewritten (<see cref="Rewrite(IndexReader)"/>) form.
        /// </summary>
        /// <exception cref="InvalidOperationException"> If this query is not yet rewritten </exception>
        public override void ExtractTerms(ISet<Term> terms)
        {
            foreach (Query query in disjuncts)
            {
                query.ExtractTerms(terms);
            }
        }

        /// <summary>
        /// Prettyprint us. </summary>
        /// <param name="field"> The field to which we are applied </param>
        /// <returns> A string that shows what we do, of the form "(disjunct1 | disjunct2 | ... | disjunctn)^boost" </returns>
        public override string ToString(string field)
        {
            StringBuilder buffer = new StringBuilder();
            buffer.Append('(');
            int numDisjunctions = disjuncts.Count;
            for (int i = 0; i < numDisjunctions; i++)
            {
                Query subquery = disjuncts[i];
                if (subquery is BooleanQuery) // wrap sub-bools in parens
                {
                    buffer.Append('(');
                    buffer.Append(subquery.ToString(field));
                    buffer.Append(')');
                }
                else
                {
                    buffer.Append(subquery.ToString(field));
                }
                if (i != numDisjunctions - 1)
                {
                    buffer.Append(" | ");
                }
            }
            buffer.Append(')');
            if (tieBreakerMultiplier != 0.0f)
            {
                buffer.Append('~');
                buffer.Append(tieBreakerMultiplier);
            }
            if (Boost != 1.0)
            {
                buffer.Append('^');
                buffer.Append(Boost);
            }
            return buffer.ToString();
        }

        /// <summary>
        /// Return <c>true</c> if we represent the same query as <paramref name="o"/> </summary>
        /// <param name="o"> Another object </param>
        /// <returns> <c>true</c> if <paramref name="o"/> is a <see cref="DisjunctionMaxQuery"/> with the same boost and the same subqueries, in the same order, as us </returns>
        public override bool Equals(object o)
        {
            if (!(o is DisjunctionMaxQuery))
            {
                return false;
            }
            DisjunctionMaxQuery other = (DisjunctionMaxQuery)o;
            // LUCENENET specific - compare bits rather than using equality operators to prevent these comparisons from failing in x86 in .NET Framework with optimizations enabled
            return NumericUtils.SingleToSortableInt32(this.Boost) == NumericUtils.SingleToSortableInt32(other.Boost)
                && NumericUtils.SingleToSortableInt32(this.tieBreakerMultiplier) == NumericUtils.SingleToSortableInt32(other.tieBreakerMultiplier)
                && this.disjuncts.Equals(other.disjuncts);
        }

        /// <summary>
        /// Compute a hash code for hashing us </summary>
        /// <returns> the hash code </returns>
        public override int GetHashCode()
        {
            return J2N.BitConversion.SingleToInt32Bits(Boost) 
                + J2N.BitConversion.SingleToInt32Bits(tieBreakerMultiplier) 
                + disjuncts.GetHashCode();
        }
    }
}