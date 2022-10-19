using J2N;
using J2N.Collections.Generic.Extensions;
using Lucene.Net.Util;
using System;
using System.Collections;
using System.Collections.Generic;
#if FEATURE_SERIALIZABLE_EXCEPTIONS
using System.Runtime.Serialization;
#endif
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
    using Occur_e = Lucene.Net.Search.Occur;
    using Similarity = Lucene.Net.Search.Similarities.Similarity;
    using Term = Lucene.Net.Index.Term;
    using ToStringUtils = Lucene.Net.Util.ToStringUtils;

    /// <summary>
    /// A <see cref="Query"/> that matches documents matching boolean combinations of other
    /// queries, e.g. <see cref="TermQuery"/>s, <see cref="PhraseQuery"/>s or other
    /// <see cref="BooleanQuery"/>s.
    /// <para/>
    /// Collection initializer note: To create and populate a <see cref="BooleanQuery"/>
    /// in a single statement, you can use the following example as a guide:
    /// 
    /// <code>
    /// var booleanQuery = new BooleanQuery() {
    ///     { new WildcardQuery(new Term("field2", "foobar")), Occur.SHOULD },
    ///     { new MultiPhraseQuery() {
    ///         new Term("field", "microsoft"), 
    ///         new Term("field", "office")
    ///     }, Occur.SHOULD }
    /// };
    /// 
    /// // or
    /// 
    /// var booleanQuery = new BooleanQuery() {
    ///     new BooleanClause(new WildcardQuery(new Term("field2", "foobar")), Occur.SHOULD),
    ///     new BooleanClause(new MultiPhraseQuery() {
    ///         new Term("field", "microsoft"), 
    ///         new Term("field", "office")
    ///     }, Occur.SHOULD)
    /// };
    /// </code>
    /// </summary>
    public class BooleanQuery : Query, IEnumerable<BooleanClause>
    {
        private static int maxClauseCount = 1024;

        /// <summary>
        /// Thrown when an attempt is made to add more than 
        /// <see cref="MaxClauseCount"/> clauses. This typically happens if
        /// a <see cref="PrefixQuery"/>, <see cref="FuzzyQuery"/>, <see cref="WildcardQuery"/>, or <see cref="TermRangeQuery"/>
        /// is expanded to many terms during search.
        /// </summary>
        // LUCENENET: It is no longer good practice to use binary serialization. 
        // See: https://github.com/dotnet/corefx/issues/23584#issuecomment-325724568
#if FEATURE_SERIALIZABLE_EXCEPTIONS
        [Serializable]
#endif
        // LUCENENET specific: Added IRuntimeException for identification of the Java superclass in .NET
        public class TooManyClausesException : Exception, IRuntimeException // LUCENENET specific - added Exception suffix
        {
            public TooManyClausesException()
                : base("maxClauseCount is set to " + maxClauseCount)
            {
            }

#if FEATURE_SERIALIZABLE_EXCEPTIONS
            /// <summary>
            /// Initializes a new instance of this class with serialized data.
            /// </summary>
            /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
            /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
            protected TooManyClausesException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {
            }
#endif
        }

        /// <summary>
        /// Return the maximum number of clauses permitted, 1024 by default.
        /// Attempts to add more than the permitted number of clauses cause 
        /// <see cref="TooManyClausesException"/> to be thrown. </summary>
        public static int MaxClauseCount
        {
            get => maxClauseCount;
            set
            {
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(MaxClauseCount), "maxClauseCount must be >= 1"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
                }
                BooleanQuery.maxClauseCount = value;
            }
        }

        private IList<BooleanClause> clauses = new JCG.List<BooleanClause>();
        private readonly bool disableCoord;

        /// <summary>
        /// Constructs an empty boolean query. </summary>
        public BooleanQuery()
        {
            disableCoord = false;
        }

        /// <summary>
        /// Constructs an empty boolean query.
        /// <para/>
        /// <see cref="Similarity.Coord(int,int)"/> may be disabled in scoring, as
        /// appropriate. For example, this score factor does not make sense for most
        /// automatically generated queries, like <see cref="WildcardQuery"/> and 
        /// <see cref="FuzzyQuery"/>.
        /// </summary>
        /// <param name="disableCoord"> Disables <see cref="Similarity.Coord(int,int)"/> in scoring. </param>
        public BooleanQuery(bool disableCoord)
        {
            this.disableCoord = disableCoord;
        }

        /// <summary>
        /// Returns true if <see cref="Similarity.Coord(int,int)"/> is disabled in
        /// scoring for this query instance. </summary>
        /// <seealso cref="BooleanQuery(bool)"/>
        public virtual bool CoordDisabled => disableCoord; // LUCENENET TODO: API Change to CoordEnabled? Per MSDN, properties should be in the affirmative.

        /// <summary>
        /// Specifies a minimum number of the optional <see cref="BooleanClause"/>s
        /// which must be satisfied.
        ///
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
        /// <param name="value"> The number of optional clauses that must match </param>
        public virtual int MinimumNumberShouldMatch
        {
            get => m_minNrShouldMatch;
            set => this.m_minNrShouldMatch = value;
        }

        protected int m_minNrShouldMatch = 0;

        /// <summary>
        /// Adds a clause to a boolean query.
        /// </summary>
        /// <exception cref="TooManyClausesException"> If the new number of clauses exceeds the maximum clause number </exception>
        /// <seealso cref="MaxClauseCount"/>
        public virtual void Add(Query query, Occur occur)
        {
            Add(new BooleanClause(query, occur));
        }

        /// <summary>
        /// Adds a clause to a boolean query. </summary>
        /// <exception cref="TooManyClausesException"> If the new number of clauses exceeds the maximum clause number </exception>
        /// <seealso cref="MaxClauseCount"/>
        public virtual void Add(BooleanClause clause)
        {
            if (clauses.Count >= maxClauseCount)
            {
                throw new TooManyClausesException();
            }

            clauses.Add(clause);
        }

        /// <summary>
        /// Returns the set of clauses in this query. </summary>
        public virtual BooleanClause[] GetClauses()
        {
            return clauses.ToArray();
        }

        /// <summary>
        /// Returns the list of clauses in this query. </summary>
        public virtual IList<BooleanClause> Clauses => clauses;

        /// <summary>
        /// Returns an iterator on the clauses in this query. It implements the <see cref="T:IEnumerable{BooleanClause}"/> interface to
        /// make it possible to do:
        /// <code>foreach (BooleanClause clause in booleanQuery) {}</code>
        /// </summary>
        public IEnumerator<BooleanClause> GetEnumerator()
        {
            return Clauses.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Expert: the <see cref="Weight"/> for <see cref="BooleanQuery"/>, used to
        /// normalize, score and explain these queries.
        /// <para/>
        /// @lucene.experimental
        /// </summary>
        public class BooleanWeight : Weight
        {
            private readonly BooleanQuery outerInstance;

            /// <summary>
            /// The <see cref="Similarities.Similarity"/> implementation. </summary>
            protected Similarity m_similarity;

            protected IList<Weight> m_weights;
            protected int m_maxCoord; // num optional + num required
            private readonly bool disableCoord;

            public BooleanWeight(BooleanQuery outerInstance, IndexSearcher searcher, bool disableCoord)
            {
                this.outerInstance = outerInstance;
                this.m_similarity = searcher.Similarity;
                this.disableCoord = disableCoord;
                m_weights = new JCG.List<Weight>(outerInstance.clauses.Count);
                for (int i = 0; i < outerInstance.clauses.Count; i++)
                {
                    BooleanClause c = outerInstance.clauses[i];
                    Weight w = c.Query.CreateWeight(searcher);
                    m_weights.Add(w);
                    if (!c.IsProhibited)
                    {
                        m_maxCoord++;
                    }
                }
            }

            public Similarity Similarity => m_similarity;

            public int MaxCoord => m_maxCoord;

            public override Query Query => outerInstance;

            public override float GetValueForNormalization()
            {
                float sum = 0.0f;
                for (int i = 0; i < m_weights.Count; i++)
                {
                    // call sumOfSquaredWeights for all clauses in case of side effects
                    float s = m_weights[i].GetValueForNormalization(); // sum sub weights
                    if (!outerInstance.clauses[i].IsProhibited)
                    {
                        // only add to sum for non-prohibited clauses
                        sum += s;
                    }
                }

                sum *= outerInstance.Boost * outerInstance.Boost; // boost each sub-weight

                return sum;
            }

            public virtual float Coord(int overlap, int maxOverlap)
            {
                // LUCENE-4300: in most cases of maxOverlap=1, BQ rewrites itself away,
                // so coord() is not applied. But when BQ cannot optimize itself away
                // for a single clause (minNrShouldMatch, prohibited clauses, etc), its
                // important not to apply coord(1,1) for consistency, it might not be 1.0F
                return maxOverlap == 1 ? 1F : m_similarity.Coord(overlap, maxOverlap);
            }

            public override void Normalize(float norm, float topLevelBoost)
            {
                topLevelBoost *= outerInstance.Boost; // incorporate boost
                foreach (Weight w in m_weights)
                {
                    // normalize all clauses, (even if prohibited in case of side affects)
                    w.Normalize(norm, topLevelBoost);
                }
            }

            public override Explanation Explain(AtomicReaderContext context, int doc)
            {
                int minShouldMatch = outerInstance.MinimumNumberShouldMatch;
                ComplexExplanation sumExpl = new ComplexExplanation();
                sumExpl.Description = "sum of:";
                int coord = 0;
                float sum = 0.0f;
                bool fail = false;
                int shouldMatchCount = 0;
                using (IEnumerator<BooleanClause> cIter = outerInstance.clauses.GetEnumerator())
                {
                    foreach (Weight w in m_weights)
                    {
                        cIter.MoveNext();
                        BooleanClause c = cIter.Current;
                        if (w.GetScorer(context, context.AtomicReader.LiveDocs) is null)
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
                            if (c.Occur == Occur_e.SHOULD)
                            {
                                shouldMatchCount++;
                            }
                        }
                        else if (c.IsRequired)
                        {
                            Explanation r = new Explanation(0.0f, "no match on required clause (" + c.Query.ToString() + ")");
                            r.AddDetail(e);
                            sumExpl.AddDetail(r);
                            fail = true;
                        }
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

                float coordFactor = disableCoord ? 1.0f : Coord(coord, m_maxCoord);
                if (coordFactor == 1.0f)
                {
                    return sumExpl; // eliminate wrapper
                }
                else
                {
                    ComplexExplanation result = new ComplexExplanation(sumExpl.IsMatch, sum * coordFactor, "product of:");
                    result.AddDetail(sumExpl);
                    result.AddDetail(new Explanation(coordFactor, "coord(" + coord + "/" + m_maxCoord + ")"));
                    return result;
                }
            }

            public override BulkScorer GetBulkScorer(AtomicReaderContext context, bool scoreDocsInOrder, IBits acceptDocs)
            {
                if (scoreDocsInOrder || outerInstance.m_minNrShouldMatch > 1)
                {
                    // TODO: (LUCENE-4872) in some cases BooleanScorer may be faster for minNrShouldMatch
                    // but the same is even true of pure conjunctions...
                    return base.GetBulkScorer(context, scoreDocsInOrder, acceptDocs);
                }

                IList<BulkScorer> prohibited = new JCG.List<BulkScorer>();
                IList<BulkScorer> optional = new JCG.List<BulkScorer>();
                using (IEnumerator<BooleanClause> cIter = outerInstance.clauses.GetEnumerator())
                {
                    foreach (Weight w in m_weights)
                    {
                        cIter.MoveNext();
                        BooleanClause c = cIter.Current;
                        BulkScorer subScorer = w.GetBulkScorer(context, false, acceptDocs);
                        if (subScorer is null)
                        {
                            if (c.IsRequired)
                            {
                                return null;
                            }
                        }
                        else if (c.IsRequired)
                        {
                            // TODO: there are some cases where BooleanScorer
                            // would handle conjunctions faster than
                            // BooleanScorer2...
                            return base.GetBulkScorer(context, scoreDocsInOrder, acceptDocs);
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
                }

                // Check if we can and should return a BooleanScorer
                return new BooleanScorer(this, disableCoord, outerInstance.m_minNrShouldMatch, optional, prohibited, m_maxCoord);
            }

            public override Scorer GetScorer(AtomicReaderContext context, IBits acceptDocs)
            {
                IList<Scorer> required = new JCG.List<Scorer>();
                IList<Scorer> prohibited = new JCG.List<Scorer>();
                IList<Scorer> optional = new JCG.List<Scorer>();
                IEnumerator<BooleanClause> cIter = outerInstance.clauses.GetEnumerator();
                foreach (Weight w in m_weights)
                {
                    cIter.MoveNext();
                    BooleanClause c = cIter.Current;
                    Scorer subScorer = w.GetScorer(context, acceptDocs);
                    if (subScorer is null)
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

                if (required.Count == 0 && optional.Count == 0)
                {
                    // no required and optional clauses.
                    return null;
                }
                else if (optional.Count < outerInstance.m_minNrShouldMatch)
                {
                    // either >1 req scorer, or there are 0 req scorers and at least 1
                    // optional scorer. Therefore if there are not enough optional scorers
                    // no documents will be matched by the query
                    return null;
                }

                // simple conjunction
                if (optional.Count == 0 && prohibited.Count == 0)
                {
                    float coord = disableCoord ? 1.0f : Coord(required.Count, m_maxCoord);
                    return new ConjunctionScorer(this, required.ToArray(), coord);
                }

                // simple disjunction
                if (required.Count == 0 && prohibited.Count == 0 && outerInstance.m_minNrShouldMatch <= 1 && optional.Count > 1)
                {
                    var coord = new float[optional.Count + 1];
                    for (int i = 0; i < coord.Length; i++)
                    {
                        coord[i] = disableCoord ? 1.0f : Coord(i, m_maxCoord);
                    }
                    return new DisjunctionSumScorer(this, optional.ToArray(), coord);
                }

                // Return a BooleanScorer2
                return new BooleanScorer2(this, disableCoord, outerInstance.m_minNrShouldMatch, required, prohibited, optional, m_maxCoord);
            }

            public override bool ScoresDocsOutOfOrder
            {
                get
                {
                    if (outerInstance.m_minNrShouldMatch > 1)
                    {
                        // BS2 (in-order) will be used by scorer()
                        return false;
                    }
                    foreach (BooleanClause c in outerInstance.clauses)
                    {
                        if (c.IsRequired)
                        {
                            // BS2 (in-order) will be used by scorer()
                            return false;
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
            if (m_minNrShouldMatch == 0 && clauses.Count == 1) // optimize 1-clause queries
            {
                BooleanClause c = clauses[0];
                if (!c.IsProhibited) // just return clause
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
                    if (clone is null)
                    {
                        // The BooleanQuery clone is lazily initialized so only initialize
                        // it if a rewritten clause differs from the original clause (and hasn't been
                        // initialized already).  If nothing differs, the clone isn't needlessly created
                        clone = (BooleanQuery)this.Clone();
                    }
                    clone.clauses[i] = new BooleanClause(query, c.Occur);
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
                if (clause.Occur != Occur_e.MUST_NOT)
                {
                    clause.Query.ExtractTerms(terms);
                }
            }
        }

        public override object Clone()
        {
            BooleanQuery clone = (BooleanQuery)base.Clone();
            clone.clauses = new JCG.List<BooleanClause>(this.clauses);
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
                buffer.Append('(');
            }

            for (int i = 0; i < clauses.Count; i++)
            {
                BooleanClause c = clauses[i];
                if (c.IsProhibited)
                {
                    buffer.Append('-');
                }
                else if (c.IsRequired)
                {
                    buffer.Append('+');
                }

                Query subQuery = c.Query;
                if (subQuery != null)
                {
                    if (subQuery is BooleanQuery) // wrap sub-bools in parens
                    {
                        buffer.Append('(');
                        buffer.Append(subQuery.ToString(field));
                        buffer.Append(')');
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
                    buffer.Append(' ');
                }
            }

            if (needParens)
            {
                buffer.Append(')');
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
        /// Returns <c>true</c> if <paramref name="o"/> is equal to this. </summary>
        public override bool Equals(object o)
        {
            if (!(o is BooleanQuery))
            {
                return false;
            }
            BooleanQuery other = (BooleanQuery)o;
            // LUCENENET specific - compare bits rather than using equality operators to prevent these comparisons from failing in x86 in .NET Framework with optimizations enabled
            return NumericUtils.SingleToSortableInt32(this.Boost) == NumericUtils.SingleToSortableInt32(other.Boost)
                && this.clauses.Equals(other.clauses)
                && this.MinimumNumberShouldMatch == other.MinimumNumberShouldMatch
                && this.disableCoord == other.disableCoord;
        }

        /// <summary>
        /// Returns a hash code value for this object. </summary>
        public override int GetHashCode()
        {
            return BitConversion.SingleToInt32Bits(Boost) ^ clauses.GetHashCode()
                + MinimumNumberShouldMatch + (disableCoord ? 17 : 0);
        }
    }
}