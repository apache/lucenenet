using System;
using System.Text;

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

    using AttributeSource = Lucene.Net.Util.AttributeSource;
    using LevenshteinAutomata = Lucene.Net.Util.Automaton.LevenshteinAutomata;
    using SingleTermsEnum = Lucene.Net.Index.SingleTermsEnum;
    using Term = Lucene.Net.Index.Term;
    using Terms = Lucene.Net.Index.Terms;
    using TermsEnum = Lucene.Net.Index.TermsEnum;
    using ToStringUtils = Lucene.Net.Util.ToStringUtils;

    /// <summary>
    /// Implements the fuzzy search query. The similarity measurement
    /// is based on the Damerau-Levenshtein (optimal string alignment) algorithm,
    /// though you can explicitly choose classic Levenshtein by passing <c>false</c>
    /// to the <c>transpositions</c> parameter.
    ///
    /// <para/>this query uses <see cref="MultiTermQuery.TopTermsScoringBooleanQueryRewrite"/>
    /// as default. So terms will be collected and scored according to their
    /// edit distance. Only the top terms are used for building the <see cref="BooleanQuery"/>.
    /// It is not recommended to change the rewrite mode for fuzzy queries.
    ///
    /// <para/>At most, this query will match terms up to
    /// <see cref="Lucene.Net.Util.Automaton.LevenshteinAutomata.MAXIMUM_SUPPORTED_DISTANCE"/> edits.
    /// Higher distances (especially with transpositions enabled), are generally not useful and
    /// will match a significant amount of the term dictionary. If you really want this, consider
    /// using an n-gram indexing technique (such as the SpellChecker in the
    /// <a href="{@docRoot}/../suggest/overview-summary.html">suggest module</a>) instead.
    ///
    /// <para/>NOTE: terms of length 1 or 2 will sometimes not match because of how the scaled
    /// distance between two terms is computed.  For a term to match, the edit distance between
    /// the terms must be less than the minimum length term (either the input term, or
    /// the candidate term).  For example, <see cref="FuzzyQuery"/> on term "abcd" with maxEdits=2 will
    /// not match an indexed term "ab", and <see cref="FuzzyQuery"/> on term "a" with maxEdits=2 will not
    /// match an indexed term "abc".
    /// </summary>
    public class FuzzyQuery : MultiTermQuery
    {
        public const int DefaultMaxEdits = LevenshteinAutomata.MAXIMUM_SUPPORTED_DISTANCE;
        public const int DefaultPrefixLength = 0;
        public const int DefaultMaxExpansions = 50;
        public const bool DefaultTranspositions = true;

        private readonly int maxEdits;
        private readonly int maxExpansions;
        private readonly bool transpositions;
        private readonly int prefixLength;
        private readonly Term term;

        /// <summary>
        /// Create a new <see cref="FuzzyQuery"/> that will match terms with an edit distance
        /// of at most <paramref name="maxEdits"/> to <paramref name="term"/>.
        /// If a <paramref name="prefixLength"/> &gt; 0 is specified, a common prefix
        /// of that length is also required.
        /// </summary>
        /// <param name="term"> The term to search for </param>
        /// <param name="maxEdits"> Must be &gt;= 0 and &lt;= <see cref="LevenshteinAutomata.MAXIMUM_SUPPORTED_DISTANCE"/>. </param>
        /// <param name="prefixLength"> Length of common (non-fuzzy) prefix </param>
        /// <param name="maxExpansions"> The maximum number of terms to match. If this number is
        /// greater than <see cref="BooleanQuery.MaxClauseCount"/> when the query is rewritten,
        /// then the maxClauseCount will be used instead. </param>
        /// <param name="transpositions"> <c>true</c> if transpositions should be treated as a primitive
        ///        edit operation. If this is <c>false</c>, comparisons will implement the classic
        ///        Levenshtein algorithm. </param>
        public FuzzyQuery(Term term, int maxEdits, int prefixLength, int maxExpansions, bool transpositions)
            : base(term.Field)
        {
            if (maxEdits < 0 || maxEdits > LevenshteinAutomata.MAXIMUM_SUPPORTED_DISTANCE)
            {
                throw new ArgumentOutOfRangeException(nameof(maxEdits), "maxEdits must be between 0 and " + LevenshteinAutomata.MAXIMUM_SUPPORTED_DISTANCE); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            if (prefixLength < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(prefixLength), "prefixLength cannot be negative."); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            if (maxExpansions < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExpansions), "maxExpansions cannot be negative."); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }

            this.term = term;
            this.maxEdits = maxEdits;
            this.prefixLength = prefixLength;
            this.transpositions = transpositions;
            this.maxExpansions = maxExpansions;
            MultiTermRewriteMethod = new MultiTermQuery.TopTermsScoringBooleanQueryRewrite(maxExpansions);
        }

        /// <summary>
        /// Calls <see cref="FuzzyQuery.FuzzyQuery(Term, int, int, int, bool)">
        /// FuzzyQuery(term, maxEdits, prefixLength, defaultMaxExpansions, defaultTranspositions)</see>.
        /// </summary>
        public FuzzyQuery(Term term, int maxEdits, int prefixLength)
            : this(term, maxEdits, prefixLength, DefaultMaxExpansions, DefaultTranspositions)
        {
        }

        /// <summary>
        /// Calls <see cref="FuzzyQuery(Term, int, int)">FuzzyQuery(term, maxEdits, defaultPrefixLength)</see>.
        /// </summary>
        public FuzzyQuery(Term term, int maxEdits)
            : this(term, maxEdits, DefaultPrefixLength)
        {
        }

        /// <summary>
        /// Calls <see cref="FuzzyQuery(Term, int)">FuzzyQuery(term, defaultMaxEdits)</see>.
        /// </summary>
        public FuzzyQuery(Term term)
            : this(term, DefaultMaxEdits)
        {
        }

        /// <returns> The maximum number of edit distances allowed for this query to match. </returns>
        public virtual int MaxEdits => maxEdits;

        /// <summary>
        /// Returns the non-fuzzy prefix length. This is the number of characters at the start
        /// of a term that must be identical (not fuzzy) to the query term if the query
        /// is to match that term.
        /// </summary>
        public virtual int PrefixLength => prefixLength;

        /// <summary>
        /// Returns <c>true</c> if transpositions should be treated as a primitive edit operation.
        /// If this is <c>false</c>, comparisons will implement the classic Levenshtein algorithm.
        /// </summary>
        public virtual bool Transpositions => transpositions;

        protected override TermsEnum GetTermsEnum(Terms terms, AttributeSource atts)
        {
            if (maxEdits == 0 || prefixLength >= term.Text.Length) // can only match if it's exact
            {
                return new SingleTermsEnum(terms.GetEnumerator(), term.Bytes);
            }
            return new FuzzyTermsEnum(terms, atts, Term, maxEdits, prefixLength, transpositions);
        }

        /// <summary>
        /// Returns the pattern term.
        /// </summary>
        public virtual Term Term => term;

        public override string ToString(string field)
        {
            var buffer = new StringBuilder();
            if (!term.Field.Equals(field, StringComparison.Ordinal))
            {
                buffer.Append(term.Field);
                buffer.Append(':');
            }
            buffer.Append(term.Text);
            buffer.Append('~');
            buffer.Append(Convert.ToString(maxEdits));
            buffer.Append(ToStringUtils.Boost(Boost));
            return buffer.ToString();
        }

        public override int GetHashCode()
        {
            const int prime = 31;
            int result = base.GetHashCode();
            result = prime * result + maxEdits;
            result = prime * result + prefixLength;
            result = prime * result + maxExpansions;
            result = prime * result + (transpositions ? 0 : 1);
            result = prime * result + ((term is null) ? 0 : term.GetHashCode());
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
            FuzzyQuery other = (FuzzyQuery)obj;
            if (maxEdits != other.maxEdits)
            {
                return false;
            }
            if (prefixLength != other.prefixLength)
            {
                return false;
            }
            if (maxExpansions != other.maxExpansions)
            {
                return false;
            }
            if (transpositions != other.transpositions)
            {
                return false;
            }
            if (term is null)
            {
                if (other.term != null)
                {
                    return false;
                }
            }
            else if (!term.Equals(other.term))
            {
                return false;
            }
            return true;
        }

        /// @deprecated pass integer edit distances instead.
        [Obsolete("pass integer edit distances instead.")]
        public const float DefaultMinSimilarity = LevenshteinAutomata.MAXIMUM_SUPPORTED_DISTANCE;

        /// <summary>
        /// Helper function to convert from deprecated "minimumSimilarity" fractions
        /// to raw edit distances.
        /// <para/>
        /// NOTE: this was floatToEdits() in Lucene
        /// </summary>
        /// <param name="minimumSimilarity"> Scaled similarity </param>
        /// <param name="termLen"> Length (in unicode codepoints) of the term. </param>
        /// <returns> Equivalent number of maxEdits </returns>
        [Obsolete("pass integer edit distances instead.")]
        public static int SingleToEdits(float minimumSimilarity, int termLen)
        {
            if (minimumSimilarity >= 1f)
            {
                return (int)Math.Min(minimumSimilarity, LevenshteinAutomata.MAXIMUM_SUPPORTED_DISTANCE);
            }
            else if (minimumSimilarity == 0.0f)
            {
                return 0; // 0 means exact, not infinite # of edits!
            }
            else
            {
                return Math.Min((int)((1D - minimumSimilarity) * termLen), LevenshteinAutomata.MAXIMUM_SUPPORTED_DISTANCE);
            }
        }
    }
}