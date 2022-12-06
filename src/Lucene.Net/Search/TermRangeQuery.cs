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
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Term = Lucene.Net.Index.Term;
    using Terms = Lucene.Net.Index.Terms;
    using TermsEnum = Lucene.Net.Index.TermsEnum;
    using ToStringUtils = Lucene.Net.Util.ToStringUtils;

    /// <summary>
    /// A <see cref="Query"/> that matches documents within an range of terms.
    ///
    /// <para/>This query matches the documents looking for terms that fall into the
    /// supplied range according to 
    /// <see cref="byte.CompareTo(byte)"/>. It is not intended
    /// for numerical ranges; use <see cref="NumericRangeQuery"/> instead.
    ///
    /// <para/>This query uses the
    /// <see cref="MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT"/>
    /// rewrite method.
    /// <para/>
    /// @since 2.9
    /// </summary>

    public class TermRangeQuery : MultiTermQuery
    {
        private readonly BytesRef lowerTerm; // LUCENENET: marked readonly
        private readonly BytesRef upperTerm; // LUCENENET: marked readonly
        private readonly bool includeLower; // LUCENENET: marked readonly
        private readonly bool includeUpper; // LUCENENET: marked readonly

        /// <summary>
        /// Constructs a query selecting all terms greater/equal than <paramref name="lowerTerm"/>
        /// but less/equal than <paramref name="upperTerm"/>.
        ///
        /// <para/>
        /// If an endpoint is <c>null</c>, it is said
        /// to be "open". Either or both endpoints may be open.  Open endpoints may not
        /// be exclusive (you can't select all but the first or last term without
        /// explicitly specifying the term to exclude.)
        /// </summary>
        /// <param name="field"> The field that holds both lower and upper terms. </param>
        /// <param name="lowerTerm">
        ///          The term text at the lower end of the range. </param>
        /// <param name="upperTerm">
        ///          The term text at the upper end of the range. </param>
        /// <param name="includeLower">
        ///          If true, the <paramref name="lowerTerm"/> is
        ///          included in the range. </param>
        /// <param name="includeUpper">
        ///          If true, the <paramref name="upperTerm"/> is
        ///          included in the range. </param>
        public TermRangeQuery(string field, BytesRef lowerTerm, BytesRef upperTerm, bool includeLower, bool includeUpper)
            : base(field)
        {
            this.lowerTerm = lowerTerm;
            this.upperTerm = upperTerm;
            this.includeLower = includeLower;
            this.includeUpper = includeUpper;
        }

        /// <summary>
        /// Factory that creates a new <see cref="TermRangeQuery"/> using <see cref="string"/>s for term text.
        /// </summary>
        public static TermRangeQuery NewStringRange(string field, string lowerTerm, string upperTerm, bool includeLower, bool includeUpper)
        {
            BytesRef lower = lowerTerm is null ? null : new BytesRef(lowerTerm);
            BytesRef upper = upperTerm is null ? null : new BytesRef(upperTerm);
            return new TermRangeQuery(field, lower, upper, includeLower, includeUpper);
        }

        /// <summary>
        /// Returns the lower value of this range query </summary>
        public virtual BytesRef LowerTerm => lowerTerm;

        /// <summary>
        /// Returns the upper value of this range query </summary>
        public virtual BytesRef UpperTerm => upperTerm;

        /// <summary>
        /// Returns <c>true</c> if the lower endpoint is inclusive </summary>
        public virtual bool IncludesLower => includeLower;

        /// <summary>
        /// Returns <c>true</c> if the upper endpoint is inclusive </summary>
        public virtual bool IncludesUpper => includeUpper;

        protected override TermsEnum GetTermsEnum(Terms terms, AttributeSource atts)
        {
            if (lowerTerm != null && upperTerm != null && lowerTerm.CompareTo(upperTerm) > 0)
            {
                return TermsEnum.EMPTY;
            }

            TermsEnum tenum = terms.GetEnumerator();

            if ((lowerTerm is null || (includeLower && lowerTerm.Length == 0)) && upperTerm is null)
            {
                return tenum;
            }
            return new TermRangeTermsEnum(tenum, lowerTerm, upperTerm, includeLower, includeUpper);
        }

        /// <summary>
        /// Prints a user-readable version of this query. </summary>
        public override string ToString(string field)
        {
            StringBuilder buffer = new StringBuilder();
            if (!Field.Equals(field, StringComparison.Ordinal))
            {
                buffer.Append(Field);
                buffer.Append(':');
            }
            buffer.Append(includeLower ? '[' : '{');
            // TODO: all these toStrings for queries should just output the bytes, it might not be UTF-8!
            buffer.Append(lowerTerm != null ? ("*".Equals(Term.ToString(lowerTerm), StringComparison.Ordinal) ? "\\*" : Term.ToString(lowerTerm)) : "*");
            buffer.Append(" TO ");
            buffer.Append(upperTerm != null ? ("*".Equals(Term.ToString(upperTerm), StringComparison.Ordinal) ? "\\*" : Term.ToString(upperTerm)) : "*");
            buffer.Append(includeUpper ? ']' : '}');
            buffer.Append(ToStringUtils.Boost(Boost));
            return buffer.ToString();
        }

        public override int GetHashCode()
        {
            const int prime = 31;
            int result = base.GetHashCode();
            result = prime * result + (includeLower ? 1231 : 1237);
            result = prime * result + (includeUpper ? 1231 : 1237);
            result = prime * result + ((lowerTerm is null) ? 0 : lowerTerm.GetHashCode());
            result = prime * result + ((upperTerm is null) ? 0 : upperTerm.GetHashCode());
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
            TermRangeQuery other = (TermRangeQuery)obj;
            if (includeLower != other.includeLower)
            {
                return false;
            }
            if (includeUpper != other.includeUpper)
            {
                return false;
            }
            if (lowerTerm is null)
            {
                if (other.lowerTerm != null)
                {
                    return false;
                }
            }
            else if (!lowerTerm.Equals(other.lowerTerm))
            {
                return false;
            }
            if (upperTerm is null)
            {
                if (other.upperTerm != null)
                {
                    return false;
                }
            }
            else if (!upperTerm.Equals(other.upperTerm))
            {
                return false;
            }
            return true;
        }
    }
}