// Lucene version compatibility level 4.8.1
#if FEATURE_BREAKITERATOR
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using System;
using System.IO;

namespace Lucene.Net.Analysis.Th
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
    /// <see cref="Analyzer"/> for Thai language. It uses <see cref="ICU4N.Text.BreakIterator"/> to break words.
    /// <para>You must specify the required <see cref="LuceneVersion"/>
    /// compatibility when creating <see cref="ThaiAnalyzer"/>:
    /// <list type="bullet">
    ///     <item><description> As of 3.6, a set of Thai stopwords is used by default</description></item>
    /// </list>
    /// </para>
    /// </summary>
    public sealed class ThaiAnalyzer : StopwordAnalyzerBase
    {
        /// <summary>
        /// File containing default Thai stopwords. </summary>
        public const string DEFAULT_STOPWORD_FILE = "stopwords.txt";
        /// <summary>
        /// The comment character in the stopwords file.  
        /// All lines prefixed with this will be ignored.
        /// </summary>
        private const string STOPWORDS_COMMENT = "#";

        /// <summary>
        /// Returns an unmodifiable instance of the default stop words set. </summary>
        /// <returns> default stop words set. </returns>
        public static CharArraySet DefaultStopSet => DefaultSetHolder.DEFAULT_STOP_SET;

        /// <summary>
        /// Atomically loads the <see cref="DEFAULT_STOP_SET"/> in a lazy fashion once the outer class 
        /// accesses the static final set the first time.;
        /// </summary>
        private static class DefaultSetHolder
        {
            internal static readonly CharArraySet DEFAULT_STOP_SET = LoadDefaultStopSet();

            private static CharArraySet LoadDefaultStopSet() // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
            {
                try
                {
                    return LoadStopwordSet(false, typeof(ThaiAnalyzer), DEFAULT_STOPWORD_FILE, STOPWORDS_COMMENT).AsReadOnly(); // LUCENENET: Made readonly as stated in the docs: https://github.com/apache/lucene/issues/11866
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
        /// Builds an analyzer with the default stop words.
        /// </summary>
        /// <param name="matchVersion"> lucene compatibility version </param>
        public ThaiAnalyzer(LuceneVersion matchVersion)
            : this(matchVersion,
#pragma warning disable 612, 618
                    matchVersion.OnOrAfter(LuceneVersion.LUCENE_36) ?
#pragma warning restore 612, 618
                    DefaultSetHolder.DEFAULT_STOP_SET : StopAnalyzer.ENGLISH_STOP_WORDS_SET)
        {
        }

        /// <summary>
        /// Builds an analyzer with the given stop words.
        /// </summary>
        /// <param name="matchVersion"> lucene compatibility version </param>
        /// <param name="stopwords"> a stopword set </param>
        public ThaiAnalyzer(LuceneVersion matchVersion, CharArraySet stopwords)
            : base(matchVersion, stopwords)
        {
        }

        /// <summary>
        /// Creates
        /// <see cref="TokenStreamComponents"/>
        /// used to tokenize all the text in the provided <see cref="TextReader"/>.
        /// </summary>
        /// <returns> <see cref="TokenStreamComponents"/>
        ///         built from a <see cref="StandardTokenizer"/> filtered with
        ///         <see cref="StandardFilter"/>, <see cref="LowerCaseFilter"/>, <see cref="ThaiWordFilter"/>, and
        ///         <see cref="StopFilter"/> </returns>
        protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
            if (m_matchVersion.OnOrAfter(LuceneVersion.LUCENE_48))
            {
                Tokenizer source = new ThaiTokenizer(reader);
                TokenStream result = new LowerCaseFilter(m_matchVersion, source);
                result = new StopFilter(m_matchVersion, result, m_stopwords);
                return new TokenStreamComponents(source, result);
            }
            else
            {
                Tokenizer source = new StandardTokenizer(m_matchVersion, reader);
                TokenStream result = new StandardFilter(m_matchVersion, source);
#pragma warning disable 612, 618
                if (m_matchVersion.OnOrAfter(LuceneVersion.LUCENE_31))
#pragma warning restore 612, 618
                {
                    result = new LowerCaseFilter(m_matchVersion, result);
                }
#pragma warning disable 612, 618
                result = new ThaiWordFilter(m_matchVersion, result);
#pragma warning restore 612, 618
                return new TokenStreamComponents(source, new StopFilter(m_matchVersion, result, m_stopwords));
            }
        }
    }
}
#endif