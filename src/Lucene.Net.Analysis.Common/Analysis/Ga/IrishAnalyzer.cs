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

namespace Lucene.Net.Analysis.Ga
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
    /// <seealso cref="Analyzer"/> for Irish.
    /// </summary>
    public sealed class IrishAnalyzer : StopwordAnalyzerBase
    {
        private readonly CharArraySet stemExclusionSet;

        /// <summary>
        /// File containing default Irish stopwords. </summary>
        public const string DEFAULT_STOPWORD_FILE = "stopwords.txt";

        private static readonly CharArraySet DEFAULT_ARTICLES = CharArraySet.UnmodifiableSet(new CharArraySet(
#pragma warning disable 612, 618
            LuceneVersion.LUCENE_CURRENT,
#pragma warning restore 612, 618
            Arrays.AsList("d", "m", "b"), true));

        /// <summary>
        /// When StandardTokenizer splits t‑athair into {t, athair}, we don't
        /// want to cause a position increment, otherwise there will be problems
        /// with phrase queries versus tAthair (which would not have a gap).
        /// </summary>
        private static readonly CharArraySet HYPHENATIONS = CharArraySet.UnmodifiableSet(new CharArraySet(
#pragma warning disable 612, 618
            LuceneVersion.LUCENE_CURRENT,
#pragma warning restore 612, 618
            Arrays.AsList("h", "n", "t"), true));

        /// <summary>
        /// Returns an unmodifiable instance of the default stop words set. </summary>
        /// <returns> default stop words set. </returns>
        public static CharArraySet DefaultStopSet
        {
            get
            {
                return DefaultSetHolder.DEFAULT_STOP_SET;
            }
        }

        /// <summary>
        /// Atomically loads the DEFAULT_STOP_SET in a lazy fashion once the outer class 
        /// accesses the static final set the first time.;
        /// </summary>
        private class DefaultSetHolder
        {
            internal static readonly CharArraySet DEFAULT_STOP_SET;

            static DefaultSetHolder()
            {
                try
                {
                    var resource = typeof(IrishAnalyzer).GetAnalysisResourceName(DEFAULT_STOPWORD_FILE);
                    DEFAULT_STOP_SET = LoadStopwordSet(false, typeof(IrishAnalyzer), resource, "#");
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
        /// Builds an analyzer with the default stop words: <seealso cref="#DEFAULT_STOPWORD_FILE"/>.
        /// </summary>
        public IrishAnalyzer(LuceneVersion matchVersion)
              : this(matchVersion, DefaultSetHolder.DEFAULT_STOP_SET)
        {
        }

        /// <summary>
        /// Builds an analyzer with the given stop words.
        /// </summary>
        /// <param name="matchVersion"> lucene compatibility version </param>
        /// <param name="stopwords"> a stopword set </param>
        public IrishAnalyzer(LuceneVersion matchVersion, CharArraySet stopwords)
              : this(matchVersion, stopwords, CharArraySet.EMPTY_SET)
        {
        }

        /// <summary>
        /// Builds an analyzer with the given stop words. If a non-empty stem exclusion set is
        /// provided this analyzer will add a <seealso cref="SetKeywordMarkerFilter"/> before
        /// stemming.
        /// </summary>
        /// <param name="matchVersion"> lucene compatibility version </param>
        /// <param name="stopwords"> a stopword set </param>
        /// <param name="stemExclusionSet"> a set of terms not to be stemmed </param>
        public IrishAnalyzer(LuceneVersion matchVersion, CharArraySet stopwords, CharArraySet stemExclusionSet)
              : base(matchVersion, stopwords)
        {
            this.stemExclusionSet = CharArraySet.UnmodifiableSet(CharArraySet.Copy(matchVersion, stemExclusionSet));
        }

        /// <summary>
        /// Creates a
        /// <seealso cref="org.apache.lucene.analysis.Analyzer.TokenStreamComponents"/>
        /// which tokenizes all the text in the provided <seealso cref="Reader"/>.
        /// </summary>
        /// <returns> A
        ///         <seealso cref="org.apache.lucene.analysis.Analyzer.TokenStreamComponents"/>
        ///         built from an <seealso cref="StandardTokenizer"/> filtered with
        ///         <seealso cref="StandardFilter"/>, <seealso cref="IrishLowerCaseFilter"/>, <seealso cref="StopFilter"/>
        ///         , <seealso cref="SetKeywordMarkerFilter"/> if a stem exclusion set is
        ///         provided and <seealso cref="SnowballFilter"/>. </returns>
        public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
            Tokenizer source = new StandardTokenizer(matchVersion, reader);
            TokenStream result = new StandardFilter(matchVersion, source);
            StopFilter s = new StopFilter(matchVersion, result, HYPHENATIONS);
#pragma warning disable 612, 618
            if (!matchVersion.OnOrAfter(LuceneVersion.LUCENE_44))
#pragma warning restore 612, 618
            {
                s.EnablePositionIncrements = false;
            }
            result = s;
            result = new ElisionFilter(result, DEFAULT_ARTICLES);
            result = new IrishLowerCaseFilter(result);
            result = new StopFilter(matchVersion, result, stopwords);
            if (stemExclusionSet.Count > 0)
            {
                result = new SetKeywordMarkerFilter(result, stemExclusionSet);
            }
            result = new SnowballFilter(result, new IrishStemmer());
            return new TokenStreamComponents(source, result);
        }
    }
}