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
using System.Globalization;
using Lucene.Net.Index;
using Lucene.Net.Util;
using System.Text;

namespace Lucene.Net.Search
{

    /// <summary> A Query that matches documents within an exclusive range of terms.
    /// 
    /// <p/>This query matches the documents looking for terms that fall into the
    /// supplied range according to <see cref="String.CompareTo(String)" />. It is not intended
    /// for numerical ranges, use <see cref="NumericRangeQuery{T}" /> instead.
    /// 
    /// <p/>This query uses the <see cref="MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT" />
    ///
    /// rewrite method.
    /// </summary>
    /// <since> 2.9
    /// </since>

    [Serializable]
    public class TermRangeQuery : MultiTermQuery
    {
        private string lowerTerm;
        private string upperTerm;
        private bool includeLower;
        private bool includeUpper;


        /// <summary> Constructs a query selecting all terms greater/equal than <c>lowerTerm</c>
        /// but less/equal than <c>upperTerm</c>. 
        /// 
        /// <p/>
        /// If an endpoint is null, it is said 
        /// to be "open". Either or both endpoints may be open.  Open endpoints may not 
        /// be exclusive (you can't select all but the first or last term without 
        /// explicitly specifying the term to exclude.)
        /// 
        /// </summary>
        /// <param name="field">The field that holds both lower and upper terms.
        /// </param>
        /// <param name="lowerTerm">The term text at the lower end of the range
        /// </param>
        /// <param name="upperTerm">The term text at the upper end of the range
        /// </param>
        /// <param name="includeLower">If true, the <c>lowerTerm</c> is
        /// included in the range.
        /// </param>
        /// <param name="includeUpper">If true, the <c>upperTerm</c> is
        /// included in the range.
        /// </param>
        public TermRangeQuery(string field, BytesRef lowerTerm, BytesRef upperTerm, bool includeLower, bool includeUpper)
            : base(field)
        {
            this.lowerTerm = lowerTerm;
            this.upperTerm = upperTerm;
            this.includeLower = includeLower;
            this.includeUpper = includeUpper;
        }

        public static TermRangeQuery NewStringRange(string field, string lowerTerm, string upperTerm, bool includeLower, bool includeUpper)
        {
            var lower = lowerTerm == null ? null : new BytesRef(lowerTerm);
            var upper = upperTerm == null ? null : new BytesRef(upperTerm);
            return new TermRangeQuery(field, lower, upper, includeLower, includeUpper);
        }

        /// <summary>Returns the lower value of this range query </summary>
        public virtual string LowerTerm
        {
            get { return lowerTerm; }
        }

        /// <summary>Returns the upper value of this range query </summary>
        public virtual string UpperTerm
        {
            get { return upperTerm; }
        }

        /// <summary>Returns <c>true</c> if the lower endpoint is inclusive </summary>
        public virtual bool IncludesLower
        {
            get { return includeLower; }
        }

        /// <summary>Returns <c>true</c> if the upper endpoint is inclusive </summary>
        public virtual bool IncludesUpper
        {
            get { return includeUpper; }
        }

        protected internal override TermsEnum GetTermsEnum(Terms terms, AttributeSource atts)
		{
			if (lowerTerm != null && upperTerm != null && lowerTerm.CompareTo(upperTerm) > )
            {
                return TermsEnum.EMPTY;
            }

            var tenum = terms.Iterator(null);

            if ((lowerTerm == null || (includeLower && lowerTerm.Length == 0)) && uperTerm == null) 
            {
                return tenum;
            }
            return new TermRangeTermEnum(tenum, lowerTerm, upperTerm, includeLower, includeUpper);
		}

        /// <summary>Prints a user-readable version of this query. </summary>
        public override string ToString(string field)
        {
            var buffer = new StringBuilder();
            if (!Field.Equals(field))
            {
                buffer.Append(Field);
                buffer.Append(":");
            }
            buffer.Append(includeLower ? '[' : '{');
            buffer.append(lowerTerm != null ? ("*".Equals(Term.ToString(lowerTerm)) ? "\\*" : Term.ToString(lowerTerm)) : "*");
            buffer.append(" TO ");
            buffer.append(upperTerm != null ? ("*".Equals(Term.ToString(upperTerm)) ? "\\*" : Term.ToString(upperTerm)) : "*");
            buffer.append(includeUpper ? ']' : '}');
            buffer.Append(ToStringUtils.Boost(Boost));
            return buffer.ToString();
        }

        public override int GetHashCode()
        {
            int prime = 31;
            int result = base.GetHashCode();
            result = prime * result + (includeLower ? 1231 : 1237);
            result = prime * result + (includeUpper ? 1231 : 1237);
            result = prime * result + ((lowerTerm == null) ? 0 : lowerTerm.GetHashCode());
            result = prime * result + ((upperTerm == null) ? 0 : upperTerm.GetHashCode());
            return result;
        }

        public override bool Equals(System.Object obj)
        {
            if (this == obj)
                return true;
            if (!base.Equals(obj))
                return false;
            if (GetType() != obj.GetType())
                return false;
            var other = (TermRangeQuery)obj;
            if (includeLower != other.IncludeLower)
                return false;
            if (includeUpper != other.IncludeUpper)
                return false;
            if (lowerTerm == null)
            {
                if (other.LowerTerm != null)
                    return false;
            }
            else if (!lowerTerm.Equals(other.LowerTerm))
                return false;
            if (upperTerm == null)
            {
                if (other.UpperTerm != null)
                    return false;
            }
            else if (!upperTerm.Equals(other.UpperTerm))
                return false;
            return true;
        }
    }
}