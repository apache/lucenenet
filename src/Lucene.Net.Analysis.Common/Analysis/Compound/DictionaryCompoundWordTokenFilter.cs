// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using System;

namespace Lucene.Net.Analysis.Compound
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
    /// A <see cref="TokenFilter"/> that decomposes compound words found in many Germanic languages.
    /// <para>
    /// "Donaudampfschiff" becomes Donau, dampf, schiff so that you can find
    /// "Donaudampfschiff" even when you only enter "schiff". 
    ///  It uses a brute-force algorithm to achieve this.
    /// </para>
    /// <para>
    /// You must specify the required <see cref="LuceneVersion"/> compatibility when creating
    /// <see cref="CompoundWordTokenFilterBase"/>:
    /// <list type="bullet">
    ///     <item><description>As of 3.1, CompoundWordTokenFilterBase correctly handles Unicode 4.0
    ///     supplementary characters in strings and char arrays provided as compound word
    ///     dictionaries.</description></item>
    /// </list>
    /// </para>
    /// </summary>
    public class DictionaryCompoundWordTokenFilter : CompoundWordTokenFilterBase
    {
        /// <summary>
        /// Creates a new <see cref="DictionaryCompoundWordTokenFilter"/>
        /// </summary>
        /// <param name="matchVersion">
        ///          Lucene version to enable correct Unicode 4.0 behavior in the
        ///          dictionaries if Version > 3.0. See <a
        ///          href="CompoundWordTokenFilterBase.html#version"
        ///          >CompoundWordTokenFilterBase</a> for details. </param>
        /// <param name="input">
        ///          the <see cref="TokenStream"/> to process </param>
        /// <param name="dictionary">
        ///          the word dictionary to match against. </param>
        public DictionaryCompoundWordTokenFilter(LuceneVersion matchVersion, TokenStream input, CharArraySet dictionary)
            : base(matchVersion, input, dictionary)
        {
            if (dictionary is null)
            {
                throw new ArgumentNullException(nameof(dictionary), "dictionary cannot be null"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            }
        }

        /// <summary>
        /// Creates a new <see cref="DictionaryCompoundWordTokenFilter"/>
        /// </summary>
        /// <param name="matchVersion">
        ///          Lucene version to enable correct Unicode 4.0 behavior in the
        ///          dictionaries if Version > 3.0. See <a
        ///          href="CompoundWordTokenFilterBase.html#version"
        ///          >CompoundWordTokenFilterBase</a> for details. </param>
        /// <param name="input">
        ///          the <see cref="TokenStream"/> to process </param>
        /// <param name="dictionary">
        ///          the word dictionary to match against. </param>
        /// <param name="minWordSize">
        ///          only words longer than this get processed </param>
        /// <param name="minSubwordSize">
        ///          only subwords longer than this get to the output stream </param>
        /// <param name="maxSubwordSize">
        ///          only subwords shorter than this get to the output stream </param>
        /// <param name="onlyLongestMatch">
        ///          Add only the longest matching subword to the stream </param>
        public DictionaryCompoundWordTokenFilter(LuceneVersion matchVersion, TokenStream input, CharArraySet dictionary, int minWordSize, int minSubwordSize, int maxSubwordSize, bool onlyLongestMatch)
            : base(matchVersion, input, dictionary, minWordSize, minSubwordSize, maxSubwordSize, onlyLongestMatch)
        {
            if (dictionary is null)
            {
                throw new ArgumentNullException(nameof(dictionary), "dictionary cannot be null"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            }
        }

        protected override void Decompose()
        {
            int len = m_termAtt.Length;
            for (int i = 0; i <= len - this.m_minSubwordSize; ++i)
            {
                CompoundToken longestMatchToken = null;
                for (int j = this.m_minSubwordSize; j <= this.m_maxSubwordSize; ++j)
                {
                    if (i + j > len)
                    {
                        break;
                    }
                    if (m_dictionary.Contains(m_termAtt.Buffer, i, j))
                    {
                        if (this.m_onlyLongestMatch)
                        {
                            if (longestMatchToken != null)
                            {
                                if (longestMatchToken.Text.Length < j)
                                {
                                    longestMatchToken = new CompoundToken(this, i, j);
                                }
                            }
                            else
                            {
                                longestMatchToken = new CompoundToken(this, i, j);
                            }
                        }
                        else
                        {
                            m_tokens.Enqueue(new CompoundToken(this, i, j));
                        }
                    }
                }
                if (this.m_onlyLongestMatch && longestMatchToken != null)
                {
                    m_tokens.Enqueue(longestMatchToken);
                }
            }
        }
    }
}