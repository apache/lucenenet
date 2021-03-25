// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;

namespace Lucene.Net.Analysis.Core
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
    /// Normalizes token text to lower case.
    /// <para>You must specify the required <see cref="LuceneVersion"/>
    /// compatibility when creating LowerCaseFilter:
    /// <list type="bullet">
    ///     <item><description> As of 3.1, supplementary characters are properly lowercased.</description></item>
    /// </list>
    /// </para>
    /// </summary>
    public sealed class LowerCaseFilter : TokenFilter
    {
        private readonly CharacterUtils charUtils;
        private readonly ICharTermAttribute termAtt;

        /// <summary>
        /// Create a new <see cref="LowerCaseFilter"/>, that normalizes token text to lower case.
        /// </summary>
        /// <param name="matchVersion"> See <see cref="LuceneVersion"/> </param>
        /// <param name="in"> <see cref="TokenStream"/> to filter </param>
        public LowerCaseFilter(LuceneVersion matchVersion, TokenStream @in)
            : base(@in)
        {
            termAtt = AddAttribute<ICharTermAttribute>();
            charUtils = CharacterUtils.GetInstance(matchVersion);
        }

        public override sealed bool IncrementToken()
        {
            if (m_input.IncrementToken())
            {
                charUtils.ToLower(termAtt.Buffer, 0, termAtt.Length);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}