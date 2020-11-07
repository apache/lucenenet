using Lucene.Net.Analysis.Util;
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
    /// Factory for <see cref="JapaneseIterationMarkCharFilter"/>.
    /// <code>
    /// &lt;fieldType name="text_ja" class="solr.TextField" positionIncrementGap="100" autoGeneratePhraseQueries="false"&gt;
    ///   &lt;analyzer&gt;
    ///     &lt;charFilter class="solr.JapaneseIterationMarkCharFilterFactory normalizeKanji="true" normalizeKana="true"/&gt;
    ///     &lt;tokenizer class="solr.JapaneseTokenizerFactory"/&gt;
    ///   &lt;/analyzer&gt;
    /// &lt;/fieldType&gt;
    /// </code>
    /// </summary>
    public class JapaneseIterationMarkCharFilterFactory : CharFilterFactory, IMultiTermAwareComponent
    {
        private const string NORMALIZE_KANJI_PARAM = "normalizeKanji";
        private const string NORMALIZE_KANA_PARAM = "normalizeKana";

        private readonly bool normalizeKanji;
        private readonly bool normalizeKana;

        /// <summary>Creates a new <see cref="JapaneseIterationMarkCharFilterFactory"/></summary>
        public JapaneseIterationMarkCharFilterFactory(IDictionary<string, string> args)
            : base(args)
        {
            normalizeKanji = GetBoolean(args, NORMALIZE_KANJI_PARAM, JapaneseIterationMarkCharFilter.NORMALIZE_KANJI_DEFAULT);
            normalizeKana = GetBoolean(args, NORMALIZE_KANA_PARAM, JapaneseIterationMarkCharFilter.NORMALIZE_KANA_DEFAULT);
            if (args.Count > 0)
            {
                throw new ArgumentException(string.Format(J2N.Text.StringFormatter.CurrentCulture, "Unknown parameters: {0}", args));
            }
        }

        public override TextReader Create(TextReader input)
        {
            return new JapaneseIterationMarkCharFilter(input, normalizeKanji, normalizeKana);
        }

        public virtual AbstractAnalysisFactory GetMultiTermComponent()
        {
            return this;
        }
    }
}
