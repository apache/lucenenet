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

using System;
using System.Linq;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Standard;
using System.IO;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Analysis.BR
{
    /// <summary>
    /// Analyzer for Brazilian language. Supports an external list of stopwords (words that
    /// will not be indexed at all) and an external list of exclusions (word that will
    /// not be stemmed, but indexed).
    /// </summary>
    public sealed class BrazilianAnalyzer : StopwordAnalyzerBase
    {
        public readonly static String DEFAULT_STOPWORD_FILE = "BrazilianStopWords.txt";

        public static CharArraySet DefaultStopSet
        {
            get { return DefaultSetHolder.DEFAULT_STOP_SET; }
        }

        private static class DefaultSetHolder
        {
            internal static readonly CharArraySet DEFAULT_STOP_SET;

            static DefaultSetHolder()
            {
                try
                {
                    DEFAULT_STOP_SET = WordlistLoader.GetWordSet(IOUtils.GetDecodingReader(typeof(BrazilianAnalyzer),
                        DEFAULT_STOPWORD_FILE, IOUtils.CHARSET_UTF_8), "#", Version.LUCENE_CURRENT);
                }
                catch (IOException ex)
                {
                    // default set should always be present as it is part of the
                    // assembly
                    throw new Exception("Unable to load default stopword set");
                }
            }
        }

        private readonly CharArraySet _stemExclusionSet = CharArraySet.EMPTY_SET;

        /**
         * Builds an analyzer with the default stop words ({@link #getDefaultStopSet()}).
         */
        public BrazilianAnalyzer(Version matchVersion) : this(matchVersion, DefaultSetHolder.DEFAULT_STOP_SET) { }

        /**
         * Builds an analyzer with the given stop words
         * 
         * @param matchVersion
         *          lucene compatibility version
         * @param stopwords
         *          a stopword set
         */
        public BrazilianAnalyzer(Version matchVersion, CharArraySet stopwords) : base(matchVersion, stopwords) { }

        /**
         * Builds an analyzer with the given stop words and stemming exclusion words
         * 
         * @param matchVersion
         *          lucene compatibility version
         * @param stopwords
         *          a stopword set
         */
        public BrazilianAnalyzer(Version matchVersion, CharArraySet stopwords, CharArraySet stemExclusionSet)
            : this(matchVersion, stopwords)
        {
            _stemExclusionSet = CharArraySet.UnmodifiableSet(CharArraySet
                .Copy(matchVersion, stemExclusionSet));
        }

        /**
         * Creates
         * {@link org.apache.lucene.analysis.Analyzer.TokenStreamComponents}
         * used to tokenize all the text in the provided {@link Reader}.
         * 
         * @return {@link org.apache.lucene.analysis.Analyzer.TokenStreamComponents}
         *         built from a {@link StandardTokenizer} filtered with
         *         {@link LowerCaseFilter}, {@link StandardFilter}, {@link StopFilter}
         *         , and {@link BrazilianStemFilter}.
         */
        public override TokenStreamComponents CreateComponents(String fieldName,
            TextReader reader)
        {
            Tokenizer source = new StandardTokenizer(matchVersion, reader);
            TokenStream result = new LowerCaseFilter(matchVersion, source);
            result = new StandardFilter(matchVersion, result);
            result = new StopFilter(matchVersion, result, stopwords);
            if (_stemExclusionSet != null && _stemExclusionSet.Any())
                result = new SetKeywordMarkerFilter(result, _stemExclusionSet);
            return new TokenStreamComponents(source, new BrazilianStemFilter(result));
        }
    }
}
