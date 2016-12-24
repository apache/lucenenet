namespace Lucene.Net.Search
{
    using BytesRef = Lucene.Net.Util.BytesRef;

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
    /// A Filter that restricts search results to a range of term
    /// values in a given field.
    ///
    /// <p>this filter matches the documents looking for terms that fall into the
    /// supplied range according to {@link
    /// Byte#compareTo(Byte)},  It is not intended
    /// for numerical ranges; use <seealso cref="NumericRangeFilter"/> instead.
    ///
    /// <p>If you construct a large number of range filters with different ranges but on the
    /// same field, <seealso cref="FieldCacheRangeFilter"/> may have significantly better performance.
    /// @since 2.9
    /// </summary>
    public class TermRangeFilter : MultiTermQueryWrapperFilter<TermRangeQuery>
    {
        /// <param name="fieldName"> The field this range applies to </param>
        /// <param name="lowerTerm"> The lower bound on this range </param>
        /// <param name="upperTerm"> The upper bound on this range </param>
        /// <param name="includeLower"> Does this range include the lower bound? </param>
        /// <param name="includeUpper"> Does this range include the upper bound? </param>
        /// <exception cref="IllegalArgumentException"> if both terms are null or if
        ///  lowerTerm is null and includeLower is true (similar for upperTerm
        ///  and includeUpper) </exception>
        public TermRangeFilter(string fieldName, BytesRef lowerTerm, BytesRef upperTerm, bool includeLower, bool includeUpper)
            : base(new TermRangeQuery(fieldName, lowerTerm, upperTerm, includeLower, includeUpper))
        {
        }

        /// <summary>
        /// Factory that creates a new TermRangeFilter using Strings for term text.
        /// </summary>
        public static TermRangeFilter NewStringRange(string field, string lowerTerm, string upperTerm, bool includeLower, bool includeUpper)
        {
            BytesRef lower = lowerTerm == null ? null : new BytesRef(lowerTerm);
            BytesRef upper = upperTerm == null ? null : new BytesRef(upperTerm);
            return new TermRangeFilter(field, lower, upper, includeLower, includeUpper);
        }

        /// <summary>
        /// Constructs a filter for field <code>fieldName</code> matching
        /// less than or equal to <code>upperTerm</code>.
        /// </summary>
        public static TermRangeFilter Less(string fieldName, BytesRef upperTerm)
        {
            return new TermRangeFilter(fieldName, null, upperTerm, false, true);
        }

        /// <summary>
        /// Constructs a filter for field <code>fieldName</code> matching
        /// greater than or equal to <code>lowerTerm</code>.
        /// </summary>
        public static TermRangeFilter More(string fieldName, BytesRef lowerTerm)
        {
            return new TermRangeFilter(fieldName, lowerTerm, null, true, false);
        }

        /// <summary>
        /// Returns the lower value of this range filter </summary>
        public virtual BytesRef LowerTerm
        {
            get
            {
                return Query.LowerTerm;
            }
        }

        /// <summary>
        /// Returns the upper value of this range filter </summary>
        public virtual BytesRef UpperTerm
        {
            get
            {
                return Query.UpperTerm;
            }
        }

        /// <summary>
        /// Returns <code>true</code> if the lower endpoint is inclusive </summary>
        public virtual bool IncludesLower() // LUCENENET TODO: Make property
        {
            return Query.IncludesLower();
        }

        /// <summary>
        /// Returns <code>true</code> if the upper endpoint is inclusive </summary>
        public virtual bool IncludesUpper() // LUCENENET TODO: Make property
        {
            return Query.IncludesUpper();
        }
    }
}