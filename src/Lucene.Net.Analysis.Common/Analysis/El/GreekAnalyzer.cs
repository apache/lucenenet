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
    /// <seealso cref="Analyzer"/> for the Greek language. 
    /// <para>
    /// Supports an external list of stopwords (words
    /// that will not be indexed at all).
    /// A default set of stopwords is used unless an alternative list is specified.
    /// </para>
    /// 
    /// <a name="version"/>
    /// <para>You must specify the required <seealso cref="Version"/>
    /// compatibility when creating GreekAnalyzer:
    /// <ul>
    ///   <li> As of 3.1, StandardFilter and GreekStemmer are used by default.
    ///   <li> As of 2.9, StopFilter preserves position
    ///        increments
    /// </ul>
    /// 
    /// </para>
    /// <para><b>NOTE</b>: This class uses the same <seealso cref="Version"/>
    /// dependent settings as <seealso cref="StandardAnalyzer"/>.</para>
    /// </summary>
    public sealed class GreekAnalyzer : StopwordAnalyzerBase
    {
        /// <summary>
        /// File containing default Greek stopwords. </summary>
        public const string DEFAULT_STOPWORD_FILE = "stopwords.txt";

        /// <summary>
        /// Returns a set of default Greek-stopwords </summary>
        /// <returns> a set of default Greek-stopwords  </returns>
        public static CharArraySet DefaultStopSet
        {
            get
            {
                return DefaultSetHolder.DEFAULT_SET;
            }
        }

        private class DefaultSetHolder
        {
            internal static readonly CharArraySet DEFAULT_SET;

            static DefaultSetHolder()
            {
                try
                {
                    var resource = GetAnalysisResourceName(typeof(GreekAnalyzer), "El", DEFAULT_STOPWORD_FILE);
                    DEFAULT_SET = LoadStopwordSet(false, typeof(GreekAnalyzer), resource, "#");
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
        /// Builds an analyzer with the default stop words. </summary>
        /// <param name="matchVersion"> Lucene compatibility version,
        ///   See <a href="#version">above</a> </param>
        public GreekAnalyzer(LuceneVersion matchVersion)
              : this(matchVersion, DefaultSetHolder.DEFAULT_SET)
        {
        }

        /// <summary>
        /// Builds an analyzer with the given stop words. 
        /// <para>
        /// <b>NOTE:</b> The stopwords set should be pre-processed with the logic of 
        /// <seealso cref="GreekLowerCaseFilter"/> for best results.
        ///  
        /// </para>
        /// </summary>
        /// <param name="matchVersion"> Lucene compatibility version,
        ///   See <a href="#version">above</a> </param>
        /// <param name="stopwords"> a stopword set </param>
        public GreekAnalyzer(LuceneVersion matchVersion, CharArraySet stopwords)
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
        ///         <seealso cref="GreekLowerCaseFilter"/>, <seealso cref="StandardFilter"/>,
        ///         <seealso cref="StopFilter"/>, and <seealso cref="GreekStemFilter"/> </returns>
        public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
            Tokenizer source = new StandardTokenizer(matchVersion, reader);
            TokenStream result = new GreekLowerCaseFilter(matchVersion, source);
#pragma warning disable 612, 618
            if (matchVersion.OnOrAfter(LuceneVersion.LUCENE_31))
            {
                result = new StandardFilter(matchVersion, result);
            }
            result = new StopFilter(matchVersion, result, stopwords);
            if (matchVersion.OnOrAfter(LuceneVersion.LUCENE_31))
#pragma warning restore 612, 618
            {
                result = new GreekStemFilter(result);
            }
            return new TokenStreamComponents(source, result);
        }
    }
}