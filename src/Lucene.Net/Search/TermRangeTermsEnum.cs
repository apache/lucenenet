using System.Collections.Generic;

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
    using FilteredTermsEnum = Lucene.Net.Index.FilteredTermsEnum;
    using TermsEnum = Lucene.Net.Index.TermsEnum;

    /// <summary>
    /// Subclass of <see cref="FilteredTermsEnum"/> for enumerating all terms that match the
    /// specified range parameters.
    /// <para>Term enumerations are always ordered by
    /// <see cref="FilteredTermsEnum.Comparer"/>.  Each term in the enumeration is
    /// greater than all that precede it.</para>
    /// </summary>
    public class TermRangeTermsEnum : FilteredTermsEnum
    {
        private readonly bool includeLower;
        private readonly bool includeUpper;
        private readonly BytesRef lowerBytesRef;
        private readonly BytesRef upperBytesRef;
        private readonly IComparer<BytesRef> termComp;

        /// <summary>
        /// Enumerates all terms greater/equal than <paramref name="lowerTerm"/>
        /// but less/equal than <paramref name="upperTerm"/>.
        ///
        /// If an endpoint is <c>null</c>, it is said to be "open". Either or both
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
        ///          If true, the <paramref name="lowerTerm"/> is included in the range. </param>
        /// <param name="includeUpper">
        ///          If true, the <paramref name="upperTerm"/> is included in the range. </param>
        public TermRangeTermsEnum(TermsEnum tenum, BytesRef lowerTerm, BytesRef upperTerm, bool includeLower, bool includeUpper)
            : base(tenum)
        {
            // do a little bit of normalization...
            // open ended range queries should always be inclusive.
            if (lowerTerm is null)
            {
                this.lowerBytesRef = new BytesRef();
                this.includeLower = true;
            }
            else
            {
                this.lowerBytesRef = lowerTerm;
                this.includeLower = includeLower;
            }

            if (upperTerm is null)
            {
                this.includeUpper = true;
                upperBytesRef = null;
            }
            else
            {
                this.includeUpper = includeUpper;
                upperBytesRef = upperTerm;
            }

            SetInitialSeekTerm(lowerBytesRef);
            termComp = Comparer;
        }

        protected override AcceptStatus Accept(BytesRef term)
        {
            if (!this.includeLower && term.Equals(lowerBytesRef))
            {
                return AcceptStatus.NO;
            }

            // Use this field's default sort ordering
            if (upperBytesRef != null)
            {
                int cmp = termComp.Compare(upperBytesRef, term);
                /*
                 * if beyond the upper term, or is exclusive and this is equal to
                 * the upper term, break out
                 */
                if ((cmp < 0) || (!includeUpper && cmp == 0))
                {
                    return AcceptStatus.END;
                }
            }

            return AcceptStatus.YES;
        }
    }
}