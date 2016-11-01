using Egothor.Stemmer;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Stempel;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Lucene.Net.Analysis.Pl
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
    /// <see cref="Analyzer"/> for Polish.
    /// </summary>
    public sealed class PolishAnalyzer : StopwordAnalyzerBase
    {
        private readonly CharArraySet stemExclusionSet;
        private readonly Trie stemTable;

        /// <summary>
        /// File containing default Polish stopwords.
        /// </summary>
        public readonly static string DEFAULT_STOPWORD_FILE = "stopwords.txt";

        /// <summary>
        /// File containing default Polish stemmer table.
        /// </summary>
        public readonly static string DEFAULT_STEMMER_FILE = "stemmer_20000.tbl";

        /// <summary>
        /// Returns an unmodifiable instance of the default stop words set.
        /// </summary>
        /// <returns>default stop words set.</returns>
        public static CharArraySet GetDefaultStopSet()
        {
            return DefaultsHolder.DEFAULT_STOP_SET;
        }

        /// <summary>
        /// Returns an unmodifiable instance of the default stemmer table.
        /// </summary>
        public static Trie GetDefaultTable()
        {
            return DefaultsHolder.DEFAULT_TABLE;
        }

        /// <summary>
        /// Atomically loads the <see cref="DEFAULT_STOP_SET"/> in a lazy fashion once the outer class 
        /// accesses the static final set the first time.;
        /// </summary>
        private class DefaultsHolder
        {
            internal static readonly CharArraySet DEFAULT_STOP_SET;
            internal static readonly Trie DEFAULT_TABLE;

            static DefaultsHolder()
            {
                try
                {
                    DEFAULT_STOP_SET = WordlistLoader.GetWordSet(IOUtils.GetDecodingReader(typeof(PolishAnalyzer),
                        typeof(PolishAnalyzer).Namespace + "." + DEFAULT_STOPWORD_FILE, Encoding.UTF8), "#",
#pragma warning disable 612, 618
                        LuceneVersion.LUCENE_CURRENT);
#pragma warning restore 612, 618
                }
                catch (IOException ex)
                {
                    // default set should always be present as it is part of the
                    // distribution (embedded resource)
                    throw new SystemException("Unable to load default stopword set", ex);
                }

                try
                {
                    DEFAULT_TABLE = StempelStemmer.Load(typeof(PolishAnalyzer).Assembly.GetManifestResourceStream(
                        typeof(PolishAnalyzer).Namespace + "." + DEFAULT_STEMMER_FILE));
                }
                catch (IOException ex)
                {
                    // default set should always be present as it is part of the
                    // distribution (embedded resource)
                    throw new SystemException("Unable to load default stemming tables", ex);
                }
            }
        }

        /// <summary>
        /// Builds an analyzer with the default stop words: <see cref="DEFAULT_STOPWORD_FILE"/>.
        /// </summary>
        /// <param name="matchVersion">lucene compatibility version</param>
        public PolishAnalyzer(LuceneVersion matchVersion)
            : this(matchVersion, DefaultsHolder.DEFAULT_STOP_SET)
        {
        }

        /// <summary>
        /// Builds an analyzer with the given stop words.
        /// </summary>
        /// <param name="matchVersion">lucene compatibility version</param>
        /// <param name="stopwords">a stopword set</param>
        public PolishAnalyzer(LuceneVersion matchVersion, CharArraySet stopwords)
            : this(matchVersion, stopwords, CharArraySet.EMPTY_SET)
        {
        }

        /// <summary>
        /// Builds an analyzer with the given stop words. If a non-empty stem exclusion set is
        /// provided this analyzer will add a <see cref="SetKeywordMarkerFilter"/> before
        /// stemming.
        /// </summary>
        /// <param name="matchVersion">lucene compatibility version</param>
        /// <param name="stopwords">a stopword set</param>
        /// <param name="stemExclusionSet">a set of terms not to be stemmed</param>
        public PolishAnalyzer(LuceneVersion matchVersion, CharArraySet stopwords, CharArraySet stemExclusionSet)
            : base(matchVersion, stopwords)
        {
            this.stemTable = DefaultsHolder.DEFAULT_TABLE;
            this.stemExclusionSet = CharArraySet.UnmodifiableSet(CharArraySet.Copy(
                matchVersion, stemExclusionSet));
        }

        /// <summary>
        /// Creates a <see cref="Analyzer.TokenStreamComponents"/>
        /// which tokenizes all the text in the provided <see cref="TextReader"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="Analyzer.TokenStreamComponents"/> built from an <see cref="StandardTokenizer"/>
        /// filtered with <see cref="StandardFilter"/>, <see cref="LowerCaseFilter"/>, <see cref="StopFilter"/>, 
        /// <see cref="SetKeywordMarkerFilter"/> if a stem excusion set is provided and <see cref="StempelFilter"/>.
        /// </returns>
        public override TokenStreamComponents CreateComponents(string fieldName,
            TextReader reader)
        {
            Tokenizer source = new StandardTokenizer(matchVersion, reader);
            TokenStream result = new StandardFilter(matchVersion, source);
            result = new LowerCaseFilter(matchVersion, result);
            result = new StopFilter(matchVersion, result, stopwords);
            if (stemExclusionSet.Any())
                result = new SetKeywordMarkerFilter(result, stemExclusionSet);
            result = new StempelFilter(result, new StempelStemmer(stemTable));
            return new TokenStreamComponents(source, result);
        }
    }
}
