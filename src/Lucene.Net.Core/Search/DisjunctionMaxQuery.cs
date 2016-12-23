using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search
{
    using Lucene.Net.Support;
    using System.Collections;

    /// <summary>
    /// Copyright 2004 The Apache Software Foundation
    ///
    /// Licensed under the Apache License, Version 2.0 (the "License");
    /// you may not use this file except in compliance with the License.
    /// You may obtain a copy of the License at
    ///
    ///     http://www.apache.org/licenses/LICENSE-2.0
    ///
    /// Unless required by applicable law or agreed to in writing, software
    /// distributed under the License is distributed on an "AS IS" BASIS,
    /// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    /// See the License for the specific language governing permissions and
    /// limitations under the License.
    /// </summary>

    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using Bits = Lucene.Net.Util.Bits;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using Term = Lucene.Net.Index.Term;

    /// <summary>
    /// A query that generates the union of documents produced by its subqueries, and that scores each document with the maximum
    /// score for that document as produced by any subquery, plus a tie breaking increment for any additional matching subqueries.
    /// this is useful when searching for a word in multiple fields with different boost factors (so that the fields cannot be
    /// combined equivalently into a single search field).  We want the primary score to be the one associated with the highest boost,
    /// not the sum of the field scores (as BooleanQuery would give).
    /// If the query is "albino elephant" this ensures that "albino" matching one field and "elephant" matching
    /// another gets a higher score than "albino" matching both fields.
    /// To get this result, use both BooleanQuery and DisjunctionMaxQuery:  for each term a DisjunctionMaxQuery searches for it in
    /// each field, while the set of these DisjunctionMaxQuery's is combined into a BooleanQuery.
    /// The tie breaker capability allows results that include the same term in multiple fields to be judged better than results that
    /// include this term in only the best of those multiple fields, without confusing this with the better case of two different terms
    /// in the multiple fields.
    /// </summary>
    public class DisjunctionMaxQuery : Query, IEnumerable<Query>
    {
        /* The subqueries */
        private EquatableList<Query> disjuncts = new EquatableList<Query>();

        /* Multiple of the non-max disjunct scores added into our final score.  Non-zero values support tie-breaking. */
        private float tieBreakerMultiplier = 0.0f;

        /// <summary>
        /// Creates a new empty DisjunctionMaxQuery.  Use add() to add the subqueries. </summary>
        /// <param name="tieBreakerMultiplier"> the score of each non-maximum disjunct for a document is multiplied by this weight
        ///        and added into the final score.  If non-zero, the value should be small, on the order of 0.1, which says that
        ///        10 occurrences of word in a lower-scored field that is also in a higher scored field is just as good as a unique
        ///        word in the lower scored field (i.e., one that is not in any higher scored field. </param>
        public DisjunctionMaxQuery(float tieBreakerMultiplier)
        {
            this.tieBreakerMultiplier = tieBreakerMultiplier;
        }

        /// <summary>
        /// Creates a new DisjunctionMaxQuery </summary>
        /// <param name="disjuncts"> a {@code Collection<Query>} of all the disjuncts to add </param>
        /// <param name="tieBreakerMultiplier">   the weight to give to each matching non-maximum disjunct </param>
        public DisjunctionMaxQuery(ICollection<Query> disjuncts, float tieBreakerMultiplier)
        {
            this.tieBreakerMultiplier = tieBreakerMultiplier;
            Add(disjuncts);
        }

        /// <summary>
        /// Add a subquery to this disjunction </summary>
        /// <param name="query"> the disjunct added </param>
        public virtual void Add(Query query)
        {
            disjuncts.Add(query);
        }

        /// <summary>
        /// Add a collection of disjuncts to this disjunction
        /// via {@code Iterable<Query>} </summary>
        /// <param name="disjuncts"> a collection of queries to add as disjuncts. </param>
        public virtual void Add(ICollection<Query> disjuncts)
        {
            this.disjuncts.AddRange(disjuncts);
        }

        /// <returns> An {@code Iterator<Query>} over the disjuncts </returns>
        public virtual IEnumerator<Query> GetEnumerator()
        {
            return disjuncts.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <returns> the disjuncts. </returns>
        public virtual List<Query> Disjuncts
        {
            get
            {
                return disjuncts;
            }
        }

        /// <returns> tie breaker value for multiple matches. </returns>
        public virtual float TieBreakerMultiplier
        {
            get
            {
                return tieBreakerMultiplier;
            }
        }

        /// <summary>
        /// Expert: the Weight for DisjunctionMaxQuery, used to
        /// normalize, score and explain these queries.
        ///
        /// <p>NOTE: this API and implementation is subject to
        /// change suddenly in the next release.</p>
        /// </summary>
        protected class DisjunctionMaxWeight : Weight
        {
            private readonly DisjunctionMaxQuery OuterInstance; // LUCENENET TODO: Rename (private)

            /// <summary>
            /// The Weights for our subqueries, in 1-1 correspondence with disjuncts </summary>
             // LUCENENET TODO: Rename
            protected List<Weight> Weights = new List<Weight>(); // The Weight's for our subqueries, in 1-1 correspondence with disjuncts

            /// <summary>
            /// Construct the Weight for this Query searched by searcher.  Recursively construct subquery weights. </summary>
            public DisjunctionMaxWeight(DisjunctionMaxQuery outerInstance, IndexSearcher searcher)
            {
                this.OuterInstance = outerInstance;
                foreach (Query disjunctQuery in outerInstance.disjuncts)
                {
                    Weights.Add(disjunctQuery.CreateWeight(searcher));
                }
            }

            /// <summary>
            /// Return our associated DisjunctionMaxQuery </summary>
            public override Query Query
            {
                get
                /// <summary>
                /// Compute the sub of squared weights of us applied to our subqueries.  Used for normalization. </summary>
                {
                    return OuterInstance;
                }
            }

            public override float ValueForNormalization
            {
                get
                {
                    float max = 0.0f, sum = 0.0f;
                    foreach (Weight currentWeight in Weights)
                    {
                        float sub = currentWeight.ValueForNormalization;
                        sum += sub;
                        max = Math.Max(max, sub);
                    }
                    float boost = OuterInstance.Boost;
                    return (((sum - max) * OuterInstance.tieBreakerMultiplier * OuterInstance.tieBreakerMultiplier) + max) * boost * boost;
                }
            }

            /// <summary>
            /// Apply the computed normalization factor to our subqueries </summary>
            public override void Normalize(float norm, float topLevelBoost)
            {
                topLevelBoost *= OuterInstance.Boost; // Incorporate our boost
                foreach (Weight wt in Weights)
                {
                    wt.Normalize(norm, topLevelBoost);
                }
            }

            /// <summary>
            /// Create the scorer used to score our associated DisjunctionMaxQuery </summary>
            public override Scorer Scorer(AtomicReaderContext context, Bits acceptDocs)
            {
                IList<Scorer> scorers = new List<Scorer>();
                foreach (Weight w in Weights)
                {
                    // we will advance() subscorers
                    Scorer subScorer = w.Scorer(context, acceptDocs);
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
                DisjunctionMaxScorer result = new DisjunctionMaxScorer(this, OuterInstance.tieBreakerMultiplier, scorers.ToArray());
                return result;
            }

            /// <summary>
            /// Explain the score we computed for doc </summary>
            public override Explanation Explain(AtomicReaderContext context, int doc)
            {
                if (OuterInstance.disjuncts.Count == 1)
                {
                    return Weights[0].Explain(context, doc);
                }
                ComplexExplanation result = new ComplexExplanation();
                float max = 0.0f, sum = 0.0f;
                result.Description = OuterInstance.tieBreakerMultiplier == 0.0f ? "max of:" : "max plus " + OuterInstance.tieBreakerMultiplier + " times others of:";
                foreach (Weight wt in Weights)
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
                result.Value = max + (sum - max) * OuterInstance.tieBreakerMultiplier;
                return result;
            }
        } // end of DisjunctionMaxWeight inner class

        /// <summary>
        /// Create the Weight used to score us </summary>
        public override Weight CreateWeight(IndexSearcher searcher)
        {
            return new DisjunctionMaxWeight(this, searcher);
        }

        /// <summary>
        /// Optimize our representation and our subqueries representations </summary>
        /// <param name="reader"> the IndexReader we query </param>
        /// <returns> an optimized copy of us (which may not be a copy if there is nothing to optimize)  </returns>
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
                    if (clone == null)
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
        /// <returns> a copy of us (but reuse, don't copy, our subqueries)  </returns>
        public override object Clone()
        {
            DisjunctionMaxQuery clone = (DisjunctionMaxQuery)base.Clone();
            clone.disjuncts = (EquatableList<Query>)this.disjuncts.Clone();
            return clone;
        }

        // inherit javadoc
        public override void ExtractTerms(ISet<Term> terms)
        {
            foreach (Query query in disjuncts)
            {
                query.ExtractTerms(terms);
            }
        }

        /// <summary>
        /// Prettyprint us. </summary>
        /// <param name="field"> the field to which we are applied </param>
        /// <returns> a string that shows what we do, of the form "(disjunct1 | disjunct2 | ... | disjunctn)^boost" </returns>
        public override string ToString(string field)
        {
            StringBuilder buffer = new StringBuilder();
            buffer.Append("(");
            int numDisjunctions = disjuncts.Count;
            for (int i = 0; i < numDisjunctions; i++)
            {
                Query subquery = disjuncts[i];
                if (subquery is BooleanQuery) // wrap sub-bools in parens
                {
                    buffer.Append("(");
                    buffer.Append(subquery.ToString(field));
                    buffer.Append(")");
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
            buffer.Append(")");
            if (tieBreakerMultiplier != 0.0f)
            {
                buffer.Append("~");
                buffer.Append(tieBreakerMultiplier);
            }
            if (Boost != 1.0)
            {
                buffer.Append("^");
                buffer.Append(Boost);
            }
            return buffer.ToString();
        }

        /// <summary>
        /// Return true iff we represent the same query as o </summary>
        /// <param name="o"> another object </param>
        /// <returns> true iff o is a DisjunctionMaxQuery with the same boost and the same subqueries, in the same order, as us </returns>
        public override bool Equals(object o)
        {
            if (!(o is DisjunctionMaxQuery))
            {
                return false;
            }
            DisjunctionMaxQuery other = (DisjunctionMaxQuery)o;
            return this.Boost == other.Boost 
                && this.tieBreakerMultiplier == other.tieBreakerMultiplier 
                && this.disjuncts.Equals(other.disjuncts);
        }

        /// <summary>
        /// Compute a hash code for hashing us </summary>
        /// <returns> the hash code </returns>
        public override int GetHashCode()
        {
            return Number.FloatToIntBits(Boost) 
                + Number.FloatToIntBits(tieBreakerMultiplier) 
                + disjuncts.GetHashCode();
        }
    }
}