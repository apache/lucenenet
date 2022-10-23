// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.In;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using System;
using System.IO;

namespace Lucene.Net.Analysis.Hi
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
    /// Analyzer for Hindi.
    /// <para>You must specify the required <see cref="LuceneVersion"/>
    /// compatibility when creating HindiAnalyzer:
    /// <list type="bullet">
    ///     <item><description> As of 3.6, StandardTokenizer is used for tokenization</description></item>
    /// </list>
    /// </para>
    /// </summary>
    public sealed class HindiAnalyzer : StopwordAnalyzerBase
    {
        private readonly CharArraySet stemExclusionSet;

        /// <summary>
        /// File containing default Hindi stopwords.
        /// <para/>
        /// Default stopword list is from http://members.unine.ch/jacques.savoy/clef/index.html
        /// The stopword list is BSD-Licensed.
        /// </summary>
        public const string DEFAULT_STOPWORD_FILE = "stopwords.txt";
        private const string STOPWORDS_COMMENT = "#";

        /// <summary>
        /// Returns an unmodifiable instance of the default stop-words set. </summary>
        /// <returns> an unmodifiable instance of the default stop-words set. </returns>
        public static CharArraySet DefaultStopSet => DefaultSetHolder.DEFAULT_STOP_SET;

        /// <summary>
        /// Atomically loads the <see cref="DEFAULT_STOP_SET"/> in a lazy fashion once the outer class 
        /// accesses the static final set the first time.;
        /// </summary>
        private static class DefaultSetHolder
        {
            internal static readonly CharArraySet DEFAULT_STOP_SET = LoadDefaultStopSet();

            private static CharArraySet LoadDefaultStopSet() // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
            {
                try
                {
                    return LoadStopwordSet(false, typeof(HindiAnalyzer), DEFAULT_STOPWORD_FILE, STOPWORDS_COMMENT).AsReadOnly(); // LUCENENET: Made readonly as stated in the docs: https://github.com/apache/lucene/issues/11866
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
        /// Builds an analyzer with the given stop words
        /// </summary>
        /// <param name="version"> lucene compatibility version </param>
        /// <param name="stopwords"> a stopword set </param>
        /// <param name="stemExclusionSet"> a stemming exclusion set </param>
        public HindiAnalyzer(LuceneVersion version, CharArraySet stopwords, CharArraySet stemExclusionSet)
            : base(version, stopwords)
        {
            this.stemExclusionSet = CharArraySet.Copy(m_matchVersion, stemExclusionSet).AsReadOnly();
        }

        /// <summary>
        /// Builds an analyzer with the given stop words 
        /// </summary>
        /// <param name="version"> lucene compatibility version </param>
        /// <param name="stopwords"> a stopword set </param>
        public HindiAnalyzer(LuceneVersion version, CharArraySet stopwords)
            : this(version, stopwords, CharArraySet.Empty)
        {
        }

        /// <summary>
        /// Builds an analyzer with the default stop words:
        /// <see cref="DEFAULT_STOPWORD_FILE"/>.
        /// </summary>
        public HindiAnalyzer(LuceneVersion version)
            : this(version, DefaultSetHolder.DEFAULT_STOP_SET)
        {
        }

        /// <summary>
        /// Creates
        /// <see cref="TokenStreamComponents"/>
        /// used to tokenize all the text in the provided <see cref="TextReader"/>.
        /// </summary>
        /// <returns> <see cref="TokenStreamComponents"/>
        ///         built from a <see cref="StandardTokenizer"/> filtered with
        ///         <see cref="LowerCaseFilter"/>, <see cref="IndicNormalizationFilter"/>,
        ///         <see cref="HindiNormalizationFilter"/>, <see cref="SetKeywordMarkerFilter"/>
        ///         if a stem exclusion set is provided, <see cref="HindiStemFilter"/>, and
        ///         Hindi Stop words </returns>
        protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
            Tokenizer source;
#pragma warning disable 612, 618
            if (m_matchVersion.OnOrAfter(LuceneVersion.LUCENE_36))
            {
                source = new StandardTokenizer(m_matchVersion, reader);
            }
            else
            {
                source = new IndicTokenizer(m_matchVersion, reader);
            }
#pragma warning restore 612, 618
            TokenStream result = new LowerCaseFilter(m_matchVersion, source);
            if (stemExclusionSet.Count > 0)
            {
                result = new SetKeywordMarkerFilter(result, stemExclusionSet);
            }
            result = new IndicNormalizationFilter(result);
            result = new HindiNormalizationFilter(result);
            result = new StopFilter(m_matchVersion, result, m_stopwords);
            result = new HindiStemFilter(result);
            return new TokenStreamComponents(source, result);
        }
    }
}