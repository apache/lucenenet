using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using System;
using System.IO;

namespace Lucene.Net.Analysis.Cjk
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
    /// An <seealso cref="Analyzer"/> that tokenizes text with <seealso cref="StandardTokenizer"/>,
    /// normalizes content with <seealso cref="CJKWidthFilter"/>, folds case with
    /// <seealso cref="LowerCaseFilter"/>, forms bigrams of CJK with <seealso cref="CJKBigramFilter"/>,
    /// and filters stopwords with <seealso cref="StopFilter"/>
    /// </summary>
    public sealed class CJKAnalyzer : StopwordAnalyzerBase
    {
        /// <summary>
        /// File containing default CJK stopwords.
        /// <p/>
        /// Currently it contains some common English words that are not usually
        /// useful for searching and some double-byte interpunctions.
        /// </summary>
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
                    var resource = typeof(CJKAnalyzer).GetAnalysisResourceName(DEFAULT_STOPWORD_FILE);
                    DEFAULT_STOP_SET = LoadStopwordSet(false, typeof(CJKAnalyzer), resource, "#");
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
        /// Builds an analyzer which removes words in <seealso cref="#getDefaultStopSet()"/>.
        /// </summary>
        public CJKAnalyzer(LuceneVersion matchVersion)
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
        public CJKAnalyzer(LuceneVersion matchVersion, CharArraySet stopwords)
              : base(matchVersion, stopwords)
        {
        }

        public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
#pragma warning disable 612, 618
            if (matchVersion.OnOrAfter(LuceneVersion.LUCENE_36))
#pragma warning restore 612, 618
            {
                Tokenizer source = new StandardTokenizer(matchVersion, reader);
                // run the widthfilter first before bigramming, it sometimes combines characters.
                TokenStream result = new CJKWidthFilter(source);
                result = new LowerCaseFilter(matchVersion, result);
                result = new CJKBigramFilter(result);
                return new TokenStreamComponents(source, new StopFilter(matchVersion, result, stopwords));
            }
            else
            {
#pragma warning disable 612, 618
                Tokenizer source = new CJKTokenizer(reader);
#pragma warning restore 612, 618
                return new TokenStreamComponents(source, new StopFilter(matchVersion, source, stopwords));
            }
        }
    }
}