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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Analysis.CA
{
    public class CatalanAnalyzer : StopwordAnalyzerBase
    {
        private readonly CharArraySet _stemExclusionSet;

        public static readonly string DEFAULT_STOPWORD_FILE = "CatalanStopWords.txt";

        private static readonly CharArraySet DEFAULT_ARTICLES = CharArraySet.UnmodifiableSet(
            new CharArraySet(Version.LUCENE_CURRENT, new []
                {
                    "d", "l", "m", "n", "s", "t"
                }, true));

        public static CharArraySet DefaultStopSet
        {
            get { return DefaultSetHolder.DEFAULT_STOP_SET; }
        }

        private static class DefaultSetHolder
        {
            internal static readonly CharArraySet DEFAULT_STOP_SET;

            static DefaultSetHolder()
            {
                try
                {
                    DEFAULT_STOP_SET = LoadStopwordSet(false, typeof (CatalanAnalyzer), DEFAULT_STOP_SET, "#");
                }
                catch (IOException ex)
                {
                    throw new Exception("Unable to load default stopword set.", ex);
                }
            }
        }

        public CatalanAnalyzer(Version matchVersion) : this(matchVersion, DefaultSetHolder.DEFAULT_STOP_SET) { }

        public CatalanAnalyzer(Version matchVersion, CharArraySet stopwords) : this(matchVersion, stopwords, CharArraySet.EMPTY_SET) { }
 
        public CatalanAnalyzer(Version matchVersion, CharArraySet stopwords, CharArraySet stemExclusionSet)
            : base(matchVersion, stopwords)
        {
            this._stemExclusionSet = CharArraySet.UnmodifiableSet(CharArraySet.Copy(matchVersion, stemExclusionSet));
        }

        public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
            var source = new StandardTokenizer(matchVersion, reader);
            TokenStream result = new StandardFilter(matchVersion, source);
            if (matchVersion.Value.OnOrAfter(Version.LUCENE_36))
            {
                result = new ElisionFilter(result, DEFAULT_ARTICLES);
            }
            result = new LowerCaseFilter(matchVersion, result);
            result = new StopFilter(matchVersion, result, stopwords);
            if (_stemExclusionSet.Any())
            {
                result = new SetKeywordMarkerFilter(result, _stemExclusionSet);
            }
            result = new SnowballFilter(result, new CatalanStemmer());
            return new TokenStreamComponents(source, result);
        }
    }
}
