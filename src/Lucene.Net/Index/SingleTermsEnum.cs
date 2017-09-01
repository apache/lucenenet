namespace Lucene.Net.Index
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

    // javadocs
    using BytesRef = Lucene.Net.Util.BytesRef;

    /// <summary>
    /// Subclass of <see cref="FilteredTermsEnum"/> for enumerating a single term.
    /// <para/>
    /// For example, this can be used by <see cref="Search.MultiTermQuery"/>s
    /// that need only visit one term, but want to preserve
    /// <see cref="Search.MultiTermQuery"/> semantics such as <see cref="Search.MultiTermQuery.MultiTermRewriteMethod"/>.
    /// </summary>
    public sealed class SingleTermsEnum : FilteredTermsEnum
    {
        private readonly BytesRef singleRef;

        /// <summary>
        /// Creates a new <see cref="SingleTermsEnum"/>.
        /// <para/>
        /// After calling the constructor the enumeration is already pointing to the term,
        /// if it exists.
        /// </summary>
        public SingleTermsEnum(TermsEnum tenum, BytesRef termText)
            : base(tenum)
        {
            singleRef = termText;
            SetInitialSeekTerm(termText);
        }

        protected override AcceptStatus Accept(BytesRef term)
        {
            return term.Equals(singleRef) ? AcceptStatus.YES : AcceptStatus.END;
        }
    }
}