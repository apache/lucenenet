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
using System.IO;
using System.Linq;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Analysis.AR
{
    /*
     * <see cref="Analyzer"/> for Arabic. 
     * <p/>
     * This analyzer implements light-stemming as specified by:
     * <i>
     * Light Stemming for Arabic Information Retrieval
     * </i>    
     * http://www.mtholyoke.edu/~lballest/Pubs/arab_stem05.pdf
     * <p/>
     * The analysis package contains three primary components:
     * <ul>
     *  <li><see cref="ArabicNormalizationFilter"/>: Arabic orthographic normalization.</li>
     *  <li><see cref="ArabicStemFilter"/>: Arabic light stemming</li>
     *  <li>Arabic stop words file: a set of default Arabic stop words.</li>
     * </ul>
     * 
     */
    public sealed class ArabicAnalyzer : StopwordAnalyzerBase
    {
        public static readonly string DEFAULT_STOPWORD_FILE = "ArabicStopWords.text";

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
                    DEFAULT_STOP_SET = LoadStopwordSet(false, typeof (ArabicAnalyzer), DEFAULT_STOPWORD_FILE, "#");
                }
                catch (IOException ex)
                {
                    throw new Exception("Unable to load default stopword set.");
                }
            }
        }

        private readonly CharArraySet _stemExclusionSet;

        public ArabicAnalyzer(Version matchVersion) : this(matchVersion, DefaultSetHolder.DEFAULT_STOP_SET) {}

        public ArabicAnalyzer(Version matchVersion, CharArraySet stopwords) : this(matchVersion, stopwords, CharArraySet.EMPTY_SET) {}

        public ArabicAnalyzer(Version matchVersion, CharArraySet stopwords, CharArraySet stemExclusionSet)
            : base(matchVersion, stopwords)
        {
            this._stemExclusionSet = CharArraySet.UnmodifiableSet(CharArraySet.Copy(matchVersion, stemExclusionSet));
        }

        public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
            var source = matchVersion.Value.OnOrAfter(Version.LUCENE_31)
                             ? (Tokenizer) new StandardTokenizer(matchVersion.Value, reader)
                             : (Tokenizer) new ArabicLetterTokenizer(matchVersion.Value, reader);
            TokenStream result = new LowerCaseFilter(matchVersion, source);

            // the order here is important: the stopword list is not normalized!
            result = new StopFilter(matchVersion, result, stopwords);
            // TODO: maybe we should make ArabicNormalization filter also KeywordAttribute aware?!
            result = new ArabicNormalizationFilter(result);
            if (_stemExclusionSet.Any())
            {
                result = new SetKeywordMarkerFilter(result, _stemExclusionSet);
            }
            return new TokenStreamComponents(source, new ArabicStemFilter(result));
        }
    }
}