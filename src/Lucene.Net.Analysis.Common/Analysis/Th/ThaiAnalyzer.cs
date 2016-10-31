using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using System;
using System.IO;

namespace Lucene.Net.Analysis.Th
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
    /// <seealso cref="Analyzer"/> for Thai language. It uses <seealso cref="java.text.BreakIterator"/> to break words.
    /// <para>
    /// <a name="version"/>
    /// </para>
    /// <para>You must specify the required <seealso cref="Version"/>
    /// compatibility when creating ThaiAnalyzer:
    /// <ul>
    ///   <li> As of 3.6, a set of Thai stopwords is used by default
    /// </ul>
    /// </para>
    /// </summary>
    public sealed class ThaiAnalyzer : StopwordAnalyzerBase
    {

        /// <summary>
        /// File containing default Thai stopwords. </summary>
        public const string DEFAULT_STOPWORD_FILE = "stopwords.txt";
        /// <summary>
        /// The comment character in the stopwords file.  
        /// All lines prefixed with this will be ignored.
        /// </summary>
        private const string STOPWORDS_COMMENT = "#";

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
                    var resource = GetAnalysisResourceName(typeof(ThaiAnalyzer), "Th", DEFAULT_STOPWORD_FILE);
                    DEFAULT_STOP_SET = LoadStopwordSet(false, typeof(ThaiAnalyzer), resource, STOPWORDS_COMMENT);
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
        /// Builds an analyzer with the default stop words.
        /// </summary>
        /// <param name="matchVersion"> lucene compatibility version </param>
        public ThaiAnalyzer(LuceneVersion matchVersion)
              : this(matchVersion,
#pragma warning disable 612, 618
                    matchVersion.OnOrAfter(LuceneVersion.LUCENE_36) ?
#pragma warning restore 612, 618
                    DefaultSetHolder.DEFAULT_STOP_SET : StopAnalyzer.ENGLISH_STOP_WORDS_SET)
        {
        }

        /// <summary>
        /// Builds an analyzer with the given stop words.
        /// </summary>
        /// <param name="matchVersion"> lucene compatibility version </param>
        /// <param name="stopwords"> a stopword set </param>
        public ThaiAnalyzer(LuceneVersion matchVersion, CharArraySet stopwords)
              : base(matchVersion, stopwords)
        {
        }

        /// <summary>
        /// Creates
        /// <seealso cref="org.apache.lucene.analysis.Analyzer.TokenStreamComponents"/>
        /// used to tokenize all the text in the provided <seealso cref="Reader"/>.
        /// </summary>
        /// <returns> <seealso cref="org.apache.lucene.analysis.Analyzer.TokenStreamComponents"/>
        ///         built from a <seealso cref="StandardTokenizer"/> filtered with
        ///         <seealso cref="StandardFilter"/>, <seealso cref="LowerCaseFilter"/>, <seealso cref="ThaiWordFilter"/>, and
        ///         <seealso cref="StopFilter"/> </returns>
        public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
            if (matchVersion.OnOrAfter(LuceneVersion.LUCENE_48))
            {
                Tokenizer source = new ThaiTokenizer(reader);
                TokenStream result = new LowerCaseFilter(matchVersion, source);
                result = new StopFilter(matchVersion, result, stopwords);
                return new TokenStreamComponents(source, result);
            }
            else
            {
                Tokenizer source = new StandardTokenizer(matchVersion, reader);
                TokenStream result = new StandardFilter(matchVersion, source);
#pragma warning disable 612, 618
                if (matchVersion.OnOrAfter(LuceneVersion.LUCENE_31))
#pragma warning restore 612, 618
                {
                    result = new LowerCaseFilter(matchVersion, result);
                }
#pragma warning disable 612, 618
                result = new ThaiWordFilter(matchVersion, result);
#pragma warning restore 612, 618
                return new TokenStreamComponents(source, new StopFilter(matchVersion, result, stopwords));
            }
        }
    }
}