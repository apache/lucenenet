// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Core;
using Lucene.Net.Util;
using Lucene.Net.Analysis.Ko.Dict;
using System.Collections.Generic;
using System.IO;

namespace Lucene.Net.Analysis.Ko
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
    /// Analyzer for Korean that uses morphological analysis.
    /// </summary>
    /// <seealso cref="KoreanTokenizer"/>
    public class KoreanAnalyzer: Analyzer
    {
        private readonly UserDictionary userDict;
        private readonly KoreanTokenizer.DecompoundMode mode;
        private readonly HashSet<POS.Tag> stopTags;
        private readonly bool outputUnknownUnigrams;

        /// <summary>
        /// Creates a new KoreanAnalyzer.
        /// </summary>
        public KoreanAnalyzer()
            : this(null, KoreanTokenizer.DEFAULT_DECOMPOUND, KoreanPartOfSpeechStopFilter.DEFAULT_STOP_TAGS, false)
        {
        }

        /// <summary>
        /// Creates a new KoreanAnalyzer.
        /// </summary>
        /// <param name="userDict"> – Optional: if non-null, user dictionary. </param>
        /// <param name="mode"> – Decompound mode. </param>
        /// <param name="stopTags"> – The set of part of speech that should be filtered. </param>
        /// <param name="outputUnknownUnigrams"> – If true outputs unigrams for unknown words. </param>
        public KoreanAnalyzer(UserDictionary userDict, KoreanTokenizer.DecompoundMode mode, HashSet<POS.Tag> stopTags, bool outputUnknownUnigrams)
            : base()
        {
            this.userDict = userDict;
            this.mode = mode;
            this.stopTags = stopTags;
            this.outputUnknownUnigrams = outputUnknownUnigrams;
        }

        protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
            Tokenizer tokenizer = new KoreanTokenizer(AttributeSource.AttributeFactory.DEFAULT_ATTRIBUTE_FACTORY, reader, userDict, mode, outputUnknownUnigrams);
            TokenStream stream = new KoreanPartOfSpeechStopFilter(LuceneVersion.LUCENE_48, tokenizer, stopTags);
            stream = new KoreanReadingFormFilter(stream);
            stream = new LowerCaseFilter(LuceneVersion.LUCENE_48, stream);
            return new TokenStreamComponents(tokenizer, stream);
        }
    }
}