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
using Lucene.Net.Index;
using Lucene.Net.Util;

namespace Lucene.Net.Search
{

    /// <summary> Subclass of FilteredTermEnum for enumerating all terms that match the
    /// specified range parameters.
    /// <p/>
    /// Term enumerations are always ordered by Term.compareTo().  Each term in
    /// the enumeration is greater than all that precede it.
    /// </summary>
    /// <since> 2.9
    /// </since>
    public class TermRangeTermEnum : FilteredTermsEnum
    {
        private readonly BytesRef upperBytesRef;
        private readonly BytesRef lowerBytesRef;
        private readonly bool includeLower;
        private readonly bool includeUpper;
        private readonly IComparer<BytesRef> termComp;

        public TermRangeTermEnum(TermsEnum tenum, BytesRef lowerTerm, BytesRef upperTerm, bool includeLower, bool includeUpper)
            : base(tenum)
        {
            if (lowerTerm == null)
            {
                this.lowerBytesRef = new BytesRef();
                this.includeLower = true;
            }
            else
            {
                this.lowerBytesRef = lowerTerm;
                this.includeLower = includeLower;
            }

            if (upperTerm == null)
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
            termComp = Comparator;
        }

        protected override FilteredTermsEnum.AcceptStatus Accept(BytesRef term)
        {
            if (!this.includeLower && term.Equals(lowerBytesRef))
                return AcceptStatus.NO;

            // Use this field's default sort ordering
            if (upperBytesRef != null)
            {
                int cmp = termComp.Compare(upperBytesRef, term);
                /*
                 * if beyond the upper term, or is exclusive and this is equal to
                 * the upper term, break out
                 */
                if ((cmp < 0) ||
                    (!includeUpper && cmp == 0))
                {
                    return AcceptStatus.END;
                }
            }

            return AcceptStatus.YES;
        }
    }
}