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
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Index;
using Lucene.Net.Support;
using Lucene.Net.Util;
using IndexReader = Lucene.Net.Index.IndexReader;
using Single = Lucene.Net.Support.Single;
using Term = Lucene.Net.Index.Term;
using ToStringUtils = Lucene.Net.Util.ToStringUtils;
using Lucene.Net.Util.Automaton;
using System.Text;

namespace Lucene.Net.Search
{

    /// <summary>Implements the fuzzy search query. The similarity measurement
    /// is based on the Levenshtein (edit distance) algorithm.
    /// 
    /// Warning: this query is not very scalable with its default prefix
    /// length of 0 - in this case, *every* term will be enumerated and
    /// cause an edit score calculation.
    /// 
    /// </summary>
    [Serializable]
    public class FuzzyQuery : MultiTermQuery
    {
        public const int defaultMaxEdits = LevenshteinAutomata.MAXIMUM_SUPPORTED_DISTANCE;
        public const int defaultPrefixLength = 0;
        public const int defaultMaxExpansions = 50;
        public const bool defaultTranspositions = true;

        private readonly int maxEdits;
        private readonly int maxExpansions;
        private readonly bool transpositions;
        private readonly int prefixLength;
        private readonly Term term;

        public FuzzyQuery(Term term, int maxEdits, int prefixLength, int maxExpansions, bool transpositions)
            : base(term.Field)
        {
            if (maxEdits < 0 || maxEdits > LevenshteinAutomata.MAXIMUM_SUPPORTED_DISTANCE)
            {
                throw new ArgumentException("maxEdits must be between 0 and " + LevenshteinAutomata.MAXIMUM_SUPPORTED_DISTANCE);
            }
            if (prefixLength < 0)
            {
                throw new ArgumentException("prefixLength cannot be negative.");
            }
            if (maxExpansions < 0)
            {
                throw new ArgumentException("maxExpansions cannot be negative.");
            }

            this.term = term;
            this.maxEdits = maxEdits;
            this.prefixLength = prefixLength;
            this.transpositions = transpositions;
            this.maxExpansions = maxExpansions;
            SetRewriteMethod(new MultiTermQuery.TopTermsScoringBooleanQueryRewrite(maxExpansions));
        }

        public FuzzyQuery(Term term, int maxEdits, int prefixLength)
            : this(term, maxEdits, prefixLength, defaultMaxExpansions, defaultTranspositions)
        {
        }

        public FuzzyQuery(Term term, int maxEdits)
            : this(term, maxEdits, defaultPrefixLength)
        {
        }

        public FuzzyQuery(Term term)
            : this(term, defaultMaxEdits)
        {
        }

        public virtual int MaxEdits
        {
            get { return maxEdits; }
        }

        /// <summary> Returns the non-fuzzy prefix length. This is the number of characters at the start
        /// of a term that must be identical (not fuzzy) to the query term if the query
        /// is to match that term. 
        /// </summary>
        public virtual int PrefixLength
        {
            get { return prefixLength; }
        }

        protected internal override TermsEnum GetTermsEnum(Terms terms, AttributeSource atts)
        {
            if (maxEdits == 0 || prefixLength >= term.Text.Length)
            {  // can only match if it's exact
                return new SingleTermsEnum(terms.Iterator(null), term.bytes);
            }
            return new FuzzyTermsEnum(terms, atts, Term, maxEdits, prefixLength, transpositions);
        }

        public Term Term
        {
            get { return term; }
        }

        public override string ToString(string field)
        {
            StringBuilder buffer = new StringBuilder();
            if (!term.Field.Equals(field))
            {
                buffer.Append(term.Field);
                buffer.Append(":");
            }
            buffer.Append(term.Text);
            buffer.Append('~');
            buffer.Append(maxEdits);
            buffer.Append(ToStringUtils.Boost(Boost));
            return buffer.ToString();
        }

        public override int GetHashCode()
        {
            int prime = 31;
            int result = base.GetHashCode();
            result = prime * result + maxEdits;
            result = prime * result + prefixLength;
            result = prime * result + maxExpansions;
            result = prime * result + (transpositions ? 0 : 1);
            result = prime * result + ((term == null) ? 0 : term.GetHashCode());
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
            FuzzyQuery other = (FuzzyQuery)obj;
            if (maxEdits != other.maxEdits)
                return false;
            if (prefixLength != other.prefixLength)
                return false;
            if (maxExpansions != other.maxExpansions)
                return false;
            if (transpositions != other.transpositions)
                return false;
            if (term == null)
            {
                if (other.term != null)
                    return false;
            }
            else if (!term.Equals(other.term))
                return false;
            return true;
        }

        [Obsolete]
        public const float defaultMinSimilarity = LevenshteinAutomata.MAXIMUM_SUPPORTED_DISTANCE;

        [Obsolete]
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
                return Math.Min((int)((1D - minimumSimilarity) * termLen),
                  LevenshteinAutomata.MAXIMUM_SUPPORTED_DISTANCE);
            }
        }
    }
}