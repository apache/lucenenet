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

using Lucene.Net.Index;
using Lucene.Net.Util;

namespace Lucene.Net.Search
{

    /// <summary> Subclass of FilteredTermEnum for enumerating all terms that match the
    /// specified prefix filter term.
    /// <p/>
    /// Term enumerations are always ordered by Term.compareTo().  Each term in
    /// the enumeration is greater than all that precede it.
    /// 
    /// </summary>
    public class PrefixTermsEnum : FilteredTermsEnum
    {
        private readonly BytesRef prefixRef;

        public PrefixTermsEnum(TermsEnum tenum, BytesRef prefixText)
            : base(tenum)
        {
            SetInitialSeekTerm(this.prefixRef = prefixText);
        }

        protected override AcceptStatus Accept(BytesRef term)
        {
            if (StringHelper.StartsWith(term, prefixRef))
            {
                return AcceptStatus.YES;
            }
            else
            {
                return AcceptStatus.END;
            }
        }
    }
}