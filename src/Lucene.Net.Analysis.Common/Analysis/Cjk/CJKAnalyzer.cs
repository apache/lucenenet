// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using System;
using System.IO;

namespace Lucene.Net.Analysis.Cjk
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
    /// An <see cref="Analyzer"/> that tokenizes text with <see cref="StandardTokenizer"/>,
    /// normalizes content with <see cref="CJKWidthFilter"/>, folds case with
    /// <see cref="LowerCaseFilter"/>, forms bigrams of CJK with <see cref="CJKBigramFilter"/>,
    /// and filters stopwords with <see cref="StopFilter"/>
    /// </summary>
    public sealed class CJKAnalyzer : StopwordAnalyzerBase
    {
        /// <summary>
        /// File containing default CJK stopwords.
        /// <para/>
        /// Currently it contains some common English words that are not usually
        /// useful for searching and some double-byte interpunctions.
        /// </summary>
        public const string DEFAULT_STOPWORD_FILE = "stopwords.txt";

        /// <summary>
        /// Returns an unmodifiable instance of the default stop-words set. </summary>
        /// <returns> an unmodifiable instance of the default stop-words set. </returns>
        public static CharArraySet DefaultStopSet => DefaultSetHolder.DEFAULT_STOP_SET;

        private static class DefaultSetHolder
        {
            internal static readonly CharArraySet DEFAULT_STOP_SET = LoadDefaultStopSet();

            private static CharArraySet LoadDefaultStopSet() // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
            {
                try
                {
                    return LoadStopwordSet(false, typeof(CJKAnalyzer), DEFAULT_STOPWORD_FILE, "#").AsReadOnly(); // LUCENENET: Made readonly as stated in the docs: https://github.com/apache/lucene/issues/11866
                }
                catch (Exception ex) when (ex.IsIOException())
                {
                    // default set should always be present as it is part of the
                    // distribution (JAR)
                    throw RuntimeException.Create("Unable to load default stopword set", ex);
                }
            }
        }

        /// <summary>
        /// Builds an analyzer which removes words in <see cref="DefaultStopSet"/>.
        /// </summary>
        public CJKAnalyzer(LuceneVersion matchVersion)
              : this(matchVersion, DefaultSetHolder.DEFAULT_STOP_SET)
        {
        }

        /// <summary>
        /// Builds an analyzer with the given stop words
        /// </summary>
        /// <param name="matchVersion">
        ///          lucene compatibility version </param>
        /// <param name="stopwords">
        ///          a stopword set </param>
        public CJKAnalyzer(LuceneVersion matchVersion, CharArraySet stopwords)
              : base(matchVersion, stopwords)
        {
        }

        protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
#pragma warning disable 612, 618
            if (m_matchVersion.OnOrAfter(LuceneVersion.LUCENE_36))
#pragma warning restore 612, 618
            {
                Tokenizer source = new StandardTokenizer(m_matchVersion, reader);
                // run the widthfilter first before bigramming, it sometimes combines characters.
                TokenStream result = new CJKWidthFilter(source);
                result = new LowerCaseFilter(m_matchVersion, result);
                result = new CJKBigramFilter(result);
                return new TokenStreamComponents(source, new StopFilter(m_matchVersion, result, m_stopwords));
            }
            else
            {
#pragma warning disable 612, 618
                Tokenizer source = new CJKTokenizer(reader);
#pragma warning restore 612, 618
                return new TokenStreamComponents(source, new StopFilter(m_matchVersion, source, m_stopwords));
            }
        }
    }
}