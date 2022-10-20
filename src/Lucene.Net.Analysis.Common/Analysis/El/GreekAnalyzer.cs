// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using System;
using System.IO;

namespace Lucene.Net.Analysis.El
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
    /// <see cref="Analyzer"/> for the Greek language. 
    /// <para>
    /// Supports an external list of stopwords (words
    /// that will not be indexed at all).
    /// A default set of stopwords is used unless an alternative list is specified.
    /// </para>
    /// <para>You must specify the required <see cref="LuceneVersion"/>
    /// compatibility when creating <see cref="GreekAnalyzer"/>:
    /// <list type="bullet">
    ///   <item><description> As of 3.1, StandardFilter and GreekStemmer are used by default.</description></item>
    ///   <item><description> As of 2.9, StopFilter preserves position
    ///        increments</description></item>
    /// </list>
    /// </para>
    /// <para><c>NOTE</c>: This class uses the same <see cref="LuceneVersion"/>
    /// dependent settings as <see cref="StandardAnalyzer"/>.</para>
    /// </summary>
    public sealed class GreekAnalyzer : StopwordAnalyzerBase
    {
        /// <summary>
        /// File containing default Greek stopwords. </summary>
        public const string DEFAULT_STOPWORD_FILE = "stopwords.txt";

        /// <summary>
        /// Returns a set of default Greek-stopwords </summary>
        /// <returns> a set of default Greek-stopwords  </returns>
        public static CharArraySet DefaultStopSet => DefaultSetHolder.DEFAULT_SET;

        private static class DefaultSetHolder
        {
            internal static readonly CharArraySet DEFAULT_SET = LoadDefaultSet();

            private static CharArraySet LoadDefaultSet() // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
            {
                try
                {
                    return LoadStopwordSet(false, typeof(GreekAnalyzer), DEFAULT_STOPWORD_FILE, "#").AsReadOnly(); // LUCENENET: Made readonly as stated in the docs: https://github.com/apache/lucene/issues/11866
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
        /// Builds an analyzer with the default stop words. </summary>
        /// <param name="matchVersion"> Lucene compatibility version,
        ///   See <see cref="LuceneVersion"/> </param>
        public GreekAnalyzer(LuceneVersion matchVersion)
              : this(matchVersion, DefaultSetHolder.DEFAULT_SET)
        {
        }

        /// <summary>
        /// Builds an analyzer with the given stop words. 
        /// <para>
        /// <b>NOTE:</b> The stopwords set should be pre-processed with the logic of 
        /// <see cref="GreekLowerCaseFilter"/> for best results.
        ///  
        /// </para>
        /// </summary>
        /// <param name="matchVersion"> Lucene compatibility version,
        ///   See <see cref="LuceneVersion"/> </param>
        /// <param name="stopwords"> a stopword set </param>
        public GreekAnalyzer(LuceneVersion matchVersion, CharArraySet stopwords)
              : base(matchVersion, stopwords)
        {
        }

        /// <summary>
        /// Creates
        /// <see cref="TokenStreamComponents"/>
        /// used to tokenize all the text in the provided <see cref="TextReader"/>.
        /// </summary>
        /// <returns> <see cref="TokenStreamComponents"/>
        ///         built from a <see cref="StandardTokenizer"/> filtered with
        ///         <see cref="GreekLowerCaseFilter"/>, <see cref="StandardFilter"/>,
        ///         <see cref="StopFilter"/>, and <see cref="GreekStemFilter"/> </returns>
        protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
            Tokenizer source = new StandardTokenizer(m_matchVersion, reader);
            TokenStream result = new GreekLowerCaseFilter(m_matchVersion, source);
#pragma warning disable 612, 618
            if (m_matchVersion.OnOrAfter(LuceneVersion.LUCENE_31))
            {
                result = new StandardFilter(m_matchVersion, result);
            }
            result = new StopFilter(m_matchVersion, result, m_stopwords);
            if (m_matchVersion.OnOrAfter(LuceneVersion.LUCENE_31))
#pragma warning restore 612, 618
            {
                result = new GreekStemFilter(result);
            }
            return new TokenStreamComponents(source, result);
        }
    }
}