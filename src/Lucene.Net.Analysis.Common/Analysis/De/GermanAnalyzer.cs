// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Snowball;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Tartarus.Snowball.Ext;
using Lucene.Net.Util;
using System;
using System.IO;
using System.Text;

namespace Lucene.Net.Analysis.De
{
    // This file is encoded in UTF-8

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
    /// <see cref="Analyzer"/> for German language. 
    /// <para>
    /// Supports an external list of stopwords (words that
    /// will not be indexed at all) and an external list of exclusions (word that will
    /// not be stemmed, but indexed).
    /// A default set of stopwords is used unless an alternative list is specified, but the
    /// exclusion list is empty by default.
    /// </para>
    /// 
    /// <para>You must specify the required <see cref="LuceneVersion"/>
    /// compatibility when creating GermanAnalyzer:
    /// <list>
    ///   <item><description> As of 3.6, GermanLightStemFilter is used for less aggressive stemming.</description></item>
    ///   <item><description> As of 3.1, Snowball stemming is done with SnowballFilter, and 
    ///        Snowball stopwords are used by default.</description></item>
    ///   <item><description> As of 2.9, StopFilter preserves position
    ///        increments</description></item>
    /// </list>
    /// 
    /// </para>
    /// <para><b>NOTE</b>: This class uses the same <see cref="LuceneVersion"/>
    /// dependent settings as <see cref="StandardAnalyzer"/>.</para>
    /// </summary>
    public sealed class GermanAnalyzer : StopwordAnalyzerBase
    {
        /// @deprecated in 3.1, remove in Lucene 5.0 (index bw compat) 
        [Obsolete("in 3.1, remove in Lucene 5.0 (index bw compat)")]
        private static readonly string[] GERMAN_STOP_WORDS = new string[] {
            "einer", "eine", "eines", "einem", "einen",
            "der", "die", "das", "dass", "daß",
            "du", "er", "sie", "es",
            "was", "wer", "wie", "wir",
            "und", "oder", "ohne", "mit",
            "am", "im", "in", "aus", "auf",
            "ist", "sein", "war", "wird",
            "ihr", "ihre", "ihres",
            "als", "für", "von", "mit",
            "dich", "dir", "mich", "mir",
            "mein", "sein", "kein",
            "durch", "wegen", "wird"
        };

        /// <summary>
        /// File containing default German stopwords. </summary>
        public const string DEFAULT_STOPWORD_FILE = "german_stop.txt";

        /// <summary>
        /// Returns a set of default German-stopwords </summary>
        /// <returns> a set of default German-stopwords  </returns>
        public static CharArraySet DefaultStopSet => DefaultSetHolder.DEFAULT_SET;

        private static class DefaultSetHolder
        {
            /// @deprecated in 3.1, remove in Lucene 5.0 (index bw compat) 
            [Obsolete("in 3.1, remove in Lucene 5.0 (index bw compat)")]
            internal static readonly CharArraySet DEFAULT_SET_30 = new CharArraySet(LuceneVersion.LUCENE_CURRENT, GERMAN_STOP_WORDS, false).AsReadOnly();
            internal static readonly CharArraySet DEFAULT_SET = LoadDefaultSet();
            private static CharArraySet LoadDefaultSet() // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
            {
                try
                {
                    return WordlistLoader.GetSnowballWordSet(
                        IOUtils.GetDecodingReader(typeof(SnowballFilter), DEFAULT_STOPWORD_FILE, Encoding.UTF8),
#pragma warning disable 612, 618
                        LuceneVersion.LUCENE_CURRENT).AsReadOnly(); // LUCENENET: Made readonly as stated in the docs: https://github.com/apache/lucene/issues/11866
#pragma warning restore 612, 618
                }
                catch (Exception ex) when (ex.IsIOException())
                {
                    // default set should always be present as it is part of the
                    // distribution (JAR)
                    throw RuntimeException.Create("Unable to load default stopword set", ex);
                }
            }
        }

        ///// <summary>
        ///// Contains the stopwords used with the <see cref="StopFilter"/>.
        ///// </summary>

        /// <summary>
        /// Contains words that should be indexed but not stemmed.
        /// </summary>
        private readonly CharArraySet exclusionSet;

        /// <summary>
        /// Builds an analyzer with the default stop words:
        /// <see cref="DefaultStopSet"/>.
        /// </summary>
        public GermanAnalyzer(LuceneVersion matchVersion)
#pragma warning disable 612, 618
              : this(matchVersion, matchVersion.OnOrAfter(LuceneVersion.LUCENE_31) ? 
                    DefaultSetHolder.DEFAULT_SET : DefaultSetHolder.DEFAULT_SET_30)
#pragma warning restore 612, 618
        {
        }

        /// <summary>
        /// Builds an analyzer with the given stop words 
        /// </summary>
        /// <param name="matchVersion">
        ///          lucene compatibility version </param>
        /// <param name="stopwords">
        ///          a stopword set </param>
        public GermanAnalyzer(LuceneVersion matchVersion, CharArraySet stopwords)
              : this(matchVersion, stopwords, CharArraySet.Empty)
        {
        }

        /// <summary>
        /// Builds an analyzer with the given stop words
        /// </summary>
        /// <param name="matchVersion">
        ///          lucene compatibility version </param>
        /// <param name="stopwords">
        ///          a stopword set </param>
        /// <param name="stemExclusionSet">
        ///          a stemming exclusion set </param>
        public GermanAnalyzer(LuceneVersion matchVersion, CharArraySet stopwords, CharArraySet stemExclusionSet)
              : base(matchVersion, stopwords)
        {
            exclusionSet = CharArraySet.Copy(matchVersion, stemExclusionSet).AsReadOnly();
        }

        /// <summary>
        /// Creates
        /// <see cref="TokenStreamComponents"/>
        /// used to tokenize all the text in the provided <see cref="TextReader"/>.
        /// </summary>
        /// <returns> <see cref="TokenStreamComponents"/>
        ///         built from a <see cref="StandardTokenizer"/> filtered with
        ///         <see cref="StandardFilter"/>, <see cref="LowerCaseFilter"/>, <see cref="StopFilter"/>,
        ///         <see cref="SetKeywordMarkerFilter"/> if a stem exclusion set is
        ///         provided, <see cref="GermanNormalizationFilter"/> and <see cref="GermanLightStemFilter"/> </returns>
        protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
            Tokenizer source = new StandardTokenizer(m_matchVersion, reader);
            TokenStream result = new StandardFilter(m_matchVersion, source);
            result = new LowerCaseFilter(m_matchVersion, result);
            result = new StopFilter(m_matchVersion, result, m_stopwords);
            result = new SetKeywordMarkerFilter(result, exclusionSet);
#pragma warning disable 612, 618
            if (m_matchVersion.OnOrAfter(LuceneVersion.LUCENE_36))
            {
                result = new GermanNormalizationFilter(result);
                result = new GermanLightStemFilter(result);
            }
            else if (m_matchVersion.OnOrAfter(LuceneVersion.LUCENE_31))
#pragma warning restore 612, 618
            {
                result = new SnowballFilter(result, new German2Stemmer());
            }
            else
            {
                result = new GermanStemFilter(result);
            }
            return new TokenStreamComponents(source, result);
        }
    }
}