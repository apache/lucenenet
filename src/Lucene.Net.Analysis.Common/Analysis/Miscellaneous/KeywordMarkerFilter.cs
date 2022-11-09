// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.TokenAttributes;

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
    /// Marks terms as keywords via the <see cref="IKeywordAttribute"/>.
    /// </summary>
    /// <seealso cref="IKeywordAttribute"/>
    public abstract class KeywordMarkerFilter : TokenFilter
    {
        private readonly IKeywordAttribute keywordAttr;

        /// <summary>
        /// Creates a new <see cref="KeywordMarkerFilter"/> </summary>
        /// <param name="in"> the input stream </param>
        protected KeywordMarkerFilter(TokenStream @in)
            : base(@in)
        {
            keywordAttr = AddAttribute<IKeywordAttribute>();
        }

        public override sealed bool IncrementToken()
        {
            if (m_input.IncrementToken())
            {
                if (IsKeyword())
                {
                    keywordAttr.IsKeyword = true;
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        protected abstract bool IsKeyword();
    }
}