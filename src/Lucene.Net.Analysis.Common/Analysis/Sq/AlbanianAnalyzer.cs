// LUCENENET specific: Albanian is not supported by Apache Lucene 4.8; this analyzer is a .NET-only addition.
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using System;
using System.IO;

namespace Lucene.Net.Analysis.Sq
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
    /// <see cref="Analyzer"/> for Albanian.
    /// <para>
    /// Tokenizes with <see cref="StandardTokenizer"/>, lower-cases, folds <c>ë</c>/<c>ç</c>
    /// and in-word apostrophes with <see cref="AlbanianNormalizationFilter"/>, removes stop
    /// words and applies <see cref="AlbanianLightStemFilter"/>. Normalization runs before
    /// stop word removal, so the stop word list matches both <c>është</c> and <c>eshte</c>.
    /// </para>
    /// </summary>
    public sealed class AlbanianAnalyzer : StopwordAnalyzerBase
    {
        /// <summary>
        /// File containing default Albanian stopwords.
        ///
        /// The default stopword list is the Albanian list of the spaCy project
        /// (https://github.com/explosion/spaCy/blob/master/spacy/lang/sq/stop_words.py),
        /// which took it from https://github.com/andrixh/index-albanian; both are
        /// MIT-licensed. That list is written without diacritics; the standard-orthography
        /// spellings of the same words are included as well.
        /// </summary>
        public const string DEFAULT_STOPWORD_FILE = "stopwords.txt";

        /// <summary>
        /// Returns an unmodifiable instance of the default stop-words set.
        /// </summary>
        /// <returns> an unmodifiable instance of the default stop-words set. </returns>
        public static CharArraySet DefaultStopSet => DefaultSetHolder.DEFAULT_STOP_SET;

        /// <summary>
        /// Atomically loads the DEFAULT_STOP_SET in a lazy fashion once the outer
        /// class accesses the static final set the first time.
        /// </summary>
        private static class DefaultSetHolder
        {
            internal static readonly CharArraySet DEFAULT_STOP_SET = LoadDefaultStopSet();

            private static CharArraySet LoadDefaultStopSet() // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
            {
                try
                {
                    // the stop filter runs after the normalizer, so the list (kept readable: që, aq, çdo)
                    // is matched in its normalized spelling
                    return Normalized(LoadStopwordSet(false, typeof(AlbanianAnalyzer), DEFAULT_STOPWORD_FILE, "#"));
                }
                catch (Exception ex) when (ex.IsIOException())
                {
                    // default set should always be present as it is part of the
                    // distribution (JAR)
                    throw RuntimeException.Create("Unable to load default stopword set", ex);
                }
            }
        }

        private static CharArraySet Normalized(CharArraySet words)
        {
            var normalizer = new AlbanianNormalizer();
            var result = new CharArraySet(LuceneVersion.LUCENE_48, words.Count, ignoreCase: false);
            foreach (string word in words)
            {
                char[] chars = word.ToCharArray();
                int len = normalizer.Normalize(chars, chars.Length);
                result.Add(new string(chars, 0, len));
            }
            return result.AsReadOnly();
        }

        private readonly CharArraySet stemExclusionSet;

        /// <summary>
        /// Builds an analyzer with the default stop words:
        /// <see cref="DEFAULT_STOPWORD_FILE"/>.
        /// </summary>
        public AlbanianAnalyzer(LuceneVersion matchVersion)
            : this(matchVersion, DefaultSetHolder.DEFAULT_STOP_SET)
        {
        }

        /// <summary>
        /// Builds an analyzer with the given stop words.
        /// </summary>
        /// <param name="matchVersion"> lucene compatibility version </param>
        /// <param name="stopwords"> a stopword set </param>
        public AlbanianAnalyzer(LuceneVersion matchVersion, CharArraySet stopwords)
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
        public AlbanianAnalyzer(LuceneVersion matchVersion, CharArraySet stopwords, CharArraySet stemExclusionSet)
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
        ///         <see cref="StandardFilter"/>, <see cref="LowerCaseFilter"/>,
        ///         <see cref="AlbanianNormalizationFilter"/>, <see cref="StopFilter"/>,
        ///         <see cref="SetKeywordMarkerFilter"/> if a stem exclusion set is
        ///         provided and <see cref="AlbanianLightStemFilter"/>. </returns>
        protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
            Tokenizer source = new StandardTokenizer(m_matchVersion, reader);
            TokenStream result = new StandardFilter(m_matchVersion, source);
            result = new LowerCaseFilter(m_matchVersion, result);
            result = new AlbanianNormalizationFilter(result);
            result = new StopFilter(m_matchVersion, result, m_stopwords);
            if (stemExclusionSet.Count > 0)
            {
                result = new SetKeywordMarkerFilter(result, stemExclusionSet);
            }
            result = new AlbanianLightStemFilter(result);
            return new TokenStreamComponents(source, result);
        }
    }
}
