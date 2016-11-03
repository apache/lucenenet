using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using System;
using System.IO;
using System.Linq;
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
    /// <seealso cref="Analyzer"/> for Czech language.
    /// <para>
    /// Supports an external list of stopwords (words that will not be indexed at
    /// all). A default set of stopwords is used unless an alternative list is
    /// specified.
    /// </para>
    /// 
    /// <a name="version"/>
    /// <para>
    /// You must specify the required <seealso cref="Version"/> compatibility when creating
    /// CzechAnalyzer:
    /// <ul>
    /// <li>As of 3.1, words are stemmed with <seealso cref="CzechStemFilter"/>
    /// <li>As of 2.9, StopFilter preserves position increments
    /// <li>As of 2.4, Tokens incorrectly identified as acronyms are corrected (see
    /// <a href="https://issues.apache.org/jira/browse/LUCENE-1068">LUCENE-1068</a>)
    /// </ul>
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
        public static CharArraySet DefaultStopSet
        {
            get
            {
                return DefaultSetHolder.DEFAULT_SET;
            }
        }

        private class DefaultSetHolder
        {
            internal static readonly CharArraySet DEFAULT_SET;

            static DefaultSetHolder()
            {
                try
                {
                    var resource = typeof(CzechAnalyzer).GetAnalysisResourceName(DEFAULT_STOPWORD_FILE);
                    DEFAULT_SET = WordlistLoader.GetWordSet(
                        IOUtils.GetDecodingReader(typeof(CzechAnalyzer), resource, Encoding.UTF8), 
                        "#",
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


        private readonly CharArraySet stemExclusionTable;

        /// <summary>
        /// Builds an analyzer with the default stop words (<seealso cref="#getDefaultStopSet()"/>).
        /// </summary>
        /// <param name="matchVersion"> Lucene version to match See
        ///          <seealso cref="<a href="#version">above</a>"/> </param>
        public CzechAnalyzer(LuceneVersion matchVersion)
              : this(matchVersion, DefaultSetHolder.DEFAULT_SET)
        {
        }

        /// <summary>
        /// Builds an analyzer with the given stop words.
        /// </summary>
        /// <param name="matchVersion"> Lucene version to match See
        ///          <seealso cref="<a href="#version">above</a>"/> </param>
        /// <param name="stopwords"> a stopword set </param>
        public CzechAnalyzer(LuceneVersion matchVersion, CharArraySet stopwords)
              : this(matchVersion, stopwords, CharArraySet.EMPTY_SET)
        {
        }

        /// <summary>
        /// Builds an analyzer with the given stop words and a set of work to be
        /// excluded from the <seealso cref="CzechStemFilter"/>.
        /// </summary>
        /// <param name="matchVersion"> Lucene version to match See
        ///          <seealso cref="<a href="#version">above</a>"/> </param>
        /// <param name="stopwords"> a stopword set </param>
        /// <param name="stemExclusionTable"> a stemming exclusion set </param>
        public CzechAnalyzer(LuceneVersion matchVersion, CharArraySet stopwords, CharArraySet stemExclusionTable)
              : base(matchVersion, stopwords)
        {
            this.stemExclusionTable = CharArraySet.UnmodifiableSet(CharArraySet.Copy(matchVersion, stemExclusionTable));
        }

        /// <summary>
        /// Creates
        /// <seealso cref="org.apache.lucene.analysis.Analyzer.TokenStreamComponents"/>
        /// used to tokenize all the text in the provided <seealso cref="Reader"/>.
        /// </summary>
        /// <returns> <seealso cref="org.apache.lucene.analysis.Analyzer.TokenStreamComponents"/>
        ///         built from a <seealso cref="StandardTokenizer"/> filtered with
        ///         <seealso cref="StandardFilter"/>, <seealso cref="LowerCaseFilter"/>, <seealso cref="StopFilter"/>
        ///         , and <seealso cref="CzechStemFilter"/> (only if version is >= LUCENE_31). If
        ///         a version is >= LUCENE_31 and a stem exclusion set is provided via
        ///         <seealso cref="#CzechAnalyzer(Version, CharArraySet, CharArraySet)"/> a
        ///         <seealso cref="SetKeywordMarkerFilter"/> is added before
        ///         <seealso cref="CzechStemFilter"/>. </returns>

        public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
            Tokenizer source = new StandardTokenizer(matchVersion, reader);
            TokenStream result = new StandardFilter(matchVersion, source);
            result = new LowerCaseFilter(matchVersion, result);
            result = new StopFilter(matchVersion, result, stopwords);
#pragma warning disable 612, 618
            if (matchVersion.OnOrAfter(LuceneVersion.LUCENE_31))
#pragma warning restore 612, 618
            {
                if (this.stemExclusionTable.Any())
                {
                    result = new SetKeywordMarkerFilter(result, stemExclusionTable);
                }
                result = new CzechStemFilter(result);
            }
            return new TokenStreamComponents(source, result);
        }
    }
}