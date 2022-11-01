using J2N.Collections.Generic.Extensions;
using Lucene.Net.Analysis.Cjk;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Ja.Dict;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;
using JCG = J2N.Collections.Generic;

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

        [Obsolete("Use DefaultStopSet instead. This method will be removed in 4.8.0 release candidate."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static CharArraySet GetDefaultStopSet() => DefaultSetHolder.DEFAULT_STOP_SET;

        [Obsolete("Use DefaultStopTags instead. This method will be removed in 4.8.0 release candidate."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static ISet<string> GetDefaultStopTags() => DefaultSetHolder.DEFAULT_STOP_TAGS;

        public static CharArraySet DefaultStopSet => DefaultSetHolder.DEFAULT_STOP_SET;

        public static ISet<string> DefaultStopTags => DefaultSetHolder.DEFAULT_STOP_TAGS;

        /// <summary>
        /// Atomically loads DEFAULT_STOP_SET, DEFAULT_STOP_TAGS in a lazy fashion once the 
        /// outer class accesses the static final set the first time.
        /// </summary>
        private static class DefaultSetHolder
        {
            internal static readonly CharArraySet DEFAULT_STOP_SET = LoadDefaultStopSet();
            internal static readonly ISet<string> DEFAULT_STOP_TAGS = LoadDefaultStopTagSet();

            private static CharArraySet LoadDefaultStopSet() // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
            {
                try
                {
                    return LoadStopwordSet(true, typeof(JapaneseAnalyzer), "stopwords.txt", "#").AsReadOnly(); // LUCENENET: Made readonly as stated in the docs: https://github.com/apache/lucene/issues/11866  // ignore case
                }
                catch (Exception ex) when (ex.IsIOException())
                {
                    // default set should always be present as it is part of the distribution (JAR)
                    throw RuntimeException.Create("Unable to load default stopword set", ex);
                }
            }

            private static ISet<string> LoadDefaultStopTagSet() // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
            {
                try
                {
                    CharArraySet tagset = LoadStopwordSet(false, typeof(JapaneseAnalyzer), "stoptags.txt", "#");
                    var DEFAULT_STOP_TAGS = new JCG.HashSet<string>();
                    foreach (string element in tagset)
                    {
                        DEFAULT_STOP_TAGS.Add(element);
                    }
                    return DEFAULT_STOP_TAGS.AsReadOnly(); // LUCENENET: Made readonly as stated in the docs: https://github.com/apache/lucene/issues/11866
                }
                catch (Exception ex) when (ex.IsIOException())
                {
                    // default set should always be present as it is part of the distribution (JAR)
                    throw RuntimeException.Create("Unable to load default stoptag set", ex);
                }
            }
        }

        protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
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
