﻿// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Snowball;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Tartarus.Snowball.Ext;
using Lucene.Net.Util;
using System;
using System.IO;

namespace Lucene.Net.Analysis.Tr
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
    /// <see cref="Analyzer"/> for Turkish.
    /// </summary>
    public sealed class TurkishAnalyzer : StopwordAnalyzerBase
    {
        private readonly CharArraySet stemExclusionSet;

        /// <summary>
        /// File containing default Turkish stopwords. </summary>
        public const string DEFAULT_STOPWORD_FILE = "stopwords.txt";
        /// <summary>
        /// The comment character in the stopwords file.  
        /// All lines prefixed with this will be ignored.
        /// </summary>
        private const string STOPWORDS_COMMENT = "#";

        /// <summary>
        /// Returns an unmodifiable instance of the default stop words set. </summary>
        /// <returns> default stop words set. </returns>
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
                    return LoadStopwordSet(false, typeof(TurkishAnalyzer), DEFAULT_STOPWORD_FILE, STOPWORDS_COMMENT).AsReadOnly(); // LUCENENET: Made readonly as stated in the docs: https://github.com/apache/lucene/issues/11866
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
        /// Builds an analyzer with the default stop words: <see cref="DEFAULT_STOPWORD_FILE"/>.
        /// </summary>
        public TurkishAnalyzer(LuceneVersion matchVersion)
            : this(matchVersion, DefaultSetHolder.DEFAULT_STOP_SET)
        {
        }

        /// <summary>
        /// Builds an analyzer with the given stop words.
        /// </summary>
        /// <param name="matchVersion"> lucene compatibility version </param>
        /// <param name="stopwords"> a stopword set </param>
        public TurkishAnalyzer(LuceneVersion matchVersion, CharArraySet stopwords)
            : this(matchVersion, stopwords, CharArraySet.Empty)
        {
        }

        /// <summary>
        /// Builds an analyzer with the given stop words. If a non-empty stem exclusion set is
        /// provided this analyzer will add a <see cref="SetKeywordMarkerFilter"/> before
        /// stemming.
        /// </summary>
        /// <param name="matchVersion"> lucene compatibility version </param>
        /// <param name="stopwords"> a stopword set </param>
        /// <param name="stemExclusionSet"> a set of terms not to be stemmed </param>
        public TurkishAnalyzer(LuceneVersion matchVersion, CharArraySet stopwords, CharArraySet stemExclusionSet) 
            : base(matchVersion, stopwords)
        {
            this.stemExclusionSet = CharArraySet.Copy(matchVersion, stemExclusionSet).AsReadOnly();
        }

        /// <summary>
        /// Creates a
        /// <see cref="TokenStreamComponents"/>
        /// which tokenizes all the text in the provided <see cref="TextReader"/>.
        /// </summary>
        /// <returns> A
        ///         <see cref="TokenStreamComponents"/>
        ///         built from an <see cref="StandardTokenizer"/> filtered with
        ///         <see cref="StandardFilter"/>, <see cref="TurkishLowerCaseFilter"/>,
        ///         <see cref="StopFilter"/>, <see cref="SetKeywordMarkerFilter"/> if a stem
        ///         exclusion set is provided and <see cref="SnowballFilter"/>. </returns>
        protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
            Tokenizer source = new StandardTokenizer(m_matchVersion, reader);
            TokenStream result = new StandardFilter(m_matchVersion, source);
            if (m_matchVersion.OnOrAfter(LuceneVersion.LUCENE_48))
            {
                result = new ApostropheFilter(result);
            }
            result = new TurkishLowerCaseFilter(result);
            result = new StopFilter(m_matchVersion, result, m_stopwords);
            if (stemExclusionSet.Count > 0)
            {
                result = new SetKeywordMarkerFilter(result, stemExclusionSet);
            }
            result = new SnowballFilter(result, new TurkishStemmer());
            return new TokenStreamComponents(source, result);
        }
    }
}