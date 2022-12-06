// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using System;
using System.IO;

namespace Lucene.Net.Analysis.Ar
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
    /// <see cref="Analyzer"/> for Arabic. 
    /// <para/>
    /// This analyzer implements light-stemming as specified by:
    /// <c>
    /// Light Stemming for Arabic Information Retrieval
    /// </c>    
    /// http://www.mtholyoke.edu/~lballest/Pubs/arab_stem05.pdf
    /// <para/>
    /// The analysis package contains three primary components:
    /// <list type="bullet">
    ///     <item><description><see cref="ArabicNormalizationFilter"/>: Arabic orthographic normalization.</description></item>
    ///     <item><description><see cref="ArabicStemFilter"/>: Arabic light stemming</description></item>
    ///     <item><description>Arabic stop words file: a set of default Arabic stop words.</description></item>
    /// </list>
    /// </summary>
    public sealed class ArabicAnalyzer : StopwordAnalyzerBase
    {

        /// <summary>
        /// File containing default Arabic stopwords.
        /// 
        /// Default stopword list is from http://members.unine.ch/jacques.savoy/clef/index.html
        /// The stopword list is BSD-Licensed.
        /// </summary>
        public const string DEFAULT_STOPWORD_FILE = "stopwords.txt";

        /// <summary>
        /// Returns an unmodifiable instance of the default stop-words set. </summary>
        /// <returns> an unmodifiable instance of the default stop-words set. </returns>
        public static CharArraySet DefaultStopSet => DefaultSetHolder.DEFAULT_STOP_SET;

        /// <summary>
        /// Atomically loads the DEFAULT_STOP_SET in a lazy fashion once the outer class 
        /// accesses the static final set the first time.;
        /// </summary>
        private static class DefaultSetHolder
        {
            internal static readonly CharArraySet DEFAULT_STOP_SET = LoadDefaultStopSet();

            private static CharArraySet LoadDefaultStopSet() // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
            {
                try
                {
                    return LoadStopwordSet(false, typeof(ArabicAnalyzer), DEFAULT_STOPWORD_FILE, "#").AsReadOnly(); // LUCENENET: Made readonly as stated in the docs: https://github.com/apache/lucene/issues/11866
                }
                catch (Exception ex) when (ex.IsIOException())
                {
                    // default set should always be present as it is part of the
                    // distribution (JAR)
                    throw RuntimeException.Create("Unable to load default stopword set", ex);
                }
            }
        }

        private readonly CharArraySet stemExclusionSet;

        /// <summary>
        /// Builds an analyzer with the default stop words: <see cref="DEFAULT_STOPWORD_FILE"/>.
        /// </summary>
        public ArabicAnalyzer(LuceneVersion matchVersion)
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
        public ArabicAnalyzer(LuceneVersion matchVersion, CharArraySet stopwords)
              : this(matchVersion, stopwords, CharArraySet.Empty)
        {
        }

        /// <summary>
        /// Builds an analyzer with the given stop word. If a none-empty stem exclusion set is
        /// provided this analyzer will add a <see cref="SetKeywordMarkerFilter"/> before
        /// <see cref="ArabicStemFilter"/>.
        /// </summary>
        /// <param name="matchVersion">
        ///          lucene compatibility version </param>
        /// <param name="stopwords">
        ///          a stopword set </param>
        /// <param name="stemExclusionSet">
        ///          a set of terms not to be stemmed </param>
        public ArabicAnalyzer(LuceneVersion matchVersion, CharArraySet stopwords, CharArraySet stemExclusionSet)
              : base(matchVersion, stopwords)
        {
            this.stemExclusionSet = CharArraySet.Copy(matchVersion, stemExclusionSet).AsReadOnly();
        }

        /// <summary>
        /// Creates <see cref="TokenStreamComponents"/>
        /// used to tokenize all the text in the provided <see cref="TextReader"/>.
        /// </summary>
        /// <returns> <see cref="TokenStreamComponents"/>
        ///         built from an <see cref="StandardTokenizer"/> filtered with
        ///         <see cref="LowerCaseFilter"/>, <see cref="StopFilter"/>,
        ///         <see cref="ArabicNormalizationFilter"/>, <see cref="SetKeywordMarkerFilter"/>
        ///         if a stem exclusion set is provided and <see cref="ArabicStemFilter"/>. </returns>
        protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
#pragma warning disable 612, 618
            Tokenizer source = m_matchVersion.OnOrAfter(LuceneVersion.LUCENE_31) 
                ? new StandardTokenizer(m_matchVersion, reader) 
                : (Tokenizer)new ArabicLetterTokenizer(m_matchVersion, reader);
#pragma warning restore 612, 618
            TokenStream result = new LowerCaseFilter(m_matchVersion, source);
            // the order here is important: the stopword list is not normalized!
            result = new StopFilter(m_matchVersion, result, m_stopwords);
            // TODO maybe we should make ArabicNormalization filter also KeywordAttribute aware?!
            result = new ArabicNormalizationFilter(result);
            if (stemExclusionSet.Count > 0)
            {
                result = new SetKeywordMarkerFilter(result, stemExclusionSet);
            }
            return new TokenStreamComponents(source, new ArabicStemFilter(result));
        }
    }
}