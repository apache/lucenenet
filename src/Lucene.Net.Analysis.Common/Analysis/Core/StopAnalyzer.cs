// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

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
    /// Filters <see cref="LetterTokenizer"/> with <see cref="LowerCaseFilter"/> and <see cref="StopFilter"/>.
    /// <para>
    /// You must specify the required <see cref="LuceneVersion"/>
    /// compatibility when creating <see cref="StopAnalyzer"/>:
    /// <list type="bullet">
    ///     <item><description> As of 3.1, StopFilter correctly handles Unicode 4.0
    ///         supplementary characters in stopwords</description></item>
    ///     <item><description> As of 2.9, position increments are preserved</description></item>
    /// </list>
    /// </para>
    /// </summary>
    public sealed class StopAnalyzer : StopwordAnalyzerBase
    {
        /// <summary>
        /// An unmodifiable set containing some common English words that are not usually useful
        /// for searching.
        /// </summary>
        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "This is a SonarCloud issue")]
        [SuppressMessage("Performance", "S3887:Use an immutable collection or reduce the accessibility of the non-private readonly field", Justification = "Collection is immutable")]
        [SuppressMessage("Performance", "S2386:Use an immutable collection or reduce the accessibility of the public static field", Justification = "Collection is immutable")]
        public static readonly CharArraySet ENGLISH_STOP_WORDS_SET = LoadEnglishStopWordsSet();

        private static CharArraySet LoadEnglishStopWordsSet() // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
        {
            IList<string> stopWords = new string[] { "a", "an", "and", "are", "as", "at", "be",
                "but", "by", "for", "if", "in", "into", "is", "it", "no", "not", "of", "on",
                "or", "such", "that", "the", "their", "then", "there", "these", "they", "this",
                "to", "was", "will", "with" };
#pragma warning disable 612, 618
            var stopSet = new CharArraySet(LuceneVersion.LUCENE_CURRENT, stopWords, false);
#pragma warning restore 612, 618
            return stopSet.AsReadOnly();
        }

        /// <summary>
        /// Builds an analyzer which removes words in
        /// <see cref="ENGLISH_STOP_WORDS_SET"/>. </summary>
        /// <param name="matchVersion"> See <see cref="LuceneVersion"/> </param>
        public StopAnalyzer(LuceneVersion matchVersion)
            : this(matchVersion, ENGLISH_STOP_WORDS_SET)
        {
        }

        /// <summary>
        /// Builds an analyzer with the stop words from the given set. </summary>
        /// <param name="matchVersion"> See <see cref="LuceneVersion"/> </param>
        /// <param name="stopWords"> Set of stop words  </param>
        public StopAnalyzer(LuceneVersion matchVersion, CharArraySet stopWords)
            : base(matchVersion, stopWords)
        {
        }

        /// <summary>
        /// Builds an analyzer with the stop words from the given file. </summary>
        /// <seealso cref="WordlistLoader.GetWordSet(TextReader, LuceneVersion)"/>
        /// <param name="matchVersion"> See <see cref="LuceneVersion"/> </param>
        /// <param name="stopwordsFile"> File to load stop words from  </param>
        public StopAnalyzer(LuceneVersion matchVersion, FileInfo stopwordsFile)
            : this(matchVersion, LoadStopwordSet(stopwordsFile, matchVersion))
        {
        }

        /// <summary>
        /// Builds an analyzer with the stop words from the given reader. </summary>
        /// <seealso cref="WordlistLoader.GetWordSet(TextReader, LuceneVersion)"/>
        /// <param name="matchVersion"> See <see cref="LuceneVersion"/> </param>
        /// <param name="stopwords"> <see cref="TextReader"/> to load stop words from  </param>
        public StopAnalyzer(LuceneVersion matchVersion, TextReader stopwords)
            : this(matchVersion, LoadStopwordSet(stopwords, matchVersion))
        {
        }

        /// <summary>
        /// Creates
        /// <see cref="TokenStreamComponents"/>
        /// used to tokenize all the text in the provided <see cref="TextReader"/>.
        /// </summary>
        /// <returns> <see cref="TokenStreamComponents"/>
        ///         built from a <see cref="LowerCaseTokenizer"/> filtered with
        ///         <see cref="StopFilter"/> </returns>
        protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
            Tokenizer source = new LowerCaseTokenizer(m_matchVersion, reader);
            return new TokenStreamComponents(source, new StopFilter(m_matchVersion, source, m_stopwords));
        }
    }
}