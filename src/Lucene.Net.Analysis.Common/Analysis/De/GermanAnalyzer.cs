using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Snowball;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Support;
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
    /// <seealso cref="Analyzer"/> for German language. 
    /// <para>
    /// Supports an external list of stopwords (words that
    /// will not be indexed at all) and an external list of exclusions (word that will
    /// not be stemmed, but indexed).
    /// A default set of stopwords is used unless an alternative list is specified, but the
    /// exclusion list is empty by default.
    /// </para>
    /// 
    /// <a name="version"/>
    /// <para>You must specify the required <seealso cref="Version"/>
    /// compatibility when creating GermanAnalyzer:
    /// <ul>
    ///   <li> As of 3.6, GermanLightStemFilter is used for less aggressive stemming.
    ///   <li> As of 3.1, Snowball stemming is done with SnowballFilter, and 
    ///        Snowball stopwords are used by default.
    ///   <li> As of 2.9, StopFilter preserves position
    ///        increments
    /// </ul>
    /// 
    /// </para>
    /// <para><b>NOTE</b>: This class uses the same <seealso cref="Version"/>
    /// dependent settings as <seealso cref="StandardAnalyzer"/>.</para>
    /// </summary>
    public sealed class GermanAnalyzer : StopwordAnalyzerBase
    {

        /// @deprecated in 3.1, remove in Lucene 5.0 (index bw compat) 
        [Obsolete("in 3.1, remove in Lucene 5.0 (index bw compat)")]
        private static readonly string[] GERMAN_STOP_WORDS = new string[] { "einer", "eine", "eines", "einem", "einen", "der", "die", "das", "dass", "daß", "du", "er", "sie", "es", "was", "wer", "wie", "wir", "und", "oder", "ohne", "mit", "am", "im", "in", "aus", "auf", "ist", "sein", "war", "wird", "ihr", "ihre", "ihres", "als", "für", "von", "mit", "dich", "dir", "mich", "mir", "mein", "sein", "kein", "durch", "wegen", "wird" };

        /// <summary>
        /// File containing default German stopwords. </summary>
        public const string DEFAULT_STOPWORD_FILE = "german_stop.txt";

        /// <summary>
        /// Returns a set of default German-stopwords </summary>
        /// <returns> a set of default German-stopwords  </returns>
        public static CharArraySet DefaultStopSet
        {
            get
            {
                return DefaultSetHolder.DEFAULT_SET;
            }
        }

        private class DefaultSetHolder
        {
            /// @deprecated in 3.1, remove in Lucene 5.0 (index bw compat) 
            [Obsolete("in 3.1, remove in Lucene 5.0 (index bw compat)")]
            internal static readonly CharArraySet DEFAULT_SET_30 = CharArraySet.UnmodifiableSet(new CharArraySet(LuceneVersion.LUCENE_CURRENT, Arrays.AsList(GERMAN_STOP_WORDS), false));
            internal static readonly CharArraySet DEFAULT_SET;
            static DefaultSetHolder()
            {
                try
                {
                    var resource = GetAnalysisResourceName(typeof(SnowballFilter), "Snowball", DEFAULT_STOPWORD_FILE);
                    DEFAULT_SET = WordlistLoader.GetSnowballWordSet(
                        IOUtils.GetDecodingReader(typeof(SnowballFilter), resource, Encoding.UTF8),
#pragma warning disable 612, 618
                        LuceneVersion.LUCENE_CURRENT);
#pragma warning restore 612, 618
                }
                catch (IOException)
                {
                    // default set should always be present as it is part of the
                    // distribution (JAR)
                    throw new Exception("Unable to load default stopword set");
                }
            }
        }

        /// <summary>
        /// Contains the stopwords used with the <seealso cref="StopFilter"/>.
        /// </summary>

        /// <summary>
        /// Contains words that should be indexed but not stemmed.
        /// </summary>
        private readonly CharArraySet exclusionSet;

        /// <summary>
        /// Builds an analyzer with the default stop words:
        /// <seealso cref="#getDefaultStopSet()"/>.
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
        /// <param name="stemExclusionSet">
        ///          a stemming exclusion set </param>
        public GermanAnalyzer(LuceneVersion matchVersion, CharArraySet stopwords, CharArraySet stemExclusionSet)
              : base(matchVersion, stopwords)
        {
            exclusionSet = CharArraySet.UnmodifiableSet(CharArraySet.Copy(matchVersion, stemExclusionSet));
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
        ///         provided, <seealso cref="GermanNormalizationFilter"/> and <seealso cref="GermanLightStemFilter"/> </returns>
        public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
            Tokenizer source = new StandardTokenizer(matchVersion, reader);
            TokenStream result = new StandardFilter(matchVersion, source);
            result = new LowerCaseFilter(matchVersion, result);
            result = new StopFilter(matchVersion, result, stopwords);
            result = new SetKeywordMarkerFilter(result, exclusionSet);
#pragma warning disable 612, 618
            if (matchVersion.OnOrAfter(LuceneVersion.LUCENE_36))
            {
                result = new GermanNormalizationFilter(result);
                result = new GermanLightStemFilter(result);
            }
            else if (matchVersion.OnOrAfter(LuceneVersion.LUCENE_31))
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