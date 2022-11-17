// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using System;
using System.IO;
using System.Text;

namespace Lucene.Net.Analysis.Br
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
    /// <see cref="Analyzer"/> for Brazilian Portuguese language. 
    /// <para>
    /// Supports an external list of stopwords (words that
    /// will not be indexed at all) and an external list of exclusions (words that will
    /// not be stemmed, but indexed).
    /// </para>
    /// 
    /// <para><b>NOTE</b>: This class uses the same <see cref="LuceneVersion"/>
    /// dependent settings as <see cref="StandardAnalyzer"/>.</para>
    /// </summary>
    public sealed class BrazilianAnalyzer : StopwordAnalyzerBase
    {
        /// <summary>
        /// File containing default Brazilian Portuguese stopwords. </summary>
        public const string DEFAULT_STOPWORD_FILE = "stopwords.txt";

        /// <summary>
        /// Returns an unmodifiable instance of the default stop-words set. </summary>
        /// <returns> an unmodifiable instance of the default stop-words set. </returns>
        public static CharArraySet DefaultStopSet => DefaultSetHolder.DEFAULT_STOP_SET;

        private static class DefaultSetHolder
        {
            internal static readonly CharArraySet DEFAULT_STOP_SET = LoadDefaultStopSet();

            private static CharArraySet LoadDefaultStopSet() // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
            {
                try
                {
                    return WordlistLoader.GetWordSet(
                        IOUtils.GetDecodingReader(typeof(BrazilianAnalyzer), DEFAULT_STOPWORD_FILE, Encoding.UTF8),
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


        /// <summary>
        /// Contains words that should be indexed but not stemmed.
        /// </summary>
        private CharArraySet excltable = CharArraySet.Empty;

        /// <summary>
        /// Builds an analyzer with the default stop words (<see cref="DefaultStopSet"/>).
        /// </summary>
        public BrazilianAnalyzer(LuceneVersion matchVersion)
              : this(matchVersion, DefaultSetHolder.DEFAULT_STOP_SET)
        {
        }

        /// <summary>
        /// Builds an analyzer with the given stop words
        /// </summary>
        /// <param name="matchVersion">
        ///          lucene compatibility version </param>
        /// <param name="stopwords">
        ///          a stopword set </param>
        public BrazilianAnalyzer(LuceneVersion matchVersion, CharArraySet stopwords)
              : base(matchVersion, stopwords)
        {
        }

        /// <summary>
        /// Builds an analyzer with the given stop words and stemming exclusion words
        /// </summary>
        /// <param name="matchVersion">
        ///          lucene compatibility version </param>
        /// <param name="stopwords">
        ///          a stopword set </param>
        /// <param name="stemExclusionSet"> a set of terms not to be stemmed </param>
        public BrazilianAnalyzer(LuceneVersion matchVersion, CharArraySet stopwords, CharArraySet stemExclusionSet)
              : this(matchVersion, stopwords)
        {
            excltable = CharArraySet.Copy(matchVersion, stemExclusionSet).AsReadOnly();
        }

        /// <summary>
        /// Creates
        /// <see cref="TokenStreamComponents"/>
        /// used to tokenize all the text in the provided <see cref="TextReader"/>.
        /// </summary>
        /// <returns> <see cref="TokenStreamComponents"/>
        ///         built from a <see cref="StandardTokenizer"/> filtered with
        ///         <see cref="LowerCaseFilter"/>, <see cref="StandardFilter"/>, <see cref="StopFilter"/>,
        ///         and <see cref="BrazilianStemFilter"/>. </returns>
        protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
            Tokenizer source = new StandardTokenizer(m_matchVersion, reader);
            TokenStream result = new LowerCaseFilter(m_matchVersion, source);
            result = new StandardFilter(m_matchVersion, result);
            result = new StopFilter(m_matchVersion, result, m_stopwords);
            if (excltable != null && excltable.Count > 0)
            {
                result = new SetKeywordMarkerFilter(result, excltable);
            }
            return new TokenStreamComponents(source, new BrazilianStemFilter(result));
        }
    }
}