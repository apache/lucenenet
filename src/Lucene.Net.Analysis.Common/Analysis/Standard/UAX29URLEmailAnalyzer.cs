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
    /// Filters <seealso cref="org.apache.lucene.analysis.standard.UAX29URLEmailTokenizer"/>
    /// with <seealso cref="StandardFilter"/>,
    /// <seealso cref="LowerCaseFilter"/> and
    /// <seealso cref="StopFilter"/>, using a list of
    /// English stop words.
    /// 
    /// <a name="version"/>
    /// <para>
    ///   You must specify the required <seealso cref="org.apache.lucene.util.Version"/>
    ///   compatibility when creating UAX29URLEmailAnalyzer
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
        /// <param name="matchVersion"> Lucene version to match See {@link
        /// <a href="#version">above</a>} </param>
        /// <param name="stopWords"> stop words  </param>
        public UAX29URLEmailAnalyzer(LuceneVersion matchVersion, CharArraySet stopWords)
            : base(matchVersion, stopWords)
        {
        }

        /// <summary>
        /// Builds an analyzer with the default stop words ({@link
        /// #STOP_WORDS_SET}). </summary>
        /// <param name="matchVersion"> Lucene version to match See {@link
        /// <a href="#version">above</a>} </param>
        public UAX29URLEmailAnalyzer(LuceneVersion matchVersion)
            : this(matchVersion, STOP_WORDS_SET)
        {
        }

        /// <summary>
        /// Builds an analyzer with the stop words from the given reader. </summary>
        /// <seealso cref= org.apache.lucene.analysis.util.WordlistLoader#getWordSet(java.io.Reader, org.apache.lucene.util.Version) </seealso>
        /// <param name="matchVersion"> Lucene version to match See {@link
        /// <a href="#version">above</a>} </param>
        /// <param name="stopwords"> Reader to read stop words from  </param>
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
            set { maxTokenLength = value; }
            get { return maxTokenLength; }
        }


        protected override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
            UAX29URLEmailTokenizer src = new UAX29URLEmailTokenizer(matchVersion, reader);
            src.MaxTokenLength = maxTokenLength;
            TokenStream tok = new StandardFilter(matchVersion, src);
            tok = new LowerCaseFilter(matchVersion, tok);
            tok = new StopFilter(matchVersion, tok, stopwords);
            return new TokenStreamComponentsAnonymousInnerClassHelper(this, src, tok, reader);
        }

        private class TokenStreamComponentsAnonymousInnerClassHelper : TokenStreamComponents
        {
            private readonly UAX29URLEmailAnalyzer outerInstance;

            private TextReader reader;
            private UAX29URLEmailTokenizer src;

            public TokenStreamComponentsAnonymousInnerClassHelper(UAX29URLEmailAnalyzer outerInstance, UAX29URLEmailTokenizer src, TokenStream tok, TextReader reader)
                : base(src, tok)
            {
                this.outerInstance = outerInstance;
                this.reader = reader;
                this.src = src;
            }

            protected override TextReader Reader
            {
                set
                {
                    src.MaxTokenLength = outerInstance.maxTokenLength;
                    base.Reader = value;
                }
            }
        }
    }
}