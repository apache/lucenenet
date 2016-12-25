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
        // LUCENENET TODO: Rename (private)
        private BytesRef LowerTerm_Renamed;
        private BytesRef UpperTerm_Renamed;
        private bool IncludeLower;
        private bool IncludeUpper;

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
            this.LowerTerm_Renamed = lowerTerm;
            this.UpperTerm_Renamed = upperTerm;
            this.IncludeLower = includeLower;
            this.IncludeUpper = includeUpper;
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
                return LowerTerm_Renamed;
            }
        }

        /// <summary>
        /// Returns the upper value of this range query </summary>
        public virtual BytesRef UpperTerm
        {
            get
            {
                return UpperTerm_Renamed;
            }
        }

        /// <summary>
        /// Returns <code>true</code> if the lower endpoint is inclusive </summary>
        public virtual bool IncludesLower
        {
            get { return IncludeLower; }
        }

        /// <summary>
        /// Returns <code>true</code> if the upper endpoint is inclusive </summary>
        public virtual bool IncludesUpper
        {
            get { return IncludeUpper; }
        }

        protected override TermsEnum GetTermsEnum(Terms terms, AttributeSource atts)
        {
            if (LowerTerm_Renamed != null && UpperTerm_Renamed != null && LowerTerm_Renamed.CompareTo(UpperTerm_Renamed) > 0)
            {
                return TermsEnum.EMPTY;
            }

            TermsEnum tenum = terms.Iterator(null);

            if ((LowerTerm_Renamed == null || (IncludeLower && LowerTerm_Renamed.Length == 0)) && UpperTerm_Renamed == null)
            {
                return tenum;
            }
            return new TermRangeTermsEnum(tenum, LowerTerm_Renamed, UpperTerm_Renamed, IncludeLower, IncludeUpper);
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
            buffer.Append(IncludeLower ? '[' : '{');
            // TODO: all these toStrings for queries should just output the bytes, it might not be UTF-8!
            buffer.Append(LowerTerm_Renamed != null ? ("*".Equals(Term.ToString(LowerTerm_Renamed)) ? "\\*" : Term.ToString(LowerTerm_Renamed)) : "*");
            buffer.Append(" TO ");
            buffer.Append(UpperTerm_Renamed != null ? ("*".Equals(Term.ToString(UpperTerm_Renamed)) ? "\\*" : Term.ToString(UpperTerm_Renamed)) : "*");
            buffer.Append(IncludeUpper ? ']' : '}');
            buffer.Append(ToStringUtils.Boost(Boost));
            return buffer.ToString();
        }

        public override int GetHashCode()
        {
            const int prime = 31;
            int result = base.GetHashCode();
            result = prime * result + (IncludeLower ? 1231 : 1237);
            result = prime * result + (IncludeUpper ? 1231 : 1237);
            result = prime * result + ((LowerTerm_Renamed == null) ? 0 : LowerTerm_Renamed.GetHashCode());
            result = prime * result + ((UpperTerm_Renamed == null) ? 0 : UpperTerm_Renamed.GetHashCode());
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
            if (IncludeLower != other.IncludeLower)
            {
                return false;
            }
            if (IncludeUpper != other.IncludeUpper)
            {
                return false;
            }
            if (LowerTerm_Renamed == null)
            {
                if (other.LowerTerm_Renamed != null)
                {
                    return false;
                }
            }
            else if (!LowerTerm_Renamed.Equals(other.LowerTerm_Renamed))
            {
                return false;
            }
            if (UpperTerm_Renamed == null)
            {
                if (other.UpperTerm_Renamed != null)
                {
                    return false;
                }
            }
            else if (!UpperTerm_Renamed.Equals(other.UpperTerm_Renamed))
            {
                return false;
            }
            return true;
        }
    }
}