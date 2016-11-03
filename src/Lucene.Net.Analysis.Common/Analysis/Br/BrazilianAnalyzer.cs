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
    /// <seealso cref="Analyzer"/> for Brazilian Portuguese language. 
    /// <para>
    /// Supports an external list of stopwords (words that
    /// will not be indexed at all) and an external list of exclusions (words that will
    /// not be stemmed, but indexed).
    /// </para>
    /// 
    /// <para><b>NOTE</b>: This class uses the same <seealso cref="Version"/>
    /// dependent settings as <seealso cref="StandardAnalyzer"/>.</para>
    /// </summary>
    public sealed class BrazilianAnalyzer : StopwordAnalyzerBase
    {
        /// <summary>
        /// File containing default Brazilian Portuguese stopwords. </summary>
        public const string DEFAULT_STOPWORD_FILE = "stopwords.txt";

        /// <summary>
        /// Returns an unmodifiable instance of the default stop-words set. </summary>
        /// <returns> an unmodifiable instance of the default stop-words set. </returns>
        public static CharArraySet DefaultStopSet
        {
            get
            {
                return DefaultSetHolder.DEFAULT_STOP_SET;
            }
        }

        private class DefaultSetHolder
        {
            internal static readonly CharArraySet DEFAULT_STOP_SET;

            static DefaultSetHolder()
            {
                try
                {
                    var resource = typeof(BrazilianAnalyzer).GetAnalysisResourceName(DEFAULT_STOPWORD_FILE);
                    DEFAULT_STOP_SET = WordlistLoader.GetWordSet(
                        IOUtils.GetDecodingReader(typeof(BrazilianAnalyzer), resource, Encoding.UTF8),
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


        /// <summary>
        /// Contains words that should be indexed but not stemmed.
        /// </summary>
        private CharArraySet excltable = CharArraySet.EMPTY_SET;

        /// <summary>
        /// Builds an analyzer with the default stop words (<seealso cref="#getDefaultStopSet()"/>).
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
        public BrazilianAnalyzer(LuceneVersion matchVersion, CharArraySet stopwords, CharArraySet stemExclusionSet)
              : this(matchVersion, stopwords)
        {
            excltable = CharArraySet.UnmodifiableSet(CharArraySet.Copy(matchVersion, stemExclusionSet));
        }

        /// <summary>
        /// Creates
        /// <seealso cref="org.apache.lucene.analysis.Analyzer.TokenStreamComponents"/>
        /// used to tokenize all the text in the provided <seealso cref="Reader"/>.
        /// </summary>
        /// <returns> <seealso cref="org.apache.lucene.analysis.Analyzer.TokenStreamComponents"/>
        ///         built from a <seealso cref="StandardTokenizer"/> filtered with
        ///         <seealso cref="LowerCaseFilter"/>, <seealso cref="StandardFilter"/>, <seealso cref="StopFilter"/>
        ///         , and <seealso cref="BrazilianStemFilter"/>. </returns>
        public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
            Tokenizer source = new StandardTokenizer(matchVersion, reader);
            TokenStream result = new LowerCaseFilter(matchVersion, source);
            result = new StandardFilter(matchVersion, result);
            result = new StopFilter(matchVersion, result, stopwords);
            if (excltable != null && excltable.Count > 0)
            {
                result = new SetKeywordMarkerFilter(result, excltable);
            }
            return new TokenStreamComponents(source, new BrazilianStemFilter(result));
        }
    }
}