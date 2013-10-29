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

using System.IO;
using System.Linq;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;

namespace Lucene.Net.Analysis.En
{
    /// <summary>
    /// Analyzer for English
    /// </summary>
    public class EnglishAnalyzer : StopwordAnalyzerBase
    {
        private readonly CharArraySet stemExclusionSet;

        public static CharArraySet DefaultStopSet
        {
            get { return DefaultSetHolder.DEFAULT_STOP_SET; }
        }

        private static class DefaultSetHolder
        {
            internal static readonly CharArraySet DEFAULT_STOP_SET = StandardAnalyzer.STOP_WORDS_SET;
        }

        public EnglishAnalyzer(Version matchVersion) : this(matchVersion, DefaultSetHolder.DEFAULT_STOP_SET) { }

        public EnglishAnalyzer(Version matchVersion, CharArraySet stopWords) : this(matchVersion, stopWords, CharArraySet.EMPTY_SET) { }

        public EnglishAnalyzer(Version matchVersion, CharArraySet stopWords, CharArraySet stemExclusionSet)
            : base(matchVersion, stopWords)
        {
            this.stemExclusionSet = CharArraySet.UnmodifiableSet(CharArraySet.Copy(matchVersion, stemExclusionSet));
        }

        public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
            var source = new StandardTokenizer(matchVersion, reader);
            TokenStream result = new StandardFilter(matchVersion, source);
            // prior to this we get the classic behavior, standardfilter does it for us.
            if (matchVersion.Value.OnOrAfter(Version.LUCENE_31))
            {
                result = new EnglishPossessiveFilter(matchVersion.Value, result);
            }
            result = new LowerCaseFilter(matchVersion.Value, result);
            result = new StopFilter(matchVersion.Value, result, stopwords);
            if (stemExclusionSet.Any())
            {
                result = new SetKeywordMarkerFilter(result, stemExclusionSet);
            }
            result = new PorterStemFilter(result);
            return new TokenStreamComponents(source, result);
        }
    }
}
