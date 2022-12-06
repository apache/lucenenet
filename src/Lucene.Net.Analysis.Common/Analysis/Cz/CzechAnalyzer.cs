// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using System;
using System.IO;
using System.Text;

namespace Lucene.Net.Analysis.Cz
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
    /// <see cref="Analyzer"/> for Czech language.
    /// <para>
    /// Supports an external list of stopwords (words that will not be indexed at
    /// all). A default set of stopwords is used unless an alternative list is
    /// specified.
    /// </para>
    /// <para>
    /// You must specify the required <see cref="LuceneVersion"/> compatibility when creating
    /// <see cref="CzechAnalyzer"/>:
    /// <list type="bullet">
    ///     <item><description>As of 3.1, words are stemmed with <see cref="CzechStemFilter"/></description></item>
    ///     <item><description>As of 2.9, StopFilter preserves position increments</description></item>
    ///     <item><description>As of 2.4, Tokens incorrectly identified as acronyms are corrected (see
    ///     <a href="https://issues.apache.org/jira/browse/LUCENE-1068">LUCENE-1068</a>)</description></item>
    /// </list>
    /// </para>
    /// </summary>
    public sealed class CzechAnalyzer : StopwordAnalyzerBase
    {
        /// <summary>
        /// File containing default Czech stopwords. </summary>
        public const string DEFAULT_STOPWORD_FILE = "stopwords.txt";

        /// <summary>
        /// Returns a set of default Czech-stopwords
        /// </summary>
        /// <returns> a set of default Czech-stopwords </returns>
        public static CharArraySet DefaultStopSet => DefaultSetHolder.DEFAULT_SET;

        private static class DefaultSetHolder
        {
            internal static readonly CharArraySet DEFAULT_SET = LoadDefaultSet();

            private static CharArraySet LoadDefaultSet() // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
            {
                try
                {
                    return WordlistLoader.GetWordSet(
                        IOUtils.GetDecodingReader(typeof(CzechAnalyzer), DEFAULT_STOPWORD_FILE, Encoding.UTF8), 
                        "#",
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


        private readonly CharArraySet stemExclusionTable;

        /// <summary>
        /// Builds an analyzer with the default stop words (<see cref="DefaultStopSet"/>).
        /// </summary>
        /// <param name="matchVersion"> <see cref="LuceneVersion"/> to match </param>
        public CzechAnalyzer(LuceneVersion matchVersion)
              : this(matchVersion, DefaultSetHolder.DEFAULT_SET)
        {
        }

        /// <summary>
        /// Builds an analyzer with the given stop words.
        /// </summary>
        /// <param name="matchVersion"> <see cref="LuceneVersion"/> to match </param>
        /// <param name="stopwords"> a stopword set </param>
        public CzechAnalyzer(LuceneVersion matchVersion, CharArraySet stopwords)
              : this(matchVersion, stopwords, CharArraySet.Empty)
        {
        }

        /// <summary>
        /// Builds an analyzer with the given stop words and a set of work to be
        /// excluded from the <see cref="CzechStemFilter"/>.
        /// </summary>
        /// <param name="matchVersion"> <see cref="LuceneVersion"/> to match </param>
        /// <param name="stopwords"> a stopword set </param>
        /// <param name="stemExclusionTable"> a stemming exclusion set </param>
        public CzechAnalyzer(LuceneVersion matchVersion, CharArraySet stopwords, CharArraySet stemExclusionTable)
              : base(matchVersion, stopwords)
        {
            this.stemExclusionTable = CharArraySet.Copy(matchVersion, stemExclusionTable).AsReadOnly();
        }

        /// <summary>
        /// Creates
        /// <see cref="TokenStreamComponents"/>
        /// used to tokenize all the text in the provided <see cref="TextReader"/>.
        /// </summary>
        /// <returns> <see cref="TokenStreamComponents"/>
        ///         built from a <see cref="StandardTokenizer"/> filtered with
        ///         <see cref="StandardFilter"/>, <see cref="LowerCaseFilter"/>, <see cref="StopFilter"/>,
        ///         and <see cref="CzechStemFilter"/> (only if version is >= LUCENE_31). If
        ///         a version is >= LUCENE_31 and a stem exclusion set is provided via
        ///         <see cref="CzechAnalyzer(LuceneVersion, CharArraySet, CharArraySet)"/> a
        ///         <see cref="SetKeywordMarkerFilter"/> is added before
        ///         <see cref="CzechStemFilter"/>. </returns>

        protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
            Tokenizer source = new StandardTokenizer(m_matchVersion, reader);
            TokenStream result = new StandardFilter(m_matchVersion, source);
            result = new LowerCaseFilter(m_matchVersion, result);
            result = new StopFilter(m_matchVersion, result, m_stopwords);
#pragma warning disable 612, 618
            if (m_matchVersion.OnOrAfter(LuceneVersion.LUCENE_31))
#pragma warning restore 612, 618
            {
                if (this.stemExclusionTable.Count > 0)
                {
                    result = new SetKeywordMarkerFilter(result, stemExclusionTable);
                }
                result = new CzechStemFilter(result);
            }
            return new TokenStreamComponents(source, result);
        }
    }
}