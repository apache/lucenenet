using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Snowball;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Support;
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
    /// <seealso cref="Analyzer"/> for Russian language. 
    /// <para>
    /// Supports an external list of stopwords (words that
    /// will not be indexed at all).
    /// A default set of stopwords is used unless an alternative list is specified.
    /// </para>
    /// <a name="version"/>
    /// <para>You must specify the required <seealso cref="Version"/>
    /// compatibility when creating RussianAnalyzer:
    /// <ul>
    ///   <li> As of 3.1, StandardTokenizer is used, Snowball stemming is done with
    ///        SnowballFilter, and Snowball stopwords are used by default.
    /// </ul>
    /// </para>
    /// </summary>
    public sealed class RussianAnalyzer : StopwordAnalyzerBase
    {
        /// <summary>
        /// List of typical Russian stopwords. (for backwards compatibility) </summary>
        /// @deprecated (3.1) Remove this for LUCENE 5.0 
        [Obsolete("(3.1) Remove this for LUCENE 5.0")]
        private static readonly string[] RUSSIAN_STOP_WORDS_30 = new string[] { "а", "без", "более", "бы", "был", "была", "были", "было", "быть", "в", "вам", "вас", "весь", "во", "вот", "все", "всего", "всех", "вы", "где", "да", "даже", "для", "до", "его", "ее", "ей", "ею", "если", "есть", "еще", "же", "за", "здесь", "и", "из", "или", "им", "их", "к", "как", "ко", "когда", "кто", "ли", "либо", "мне", "может", "мы", "на", "надо", "наш", "не", "него", "нее", "нет", "ни", "них", "но", "ну", "о", "об", "однако", "он", "она", "они", "оно", "от", "очень", "по", "под", "при", "с", "со", "так", "также", "такой", "там", "те", "тем", "то", "того", "тоже", "той", "только", "том", "ты", "у", "уже", "хотя", "чего", "чей", "чем", "что", "чтобы", "чье", "чья", "эта", "эти", "это", "я" };

        /// <summary>
        /// File containing default Russian stopwords. </summary>
        public const string DEFAULT_STOPWORD_FILE = "russian_stop.txt";

        private class DefaultSetHolder
        {
            /// @deprecated (3.1) remove this for Lucene 5.0 
            [Obsolete("(3.1) remove this for Lucene 5.0")]
            internal static readonly CharArraySet DEFAULT_STOP_SET_30 = CharArraySet.UnmodifiableSet(new CharArraySet(LuceneVersion.LUCENE_CURRENT, Arrays.AsList(RUSSIAN_STOP_WORDS_30), false));
            internal static readonly CharArraySet DEFAULT_STOP_SET;

            static DefaultSetHolder()
            {
                try
                {
                    var resource = typeof(SnowballFilter).GetAnalysisResourceName(DEFAULT_STOPWORD_FILE);
                    DEFAULT_STOP_SET = WordlistLoader.GetSnowballWordSet(
                        IOUtils.GetDecodingReader(typeof(SnowballFilter), resource, Encoding.UTF8),
#pragma warning disable 612, 618
                        LuceneVersion.LUCENE_CURRENT);
#pragma warning restore 612, 618
                }
                catch (IOException ex)
                {
                    // default set should always be present as it is part of the
                    // distribution (JAR)
                    throw new Exception("Unable to load default stopword set", ex);
                }
            }
        }

        private readonly CharArraySet stemExclusionSet;

        /// <summary>
        /// Returns an unmodifiable instance of the default stop-words set.
        /// </summary>
        /// <returns> an unmodifiable instance of the default stop-words set. </returns>
        public static CharArraySet DefaultStopSet
        {
            get
            {
                return DefaultSetHolder.DEFAULT_STOP_SET;
            }
        }

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
            : this(matchVersion, stopwords, CharArraySet.EMPTY_SET)
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
            this.stemExclusionSet = CharArraySet.UnmodifiableSet(CharArraySet.Copy(matchVersion, stemExclusionSet));
        }

        /// <summary>
        /// Creates
        /// <seealso cref="org.apache.lucene.analysis.Analyzer.TokenStreamComponents"/>
        /// used to tokenize all the text in the provided <seealso cref="Reader"/>.
        /// </summary>
        /// <returns> <seealso cref="org.apache.lucene.analysis.Analyzer.TokenStreamComponents"/>
        ///         built from a <seealso cref="StandardTokenizer"/> filtered with
        ///         <seealso cref="StandardFilter"/>, <seealso cref="LowerCaseFilter"/>, <seealso cref="StopFilter"/>
        ///         , <seealso cref="SetKeywordMarkerFilter"/> if a stem exclusion set is
        ///         provided, and <seealso cref="SnowballFilter"/> </returns>
        public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
#pragma warning disable 612, 618
            if (matchVersion.OnOrAfter(LuceneVersion.LUCENE_31))
#pragma warning restore 612, 618
            {
                Tokenizer source = new StandardTokenizer(matchVersion, reader);
                TokenStream result = new StandardFilter(matchVersion, source);
                result = new LowerCaseFilter(matchVersion, result);
                result = new StopFilter(matchVersion, result, stopwords);
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
                Tokenizer source = new RussianLetterTokenizer(matchVersion, reader);
#pragma warning restore 612, 618
                TokenStream result = new LowerCaseFilter(matchVersion, source);
                result = new StopFilter(matchVersion, result, stopwords);
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