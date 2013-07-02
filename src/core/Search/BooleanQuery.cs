/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections;
using Lucene.Net.Index;
using Lucene.Net.Support;
using IndexReader = Lucene.Net.Index.IndexReader;
using ToStringUtils = Lucene.Net.Util.ToStringUtils;
using Occur = Lucene.Net.Search.Occur;
using System.Collections.Generic;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Util;
using System.Text;

namespace Lucene.Net.Search
{

    /// <summary>A Query that matches documents matching boolean combinations of other
    /// queries, e.g. <see cref="TermQuery" />s, <see cref="PhraseQuery" />s or other
    /// BooleanQuerys.
    /// </summary>
    [Serializable]
    public class BooleanQuery : Query, IEnumerable<BooleanClause>, ICloneable
    {
        private static int maxClauseCount = 1024;

        /// <summary>Thrown when an attempt is made to add more than <see cref="MaxClauseCount" />
        /// clauses. This typically happens if
        /// a PrefixQuery, FuzzyQuery, WildcardQuery, or TermRangeQuery 
        /// is expanded to many terms during search. 
        /// </summary>
        [Serializable]
        public class TooManyClauses : SystemException
        {
            public TooManyClauses()
                : base("maxClauseCount is set to " + BooleanQuery.maxClauseCount)
            {
            }
        }

        /// <summary>Gets or sets the maximum number of clauses permitted, 1024 by default.
        /// Attempts to add more than the permitted number of clauses cause <see cref="TooManyClauses" />
        /// to be thrown.
        /// </summary>
        public static int MaxClauseCount
        {
            get { return maxClauseCount; }
            set
            {
                if (value < 1)
                    throw new ArgumentException("maxClauseCount must be >= 1");
                maxClauseCount = value;
            }
        }

        private EquatableList<BooleanClause> clauses = new EquatableList<BooleanClause>();
        private readonly bool disableCoord;

        /// <summary>Constructs an empty boolean query. </summary>
        public BooleanQuery()
        {
            disableCoord = false;
        }

        /// <summary>Constructs an empty boolean query.
        /// 
        /// <see cref="Similarity.Coord(int,int)" /> may be disabled in scoring, as
        /// appropriate. For example, this score factor does not make sense for most
        /// automatically generated queries, like <see cref="WildcardQuery" /> and <see cref="FuzzyQuery" />
        ///.
        /// 
        /// </summary>
        /// <param name="disableCoord">disables <see cref="Similarity.Coord(int,int)" /> in scoring.
        /// </param>
        public BooleanQuery(bool disableCoord)
        {
            this.disableCoord = disableCoord;
        }

        /// <summary>Returns true iff <see cref="Similarity.Coord(int,int)" /> is disabled in
        /// scoring for this query instance.
        /// </summary>
        /// <seealso cref="BooleanQuery(bool)">
        /// </seealso>
        public virtual bool IsCoordDisabled
        {
            get
            {
                return disableCoord;
            }
        }

        protected internal int minNrShouldMatch = 0;

        /// <summary>
        /// Specifies a minimum number of the optional BooleanClauses
        /// which must be satisfied.
        /// <para>
        /// By default no optional clauses are necessary for a match
        /// (unless there are no required clauses).  If this method is used,
        /// then the specified number of clauses is required.
        /// </para>
        /// <para>
        /// Use of this method is totally independent of specifying that
        /// any specific clauses are required (or prohibited).  This number will
        /// only be compared against the number of matching optional clauses.
        /// </para>
        /// </summary>
        public virtual int MinimumNumberShouldMatch
        {
            set { this.minNrShouldMatch = value; }
            get { return minNrShouldMatch; }
        }

        /// <summary>Adds a clause to a boolean query.
        /// 
        /// </summary>
        /// <throws>  TooManyClauses if the new number of clauses exceeds the maximum clause number </throws>
        /// <seealso cref="MaxClauseCount">
        /// </seealso>
        public virtual void Add(Query query, Occur occur)
        {
            Add(new BooleanClause(query, occur));
        }

        /// <summary>Adds a clause to a boolean query.</summary>
        /// <throws>  TooManyClauses if the new number of clauses exceeds the maximum clause number </throws>
        /// <seealso cref="MaxClauseCount">
        /// </seealso>
        public virtual void Add(BooleanClause clause)
        {
            if (clauses.Count >= maxClauseCount)
                throw new TooManyClauses();

            clauses.Add(clause);
        }

        /// <summary>Returns the set of clauses in this query. </summary>
        public virtual BooleanClause[] Clauses
        {
            get
            {
                return clauses.ToArray();
            }
        }

        /// <summary>
        /// Returns an iterator on the clauses in this query.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<BooleanClause> GetEnumerator()
        {
            return clauses.GetEnumerator();
        }

        /// <summary> Expert: the Weight for BooleanQuery, used to
        /// normalize, score and explain these queries.
        /// 
        /// <p/>NOTE: this API and implementation is subject to
        /// change suddenly in the next release.<p/>
        /// </summary>
        [Serializable]
        public class BooleanWeight : Weight
        {
            private readonly BooleanQuery enclosingInstance;

            /// <summary>The Similarity implementation. </summary>
            protected internal Similarity similarity;
            protected internal List<Weight> weights;
            protected int maxCoord;  // num optional + num required
            private readonly bool disableCoord;

            public BooleanWeight(BooleanQuery enclosingInstance, IndexSearcher searcher, bool disableCoord)
            {
                this.enclosingInstance = enclosingInstance;
                this.similarity = searcher.Similarity;
                this.disableCoord = disableCoord;
                weights = new List<Weight>(enclosingInstance.clauses.Count);
                for (int i = 0; i < enclosingInstance.clauses.Count; i++)
                {
                    BooleanClause c = enclosingInstance.clauses[i];
                    Weight w = c.Query.CreateWeight(searcher);
                    weights.Add(w);
                    if (!c.IsProhibited) maxCoord++;
                }
            }

            public override Query Query
            {
                get { return enclosingInstance; }
            }

            public override float ValueForNormalization
            {
                get
                {
                    float sum = 0.0f;
                    for (int i = 0; i < weights.Count; i++)
                    {
                        // call sumOfSquaredWeights for all clauses in case of side effects
                        float s = weights[i].ValueForNormalization;         // sum sub weights
                        if (!enclosingInstance.clauses[i].IsProhibited)
                            // only add to sum for non-prohibited clauses
                            sum += s;
                    }

                    sum *= enclosingInstance.Boost * enclosingInstance.Boost;             // boost each sub-weight

                    return sum;
                }
            }

            public float Coord(int overlap, int maxOverlap)
            {
                // LUCENE-4300: in most cases of maxOverlap=1, BQ rewrites itself away,
                // so coord() is not applied. But when BQ cannot optimize itself away
                // for a single clause (minNrShouldMatch, prohibited clauses, etc), its
                // important not to apply coord(1,1) for consistency, it might not be 1.0F
                return maxOverlap == 1 ? 1F : similarity.Coord(overlap, maxOverlap);
            }

            public override void Normalize(float norm, float topLevelBoost)
            {
                topLevelBoost *= enclosingInstance.Boost; // incorporate boost
                foreach (Weight w in weights)
                {
                    // normalize all clauses, (even if prohibited in case of side affects)
                    w.Normalize(norm, topLevelBoost);
                }
            }

            public override Explanation Explain(AtomicReaderContext context, int doc)
            {
                int minShouldMatch = enclosingInstance.MinimumNumberShouldMatch;
                ComplexExplanation sumExpl = new ComplexExplanation();
                sumExpl.Description = "sum of:";
                int coord = 0;
                float sum = 0.0f;
                bool fail = false;
                int shouldMatchCount = 0;
                IEnumerator<BooleanClause> cIter = enclosingInstance.clauses.GetEnumerator();
                for (IEnumerator<Weight> wIter = weights.GetEnumerator(); wIter.MoveNext(); )
                {
                    cIter.MoveNext();
                    Weight w = wIter.Current;
                    BooleanClause c = cIter.Current;
                    if (w.Scorer(context, true, true, context.Reader.LiveDocs) == null)
                    {
                        if (c.IsRequired)
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
                        if (!c.IsProhibited)
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
                        if (c.Occur == Occur.SHOULD)
                            shouldMatchCount++;
                    }
                    else if (c.IsRequired)
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
                    sumExpl.Description = "Failure to match minimum number of optional clauses: " + minShouldMatch;
                    return sumExpl;
                }

                sumExpl.Match = 0 < coord ? true : false;
                sumExpl.Value = sum;

                float coordFactor = disableCoord ? 1.0f : Coord(coord, maxCoord);
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

            public override Scorer Scorer(AtomicReaderContext context, bool scoreDocsInOrder, bool topScorer, IBits acceptDocs)
            {
                var required = new List<Scorer>();
                var prohibited = new List<Scorer>();
                var optional = new List<Scorer>();

                IEnumerator<BooleanClause> cIter = enclosingInstance.clauses.GetEnumerator();
                foreach (Weight w in weights)
                {
                    cIter.MoveNext();
                    BooleanClause c = (BooleanClause)cIter.Current;
                    Scorer subScorer = w.Scorer(context, true, false, acceptDocs);
                    if (subScorer == null)
                    {
                        if (c.IsRequired)
                        {
                            return null;
                        }
                    }
                    else if (c.IsRequired)
                    {
                        required.Add(subScorer);
                    }
                    else if (c.IsProhibited)
                    {
                        prohibited.Add(subScorer);
                    }
                    else
                    {
                        optional.Add(subScorer);
                    }
                }

                // NOTE: we could also use BooleanScorer, if we knew
                // this BooleanQuery was embedded in another
                // BooleanQuery that was also using BooleanScorer (ie,
                // BooleanScorer can nest).  But this is hard to
                // detect and we never do so today... (ie, we only
                // return BooleanScorer for topScorer):

                // Check if we can and should return a BooleanScorer
                // TODO: (LUCENE-4872) in some cases BooleanScorer may be faster for minNrShouldMatch
                // but the same is even true of pure conjunctions...
                if (!scoreDocsInOrder && topScorer && required.Count == 0 && enclosingInstance.minNrShouldMatch <= 1)
                {
                    return new BooleanScorer(this, disableCoord, enclosingInstance.minNrShouldMatch, optional, prohibited, maxCoord);
                }

                if (required.Count == 0 && optional.Count == 0)
                {
                    // no required and optional clauses.
                    return null;
                }
                else if (optional.Count < enclosingInstance.minNrShouldMatch)
                {
                    // either >1 req scorer, or there are 0 req scorers and at least 1
                    // optional scorer. Therefore if there are not enough optional scorers
                    // no documents will be matched by the query
                    return null;
                }

                // simple conjunction
                if (optional.Count == 0 && prohibited.Count == 0)
                {
                    float coord = disableCoord ? 1.0f : Coord(required.Count, maxCoord);
                    return new ConjunctionScorer(this, required.ToArray(), coord);
                }

                // simple disjunction
                if (required.Count == 0 && prohibited.Count == 0 && enclosingInstance.minNrShouldMatch <= 1 && optional.Count > 1)
                {
                    float[] coord = new float[optional.Count + 1];
                    for (int i = 0; i < coord.Length; i++)
                    {
                        coord[i] = disableCoord ? 1.0f : Coord(i, maxCoord);
                    }
                    return new DisjunctionSumScorer(this, optional.ToArray(), coord);
                }

                // Return a BooleanScorer2
                return new BooleanScorer2(this, disableCoord, enclosingInstance.minNrShouldMatch, required, prohibited, optional, maxCoord);
            }

            public override bool ScoresDocsOutOfOrder
            {
                get
                {
                    foreach (BooleanClause c in enclosingInstance.clauses)
                    {
                        if (c.IsRequired)
                        {
                            return false; // BS2 (in-order) will be used by scorer()
                        }
                    }

                    // scorer() will return an out-of-order scorer if requested.
                    return true;
                }
            }
        }

        public override Weight CreateWeight(IndexSearcher searcher)
        {
            return new BooleanWeight(this, searcher, disableCoord);
        }

        public override Query Rewrite(IndexReader reader)
        {
            if (minNrShouldMatch == 0 && clauses.Count == 1)
            {
                // optimize 1-clause queries
                BooleanClause c = clauses[0];
                if (!c.IsProhibited)
                {
                    // just return clause

                    Query query = c.Query.Rewrite(reader); // rewrite first

                    if (Boost != 1.0f)
                    {
                        // incorporate boost
                        if (query == c.Query)
                            // if rewrite was no-op
                            query = (Query)query.Clone(); // then clone before boost
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
                if (query != c.Query)
                {
                    // clause rewrote: must clone
                    if (clone == null)
                        clone = (BooleanQuery)this.Clone();
                    clone.clauses[i] = new BooleanClause(query, c.Occur);
                }
            }
            if (clone != null)
            {
                return clone; // some clauses rewrote
            }
            else
                return this; // no clauses rewrote
        }

        // inherit javadoc
        public override void ExtractTerms(ISet<Term> terms)
        {
            foreach (BooleanClause clause in clauses)
            {
                if (clause.Occur != Occur.MUST_NOT)
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

        /// <summary>Prints a user-readable version of this query. </summary>
        public override string ToString(string field)
        {
            StringBuilder buffer = new StringBuilder();
            bool needParens = (Boost != 1.0) || (MinimumNumberShouldMatch > 0);
            if (needParens)
            {
                buffer.Append("(");
            }

            for (int i = 0; i < clauses.Count; i++)
            {
                BooleanClause c = clauses[i];
                if (c.IsProhibited)
                    buffer.Append("-");
                else if (c.IsRequired)
                    buffer.Append("+");

                Query subQuery = c.Query;
                if (subQuery != null)
                {
                    if (subQuery is BooleanQuery)
                    {
                        // wrap sub-bools in parens
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
                    buffer.Append(" ");
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

        /// <summary>Returns true iff <c>o</c> is equal to this. </summary>
        public override bool Equals(object o)
        {
            if (!(o is BooleanQuery))
                return false;
            BooleanQuery other = (BooleanQuery)o;
            return (this.Boost == other.Boost)
                    && this.clauses.Equals(other.clauses)
                    && this.MinimumNumberShouldMatch == other.MinimumNumberShouldMatch
                    && this.disableCoord == other.disableCoord;
        }

        /// <summary>Returns a hash code value for this object.</summary>
        public override int GetHashCode()
        {
            return BitConverter.ToInt32(BitConverter.GetBytes(Boost), 0) ^ clauses.GetHashCode() + MinimumNumberShouldMatch + (disableCoord ? 17 : 0);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}