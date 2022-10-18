// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using System.IO;

namespace Lucene.Net.Analysis.Standard
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
    /// Filters <see cref="UAX29URLEmailTokenizer"/>
    /// with <see cref="StandardFilter"/>,
    /// <see cref="LowerCaseFilter"/> and
    /// <see cref="StopFilter"/>, using a list of
    /// English stop words.
    /// 
    /// <para>
    ///   You must specify the required <see cref="LuceneVersion"/>
    ///   compatibility when creating <see cref="UAX29URLEmailAnalyzer"/>
    /// </para>
    /// </summary>
    public sealed class UAX29URLEmailAnalyzer : StopwordAnalyzerBase
    {
        /// <summary>
        /// Default maximum allowed token length </summary>
        public const int DEFAULT_MAX_TOKEN_LENGTH = StandardAnalyzer.DEFAULT_MAX_TOKEN_LENGTH;

        private int maxTokenLength = DEFAULT_MAX_TOKEN_LENGTH;

        /// <summary>
        /// An unmodifiable set containing some common English words that are usually not
        /// useful for searching. 
        /// </summary>
        public static readonly CharArraySet STOP_WORDS_SET = StopAnalyzer.ENGLISH_STOP_WORDS_SET;

        /// <summary>
        /// Builds an analyzer with the given stop words. </summary>
        /// <param name="matchVersion"> Lucene version to match - See <see cref="UAX29URLEmailAnalyzer"/> </param>
        /// <param name="stopWords"> stop words  </param>
        public UAX29URLEmailAnalyzer(LuceneVersion matchVersion, CharArraySet stopWords)
            : base(matchVersion, stopWords)
        {
        }

        /// <summary>
        /// Builds an analyzer with the default stop words (<see cref="STOP_WORDS_SET"/>.
        /// </summary>
        /// <param name="matchVersion"> Lucene version to match - See <see cref="UAX29URLEmailAnalyzer"/> </param>
        public UAX29URLEmailAnalyzer(LuceneVersion matchVersion)
            : this(matchVersion, STOP_WORDS_SET)
        {
        }

        /// <summary>
        /// Builds an analyzer with the stop words from the given reader. </summary>
        /// <seealso cref="WordlistLoader.GetWordSet(TextReader, LuceneVersion)"/>
        /// <param name="matchVersion"> Lucene version to match - See <see cref="UAX29URLEmailAnalyzer"/> </param>
        /// <param name="stopwords"> <see cref="TextReader"/> to read stop words from  </param>
        public UAX29URLEmailAnalyzer(LuceneVersion matchVersion, TextReader stopwords)
            : this(matchVersion, LoadStopwordSet(stopwords, matchVersion))
        {
        }

        /// <summary>
        /// Set maximum allowed token length.  If a token is seen
        /// that exceeds this length then it is discarded.  This
        /// setting only takes effect the next time tokenStream or
        /// tokenStream is called.
        /// </summary>
        public int MaxTokenLength
        {
            set => maxTokenLength = value;
            get => maxTokenLength;
        }

        protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
            UAX29URLEmailTokenizer src = new UAX29URLEmailTokenizer(m_matchVersion, reader);
            src.MaxTokenLength = maxTokenLength;
            TokenStream tok = new StandardFilter(m_matchVersion, src);
            tok = new LowerCaseFilter(m_matchVersion, tok);
            tok = new StopFilter(m_matchVersion, tok, m_stopwords);
            return new TokenStreamComponentsAnonymousClass(this, src, tok);
        }

        private sealed class TokenStreamComponentsAnonymousClass : TokenStreamComponents
        {
            private readonly UAX29URLEmailAnalyzer outerInstance;

            private readonly UAX29URLEmailTokenizer src;

            public TokenStreamComponentsAnonymousClass(UAX29URLEmailAnalyzer outerInstance, UAX29URLEmailTokenizer src, TokenStream tok)
                : base(src, tok)
            {
                this.outerInstance = outerInstance;
                this.src = src;
            }

            protected internal override void SetReader(TextReader reader)
            {
                src.MaxTokenLength = outerInstance.maxTokenLength;
                base.SetReader(reader);
            }
        }
    }
}