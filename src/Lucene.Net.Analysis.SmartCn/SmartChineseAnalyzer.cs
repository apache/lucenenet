// lucene version compatibility level: 4.8.1
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.En;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using System;
using System.IO;
using System.Text;

namespace Lucene.Net.Analysis.Cn.Smart
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
    /// <para>
    /// <see cref="SmartChineseAnalyzer"/> is an analyzer for Chinese or mixed Chinese-English text.
    /// The analyzer uses probabilistic knowledge to find the optimal word segmentation for Simplified Chinese text.
    /// The text is first broken into sentences, then each sentence is segmented into words.
    /// </para>
    /// <para>
    /// Segmentation is based upon the <a href="http://en.wikipedia.org/wiki/Hidden_Markov_Model">Hidden Markov Model</a>.
    /// A large training corpus was used to calculate Chinese word frequency probability.
    /// </para>
    /// <para>
    /// This analyzer requires a dictionary to provide statistical data. 
    /// <see cref="SmartChineseAnalyzer"/> has an included dictionary out-of-box.
    /// </para>
    /// <para>
    /// The included dictionary data is from <a href="http://www.ictclas.org">ICTCLAS1.0</a>.
    /// Thanks to ICTCLAS for their hard work, and for contributing the data under the Apache 2 License!
    /// </para>
    /// @lucene.experimental
    /// </summary>
    public sealed class SmartChineseAnalyzer : Analyzer
    {
        private readonly CharArraySet stopWords;

        private const string DEFAULT_STOPWORD_FILE = "stopwords.txt";

        private const string STOPWORD_FILE_COMMENT = "//";

        [Obsolete("Use DefaultStopSet instead. This method will be removed in 4.8.0 release candidate."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static CharArraySet GetDefaultStopSet() => DefaultSetHolder.DEFAULT_STOP_SET;

        /// <summary>
        /// Returns an unmodifiable instance of the default stop-words set.
        /// </summary>
        /// <returns>An unmodifiable instance of the default stop-words set.</returns>
        public static CharArraySet DefaultStopSet => DefaultSetHolder.DEFAULT_STOP_SET;

        /// <summary>
        /// Atomically loads the DEFAULT_STOP_SET in a lazy fashion once the outer class 
        /// accesses the static final set the first time.
        /// </summary>
        private static class DefaultSetHolder
        {
            internal static readonly CharArraySet DEFAULT_STOP_SET = LoadDefaultStopSet();

            private static CharArraySet LoadDefaultStopSet() // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
            {
                try
                {
                    return LoadDefaultStopWordSet();
                }
                catch (Exception ex) when (ex.IsIOException())
                {
                    // default set should always be present as it is part of the
                    // distribution (JAR)
                    throw RuntimeException.Create("Unable to load default stopword set", ex);
                }
            }

            internal static CharArraySet LoadDefaultStopWordSet()
            {
                // make sure it is unmodifiable as we expose it in the outer class
                return WordlistLoader.GetWordSet(IOUtils
                    .GetDecodingReader(typeof(SmartChineseAnalyzer), DEFAULT_STOPWORD_FILE,
                        Encoding.UTF8), STOPWORD_FILE_COMMENT,
#pragma warning disable 612, 618
                    LuceneVersion.LUCENE_CURRENT).AsReadOnly();
#pragma warning restore 612, 618
            }
        }

        private readonly LuceneVersion matchVersion;

        /// <summary>
        /// Create a new <see cref="SmartChineseAnalyzer"/>, using the default stopword list.
        /// </summary>
        public SmartChineseAnalyzer(LuceneVersion matchVersion)
              : this(matchVersion, true)
        {
        }

        /// <summary>
        /// <para>
        /// Create a new <see cref="SmartChineseAnalyzer"/>, optionally using the default stopword list.
        /// </para>
        /// <para>
        /// The included default stopword list is simply a list of punctuation.
        /// If you do not use this list, punctuation will not be removed from the text!
        /// </para>
        /// </summary>
        /// <param name="matchVersion"></param>
        /// <param name="useDefaultStopWords"><c>true</c> to use the default stopword list.</param>
        public SmartChineseAnalyzer(LuceneVersion matchVersion, bool useDefaultStopWords)
        {
            stopWords = useDefaultStopWords ? DefaultSetHolder.DEFAULT_STOP_SET
              : CharArraySet.Empty;
            this.matchVersion = matchVersion;
        }

        /// <summary>
        /// <para>
        /// Create a new <see cref="SmartChineseAnalyzer"/>, using the provided <see cref="CharArraySet"/> of stopwords.
        /// </para>
        /// <para>
        /// Note: the set should include punctuation, unless you want to index punctuation!
        /// </para>
        /// </summary>
        /// <param name="matchVersion"></param>
        /// <param name="stopWords"><see cref="CharArraySet"/> of stopwords to use.</param>
        public SmartChineseAnalyzer(LuceneVersion matchVersion, CharArraySet stopWords)
        {
            this.stopWords = stopWords ?? CharArraySet.Empty;
            this.matchVersion = matchVersion;
        }

        protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
            Tokenizer tokenizer;
            TokenStream result;
            if (matchVersion.OnOrAfter(LuceneVersion.LUCENE_48))
            {
                tokenizer = new HMMChineseTokenizer(reader);
                result = tokenizer;
            }
            else
            {
#pragma warning disable 612, 618
                tokenizer = new SentenceTokenizer(reader);
                result = new WordTokenFilter(tokenizer);
#pragma warning restore 612, 618
            }
            // result = new LowerCaseFilter(result);
            // LowerCaseFilter is not needed, as SegTokenFilter lowercases Basic Latin text.
            // The porter stemming is too strict, this is not a bug, this is a feature:)
            result = new PorterStemFilter(result);
            if (stopWords.Count > 0)
            {
                result = new StopFilter(matchVersion, result, stopWords);
            }
            return new TokenStreamComponents(tokenizer, result);
        }
    }
}
