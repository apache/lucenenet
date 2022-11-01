// Lucene version compatibility level 4.8.1
using System.IO;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;

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
    /// Filters <see cref="StandardTokenizer"/> with <see cref="StandardFilter"/>, 
    /// <see cref="LowerCaseFilter"/> and <see cref="StopFilter"/>, using a list of
    /// English stop words.
    /// 
    /// <para>You must specify the required <see cref="LuceneVersion"/>
    /// compatibility when creating <see cref="StandardAnalyzer"/>:
    /// <list type="bullet">
    ///   <item><description> As of 3.4, Hiragana and Han characters are no longer wrongly split
    ///        from their combining characters. If you use a previous version number,
    ///        you get the exact broken behavior for backwards compatibility.</description></item>
    ///   <item><description> As of 3.1, <see cref="StandardTokenizer"/> implements Unicode text segmentation,
    ///        and <see cref="StopFilter"/> correctly handles Unicode 4.0 supplementary characters
    ///        in stopwords.  <see cref="ClassicTokenizer"/> and <see cref="ClassicAnalyzer"/> 
    ///        are the pre-3.1 implementations of <see cref="StandardTokenizer"/> and
    ///        <see cref="StandardAnalyzer"/>.</description></item>
    ///   <item><description> As of 2.9, <see cref="StopFilter"/> preserves position increments</description></item>
    ///   <item><description> As of 2.4, <see cref="Token"/>s incorrectly identified as acronyms
    ///        are corrected (see <a href="https://issues.apache.org/jira/browse/LUCENE-1068">LUCENE-1068</a>)</description></item>
    /// </list>
    /// </para>
    /// </summary>
    public sealed class StandardAnalyzer : StopwordAnalyzerBase
    {
        /// <summary>
        /// Default maximum allowed token length </summary>
        public const int DEFAULT_MAX_TOKEN_LENGTH = 255;

        private int maxTokenLength = DEFAULT_MAX_TOKEN_LENGTH;

        /// <summary>
        /// An unmodifiable set containing some common English words that are usually not
        /// useful for searching. 
        /// </summary>
        public static readonly CharArraySet STOP_WORDS_SET = StopAnalyzer.ENGLISH_STOP_WORDS_SET;

        /// <summary>
        /// Builds an analyzer with the given stop words. </summary>
        /// <param name="matchVersion"> Lucene compatibility version - See <see cref="StandardAnalyzer"/> </param>
        /// <param name="stopWords"> stop words  </param>
        public StandardAnalyzer(LuceneVersion matchVersion, CharArraySet stopWords)
            : base(matchVersion, stopWords)
        {
        }

        /// <summary>
        /// Builds an analyzer with the default stop words (<see cref="STOP_WORDS_SET"/>). </summary>
        /// <param name="matchVersion"> Lucene compatibility version - See <see cref="StandardAnalyzer"/> </param>
        public StandardAnalyzer(LuceneVersion matchVersion)
            : this(matchVersion, STOP_WORDS_SET)
        {
        }

        /// <summary>
        /// Builds an analyzer with the stop words from the given reader. </summary>
        /// <seealso cref="WordlistLoader.GetWordSet(TextReader, LuceneVersion)"/>
        /// <param name="matchVersion"> Lucene compatibility version - See <see cref="StandardAnalyzer"/> </param>
        /// <param name="stopwords"> <see cref="TextReader"/> to read stop words from  </param>
        public StandardAnalyzer(LuceneVersion matchVersion, TextReader stopwords)
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
            get => maxTokenLength;
            set => maxTokenLength = value;
        }


        protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
            var src = new StandardTokenizer(m_matchVersion, reader);
            src.MaxTokenLength = maxTokenLength;
            TokenStream tok = new StandardFilter(m_matchVersion, src);
            tok = new LowerCaseFilter(m_matchVersion, tok);
            tok = new StopFilter(m_matchVersion, tok, m_stopwords);
            return new TokenStreamComponentsAnonymousClass(this, src, tok);
        }

        private sealed class TokenStreamComponentsAnonymousClass : TokenStreamComponents
        {
            private readonly StandardAnalyzer outerInstance;

            private readonly StandardTokenizer src;

            public TokenStreamComponentsAnonymousClass(StandardAnalyzer outerInstance, StandardTokenizer src, TokenStream tok)
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