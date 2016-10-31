using Lucene.Net.Analysis.Ar;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using System;
using System.IO;

namespace Lucene.Net.Analysis.Fa
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
    /// <seealso cref="Analyzer"/> for Persian.
    /// <para>
    /// This Analyzer uses <seealso cref="PersianCharFilter"/> which implies tokenizing around
    /// zero-width non-joiner in addition to whitespace. Some persian-specific variant forms (such as farsi
    /// yeh and keheh) are standardized. "Stemming" is accomplished via stopwords.
    /// </para>
    /// </summary>
    public sealed class PersianAnalyzer : StopwordAnalyzerBase
    {

        /// <summary>
        /// File containing default Persian stopwords.
        /// 
        /// Default stopword list is from
        /// http://members.unine.ch/jacques.savoy/clef/index.html The stopword list is
        /// BSD-Licensed.
        /// 
        /// </summary>
        public const string DEFAULT_STOPWORD_FILE = "stopwords.txt";

        /// <summary>
        /// The comment character in the stopwords file. All lines prefixed with this
        /// will be ignored
        /// </summary>
        public const string STOPWORDS_COMMENT = "#";

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
                    var resource = GetAnalysisResourceName(typeof(PersianAnalyzer), "Fa", DEFAULT_STOPWORD_FILE);
                    DEFAULT_STOP_SET = LoadStopwordSet(false, typeof(PersianAnalyzer), resource, STOPWORDS_COMMENT);
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
        /// Builds an analyzer with the default stop words:
        /// <seealso cref="#DEFAULT_STOPWORD_FILE"/>.
        /// </summary>
        public PersianAnalyzer(LuceneVersion matchVersion)
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
        public PersianAnalyzer(LuceneVersion matchVersion, CharArraySet stopwords)
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
        ///         <seealso cref="LowerCaseFilter"/>, <seealso cref="ArabicNormalizationFilter"/>,
        ///         <seealso cref="PersianNormalizationFilter"/> and Persian Stop words </returns>
        public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
            Tokenizer source;
#pragma warning disable 612, 618
            if (matchVersion.OnOrAfter(LuceneVersion.LUCENE_31))
#pragma warning restore 612, 618
            {
                source = new StandardTokenizer(matchVersion, reader);
            }
            else
            {
#pragma warning disable 612, 618
                source = new ArabicLetterTokenizer(matchVersion, reader);
#pragma warning restore 612, 618
            }
            TokenStream result = new LowerCaseFilter(matchVersion, source);
            result = new ArabicNormalizationFilter(result);
            /* additional persian-specific normalization */
            result = new PersianNormalizationFilter(result);
            /*
             * the order here is important: the stopword list is normalized with the
             * above!
             */
            return new TokenStreamComponents(source, new StopFilter(matchVersion, result, stopwords));
        }

        /// <summary>
        /// Wraps the Reader with <seealso cref="PersianCharFilter"/>
        /// </summary>
        public override TextReader InitReader(string fieldName, TextReader reader)
        {
#pragma warning disable 612, 618
            return matchVersion.OnOrAfter(LuceneVersion.LUCENE_31) ?
#pragma warning restore 612, 618
                new PersianCharFilter(reader) : reader;
        }
    }
}