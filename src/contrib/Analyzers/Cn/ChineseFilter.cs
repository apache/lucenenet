/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Collections;
using System.Globalization;
using System.Linq;

using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Analysis.Util;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Analysis.Cn
{
    /// <summary>
    /// A <see cref="Lucene.Net.Analysis.TokenFilter"/> with a stop word table.  
    /// <list type="bullet">
    /// <item><description>Numeric tokens are removed.</description></item>
    /// <item><description>English tokens must be larger than 1 char.</description></item>
    /// <item><description>One Chinese char as one Chinese word.</description></item>
    /// </list>
    /// TO DO:
    /// <list type="number">
    /// <item><description>Add Chinese stop words, such as \ue400</description></item>
    /// <item><description>Dictionary based Chinese word extraction</description></item>
    /// <item><description>Intelligent Chinese word extraction</description></item>
    /// </list>
    /// </summary>
    [Obsolete("(3.1) Use {Lucene.Net.Analysis.Core.StopFilter} instead, which has the same functionality. This filter will be removed in Lucene 5.0")]
    public sealed class ChineseFilter : TokenFilter
    {
        // Only English now, Chinese to be added later.
        public static readonly String[] STOP_WORDS =
            {
                "and", "are", "as", "at", "be", "but", "by",
                "for", "if", "in", "into", "is", "it",
                "no", "not", "of", "on", "or", "such",
                "that", "the", "their", "then", "there", "these",
                "they", "this", "to", "was", "will", "with"
            };

        private CharArraySet stopTable;
        private ICharTermAttribute termAtt;

        public ChineseFilter(TokenStream _in)
            : base(_in)
        {
            stopTable = new CharArraySet(Version.LUCENE_CURRENT, STOP_WORDS.ToList<object>(), false);
            termAtt = AddAttribute<ICharTermAttribute>();
        }

        public override bool IncrementToken()
        {
            while (input.IncrementToken())
            {
                char[] text = termAtt.Buffer;
                int termLength = termAtt.Length;

                // why not key off token type here assuming ChineseTokenizer comes first?
                if (!stopTable.Contains(text, 0, termLength))
                {
                    switch (char.GetUnicodeCategory(text[0]))
                    {
                        case UnicodeCategory.LowercaseLetter:
                        case UnicodeCategory.UppercaseLetter:
                            // English word/token should larger than 1 char.
                            if (termLength > 1)
                            {
                                return true;
                            }
                            break;
                        case UnicodeCategory.OtherLetter:
                            // One Chinese char as one Chinese word.
                            // Chinese word extraction to be added later here.
                            return true;
                    }
                }
            }
            return false;
        }
    }
}
