using System;

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

    using BytesRef = Lucene.Net.Util.BytesRef;

    /// <summary>
    /// A <see cref="Filter"/> that restricts search results to a range of term
    /// values in a given field.
    ///
    /// <para/>This filter matches the documents looking for terms that fall into the
    /// supplied range according to 
    /// <see cref="byte.CompareTo(byte)"/>,  It is not intended
    /// for numerical ranges; use <see cref="NumericRangeFilter"/> instead.
    ///
    /// <para/>If you construct a large number of range filters with different ranges but on the
    /// same field, <see cref="FieldCacheRangeFilter"/> may have significantly better performance.
    /// <para/>
    /// @since 2.9
    /// </summary>
    public class TermRangeFilter : MultiTermQueryWrapperFilter<TermRangeQuery>
    {
        /// <param name="fieldName"> The field this range applies to </param>
        /// <param name="lowerTerm"> The lower bound on this range </param>
        /// <param name="upperTerm"> The upper bound on this range </param>
        /// <param name="includeLower"> Does this range include the lower bound? </param>
        /// <param name="includeUpper"> Does this range include the upper bound? </param>
        /// <exception cref="ArgumentException"> if both terms are <c>null</c> or if
        ///  lowerTerm is <c>null</c> and includeLower is <c>true</c> (similar for upperTerm
        ///  and includeUpper) </exception>
        public TermRangeFilter(string fieldName, BytesRef lowerTerm, BytesRef upperTerm, bool includeLower, bool includeUpper)
            : base(new TermRangeQuery(fieldName, lowerTerm, upperTerm, includeLower, includeUpper))
        {
        }

        /// <summary>
        /// Factory that creates a new <see cref="TermRangeFilter"/> using <see cref="string"/>s for term text.
        /// </summary>
        public static TermRangeFilter NewStringRange(string field, string lowerTerm, string upperTerm, bool includeLower, bool includeUpper)
        {
            BytesRef lower = lowerTerm is null ? null : new BytesRef(lowerTerm);
            BytesRef upper = upperTerm is null ? null : new BytesRef(upperTerm);
            return new TermRangeFilter(field, lower, upper, includeLower, includeUpper);
        }

        /// <summary>
        /// Constructs a filter for field <paramref name="fieldName"/> matching
        /// less than or equal to <paramref name="upperTerm"/>.
        /// </summary>
        public static TermRangeFilter Less(string fieldName, BytesRef upperTerm)
        {
            return new TermRangeFilter(fieldName, null, upperTerm, false, true);
        }

        /// <summary>
        /// Constructs a filter for field <paramref name="fieldName"/> matching
        /// greater than or equal to <paramref name="lowerTerm"/>.
        /// </summary>
        public static TermRangeFilter More(string fieldName, BytesRef lowerTerm)
        {
            return new TermRangeFilter(fieldName, lowerTerm, null, true, false);
        }

        /// <summary>
        /// Returns the lower value of this range filter </summary>
        public virtual BytesRef LowerTerm => m_query.LowerTerm;

        /// <summary>
        /// Returns the upper value of this range filter </summary>
        public virtual BytesRef UpperTerm => m_query.UpperTerm;

        /// <summary>
        /// Returns <c>true</c> if the lower endpoint is inclusive </summary>
        public virtual bool IncludesLower => m_query.IncludesLower;

        /// <summary>
        /// Returns <c>true</c> if the upper endpoint is inclusive </summary>
        public virtual bool IncludesUpper => m_query.IncludesUpper;
    }
}