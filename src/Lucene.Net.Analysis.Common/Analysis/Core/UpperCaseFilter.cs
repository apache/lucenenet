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
    /// Normalizes token text to UPPER CASE.
    /// <para>
    /// You must specify the required <see cref="LuceneVersion"/>
    /// compatibility when creating <see cref="UpperCaseFilter"/>
    /// </para>
    /// <para><b>NOTE:</b> In Unicode, this transformation may lose information when the
    /// upper case character represents more than one lower case character. Use this filter
    /// when you Require uppercase tokens.  Use the <see cref="LowerCaseFilter"/> for 
    /// general search matching
    /// </para>
    /// </summary>
    public sealed class UpperCaseFilter : TokenFilter
    {
        private readonly CharacterUtils charUtils;
        private readonly ICharTermAttribute termAtt;

        /// <summary>
        /// Create a new <see cref="UpperCaseFilter"/>, that normalizes token text to upper case.
        /// </summary>
        /// <param name="matchVersion"> See <see cref="LuceneVersion"/> </param>
        /// <param name="in"> <see cref="TokenStream"/> to filter </param>
        public UpperCaseFilter(LuceneVersion matchVersion, TokenStream @in)
            : base(@in)
        {
            termAtt = AddAttribute<ICharTermAttribute>();
            charUtils = CharacterUtils.GetInstance(matchVersion);
        }

        public override sealed bool IncrementToken()
        {
            if (m_input.IncrementToken())
            {
                charUtils.ToUpper(termAtt.Buffer, 0, termAtt.Length);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}