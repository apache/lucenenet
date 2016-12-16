using System.Collections.Generic;
using System.IO;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Support;
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
    /// Filters <seealso cref="LetterTokenizer"/> with <seealso cref="LowerCaseFilter"/> and <seealso cref="StopFilter"/>.
    /// 
    /// <a name="version"/>
    /// <para>You must specify the required <seealso cref="LuceneVersion"/>
    /// compatibility when creating StopAnalyzer:
    /// <ul>
    ///    <li> As of 3.1, StopFilter correctly handles Unicode 4.0
    ///         supplementary characters in stopwords
    ///   <li> As of 2.9, position increments are preserved
    /// </ul>
    /// </para>
    /// </summary>
    public sealed class StopAnalyzer : StopwordAnalyzerBase
    {

        /// <summary>
        /// An unmodifiable set containing some common English words that are not usually useful
        /// for searching.
        /// </summary>
        public static readonly CharArraySet ENGLISH_STOP_WORDS_SET;

        static StopAnalyzer()
        {
            IList<string> stopWords = Arrays.AsList("a", "an", "and", "are", "as", "at", "be", 
                "but", "by", "for", "if", "in", "into", "is", "it", "no", "not", "of", "on", 
                "or", "such", "that", "the", "their", "then", "there", "these", "they", "this", 
                "to", "was", "will", "with");
#pragma warning disable 612, 618
            var stopSet = new CharArraySet(LuceneVersion.LUCENE_CURRENT, stopWords, false);
#pragma warning restore 612, 618
            ENGLISH_STOP_WORDS_SET = CharArraySet.UnmodifiableSet(stopSet);
        }

        /// <summary>
        /// Builds an analyzer which removes words in
        ///  <seealso cref="#ENGLISH_STOP_WORDS_SET"/>. </summary>
        /// <param name="matchVersion"> See <a href="#version">above</a> </param>
        public StopAnalyzer(LuceneVersion matchVersion)
            : this(matchVersion, ENGLISH_STOP_WORDS_SET)
        {
        }

        /// <summary>
        /// Builds an analyzer with the stop words from the given set. </summary>
        /// <param name="matchVersion"> See <a href="#version">above</a> </param>
        /// <param name="stopWords"> Set of stop words  </param>
        public StopAnalyzer(LuceneVersion matchVersion, CharArraySet stopWords)
            : base(matchVersion, stopWords)
        {
        }

        /// <summary>
        /// Builds an analyzer with the stop words from the given file. </summary>
        /// <seealso cref= WordlistLoader#getWordSet(Reader, Version) </seealso>
        /// <param name="matchVersion"> See <a href="#version">above</a> </param>
        /// <param name="stopwordsFile"> File to load stop words from  </param>
        public StopAnalyzer(LuceneVersion matchVersion, FileInfo stopwordsFile)
            : this(matchVersion, LoadStopwordSet(stopwordsFile, matchVersion))
        {
        }

        /// <summary>
        /// Builds an analyzer with the stop words from the given reader. </summary>
        /// <seealso cref= WordlistLoader#getWordSet(Reader, Version) </seealso>
        /// <param name="matchVersion"> See <a href="#version">above</a> </param>
        /// <param name="stopwords"> TextReader to load stop words from  </param>
        public StopAnalyzer(LuceneVersion matchVersion, TextReader stopwords)
            : this(matchVersion, LoadStopwordSet(stopwords, matchVersion))
        {
        }

        /// <summary>
        /// Creates
        /// <seealso cref="Analyzer.TokenStreamComponents"/>
        /// used to tokenize all the text in the provided <seealso cref="TextReader"/>.
        /// </summary>
        /// <returns> <seealso cref="Analyzer.TokenStreamComponents"/>
        ///         built from a <seealso cref="LowerCaseTokenizer"/> filtered with
        ///         <seealso cref="StopFilter"/> </returns>
        protected override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
            Tokenizer source = new LowerCaseTokenizer(matchVersion, reader);
            return new TokenStreamComponents(source, new StopFilter(matchVersion, source, stopwords));
        }
    }
}