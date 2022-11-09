// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.TokenAttributes;

namespace Lucene.Net.Analysis.Util
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
    /// Removes elisions from a <see cref="TokenStream"/>. For example, "l'avion" (the plane) will be
    /// tokenized as "avion" (plane).
    /// <para/>
    /// <a href="http://fr.wikipedia.org/wiki/%C3%89lision">Elision in Wikipedia</a>
    /// </summary>
    public sealed class ElisionFilter : TokenFilter
    {
        private readonly CharArraySet articles;
        private readonly ICharTermAttribute termAtt;

        /// <summary>
        /// Constructs an elision filter with a <see cref="CharArraySet"/> of stop words </summary>
        /// <param name="input"> the source <see cref="TokenStream"/> </param>
        /// <param name="articles"> a set of stopword articles </param>
        public ElisionFilter(TokenStream input, CharArraySet articles)
            : base(input)
        {
            this.articles = articles;
            termAtt = AddAttribute<ICharTermAttribute>();
        }

        /// <summary>
        /// Increments the <see cref="TokenStream"/> with a <see cref="ICharTermAttribute"/> without elisioned start
        /// </summary>
        public override sealed bool IncrementToken()
        {
            if (m_input.IncrementToken())
            {
                char[] termBuffer = termAtt.Buffer;
                int termLength = termAtt.Length;

                int index = -1;
                for (int i = 0; i < termLength; i++)
                {
                    char ch = termBuffer[i];
                    if (ch == '\'' || ch == '\u2019')
                    {
                        index = i;
                        break;
                    }
                }

                // An apostrophe has been found. If the prefix is an article strip it off.
                if (index >= 0 && articles.Contains(termBuffer, 0, index))
                {
                    termAtt.CopyBuffer(termBuffer, index + 1, termLength - (index + 1));
                }

                return true;
            }
            else
            {
                return false;
            }
        }
    }
}