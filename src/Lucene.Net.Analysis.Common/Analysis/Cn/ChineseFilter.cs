// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using System;
using System.Globalization;

namespace Lucene.Net.Analysis.Cn
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
    /// A <see cref="TokenFilter"/> with a stop word table.  
    /// <list type="bullet">
    ///     <item><description>Numeric tokens are removed.</description></item>
    ///     <item><description>English tokens must be larger than 1 character.</description></item>
    ///     <item><description>One Chinese character as one Chinese word.</description></item>
    /// </list>
    /// TO DO:
    /// <list type="number">
    ///     <item><description>Add Chinese stop words, such as \ue400</description></item>
    ///     <item><description>Dictionary based Chinese word extraction</description></item>
    ///     <item><description>Intelligent Chinese word extraction</description></item>
    /// </list>
    /// </summary>
    /// @deprecated (3.1) Use <see cref="Core.StopFilter"/> instead, which has the same functionality.
    /// This filter will be removed in Lucene 5.0 
    [Obsolete("(3.1) Use StopFilter instead, which has the same functionality.")]
    public sealed class ChineseFilter : TokenFilter
    {
        // Only English now, Chinese to be added later.
        public static readonly string[] STOP_WORDS = new string[] {
            "and", "are", "as", "at", "be", "but", "by",
            "for", "if", "in", "into", "is", "it",
            "no", "not", "of", "on", "or", "such",
            "that", "the", "their", "then", "there", "these",
            "they", "this", "to", "was", "will", "with"
        };

        private CharArraySet stopTable;

        private ICharTermAttribute termAtt;

        public ChineseFilter(TokenStream @in)
            : base(@in)
        {
            stopTable = new CharArraySet(LuceneVersion.LUCENE_CURRENT, STOP_WORDS, false);
            termAtt = AddAttribute<ICharTermAttribute>();
        }

        public override bool IncrementToken()
        {
            while (m_input.IncrementToken())
            {
                char[] text = termAtt.Buffer;
                int termLength = termAtt.Length;

                // why not key off token type here assuming ChineseTokenizer comes first?
                if (!stopTable.Contains(text, 0, termLength))
                {
                    switch (CharUnicodeInfo.GetUnicodeCategory(text[0]))
                    {

                        case UnicodeCategory.LowercaseLetter:
                        case UnicodeCategory.UppercaseLetter:

                            // English word/token should larger than 1 character.
                            if (termLength > 1)
                            {
                                return true;
                            }
                            break;
                        case UnicodeCategory.OtherLetter:

                            // One Chinese character as one Chinese word.
                            // Chinese word extraction to be added later here.

                            return true;
                    }
                }
            }
            return false;
        }
    }
}