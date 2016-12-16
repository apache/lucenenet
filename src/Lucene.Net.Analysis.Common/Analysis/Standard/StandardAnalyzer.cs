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
    /// Filters <seealso cref="StandardTokenizer"/> with <seealso cref="StandardFilter"/>, {@link
    /// LowerCaseFilter} and <seealso cref="StopFilter"/>, using a list of
    /// English stop words.
    /// 
    /// <a name="version"/>
    /// <para>You must specify the required <seealso cref="LuceneVersion"/>
    /// compatibility when creating StandardAnalyzer:
    /// <ul>
    ///   <li> As of 3.4, Hiragana and Han characters are no longer wrongly split
    ///        from their combining characters. If you use a previous version number,
    ///        you get the exact broken behavior for backwards compatibility.
    ///   <li> As of 3.1, StandardTokenizer implements Unicode text segmentation,
    ///        and StopFilter correctly handles Unicode 4.0 supplementary characters
    ///        in stopwords.  <seealso cref="ClassicTokenizer"/> and <seealso cref="ClassicAnalyzer"/> 
    ///        are the pre-3.1 implementations of StandardTokenizer and
    ///        StandardAnalyzer.
    ///   <li> As of 2.9, StopFilter preserves position increments
    ///   <li> As of 2.4, Tokens incorrectly identified as acronyms
    ///        are corrected (see <a href="https://issues.apache.org/jira/browse/LUCENE-1068">LUCENE-1068</a>)
    /// </ul>
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
        /// <param name="matchVersion"> Lucene version to match See {@link
        /// <a href="#version">above</a>} </param>
        /// <param name="stopWords"> stop words  </param>
        public StandardAnalyzer(LuceneVersion matchVersion, CharArraySet stopWords)
            : base(matchVersion, stopWords)
        {
        }

        /// <summary>
        /// Builds an analyzer with the default stop words ({@link
        /// #STOP_WORDS_SET}). </summary>
        /// <param name="matchVersion"> Lucene version to match See {@link
        /// <a href="#version">above</a>} </param>
        public StandardAnalyzer(LuceneVersion matchVersion)
            : this(matchVersion, STOP_WORDS_SET)
        {
        }

        /// <summary>
        /// Builds an analyzer with the stop words from the given reader. </summary>
        /// <seealso cref= WordlistLoader#getWordSet(Reader, Version) </seealso>
        /// <param name="matchVersion"> Lucene version to match See {@link
        /// <a href="#version">above</a>} </param>
        /// <param name="stopwords"> SetReader to read stop words from  </param>
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
            set
            {
                maxTokenLength = value;
            }
            get
            {
                return maxTokenLength;
            }
        }


        protected override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
            var src = new StandardTokenizer(matchVersion, reader);
            src.MaxTokenLength = maxTokenLength;
            TokenStream tok = new StandardFilter(matchVersion, src);
            tok = new LowerCaseFilter(matchVersion, tok);
            tok = new StopFilter(matchVersion, tok, stopwords);
            return new TokenStreamComponentsAnonymousInnerClassHelper(this, src, tok, reader);
        }

        private class TokenStreamComponentsAnonymousInnerClassHelper : TokenStreamComponents
        {
            private readonly StandardAnalyzer outerInstance;

            private TextReader reader;
            private readonly StandardTokenizer src;

            public TokenStreamComponentsAnonymousInnerClassHelper(StandardAnalyzer outerInstance, StandardTokenizer src, TokenStream tok, TextReader reader)
                : base(src, tok)
            {
                this.outerInstance = outerInstance;
                this.reader = reader;
                this.src = src;
            }

            protected override void SetReader(TextReader reader)
            {
                src.MaxTokenLength = outerInstance.maxTokenLength;
                base.SetReader(reader);
            }
        }
    }
}