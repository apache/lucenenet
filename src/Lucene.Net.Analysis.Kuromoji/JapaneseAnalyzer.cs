using Lucene.Net.Analysis.Cjk;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Ja.Dict;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;

namespace Lucene.Net.Analysis.Ja
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
    /// Analyzer for Japanese that uses morphological analysis.
    /// </summary>
    /// <seealso cref="JapaneseTokenizer"/>
    public class JapaneseAnalyzer : StopwordAnalyzerBase
    {
        private readonly JapaneseTokenizerMode mode;
        private readonly ISet<string> stoptags;
        private readonly UserDictionary userDict;

        public JapaneseAnalyzer(LuceneVersion matchVersion)
            : this(matchVersion, null, JapaneseTokenizer.DEFAULT_MODE, DefaultSetHolder.DEFAULT_STOP_SET, DefaultSetHolder.DEFAULT_STOP_TAGS)
        {
        }

        public JapaneseAnalyzer(LuceneVersion matchVersion, UserDictionary userDict, JapaneseTokenizerMode mode, CharArraySet stopwords, ISet<string> stoptags)
            : base(matchVersion, stopwords)
        {
            this.userDict = userDict;
            this.mode = mode;
            this.stoptags = stoptags;
        }

        public static CharArraySet GetDefaultStopSet()
        {
            return DefaultSetHolder.DEFAULT_STOP_SET;
        }

        public static ISet<string> GetDefaultStopTags()
        {
            return DefaultSetHolder.DEFAULT_STOP_TAGS;
        }

        /// <summary>
        /// Atomically loads DEFAULT_STOP_SET, DEFAULT_STOP_TAGS in a lazy fashion once the 
        /// outer class accesses the static final set the first time.
        /// </summary>
        private static class DefaultSetHolder
        {
            internal static readonly CharArraySet DEFAULT_STOP_SET;
            internal static readonly ISet<string> DEFAULT_STOP_TAGS;

            static DefaultSetHolder()
            {
                try
                {
                    DEFAULT_STOP_SET = LoadStopwordSet(true, typeof(JapaneseAnalyzer), "stopwords.txt", "#");  // ignore case
                    CharArraySet tagset = LoadStopwordSet(false, typeof(JapaneseAnalyzer), "stoptags.txt", "#");
                    DEFAULT_STOP_TAGS = new HashSet<string>();
                    foreach (string element in tagset)
                    {
                        DEFAULT_STOP_TAGS.Add(element);
                    }
                }
                catch (IOException ex)
                {
                    // default set should always be present as it is part of the distribution (JAR)
                    throw new Exception("Unable to load default stopword or stoptag set", ex);
                }
            }
        }

        protected override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
            Tokenizer tokenizer = new JapaneseTokenizer(reader, userDict, true, mode);
            TokenStream stream = new JapaneseBaseFormFilter(tokenizer);
            stream = new JapanesePartOfSpeechStopFilter(m_matchVersion, stream, stoptags);
            stream = new CJKWidthFilter(stream);
            stream = new StopFilter(m_matchVersion, stream, m_stopwords);
            stream = new JapaneseKatakanaStemFilter(stream);
            stream = new LowerCaseFilter(m_matchVersion, stream);
            return new TokenStreamComponents(tokenizer, stream);
        }
    }
}
