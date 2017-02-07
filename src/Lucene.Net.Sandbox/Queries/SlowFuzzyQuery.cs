using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using System;
using System.Text;

namespace Lucene.Net.Sandbox.Queries
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
    /// Implements the classic fuzzy search query. The similarity measurement
    /// is based on the Levenshtein (edit distance) algorithm.
    /// <para/>
    /// Note that, unlike <see cref="FuzzyQuery"/>, this query will silently allow
    /// for a (possibly huge) number of edit distances in comparisons, and may
    /// be extremely slow (comparing every term in the index).
    /// </summary>
    [Obsolete("Use FuzzyQuery instead.")]
    public class SlowFuzzyQuery : MultiTermQuery
    {
        public readonly static float defaultMinSimilarity = LevenshteinAutomata.MAXIMUM_SUPPORTED_DISTANCE;
        public readonly static int defaultPrefixLength = 0;
        public readonly static int defaultMaxExpansions = 50;

        private float minimumSimilarity;
        private int prefixLength;
        private bool termLongEnough = false;

        protected Term m_term;

        /// <summary>
        /// Create a new <see cref="SlowFuzzyQuery"/> that will match terms with a similarity 
        /// of at least <paramref name="minimumSimilarity"/> to <paramref name="term"/>.
        /// If a <paramref name="prefixLength"/> &gt; 0 is specified, a common prefix
        /// of that length is also required.
        /// </summary>
        /// <param name="term">the term to search for</param>
        /// <param name="minimumSimilarity">
        /// a value between 0 and 1 to set the required similarity
        /// between the query term and the matching terms. For example, for a
        /// <paramref name="minimumSimilarity"/> of <c>0.5</c> a term of the same length
        /// as the query term is considered similar to the query term if the edit distance
        /// between both terms is less than <c>length(term)*0.5</c>
        /// <para/>
        /// Alternatively, if <paramref name="minimumSimilarity"/> is >= 1f, it is interpreted
        /// as a pure Levenshtein edit distance. For example, a value of <c>2f</c>
        /// will match all terms within an edit distance of <c>2</c> from the
        /// query term. Edit distances specified in this way may not be fractional.
        /// </param>
        /// <param name="prefixLength">length of common (non-fuzzy) prefix</param>
        /// <param name="maxExpansions">
        /// the maximum number of terms to match. If this number is
        /// greater than <see cref="BooleanQuery.MaxClauseCount"/> when the query is rewritten,
        /// then the maxClauseCount will be used instead.
        /// </param>
        /// <exception cref="ArgumentException">
        /// if <paramref name="minimumSimilarity"/> is &gt;= 1 or &lt; 0
        /// or if <paramref name="prefixLength"/> &lt; 0
        /// </exception>
        public SlowFuzzyQuery(Term term, float minimumSimilarity, int prefixLength,
            int maxExpansions)
            : base(term.Field)
        {
            this.m_term = term;

            if (minimumSimilarity >= 1.0f && minimumSimilarity != (int)minimumSimilarity)
                throw new ArgumentException("fractional edit distances are not allowed");
            if (minimumSimilarity < 0.0f)
                throw new ArgumentException("minimumSimilarity < 0");
            if (prefixLength < 0)
                throw new ArgumentException("prefixLength < 0");
            if (maxExpansions < 0)
                throw new ArgumentException("maxExpansions < 0");

            MultiTermRewriteMethod = new MultiTermQuery.TopTermsScoringBooleanQueryRewrite(maxExpansions);

            string text = term.Text();
            int len = text.CodePointCount(0, text.Length);
            if (len > 0 && (minimumSimilarity >= 1f || len > 1.0f / (1.0f - minimumSimilarity)))
            {
                this.termLongEnough = true;
            }

            this.minimumSimilarity = minimumSimilarity;
            this.prefixLength = prefixLength;
        }

        /// <summary>
        /// Calls <see cref="SlowFuzzyQuery(Term, float)">SlowFuzzyQuery(term, minimumSimilarity, prefixLength, defaultMaxExpansions)</see>.
        /// </summary>
        public SlowFuzzyQuery(Term term, float minimumSimilarity, int prefixLength)
            : this(term, minimumSimilarity, prefixLength, defaultMaxExpansions)
        {
        }

        /// <summary>
        /// Calls <see cref="SlowFuzzyQuery(Term, float)">SlowFuzzyQuery(term, minimumSimilarity, 0, defaultMaxExpansions)</see>.
        /// </summary>
        public SlowFuzzyQuery(Term term, float minimumSimilarity)
            : this(term, minimumSimilarity, defaultPrefixLength, defaultMaxExpansions)
        {
        }

        /// <summary>
        /// Calls <see cref="SlowFuzzyQuery(Term, float)">SlowFuzzyQuery(term, defaultMinSimilarity, 0, defaultMaxExpansions)</see>.
        /// </summary>
        public SlowFuzzyQuery(Term term)
            : this(term, defaultMinSimilarity, defaultPrefixLength, defaultMaxExpansions)
        {
        }

        /// <summary>
        /// Gets the minimum similarity that is required for this query to match.
        /// Returns float value between 0.0 and 1.0.
        /// </summary>
        public virtual float MinSimilarity
        {
            get { return minimumSimilarity; }
        }

        /// <summary>
        /// Gets the non-fuzzy prefix length. This is the number of characters at the start
        /// of a term that must be identical (not fuzzy) to the query term if the query
        /// is to match that term.
        /// </summary>
        public virtual int PrefixLength
        {
            get { return prefixLength; }
        }

        protected override TermsEnum GetTermsEnum(Terms terms, AttributeSource atts)
        {
            if (!termLongEnough)
            {  // can only match if it's exact
                return new SingleTermsEnum(terms.GetIterator(null), m_term.Bytes);
            }
            return new SlowFuzzyTermsEnum(terms, atts, Term, minimumSimilarity, prefixLength);
        }

        /// <summary>
        /// Gets the pattern term.
        /// </summary>
        public virtual Term Term
        {
            get { return m_term; }
        }

        public override string ToString(string field)
        {
            StringBuilder buffer = new StringBuilder();
            if (!m_term.Field.Equals(field))
            {
                buffer.Append(m_term.Field);
                buffer.Append(":");
            }
            buffer.Append(m_term.Text());
            buffer.Append('~');
            buffer.Append(Number.ToString(minimumSimilarity));
            buffer.Append(ToStringUtils.Boost(Boost));
            return buffer.ToString();
        }

        public override int GetHashCode()
        {
            int prime = 31;
            int result = base.GetHashCode();
            result = prime * result + Number.SingleToInt32Bits(minimumSimilarity);
            result = prime * result + prefixLength;
            result = prime * result + ((m_term == null) ? 0 : m_term.GetHashCode());
            return result;
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
                return true;
            if (!base.Equals(obj))
                return false;
            if (GetType() != obj.GetType())
                return false;
            SlowFuzzyQuery other = (SlowFuzzyQuery)obj;
            if (Number.SingleToInt32Bits(minimumSimilarity) != Number
                .SingleToInt32Bits(other.minimumSimilarity))
                return false;
            if (prefixLength != other.prefixLength)
                return false;
            if (m_term == null)
            {
                if (other.m_term != null)
                    return false;
            }
            else if (!m_term.Equals(other.m_term))
                return false;
            return true;
        }
    }
}
