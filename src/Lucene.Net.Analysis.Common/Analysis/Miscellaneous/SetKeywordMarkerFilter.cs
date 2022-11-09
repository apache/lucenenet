// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Analysis.Util;

namespace Lucene.Net.Analysis.Miscellaneous
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

    /// <summary>
    /// Marks terms as keywords via the <see cref="IKeywordAttribute"/>. Each token
    /// contained in the provided set is marked as a keyword by setting
    /// <see cref="IKeywordAttribute.IsKeyword"/> to <c>true</c>.
    /// </summary>
    public sealed class SetKeywordMarkerFilter : KeywordMarkerFilter
    {
        private readonly ICharTermAttribute termAtt;
        private readonly CharArraySet keywordSet;

        /// <summary>
        /// Create a new <see cref="SetKeywordMarkerFilter"/>, that marks the current token as a
        /// keyword if the tokens term buffer is contained in the given set via the
        /// <see cref="IKeywordAttribute"/>.
        /// </summary>
        /// <param name="in">
        ///          <see cref="TokenStream"/> to filter </param>
        /// <param name="keywordSet">
        ///          the keywords set to lookup the current termbuffer </param>
        public SetKeywordMarkerFilter(TokenStream @in, CharArraySet keywordSet)
            : base(@in)
        {
            this.keywordSet = keywordSet;
            termAtt = AddAttribute<ICharTermAttribute>();
        }

        protected override bool IsKeyword()
        {
            return keywordSet.Contains(termAtt.Buffer, 0, termAtt.Length);
        }
    }
}