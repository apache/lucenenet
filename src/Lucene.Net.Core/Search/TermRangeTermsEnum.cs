using System.Collections.Generic;

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

    using FilteredTermsEnum = Lucene.Net.Index.FilteredTermsEnum;
    using TermsEnum = Lucene.Net.Index.TermsEnum;

    /// <summary>
    /// Subclass of FilteredTermEnum for enumerating all terms that match the
    /// specified range parameters.
    /// <p>Term enumerations are always ordered by
    /// <seealso cref="#getComparator"/>.  Each term in the enumeration is
    /// greater than all that precede it.</p>
    /// </summary>
    public class TermRangeTermsEnum : FilteredTermsEnum
    {
        private readonly bool IncludeLower;
        private readonly bool IncludeUpper;
        private readonly BytesRef LowerBytesRef;
        private readonly BytesRef UpperBytesRef;
        private readonly IComparer<BytesRef> TermComp;

        /// <summary>
        /// Enumerates all terms greater/equal than <code>lowerTerm</code>
        /// but less/equal than <code>upperTerm</code>.
        ///
        /// If an endpoint is null, it is said to be "open". Either or both
        /// endpoints may be open.  Open endpoints may not be exclusive
        /// (you can't select all but the first or last term without
        /// explicitly specifying the term to exclude.)
        /// </summary>
        /// <param name="tenum">
        ///          TermsEnum to filter </param>
        /// <param name="lowerTerm">
        ///          The term text at the lower end of the range </param>
        /// <param name="upperTerm">
        ///          The term text at the upper end of the range </param>
        /// <param name="includeLower">
        ///          If true, the <code>lowerTerm</code> is included in the range. </param>
        /// <param name="includeUpper">
        ///          If true, the <code>upperTerm</code> is included in the range. </param>
        public TermRangeTermsEnum(TermsEnum tenum, BytesRef lowerTerm, BytesRef upperTerm, bool includeLower, bool includeUpper)
            : base(tenum)
        {
            // do a little bit of normalization...
            // open ended range queries should always be inclusive.
            if (lowerTerm == null)
            {
                this.LowerBytesRef = new BytesRef();
                this.IncludeLower = true;
            }
            else
            {
                this.LowerBytesRef = lowerTerm;
                this.IncludeLower = includeLower;
            }

            if (upperTerm == null)
            {
                this.IncludeUpper = true;
                UpperBytesRef = null;
            }
            else
            {
                this.IncludeUpper = includeUpper;
                UpperBytesRef = upperTerm;
            }

            InitialSeekTerm = LowerBytesRef;
            TermComp = Comparator;
        }

        protected override AcceptStatus Accept(BytesRef term)
        {
            if (!this.IncludeLower && term.Equals(LowerBytesRef))
            {
                return AcceptStatus.NO;
            }

            // Use this field's default sort ordering
            if (UpperBytesRef != null)
            {
                int cmp = TermComp.Compare(UpperBytesRef, term);
                /*
                 * if beyond the upper term, or is exclusive and this is equal to
                 * the upper term, break out
                 */
                if ((cmp < 0) || (!IncludeUpper && cmp == 0))
                {
                    return AcceptStatus.END;
                }
            }

            return AcceptStatus.YES;
        }
    }
}