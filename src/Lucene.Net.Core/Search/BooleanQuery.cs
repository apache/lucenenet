using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search
{
    using Lucene.Net.Index;
    using Lucene.Net.Support;
    using System.Collections;

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
    using Bits = Lucene.Net.Util.Bits;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using Occur_e = Lucene.Net.Search.BooleanClause.Occur;
    using Similarity = Lucene.Net.Search.Similarities.Similarity;
    using Term = Lucene.Net.Index.Term;
    using ToStringUtils = Lucene.Net.Util.ToStringUtils;

    /// <summary>
    /// A Query that matches documents matching boolean combinations of other
    /// queries, e.g. <seealso cref="TermQuery"/>s, <seealso cref="PhraseQuery"/>s or other
    /// BooleanQuerys.
    /// </summary>
    public class BooleanQuery : Query, IEnumerable<BooleanClause>
    {
        private static int maxClauseCount = 1024;

        /// <summary>
        /// Thrown when an attempt is made to add more than {@link
        /// #getMaxClauseCount()} clauses. this typically happens if
        /// a PrefixQuery, FuzzyQuery, WildcardQuery, or TermRangeQuery
        /// is expanded to many terms during search.
        /// </summary>
        public class TooManyClauses : Exception
        {
            public TooManyClauses()
                : base("maxClauseCount is set to " + maxClauseCount)
            {
            }
        }

        /// <summary>
        /// Return the maximum number of clauses permitted, 1024 by default.
        /// Attempts to add more than the permitted number of clauses cause {@link
        /// TooManyClauses} to be thrown. </summary>
        /// <seealso cref= #setMaxClauseCount(int) </seealso>
        public static int MaxClauseCount
        {
            get
            {
                return maxClauseCount;
            }
            set
            {
                if (value < 1)
                {
                    throw new System.ArgumentException("maxClauseCount must be >= 1");
                }
                BooleanQuery.maxClauseCount = value;
            }
        }

        private EquatableList<BooleanClause> clauses = new EquatableList<BooleanClause>();
        private readonly bool DisableCoord;

        /// <summary>
        /// Constructs an empty boolean query. </summary>
        public BooleanQuery()
        {
            DisableCoord = false;
        }

        /// <summary>
        /// Constructs an empty boolean query.
        ///
        /// <seealso cref="Similarity#coord(int,int)"/> may be disabled in scoring, as
        /// appropriate. For example, this score factor does not make sense for most
        /// automatically generated queries, like <seealso cref="WildcardQuery"/> and {@link
        /// FuzzyQuery}.
        /// </summary>
        /// <param name="disableCoord"> disables <seealso cref="Similarity#coord(int,int)"/> in scoring. </param>
        public BooleanQuery(bool disableCoord)
        {
            this.DisableCoord = disableCoord;
        }

        /// <summary>
        /// Returns true iff <seealso cref="Similarity#coord(int,int)"/> is disabled in
        /// scoring for this query instance. </summary>
        /// <seealso cref= #BooleanQuery(boolean) </seealso>
        public virtual bool CoordDisabled
        {
            get
            {
                return DisableCoord;
            }
        }

        /// <summary>
        /// Specifies a minimum number of the optional BooleanClauses
        /// which must be satisfied.
        ///
        /// <p>
        /// By default no optional clauses are necessary for a match
        /// (unless there are no required clauses).  If this method is used,
        /// then the specified number of clauses is required.
        /// </p>
        /// <p>
        /// Use of this method is totally independent of specifying that
        /// any specific clauses are required (or prohibited).  this number will
        /// only be compared against the number of matching optional clauses.
        /// </p>
        /// </summary>
        /// <param name="min"> the number of optional clauses that must match </param>
        public virtual int MinimumNumberShouldMatch
        {
            set
            {
                this.MinNrShouldMatch = value;
            }
            get
            {
                return MinNrShouldMatch;
            }
        }

        protected internal int MinNrShouldMatch = 0;

        /// <summary>
        /// Adds a clause to a boolean query.
        /// </summary>
        /// <exception cref="TooManyClauses"> if the new number of clauses exceeds the maximum clause number </exception>
        /// <seealso cref= #getMaxClauseCount() </seealso>
        public virtual void Add(Query query, BooleanClause.Occur occur)
        {
            Add(new BooleanClause(query, occur));
        }

        /// <summary>
        /// Adds a clause to a boolean query. </summary>
        /// <exception cref="TooManyClauses"> if the new number of clauses exceeds the maximum clause number </exception>
        /// <seealso cref= #getMaxClauseCount() </seealso>
        public virtual void Add(BooleanClause clause)
        {
            if (clauses.Count >= maxClauseCount)
            {
                throw new TooManyClauses();
            }

            clauses.Add(clause);
        }

        /// <summary>
        /// Returns the set of clauses in this query. </summary>
        public virtual BooleanClause[] Clauses
        {
            get
            {
                return clauses.ToArray();
            }
        }

        /// <summary>
        /// Returns the list of clauses in this query. </summary>
        public virtual IList<BooleanClause> GetClauses()
        {
            return clauses;
        }

        /// <summary>
        /// Returns an iterator on the clauses in this query. It implements the <seealso cref="Iterable"/> interface to
        /// make it possible to do:
        /// <pre class="prettyprint">for (BooleanClause clause : booleanQuery) {}</pre>
        /// </summary>
        public IEnumerator<BooleanClause> GetEnumerator()
        {
            return GetClauses().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Expert: the Weight for BooleanQuery, used to
        /// normalize, score and explain these queries.
        ///
        /// @lucene.experimental
        /// </summary>
        public class BooleanWeight : Weight
        {
            private readonly BooleanQuery OuterInstance;

            /// <summary>
            /// The Similarity implementation. </summary>
            protected internal Similarity similarity;

            protected internal List<Weight> Weights;
            protected internal int maxCoord; // num optional + num required
            internal readonly bool DisableCoord;

            public BooleanWeight(BooleanQuery outerInstance, IndexSearcher searcher, bool disableCoord)
            {
                this.OuterInstance = outerInstance;
                this.similarity = searcher.Similarity;
                this.DisableCoord = disableCoord;
                Weights = new List<Weight>(outerInstance.clauses.Count);
                for (int i = 0; i < outerInstance.clauses.Count; i++)
                {
                    BooleanClause c = outerInstance.clauses[i];
                    Weight w = c.Query.CreateWeight(searcher);
                    Weights.Add(w);
                    if (!c.Prohibited)
                    {
                        maxCoord++;
                    }
                }
            }

            public Similarity Similarity
            {
                get { return similarity; }
            }

            public int MaxCoord
            {
                get { return maxCoord; }
            }

            public override Query Query
            {
                get
                {
                    return OuterInstance;
                }
            }

            public override float ValueForNormalization
            {
                get
                {
                    float sum = 0.0f;
                    for (int i = 0; i < Weights.Count; i++)
                    {
                        // call sumOfSquaredWeights for all clauses in case of side effects
                        float s = Weights[i].ValueForNormalization; // sum sub weights
                        if (!OuterInstance.clauses[i].Prohibited)
                        {
                            // only add to sum for non-prohibited clauses
                            sum += s;
                        }
                    }

                    sum *= OuterInstance.Boost * OuterInstance.Boost; // boost each sub-weight

                    return sum;
                }
            }

            public virtual float Coord(int overlap, int maxOverlap)
            {
                // LUCENE-4300: in most cases of maxOverlap=1, BQ rewrites itself away,
                // so coord() is not applied. But when BQ cannot optimize itself away
                // for a single clause (minNrShouldMatch, prohibited clauses, etc), its
                // important not to apply coord(1,1) for consistency, it might not be 1.0F
                return maxOverlap == 1 ? 1F : similarity.Coord(overlap, maxOverlap);
            }

            public override void Normalize(float norm, float topLevelBoost)
            {
                topLevelBoost *= OuterInstance.Boost; // incorporate boost
                foreach (Weight w in Weights)
                {
                    // normalize all clauses, (even if prohibited in case of side affects)
                    w.Normalize(norm, topLevelBoost);
                }
            }

            public override Explanation Explain(AtomicReaderContext context, int doc)
            {
                int minShouldMatch = OuterInstance.MinimumNumberShouldMatch;
                ComplexExplanation sumExpl = new ComplexExplanation();
                sumExpl.Description = "sum of:";
                int coord = 0;
                float sum = 0.0f;
                bool fail = false;
                int shouldMatchCount = 0;
                IEnumerator<BooleanClause> cIter = OuterInstance.clauses.GetEnumerator();
                for (IEnumerator<Weight> wIter = Weights.GetEnumerator(); wIter.MoveNext(); )
                {
                    Weight w = wIter.Current;
                    cIter.MoveNext();
                    BooleanClause c = cIter.Current;
                    if (w.Scorer(context, context.AtomicReader.LiveDocs) == null)
                    {
                        if (c.Required)
                        {
                            fail = true;
                            Explanation r = new Explanation(0.0f, "no match on required clause (" + c.Query.ToString() + ")");
                            sumExpl.AddDetail(r);
                        }
                        continue;
                    }
                    Explanation e = w.Explain(context, doc);
                    if (e.IsMatch)
                    {
                        if (!c.Prohibited)
                        {
                            sumExpl.AddDetail(e);
                            sum += e.Value;
                            coord++;
                        }
                        else
                        {
                            Explanation r = new Explanation(0.0f, "match on prohibited clause (" + c.Query.ToString() + ")");
                            r.AddDetail(e);
                            sumExpl.AddDetail(r);
                            fail = true;
                        }
                        if (c.Occur_ == Occur_e.SHOULD)
                        {
                            shouldMatchCount++;
                        }
                    }
                    else if (c.Required)
                    {
                        Explanation r = new Explanation(0.0f, "no match on required clause (" + c.Query.ToString() + ")");
                        r.AddDetail(e);
                        sumExpl.AddDetail(r);
                        fail = true;
                    }
                }
                if (fail)
                {
                    sumExpl.Match = false;
                    sumExpl.Value = 0.0f;
                    sumExpl.Description = "Failure to meet condition(s) of required/prohibited clause(s)";
                    return sumExpl;
                }
                else if (shouldMatchCount < minShouldMatch)
                {
                    sumExpl.Match = false;
                    sumExpl.Value = 0.0f;
                    sumExpl.Description = "Failure to match minimum number " + "of optional clauses: " + minShouldMatch;
                    return sumExpl;
                }

                sumExpl.Match = 0 < coord ? true : false;
                sumExpl.Value = sum;

                float coordFactor = DisableCoord ? 1.0f : Coord(coord, maxCoord);
                if (coordFactor == 1.0f)
                {
                    return sumExpl; // eliminate wrapper
                }
                else
                {
                    ComplexExplanation result = new ComplexExplanation(sumExpl.IsMatch, sum * coordFactor, "product of:");
                    result.AddDetail(sumExpl);
                    result.AddDetail(new Explanation(coordFactor, "coord(" + coord + "/" + maxCoord + ")"));
                    return result;
                }
            }

            public override BulkScorer BulkScorer(AtomicReaderContext context, bool scoreDocsInOrder, Bits acceptDocs)
            {
                if (scoreDocsInOrder || OuterInstance.MinNrShouldMatch > 1)
                {
                    // TODO: (LUCENE-4872) in some cases BooleanScorer may be faster for minNrShouldMatch
                    // but the same is even true of pure conjunctions...
                    return base.BulkScorer(context, scoreDocsInOrder, acceptDocs);
                }

                IList<BulkScorer> prohibited = new List<BulkScorer>();
                IList<BulkScorer> optional = new List<BulkScorer>();
                IEnumerator<BooleanClause> cIter = OuterInstance.clauses.GetEnumerator();
                foreach (Weight w in Weights)
                {
                    cIter.MoveNext();
                    BooleanClause c = cIter.Current;
                    BulkScorer subScorer = w.BulkScorer(context, false, acceptDocs);
                    if (subScorer == null)
                    {
                        if (c.Required)
                        {
                            return null;
                        }
                    }
                    else if (c.Required)
                    {
                        // TODO: there are some cases where BooleanScorer
                        // would handle conjunctions faster than
                        // BooleanScorer2...
                        return base.BulkScorer(context, scoreDocsInOrder, acceptDocs);
                    }
                    else if (c.Prohibited)
                    {
                        prohibited.Add(subScorer);
                    }
                    else
                    {
                        optional.Add(subScorer);
                    }
                }

                // Check if we can and should return a BooleanScorer
                return new BooleanScorer(this, DisableCoord, OuterInstance.MinNrShouldMatch, optional, prohibited, maxCoord);
            }

            public override Scorer Scorer(AtomicReaderContext context, Bits acceptDocs)
            {
                IList<Scorer> required = new List<Scorer>();
                IList<Scorer> prohibited = new List<Scorer>();
                IList<Scorer> optional = new List<Scorer>();
                IEnumerator<BooleanClause> cIter = OuterInstance.clauses.GetEnumerator();
                foreach (Weight w in Weights)
                {
                    cIter.MoveNext();
                    BooleanClause c = cIter.Current;
                    Scorer subScorer = w.Scorer(context, acceptDocs);
                    if (subScorer == null)
                    {
                        if (c.Required)
                        {
                            return null;
                        }
                    }
                    else if (c.Required)
                    {
                        required.Add(subScorer);
                    }
                    else if (c.Prohibited)
                    {
                        prohibited.Add(subScorer);
                    }
                    else
                    {
                        optional.Add(subScorer);
                    }
                }

                if (required.Count == 0 && optional.Count == 0)
                {
                    // no required and optional clauses.
                    return null;
                }
                else if (optional.Count < OuterInstance.MinNrShouldMatch)
                {
                    // either >1 req scorer, or there are 0 req scorers and at least 1
                    // optional scorer. Therefore if there are not enough optional scorers
                    // no documents will be matched by the query
                    return null;
                }

                // simple conjunction
                if (optional.Count == 0 && prohibited.Count == 0)
                {
                    float coord = DisableCoord ? 1.0f : Coord(required.Count, maxCoord);
                    return new ConjunctionScorer(this, required.ToArray(), coord);
                }

                // simple disjunction
                if (required.Count == 0 && prohibited.Count == 0 && OuterInstance.MinNrShouldMatch <= 1 && optional.Count > 1)
                {
                    var coord = new float[optional.Count + 1];
                    for (int i = 0; i < coord.Length; i++)
                    {
                        coord[i] = DisableCoord ? 1.0f : Coord(i, maxCoord);
                    }
                    return new DisjunctionSumScorer(this, optional.ToArray(), coord);
                }

                // Return a BooleanScorer2
                return new BooleanScorer2(this, DisableCoord, OuterInstance.MinNrShouldMatch, required, prohibited, optional, maxCoord);
            }

            public override bool ScoresDocsOutOfOrder()
            {
                if (OuterInstance.MinNrShouldMatch > 1)
                {
                    // BS2 (in-order) will be used by scorer()
                    return false;
                }
                foreach (BooleanClause c in OuterInstance.clauses)
                {
                    if (c.Required)
                    {
                        // BS2 (in-order) will be used by scorer()
                        return false;
                    }
                }

                // scorer() will return an out-of-order scorer if requested.
                return true;
            }
        }

        public override Weight CreateWeight(IndexSearcher searcher)
        {
            return new BooleanWeight(this, searcher, DisableCoord);
        }

        public override Query Rewrite(IndexReader reader)
        {
            if (MinNrShouldMatch == 0 && clauses.Count == 1) // optimize 1-clause queries
            {
                BooleanClause c = clauses[0];
                if (!c.Prohibited) // just return clause
                {
                    Query query = c.Query.Rewrite(reader); // rewrite first

                    if (Boost != 1.0f) // incorporate boost
                    {
                        if (query == c.Query) // if rewrite was no-op
                        {
                            query = (Query)query.Clone(); // then clone before boost
                        }
                        // Since the BooleanQuery only has 1 clause, the BooleanQuery will be
                        // written out. Therefore the rewritten Query's boost must incorporate both
                        // the clause's boost, and the boost of the BooleanQuery itself
                        query.Boost = Boost * query.Boost;
                    }

                    return query;
                }
            }

            BooleanQuery clone = null; // recursively rewrite
            for (int i = 0; i < clauses.Count; i++)
            {
                BooleanClause c = clauses[i];
                Query query = c.Query.Rewrite(reader);
                if (query != c.Query) // clause rewrote: must clone
                {
                    if (clone == null)
                    {
                        // The BooleanQuery clone is lazily initialized so only initialize
                        // it if a rewritten clause differs from the original clause (and hasn't been
                        // initialized already).  If nothing differs, the clone isn't needlessly created
                        clone = (BooleanQuery)this.Clone();
                    }
                    clone.clauses[i] = new BooleanClause(query, c.Occur_);
                }
            }
            if (clone != null)
            {
                return clone; // some clauses rewrote
            }
            else
            {
                return this; // no clauses rewrote
            }
        }

        // inherit javadoc
        public override void ExtractTerms(ISet<Term> terms)
        {
            foreach (BooleanClause clause in clauses)
            {
                if (clause.Occur_ != Occur_e.MUST_NOT)
                {
                    clause.Query.ExtractTerms(terms);
                }
            }
        }

        public override object Clone()
        {
            BooleanQuery clone = (BooleanQuery)base.Clone();
            clone.clauses = (EquatableList<BooleanClause>)this.clauses.Clone();
            return clone;
        }

        /// <summary>
        /// Prints a user-readable version of this query. </summary>
        public override string ToString(string field)
        {
            StringBuilder buffer = new StringBuilder();
            bool needParens = Boost != 1.0 || MinimumNumberShouldMatch > 0;
            if (needParens)
            {
                buffer.Append("(");
            }

            for (int i = 0; i < clauses.Count; i++)
            {
                BooleanClause c = clauses[i];
                if (c.Prohibited)
                {
                    buffer.Append("-");
                }
                else if (c.Required)
                {
                    buffer.Append("+");
                }

                Query subQuery = c.Query;
                if (subQuery != null)
                {
                    if (subQuery is BooleanQuery) // wrap sub-bools in parens
                    {
                        buffer.Append("(");
                        buffer.Append(subQuery.ToString(field));
                        buffer.Append(")");
                    }
                    else
                    {
                        buffer.Append(subQuery.ToString(field));
                    }
                }
                else
                {
                    buffer.Append("null");
                }

                if (i != clauses.Count - 1)
                {
                    buffer.Append(" ");
                }
            }

            if (needParens)
            {
                buffer.Append(")");
            }

            if (MinimumNumberShouldMatch > 0)
            {
                buffer.Append('~');
                buffer.Append(MinimumNumberShouldMatch);
            }

            if (Boost != 1.0f)
            {
                buffer.Append(ToStringUtils.Boost(Boost));
            }

            return buffer.ToString();
        }

        /// <summary>
        /// Returns true iff <code>o</code> is equal to this. </summary>
        public override bool Equals(object o)
        {
            if (!(o is BooleanQuery))
            {
                return false;
            }
            BooleanQuery other = (BooleanQuery)o;
            return this.Boost == other.Boost && this.clauses.SequenceEqual(other.clauses) && this.MinimumNumberShouldMatch == other.MinimumNumberShouldMatch && this.DisableCoord == other.DisableCoord;
        }

        /// <summary>
        /// Returns a hash code value for this object. </summary>
        public override int GetHashCode()
        {
            return Number.FloatToIntBits(Boost) ^ (clauses.Count == 0 ? 0 : HashHelpers.CombineHashCodes(clauses.First().GetHashCode(), clauses.Last().GetHashCode(), clauses.Count)) + MinimumNumberShouldMatch + (DisableCoord ? 17 : 0);
        }
    }
}