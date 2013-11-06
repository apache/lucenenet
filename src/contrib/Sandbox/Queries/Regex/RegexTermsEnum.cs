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
using Lucene.Net.Search;
using Lucene.Net.Util;

namespace Lucene.Net.Sandbox.Queries.Regex
{
    /// <summary>
    /// Subclass of FilteredTermEnum for enumerating all terms that match the
    /// specified regular expression term using the specified regular expression
    /// implementation.
    /// <para>Term enumerations are always ordered by Term.compareTo().  Each term in
    /// the enumeration is greater than all that precede it.</para>
    /// </summary>
    /// <remarks>http://www.java2s.com/Open-Source/Java-Document/Net/lucene-connector/org/apache/lucene/search/regex/RegexTermEnum.java.htm</remarks>
    public class RegexTermsEnum : FilteredTermsEnum
    {
        private IRegexCapabilities regexImpl;
        private readonly BytesRef prefixRef;

        public RegexTermsEnum(TermsEnum tenum, Term term, IRegexCapabilities regexCap)
            : base(tenum)
        {
            string text = term.Text;
            this.regexImpl = regexCap;
            regexCap.Compile(text);
            string pre = regexImpl.Prefix();
            if (pre == null)
            {
                pre = "";
            }

            InitialSeekTerm = prefixRef = new BytesRef(pre);
        }

        protected override AcceptStatus Accept(BytesRef term)
        {
            if (StringHelper.StartsWith(term, prefixRef))
            {
                return regexImpl.Match(term) ? AcceptStatus.YES : AcceptStatus.NO;
            }
            else
            {
                return AcceptStatus.NO;
            }
        }
    }
}
