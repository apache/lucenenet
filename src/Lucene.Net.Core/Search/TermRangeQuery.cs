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
    /// A Query that matches documents within an range of terms.
    ///
    /// <p>this query matches the documents looking for terms that fall into the
    /// supplied range according to {@link
    /// Byte#compareTo(Byte)}. It is not intended
    /// for numerical ranges; use <seealso cref="NumericRangeQuery"/> instead.
    ///
    /// <p>this query uses the {@link
    /// MultiTermQuery#CONSTANT_SCORE_AUTO_REWRITE_DEFAULT}
    /// rewrite method.
    /// @since 2.9
    /// </summary>

    public class TermRangeQuery : MultiTermQuery
    {
        private BytesRef lowerTerm;
        private BytesRef upperTerm;
        private bool includeLower;
        private bool includeUpper;

        /// <summary>
        /// Constructs a query selecting all terms greater/equal than <code>lowerTerm</code>
        /// but less/equal than <code>upperTerm</code>.
        ///
        /// <p>
        /// If an endpoint is null, it is said
        /// to be "open". Either or both endpoints may be open.  Open endpoints may not
        /// be exclusive (you can't select all but the first or last term without
        /// explicitly specifying the term to exclude.)
        /// </summary>
        /// <param name="field"> The field that holds both lower and upper terms. </param>
        /// <param name="lowerTerm">
        ///          The term text at the lower end of the range </param>
        /// <param name="upperTerm">
        ///          The term text at the upper end of the range </param>
        /// <param name="includeLower">
        ///          If true, the <code>lowerTerm</code> is
        ///          included in the range. </param>
        /// <param name="includeUpper">
        ///          If true, the <code>upperTerm</code> is
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
        /// Factory that creates a new TermRangeQuery using Strings for term text.
        /// </summary>
        public static TermRangeQuery NewStringRange(string field, string lowerTerm, string upperTerm, bool includeLower, bool includeUpper)
        {
            BytesRef lower = lowerTerm == null ? null : new BytesRef(lowerTerm);
            BytesRef upper = upperTerm == null ? null : new BytesRef(upperTerm);
            return new TermRangeQuery(field, lower, upper, includeLower, includeUpper);
        }

        /// <summary>
        /// Returns the lower value of this range query </summary>
        public virtual BytesRef LowerTerm
        {
            get
            {
                return lowerTerm;
            }
        }

        /// <summary>
        /// Returns the upper value of this range query </summary>
        public virtual BytesRef UpperTerm
        {
            get
            {
                return upperTerm;
            }
        }

        /// <summary>
        /// Returns <code>true</code> if the lower endpoint is inclusive </summary>
        public virtual bool IncludesLower
        {
            get { return includeLower; }
        }

        /// <summary>
        /// Returns <code>true</code> if the upper endpoint is inclusive </summary>
        public virtual bool IncludesUpper
        {
            get { return includeUpper; }
        }

        protected override TermsEnum GetTermsEnum(Terms terms, AttributeSource atts)
        {
            if (lowerTerm != null && upperTerm != null && lowerTerm.CompareTo(upperTerm) > 0)
            {
                return TermsEnum.EMPTY;
            }

            TermsEnum tenum = terms.Iterator(null);

            if ((lowerTerm == null || (includeLower && lowerTerm.Length == 0)) && upperTerm == null)
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
            if (!Field.Equals(field))
            {
                buffer.Append(Field);
                buffer.Append(":");
            }
            buffer.Append(includeLower ? '[' : '{');
            // TODO: all these toStrings for queries should just output the bytes, it might not be UTF-8!
            buffer.Append(lowerTerm != null ? ("*".Equals(Term.ToString(lowerTerm)) ? "\\*" : Term.ToString(lowerTerm)) : "*");
            buffer.Append(" TO ");
            buffer.Append(upperTerm != null ? ("*".Equals(Term.ToString(upperTerm)) ? "\\*" : Term.ToString(upperTerm)) : "*");
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
            result = prime * result + ((lowerTerm == null) ? 0 : lowerTerm.GetHashCode());
            result = prime * result + ((upperTerm == null) ? 0 : upperTerm.GetHashCode());
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
            if (lowerTerm == null)
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
            if (upperTerm == null)
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