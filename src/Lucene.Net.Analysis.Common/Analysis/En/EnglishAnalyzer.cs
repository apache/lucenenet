// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using System.IO;

namespace Lucene.Net.Analysis.En
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
    /// <see cref="Analyzer"/> for English.
    /// </summary>
    public sealed class EnglishAnalyzer : StopwordAnalyzerBase
    {
        private readonly CharArraySet stemExclusionSet;

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
            internal static readonly CharArraySet DEFAULT_STOP_SET = StandardAnalyzer.STOP_WORDS_SET;
        }

        /// <summary>
        /// Builds an analyzer with the default stop words: <see cref="DefaultStopSet"/>.
        /// </summary>
        /// <param name="matchVersion"> lucene compatibility version </param>
        public EnglishAnalyzer(LuceneVersion matchVersion)
              : this(matchVersion, DefaultSetHolder.DEFAULT_STOP_SET)
        {
        }

        /// <summary>
        /// Builds an analyzer with the given stop words.
        /// </summary>
        /// <param name="matchVersion"> lucene compatibility version </param>
        /// <param name="stopwords"> a stopword set </param>
        public EnglishAnalyzer(LuceneVersion matchVersion, CharArraySet stopwords)
              : this(matchVersion, stopwords, CharArraySet.Empty)
        {
        }

        /// <summary>
        /// Builds an analyzer with the given stop words. If a non-empty stem exclusion set is
        /// provided this analyzer will add a <see cref="SetKeywordMarkerFilter"/> before
        /// stemming.
        /// </summary>
        /// <param name="matchVersion"> lucene compatibility version </param>
        /// <param name="stopwords"> a stopword set </param>
        /// <param name="stemExclusionSet"> a set of terms not to be stemmed </param>
        public EnglishAnalyzer(LuceneVersion matchVersion, CharArraySet stopwords, CharArraySet stemExclusionSet)
            : base(matchVersion, stopwords)
        {
            this.stemExclusionSet = CharArraySet.Copy(matchVersion, stemExclusionSet).AsReadOnly();
        }

        /// <summary>
        /// Creates a
        /// <see cref="TokenStreamComponents"/>
        /// which tokenizes all the text in the provided <see cref="TextReader"/>.
        /// </summary>
        /// <returns> A
        ///         <see cref="TokenStreamComponents"/>
        ///         built from an <see cref="StandardTokenizer"/> filtered with
        ///         <see cref="StandardFilter"/>, <see cref="EnglishPossessiveFilter"/>, 
        ///         <see cref="LowerCaseFilter"/>, <see cref="StopFilter"/>,
        ///         <see cref="SetKeywordMarkerFilter"/> if a stem exclusion set is
        ///         provided and <see cref="PorterStemFilter"/>. </returns>
        protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
            Tokenizer source = new StandardTokenizer(m_matchVersion, reader);
            TokenStream result = new StandardFilter(m_matchVersion, source);
            // prior to this we get the classic behavior, standardfilter does it for us.
#pragma warning disable 612, 618
            if (m_matchVersion.OnOrAfter(LuceneVersion.LUCENE_31))
#pragma warning restore 612, 618
            {
                result = new EnglishPossessiveFilter(m_matchVersion, result);
            }
            result = new LowerCaseFilter(m_matchVersion, result);
            result = new StopFilter(m_matchVersion, result, m_stopwords);
            if (stemExclusionSet.Count > 0)
            {
                result = new SetKeywordMarkerFilter(result, stemExclusionSet);
            }
            result = new PorterStemFilter(result);
            return new TokenStreamComponents(source, result);
        }
    }
}