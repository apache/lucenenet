// Lucene version compatibility level 4.8.1
using J2N.Collections.Generic.Extensions;
using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Queries
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

    /// <summary>
    /// A query that executes high-frequency terms in a optional sub-query to prevent
    /// slow queries due to "common" terms like stopwords. This query
    /// builds 2 queries off the <see cref="Add(Term)"/> added terms: low-frequency
    /// terms are added to a required boolean clause and high-frequency terms are
    /// added to an optional boolean clause. The optional clause is only executed if
    /// the required "low-frequency" clause matches. Scores produced by this query
    /// will be slightly different than plain <see cref="BooleanQuery"/> scorer mainly due to
    /// differences in the <see cref="Search.Similarities.Similarity.Coord(int,int)"/> number of leaf queries
    /// in the required boolean clause. In most cases, high-frequency terms are
    /// unlikely to significantly contribute to the document score unless at least
    /// one of the low-frequency terms are matched.  This query can improve
    /// query execution times significantly if applicable.
    /// <para>
    /// <see cref="CommonTermsQuery"/> has several advantages over stopword filtering at
    /// index or query time since a term can be "classified" based on the actual
    /// document frequency in the index and can prevent slow queries even across
    /// domains without specialized stopword files.
    /// </para>
    /// <para>
    /// <b>Note:</b> if the query only contains high-frequency terms the query is
    /// rewritten into a plain conjunction query ie. all high-frequency terms need to
    /// match in order to match a document.
    /// </para>
    /// <para/>
    /// Collection initializer note: To create and populate a <see cref="CommonTermsQuery"/>
    /// in a single statement, you can use the following example as a guide:
    /// 
    /// <code>
    /// var query = new CommonTermsQuery() {
    ///     new Term("field", "microsoft"), 
    ///     new Term("field", "office")
    /// };
    /// </code>
    /// </summary>
    public class CommonTermsQuery : Query, IEnumerable<Term> // LUCENENET specific - implemented IEnumerable<Term>, which allows for use of collection initializer. See: https://stackoverflow.com/a/9195144
    {
        /*
         * TODO maybe it would make sense to abstract this even further and allow to
         * rewrite to dismax rather than boolean. Yet, this can already be subclassed
         * to do so.
         */
        protected readonly IList<Term> m_terms = new JCG.List<Term>();
        protected readonly bool m_disableCoord;
        protected readonly float m_maxTermFrequency;
        protected readonly Occur m_lowFreqOccur;
        protected readonly Occur m_highFreqOccur;
        protected float m_lowFreqBoost = 1.0f;
        protected float m_highFreqBoost = 1.0f;
        protected float m_lowFreqMinNrShouldMatch = 0;
        protected float m_highFreqMinNrShouldMatch = 0;

        /// <summary>
        /// Creates a new <see cref="CommonTermsQuery"/>
        /// </summary>
        /// <param name="highFreqOccur">
        ///          <see cref="Occur"/> used for high frequency terms </param>
        /// <param name="lowFreqOccur">
        ///          <see cref="Occur"/> used for low frequency terms </param>
        /// <param name="maxTermFrequency">
        ///          a value in [0..1) (or absolute number >=1) representing the
        ///          maximum threshold of a terms document frequency to be considered a
        ///          low frequency term. </param>
        /// <exception cref="ArgumentException">
        ///           if <see cref="Occur.MUST_NOT"/> is pass as <paramref name="lowFreqOccur"/> or
        ///           <paramref name="highFreqOccur"/> </exception>
        public CommonTermsQuery(Occur highFreqOccur, Occur lowFreqOccur, float maxTermFrequency)
            : this(highFreqOccur, lowFreqOccur, maxTermFrequency, false)
        {
        }

        /// <summary>
        /// Creates a new <see cref="CommonTermsQuery"/>
        /// </summary>
        /// <param name="highFreqOccur">
        ///          <see cref="Occur"/> used for high frequency terms </param>
        /// <param name="lowFreqOccur">
        ///          <see cref="Occur"/> used for low frequency terms </param>
        /// <param name="maxTermFrequency">
        ///          a value in [0..1) (or absolute number >=1) representing the
        ///          maximum threshold of a terms document frequency to be considered a
        ///          low frequency term. </param>
        /// <param name="disableCoord">
        ///          disables <see cref="Search.Similarities.Similarity.Coord(int,int)"/> in scoring for the low
        ///          / high frequency sub-queries </param>
        /// <exception cref="ArgumentException">
        ///           if <see cref="Occur.MUST_NOT"/> is pass as <paramref name="lowFreqOccur"/> or
        ///           <paramref name="highFreqOccur"/> </exception>
        public CommonTermsQuery(Occur highFreqOccur, Occur lowFreqOccur,
            float maxTermFrequency, bool disableCoord)
        {
            if (highFreqOccur == Occur.MUST_NOT)
            {
                throw new ArgumentException("highFreqOccur should be MUST or SHOULD but was MUST_NOT");
            }
            if (lowFreqOccur == Occur.MUST_NOT)
            {
                throw new ArgumentException("lowFreqOccur should be MUST or SHOULD but was MUST_NOT");
            }
            this.m_disableCoord = disableCoord;
            this.m_highFreqOccur = highFreqOccur;
            this.m_lowFreqOccur = lowFreqOccur;
            this.m_maxTermFrequency = maxTermFrequency;
        }

        /// <summary>
        /// Adds a term to the <see cref="CommonTermsQuery"/>
        /// </summary>
        /// <param name="term">
        ///          the term to add </param>
        public virtual void Add(Term term)
        {
            if (term is null)
            {
                throw new ArgumentNullException(nameof(term), "Term must not be null"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            this.m_terms.Add(term);
        }

        public override Query Rewrite(IndexReader reader)
        {
            if (this.m_terms.Count == 0)
            {
                return new BooleanQuery();
            }
            else if (this.m_terms.Count == 1)
            {
                Query tq = NewTermQuery(this.m_terms[0], null);
                tq.Boost = Boost;
                return tq;
            }
            var leaves = reader.Leaves;
            int maxDoc = reader.MaxDoc;
            var contextArray = new TermContext[m_terms.Count];
            var queryTerms = this.m_terms.ToArray();
            CollectTermContext(reader, leaves, contextArray, queryTerms);
            return BuildQuery(maxDoc, contextArray, queryTerms);
        }

        protected virtual int CalcLowFreqMinimumNumberShouldMatch(int numOptional)
        {
            return MinNrShouldMatch(m_lowFreqMinNrShouldMatch, numOptional);
        }

        protected virtual int CalcHighFreqMinimumNumberShouldMatch(int numOptional)
        {
            return MinNrShouldMatch(m_highFreqMinNrShouldMatch, numOptional);
        }

        private static int MinNrShouldMatch(float minNrShouldMatch, int numOptional) // LUCENENET: CA1822: Mark members as static
        {
            if (minNrShouldMatch >= 1.0f || minNrShouldMatch == 0.0f)
            {
                return (int)minNrShouldMatch;
            }
            return (int)Math.Round(minNrShouldMatch * numOptional);
        }

        protected virtual Query BuildQuery(int maxDoc, TermContext[] contextArray, Term[] queryTerms)
        {
            var lowFreq = new BooleanQuery(m_disableCoord);
            var highFreq = new BooleanQuery(m_disableCoord) { Boost = m_highFreqBoost };
            lowFreq.Boost = m_lowFreqBoost;
            var query = new BooleanQuery(true);
            for (int i = 0; i < queryTerms.Length; i++)
            {
                TermContext termContext = contextArray[i];
                if (termContext is null)
                {
                    lowFreq.Add(NewTermQuery(queryTerms[i], null), m_lowFreqOccur);
                }
                else
                {
                    if ((m_maxTermFrequency >= 1f && termContext.DocFreq > m_maxTermFrequency) || (termContext.DocFreq > (int)Math.Ceiling(m_maxTermFrequency * (float)maxDoc)))
                    {
                        highFreq.Add(NewTermQuery(queryTerms[i], termContext), m_highFreqOccur);
                    }
                    else
                    {
                        lowFreq.Add(NewTermQuery(queryTerms[i], termContext), m_lowFreqOccur);
                    }
                }

            }
            int numLowFreqClauses = lowFreq.GetClauses().Length;
            int numHighFreqClauses = highFreq.GetClauses().Length;
            if (m_lowFreqOccur == Occur.SHOULD && numLowFreqClauses > 0)
            {
                int minMustMatch = CalcLowFreqMinimumNumberShouldMatch(numLowFreqClauses);
                lowFreq.MinimumNumberShouldMatch = minMustMatch;
            }
            if (m_highFreqOccur == Occur.SHOULD && numHighFreqClauses > 0)
            {
                int minMustMatch = CalcHighFreqMinimumNumberShouldMatch(numHighFreqClauses);
                highFreq.MinimumNumberShouldMatch = minMustMatch;
            }
            if (lowFreq.GetClauses().Length == 0)
            {
                /*
                 * if lowFreq is empty we rewrite the high freq terms in a conjunction to
                 * prevent slow queries.
                 */
                if (highFreq.MinimumNumberShouldMatch == 0 && m_highFreqOccur != Occur.MUST)
                {
                    foreach (BooleanClause booleanClause in highFreq)
                    {
                        booleanClause.Occur = Occur.MUST;
                    }
                }
                highFreq.Boost = Boost;
                return highFreq;
            }
            else if (highFreq.GetClauses().Length == 0)
            {
                // only do low freq terms - we don't have high freq terms
                lowFreq.Boost = Boost;
                return lowFreq;
            }
            else
            {
                query.Add(highFreq, Occur.SHOULD);
                query.Add(lowFreq, Occur.MUST);
                query.Boost = Boost;
                return query;
            }
        }

        public virtual void CollectTermContext(IndexReader reader, IList<AtomicReaderContext> leaves, TermContext[] contextArray, Term[] queryTerms)
        {
            TermsEnum termsEnum = null;
            foreach (AtomicReaderContext context in leaves)
            {
                Fields fields = context.AtomicReader.Fields;
                if (fields is null)
                {
                    // reader has no fields
                    continue;
                }
                for (int i = 0; i < queryTerms.Length; i++)
                {
                    Term term = queryTerms[i];
                    TermContext termContext = contextArray[i];
                    Terms terms = fields.GetTerms(term.Field);
                    if (terms is null)
                    {
                        // field does not exist
                        continue;
                    }
                    termsEnum = terms.GetEnumerator(termsEnum);
                    if (Debugging.AssertsEnabled) Debugging.Assert(termsEnum != null);

                    if (termsEnum == TermsEnum.EMPTY)
                    {
                        continue;
                    }
                    if (termsEnum.SeekExact(term.Bytes))
                    {
                        if (termContext is null)
                        {
                            contextArray[i] = new TermContext(reader.Context, termsEnum.GetTermState(), context.Ord, termsEnum.DocFreq, termsEnum.TotalTermFreq);
                        }
                        else
                        {
                            termContext.Register(termsEnum.GetTermState(), context.Ord, termsEnum.DocFreq, termsEnum.TotalTermFreq);
                        }

                    }

                }
            }
        }

        /// <summary>
        /// Returns true iff <see cref="Search.Similarities.Similarity.Coord(int,int)"/> is disabled in scoring
        /// for the high and low frequency query instance. The top level query will
        /// always disable coords.
        /// </summary>
        public virtual bool IsCoordDisabled => m_disableCoord;

        /// <summary>
        /// Gets or Sets a minimum number of the low frequent optional BooleanClauses which must be
        /// satisfied in order to produce a match on the low frequency terms query
        /// part. This method accepts a float value in the range [0..1) as a fraction
        /// of the actual query terms in the low frequent clause or a number
        /// <tt>&gt;=1</tt> as an absolut number of clauses that need to match.
        /// 
        /// <para>
        /// By default no optional clauses are necessary for a match (unless there are
        /// no required clauses). If this method is used, then the specified number of
        /// clauses is required.
        /// </para>
        /// </summary>
        public virtual float LowFreqMinimumNumberShouldMatch
        {
            get => m_lowFreqMinNrShouldMatch;
            set => m_lowFreqMinNrShouldMatch = value;
        }


        /// <summary>
        /// Gets or Sets a minimum number of the high frequent optional BooleanClauses which must be
        /// satisfied in order to produce a match on the low frequency terms query
        /// part. This method accepts a float value in the range [0..1) as a fraction
        /// of the actual query terms in the low frequent clause or a number
        /// <tt>&gt;=1</tt> as an absolut number of clauses that need to match.
        /// 
        /// <para>
        /// By default no optional clauses are necessary for a match (unless there are
        /// no required clauses). If this method is used, then the specified number of
        /// clauses is required.
        /// </para>
        /// </summary>
        public virtual float HighFreqMinimumNumberShouldMatch
        {
            get => m_highFreqMinNrShouldMatch;
            set => m_highFreqMinNrShouldMatch = value;
        }


        public override void ExtractTerms(ISet<Term> terms)
        {
            terms.UnionWith(this.m_terms);
        }

        public override string ToString(string field)
        {
            var buffer = new StringBuilder();
            bool needParens = (Boost != 1.0) || (LowFreqMinimumNumberShouldMatch > 0);
            if (needParens)
            {
                buffer.Append('(');
            }
            for (int i = 0; i < m_terms.Count; i++)
            {
                Term t = m_terms[i];
                buffer.Append(NewTermQuery(t, null).ToString());

                if (i != m_terms.Count - 1)
                {
                    buffer.Append(", ");
                }
            }
            if (needParens)
            {
                buffer.Append(')');
            }
            if (LowFreqMinimumNumberShouldMatch > 0 || HighFreqMinimumNumberShouldMatch > 0)
            {
                buffer.Append('~');
                buffer.Append('(');
                buffer.AppendFormat(CultureInfo.InvariantCulture, "{0:0.0#######}", LowFreqMinimumNumberShouldMatch);
                buffer.AppendFormat(CultureInfo.InvariantCulture, "{0:0.0#######}", HighFreqMinimumNumberShouldMatch);
                buffer.Append(')');
            }
            if (Boost != 1.0f)
            {
                buffer.Append(ToStringUtils.Boost(Boost));
            }
            return buffer.ToString();
        }

        public override int GetHashCode()
        {
            const int prime = 31;
            int result = base.GetHashCode();
            result = prime * result + (m_disableCoord ? 1231 : 1237);
            result = prime * result + J2N.BitConversion.SingleToInt32Bits(m_highFreqBoost);
            result = prime * result + /*((highFreqOccur is null) ? 0 :*/ m_highFreqOccur.GetHashCode()/*)*/;
            result = prime * result + J2N.BitConversion.SingleToInt32Bits(m_lowFreqBoost);
            result = prime * result + /*((lowFreqOccur is null) ? 0 :*/ m_lowFreqOccur.GetHashCode()/*)*/;
            result = prime * result + J2N.BitConversion.SingleToInt32Bits(m_maxTermFrequency);
            result = prime * result + J2N.BitConversion.SingleToInt32Bits(m_lowFreqMinNrShouldMatch);
            result = prime * result + J2N.BitConversion.SingleToInt32Bits(m_highFreqMinNrShouldMatch);
            // LUCENENET specific: use structural equality comparison
            result = prime * result + ((m_terms is null) ? 0 : JCG.ListEqualityComparer<Term>.Default.GetHashCode(m_terms));
            return result;
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }
            if (!base.Equals(obj))
            {
                return false;
            }
            if (this.GetType() != obj.GetType())
            {
                return false;
            }
            var other = (CommonTermsQuery)obj;
            if (m_disableCoord != other.m_disableCoord)
            {
                return false;
            }
            if (J2N.BitConversion.SingleToInt32Bits(m_highFreqBoost) != J2N.BitConversion.SingleToInt32Bits(other.m_highFreqBoost))
            {
                return false;
            }
            if (m_highFreqOccur != other.m_highFreqOccur)
            {
                return false;
            }
            if (J2N.BitConversion.SingleToInt32Bits(m_lowFreqBoost) != J2N.BitConversion.SingleToInt32Bits(other.m_lowFreqBoost))
            {
                return false;
            }
            if (m_lowFreqOccur != other.m_lowFreqOccur)
            {
                return false;
            }
            if (J2N.BitConversion.SingleToInt32Bits(m_maxTermFrequency) != J2N.BitConversion.SingleToInt32Bits(other.m_maxTermFrequency))
            {
                return false;
            }
            if (m_lowFreqMinNrShouldMatch != other.m_lowFreqMinNrShouldMatch)
            {
                return false;
            }
            if (m_highFreqMinNrShouldMatch != other.m_highFreqMinNrShouldMatch)
            {
                return false;
            }
            if (m_terms is null)
            {
                if (other.m_terms != null)
                {
                    return false;
                }
            }
            // LUCENENET specific: use structural equality comparison
            else if (!JCG.ListEqualityComparer<Term>.Default.Equals(m_terms, other.m_terms))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Builds a new <see cref="TermQuery"/> instance.
        /// <para>This is intended for subclasses that wish to customize the generated queries.</para> </summary>
        /// <param name="term"> term </param>
        /// <param name="context"> the <see cref="TermContext"/> to be used to create the low level term query. Can be <c>null</c>. </param>
        /// <returns> new <see cref="TermQuery"/> instance </returns>
        protected virtual Query NewTermQuery(Term term, TermContext context)
        {
            return context is null ? new TermQuery(term) : new TermQuery(term, context);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="m_terms"/> collection.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the <see cref="m_terms"/> collection.</returns>
        // LUCENENET specific
        public IEnumerator<Term> GetEnumerator()
        {
            return this.m_terms.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="m_terms"/> collection.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the <see cref="m_terms"/> collection.</returns>
        // LUCENENET specific
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}