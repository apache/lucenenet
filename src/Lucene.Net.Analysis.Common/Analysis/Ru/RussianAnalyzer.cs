﻿// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Snowball;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using System;
using System.IO;
using System.Text;

namespace Lucene.Net.Analysis.Ru
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
    /// <see cref="Analyzer"/> for Russian language. 
    /// <para>
    /// Supports an external list of stopwords (words that
    /// will not be indexed at all).
    /// A default set of stopwords is used unless an alternative list is specified.
    /// </para>
    /// <para>You must specify the required <see cref="LuceneVersion"/>
    /// compatibility when creating <see cref="RussianAnalyzer"/>:
    /// <list type="bullet">
    ///     <item><description> As of 3.1, <see cref="StandardTokenizer"/> is used, Snowball stemming is done with
    ///        <see cref="SnowballFilter"/>, and Snowball stopwords are used by default.</description></item>
    /// </list>
    /// </para>
    /// </summary>
    public sealed class RussianAnalyzer : StopwordAnalyzerBase
    {
        /// <summary>
        /// List of typical Russian stopwords. (for backwards compatibility) </summary>
        /// @deprecated (3.1) Remove this for LUCENE 5.0 
        [Obsolete("(3.1) Remove this for LUCENE 5.0")]
        private static readonly string[] RUSSIAN_STOP_WORDS_30 = new string[] {
            "а", "без", "более", "бы", "был", "была", "были", "было", "быть", "в",
            "вам", "вас", "весь", "во", "вот", "все", "всего", "всех", "вы", "где",
            "да", "даже", "для", "до", "его", "ее", "ей", "ею", "если", "есть",
            "еще", "же", "за", "здесь", "и", "из", "или", "им", "их", "к", "как",
            "ко", "когда", "кто", "ли", "либо", "мне", "может", "мы", "на", "надо",
            "наш", "не", "него", "нее", "нет", "ни", "них", "но", "ну", "о", "об",
            "однако", "он", "она", "они", "оно", "от", "очень", "по", "под", "при",
            "с", "со", "так", "также", "такой", "там", "те", "тем", "то", "того",
            "тоже", "той", "только", "том", "ты", "у", "уже", "хотя", "чего", "чей",
            "чем", "что", "чтобы", "чье", "чья", "эта", "эти", "это", "я"
        };

        /// <summary>
        /// File containing default Russian stopwords. </summary>
        public const string DEFAULT_STOPWORD_FILE = "russian_stop.txt";

        private static class DefaultSetHolder
        {
            /// @deprecated (3.1) remove this for Lucene 5.0 
            [Obsolete("(3.1) remove this for Lucene 5.0")]
            internal static readonly CharArraySet DEFAULT_STOP_SET_30 = new CharArraySet(LuceneVersion.LUCENE_CURRENT, RUSSIAN_STOP_WORDS_30, false).AsReadOnly();
            internal static readonly CharArraySet DEFAULT_STOP_SET = LoadDefaultStopSet();

            private static CharArraySet LoadDefaultStopSet() // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
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

        private readonly CharArraySet stemExclusionSet;

        /// <summary>
        /// Returns an unmodifiable instance of the default stop-words set.
        /// </summary>
        /// <returns> an unmodifiable instance of the default stop-words set. </returns>
        public static CharArraySet DefaultStopSet => DefaultSetHolder.DEFAULT_STOP_SET;

        public RussianAnalyzer(LuceneVersion matchVersion)
#pragma warning disable 612, 618
            : this(matchVersion, matchVersion.OnOrAfter(LuceneVersion.LUCENE_31) ? 
                  DefaultSetHolder.DEFAULT_STOP_SET : DefaultSetHolder.DEFAULT_STOP_SET_30)
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
        public RussianAnalyzer(LuceneVersion matchVersion, CharArraySet stopwords)
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
        /// <param name="stemExclusionSet"> a set of words not to be stemmed </param>
        public RussianAnalyzer(LuceneVersion matchVersion, CharArraySet stopwords, CharArraySet stemExclusionSet)
            : base(matchVersion, stopwords)
        {
            this.stemExclusionSet = CharArraySet.Copy(matchVersion, stemExclusionSet).AsReadOnly();
        }

        /// <summary>
        /// Creates
        /// <see cref="TokenStreamComponents"/>
        /// used to tokenize all the text in the provided <see cref="TextReader"/>.
        /// </summary>
        /// <returns> <see cref="TokenStreamComponents"/>
        ///         built from a <see cref="StandardTokenizer"/> filtered with
        ///         <see cref="StandardFilter"/>, <see cref="LowerCaseFilter"/>, <see cref="StopFilter"/>
        ///         , <see cref="SetKeywordMarkerFilter"/> if a stem exclusion set is
        ///         provided, and <see cref="SnowballFilter"/> </returns>
        protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
#pragma warning disable 612, 618
            if (m_matchVersion.OnOrAfter(LuceneVersion.LUCENE_31))
#pragma warning restore 612, 618
            {
                Tokenizer source = new StandardTokenizer(m_matchVersion, reader);
                TokenStream result = new StandardFilter(m_matchVersion, source);
                result = new LowerCaseFilter(m_matchVersion, result);
                result = new StopFilter(m_matchVersion, result, m_stopwords);
                if (stemExclusionSet.Count > 0)
                {
                    result = new SetKeywordMarkerFilter(result, stemExclusionSet);
                }
                result = new SnowballFilter(result, new Tartarus.Snowball.Ext.RussianStemmer());
                return new TokenStreamComponents(source, result);
            }
            else
            {
#pragma warning disable 612, 618
                Tokenizer source = new RussianLetterTokenizer(m_matchVersion, reader);
#pragma warning restore 612, 618
                TokenStream result = new LowerCaseFilter(m_matchVersion, source);
                result = new StopFilter(m_matchVersion, result, m_stopwords);
                if (stemExclusionSet.Count > 0)
                {
                    result = new SetKeywordMarkerFilter(result, stemExclusionSet);
                }
                result = new SnowballFilter(result, new Tartarus.Snowball.Ext.RussianStemmer());
                return new TokenStreamComponents(source, result);
            }
        }
    }
}