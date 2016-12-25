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
    /// though you can explicitly choose classic Levenshtein by passing <code>false</code>
    /// to the <code>transpositions</code> parameter.
    ///
    /// <p>this query uses <seealso cref="MultiTermQuery.TopTermsScoringBooleanQueryRewrite"/>
    /// as default. So terms will be collected and scored according to their
    /// edit distance. Only the top terms are used for building the <seealso cref="BooleanQuery"/>.
    /// It is not recommended to change the rewrite mode for fuzzy queries.
    ///
    /// <p>At most, this query will match terms up to
    /// {@value Lucene.Net.Util.Automaton.LevenshteinAutomata#MAXIMUM_SUPPORTED_DISTANCE} edits.
    /// Higher distances (especially with transpositions enabled), are generally not useful and
    /// will match a significant amount of the term dictionary. If you really want this, consider
    /// using an n-gram indexing technique (such as the SpellChecker in the
    /// <a href="{@docRoot}/../suggest/overview-summary.html">suggest module</a>) instead.
    ///
    /// <p>NOTE: terms of length 1 or 2 will sometimes not match because of how the scaled
    /// distance between two terms is computed.  For a term to match, the edit distance between
    /// the terms must be less than the minimum length term (either the input term, or
    /// the candidate term).  For example, FuzzyQuery on term "abcd" with maxEdits=2 will
    /// not match an indexed term "ab", and FuzzyQuery on term "a" with maxEdits=2 will not
    /// match an indexed term "abc".
    /// </summary>
    public class FuzzyQuery : MultiTermQuery
    {
        public const int DefaultMaxEdits = LevenshteinAutomata.MAXIMUM_SUPPORTED_DISTANCE;
        public const int DefaultPrefixLength = 0;
        public const int DefaultMaxExpansions = 50;
        public const bool DefaultTranspositions = true;

        private readonly int MaxEdits_Renamed; // LUCENENET TODO: Rename (private)
        private readonly int MaxExpansions; // LUCENENET TODO: Rename (private)
        private readonly bool Transpositions_Renamed; // LUCENENET TODO: Rename (private)
        private readonly int PrefixLength_Renamed; // LUCENENET TODO: Rename (private)
        private readonly Term _term;

        /// <summary>
        /// Create a new FuzzyQuery that will match terms with an edit distance
        /// of at most <code>maxEdits</code> to <code>term</code>.
        /// If a <code>prefixLength</code> &gt; 0 is specified, a common prefix
        /// of that length is also required.
        /// </summary>
        /// <param name="term"> the term to search for </param>
        /// <param name="maxEdits"> must be >= 0 and <= <seealso cref="LevenshteinAutomata#MAXIMUM_SUPPORTED_DISTANCE"/>. </param>
        /// <param name="prefixLength"> length of common (non-fuzzy) prefix </param>
        /// <param name="maxExpansions"> the maximum number of terms to match. If this number is
        ///  greater than <seealso cref="BooleanQuery#getMaxClauseCount"/> when the query is rewritten,
        ///  then the maxClauseCount will be used instead. </param>
        /// <param name="transpositions"> true if transpositions should be treated as a primitive
        ///        edit operation. If this is false, comparisons will implement the classic
        ///        Levenshtein algorithm. </param>
        public FuzzyQuery(Term term, int maxEdits, int prefixLength, int maxExpansions, bool transpositions)
            : base(term.Field)
        {
            if (maxEdits < 0 || maxEdits > LevenshteinAutomata.MAXIMUM_SUPPORTED_DISTANCE)
            {
                throw new System.ArgumentException("maxEdits must be between 0 and " + LevenshteinAutomata.MAXIMUM_SUPPORTED_DISTANCE);
            }
            if (prefixLength < 0)
            {
                throw new System.ArgumentException("prefixLength cannot be negative.");
            }
            if (maxExpansions < 0)
            {
                throw new System.ArgumentException("maxExpansions cannot be negative.");
            }

            this._term = term;
            this.MaxEdits_Renamed = maxEdits;
            this.PrefixLength_Renamed = prefixLength;
            this.Transpositions_Renamed = transpositions;
            this.MaxExpansions = maxExpansions;
            MultiTermRewriteMethod = new MultiTermQuery.TopTermsScoringBooleanQueryRewrite(maxExpansions);
        }

        /// <summary>
        /// Calls {@link #FuzzyQuery(Term, int, int, int, boolean)
        /// FuzzyQuery(term, maxEdits, prefixLength, defaultMaxExpansions, defaultTranspositions)}.
        /// </summary>
        public FuzzyQuery(Term term, int maxEdits, int prefixLength)
            : this(term, maxEdits, prefixLength, DefaultMaxExpansions, DefaultTranspositions)
        {
        }

        /// <summary>
        /// Calls <seealso cref="#FuzzyQuery(Term, int, int) FuzzyQuery(term, maxEdits, defaultPrefixLength)"/>.
        /// </summary>
        public FuzzyQuery(Term term, int maxEdits)
            : this(term, maxEdits, DefaultPrefixLength)
        {
        }

        /// <summary>
        /// Calls <seealso cref="#FuzzyQuery(Term, int) FuzzyQuery(term, defaultMaxEdits)"/>.
        /// </summary>
        public FuzzyQuery(Term term)
            : this(term, DefaultMaxEdits)
        {
        }

        /// <returns> the maximum number of edit distances allowed for this query to match. </returns>
        public virtual int MaxEdits
        {
            get
            {
                return MaxEdits_Renamed;
            }
        }

        /// <summary>
        /// Returns the non-fuzzy prefix length. this is the number of characters at the start
        /// of a term that must be identical (not fuzzy) to the query term if the query
        /// is to match that term.
        /// </summary>
        public virtual int PrefixLength
        {
            get
            {
                return PrefixLength_Renamed;
            }
        }

        /// <summary>
        /// Returns true if transpositions should be treated as a primitive edit operation.
        /// If this is false, comparisons will implement the classic Levenshtein algorithm.
        /// </summary>
        public virtual bool Transpositions
        {
            get
            {
                return Transpositions_Renamed;
            }
        }

        protected override TermsEnum GetTermsEnum(Terms terms, AttributeSource atts)
        {
            if (MaxEdits_Renamed == 0 || PrefixLength_Renamed >= _term.Text().Length) // can only match if it's exact
            {
                return new SingleTermsEnum(terms.Iterator(null), _term.Bytes);
            }
            return new FuzzyTermsEnum(terms, atts, Term, MaxEdits_Renamed, PrefixLength_Renamed, Transpositions_Renamed);
        }

        /// <summary>
        /// Returns the pattern term.
        /// </summary>
        public virtual Term Term
        {
            get
            {
                return _term;
            }
        }

        public override string ToString(string field)
        {
            var buffer = new StringBuilder();
            if (!_term.Field.Equals(field))
            {
                buffer.Append(_term.Field);
                buffer.Append(":");
            }
            buffer.Append(_term.Text());
            buffer.Append('~');
            buffer.Append(Convert.ToString(MaxEdits_Renamed));
            buffer.Append(ToStringUtils.Boost(Boost));
            return buffer.ToString();
        }

        public override int GetHashCode()
        {
            const int prime = 31;
            int result = base.GetHashCode();
            result = prime * result + MaxEdits_Renamed;
            result = prime * result + PrefixLength_Renamed;
            result = prime * result + MaxExpansions;
            result = prime * result + (Transpositions_Renamed ? 0 : 1);
            result = prime * result + ((_term == null) ? 0 : _term.GetHashCode());
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
            if (MaxEdits_Renamed != other.MaxEdits_Renamed)
            {
                return false;
            }
            if (PrefixLength_Renamed != other.PrefixLength_Renamed)
            {
                return false;
            }
            if (MaxExpansions != other.MaxExpansions)
            {
                return false;
            }
            if (Transpositions_Renamed != other.Transpositions_Renamed)
            {
                return false;
            }
            if (_term == null)
            {
                if (other._term != null)
                {
                    return false;
                }
            }
            else if (!_term.Equals(other._term))
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
        /// </summary>
        /// <param name="minimumSimilarity"> scaled similarity </param>
        /// <param name="termLen"> length (in unicode codepoints) of the term. </param>
        /// <returns> equivalent number of maxEdits </returns>
        /// @deprecated pass integer edit distances instead.
        [Obsolete("pass integer edit distances instead.")]
        public static int FloatToEdits(float minimumSimilarity, int termLen)
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