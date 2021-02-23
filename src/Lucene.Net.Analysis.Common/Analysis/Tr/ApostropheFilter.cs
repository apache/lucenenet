// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.TokenAttributes;

namespace Lucene.Net.Analysis.Tr
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
    /// Strips all characters after an apostrophe (including the apostrophe itself).
    /// <para>
    /// In Turkish, apostrophe is used to separate suffixes from proper names
    /// (continent, sea, river, lake, mountain, upland, proper names related to
    /// religion and mythology). This filter intended to be used before stem filters.
    /// For more information, see <a href="http://www.ipcsit.com/vol57/015-ICNI2012-M021.pdf">
    /// Role of Apostrophes in Turkish Information Retrieval</a>
    /// </para>
    /// </summary>
    public sealed class ApostropheFilter : TokenFilter
    {
        private readonly ICharTermAttribute termAtt;

        public ApostropheFilter(TokenStream @in)
            : base(@in)
        {
            termAtt = AddAttribute<ICharTermAttribute>();
        }

        public override sealed bool IncrementToken()
        {
            if (!m_input.IncrementToken())
            {
                return false;
            }

            char[] buffer = termAtt.Buffer;
            int length = termAtt.Length;

            for (int i = 0; i < length; i++)
            {
                if (buffer[i] == '\'' || buffer[i] == '\u2019')
                {
                    termAtt.Length = i;
                    return true;
                }
            }
            return true;
        }
    }
}