// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Snowball;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using System;
using System.IO;
using System.Text;

namespace Lucene.Net.Analysis.Nl
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
    /// <see cref="Analyzer"/> for Dutch language. 
    /// <para>
    /// Supports an external list of stopwords (words that
    /// will not be indexed at all), an external list of exclusions (word that will
    /// not be stemmed, but indexed) and an external list of word-stem pairs that overrule
    /// the algorithm (dictionary stemming).
    /// A default set of stopwords is used unless an alternative list is specified, but the
    /// exclusion list is empty by default.
    /// </para>
    /// 
    /// <para>You must specify the required <see cref="LuceneVersion"/>
    /// compatibility when creating <see cref="DutchAnalyzer"/>:
    /// <list type="bullet">
    ///   <item><description> As of 3.6, <see cref="DutchAnalyzer(LuceneVersion, CharArraySet)"/> and
    ///        <see cref="DutchAnalyzer(LuceneVersion, CharArraySet, CharArraySet)"/> also populate
    ///        the default entries for the stem override dictionary</description></item>
    ///   <item><description> As of 3.1, Snowball stemming is done with SnowballFilter, 
    ///        LowerCaseFilter is used prior to StopFilter, and Snowball 
    ///        stopwords are used by default.</description></item>
    ///   <item><description> As of 2.9, StopFilter preserves position
    ///        increments</description></item>
    /// </list>
    /// 
    /// </para>
    /// <para><b>NOTE</b>: This class uses the same <see cref="LuceneVersion"/>
    /// dependent settings as <see cref="StandardAnalyzer"/>.</para>
    /// </summary>
    public sealed class DutchAnalyzer : Analyzer
    {
        /// <summary>
        /// File containing default Dutch stopwords. </summary>
        public const string DEFAULT_STOPWORD_FILE = "dutch_stop.txt";

        /// <summary>
        /// Returns an unmodifiable instance of the default stop-words set. </summary>
        /// <returns> an unmodifiable instance of the default stop-words set. </returns>
        public static CharArraySet DefaultStopSet => DefaultSetHolder.DEFAULT_STOP_SET;

        private static class DefaultSetHolder
        {
            internal static readonly CharArraySet DEFAULT_STOP_SET = LoadDefaultStopSet();
            internal static readonly CharArrayDictionary<string> DEFAULT_STEM_DICT = LoadDefaultStemDict();
            private static CharArraySet LoadDefaultStopSet() // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
            {
                try
                {
                    return WordlistLoader.GetSnowballWordSet(
                        IOUtils.GetDecodingReader(typeof(SnowballFilter), DEFAULT_STOPWORD_FILE, Encoding.UTF8),
#pragma warning disable 612, 618
                        LuceneVersion.LUCENE_CURRENT).AsReadOnly(); // LUCENENET: Made readonly as stated in the docs: https://github.com/apache/lucene/issues/11866
#pragma warning restore 612, 618
                }
                catch (Exception ex) when (ex.IsIOException())
                {
                    // default set should always be present as it is part of the
                    // distribution (JAR)
                    throw RuntimeException.Create("Unable to load default stopword set", ex);
                }

            }

            private static CharArrayDictionary<string> LoadDefaultStemDict() // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
            {
#pragma warning disable 612, 618
                var DEFAULT_STEM_DICT = new CharArrayDictionary<string>(LuceneVersion.LUCENE_CURRENT, 4, false);
#pragma warning restore 612, 618
                DEFAULT_STEM_DICT["fiets"] = "fiets"; //otherwise fiet
                DEFAULT_STEM_DICT["bromfiets"] = "bromfiets"; //otherwise bromfiet
                DEFAULT_STEM_DICT["ei"] = "eier";
                DEFAULT_STEM_DICT["kind"] = "kinder";
                return DEFAULT_STEM_DICT;
            }
        }


        /// <summary>
        /// Contains the stopwords used with the <see cref="StopFilter"/>.
        /// </summary>
        private readonly CharArraySet stoptable;

        /// <summary>
        /// Contains words that should be indexed but not stemmed.
        /// </summary>
        private CharArraySet excltable = CharArraySet.Empty;

        private readonly StemmerOverrideFilter.StemmerOverrideMap stemdict;

        // null if on 3.1 or later - only for bw compat
        private readonly CharArrayDictionary<string> origStemdict;
        private readonly LuceneVersion matchVersion;

        /// <summary>
        /// Builds an analyzer with the default stop words (<see cref="DefaultStopSet"/>) 
        /// and a few default entries for the stem exclusion table.
        /// </summary>
        public DutchAnalyzer(LuceneVersion matchVersion)
              : this(matchVersion, DefaultSetHolder.DEFAULT_STOP_SET, CharArraySet.Empty, DefaultSetHolder.DEFAULT_STEM_DICT)
        {
            // historically, only this ctor populated the stem dict!!!!!
        }

        public DutchAnalyzer(LuceneVersion matchVersion, CharArraySet stopwords)
              : this(matchVersion, stopwords, CharArraySet.Empty,
#pragma warning disable 612, 618
                    matchVersion.OnOrAfter(LuceneVersion.LUCENE_36) ?
#pragma warning restore 612, 618
                    DefaultSetHolder.DEFAULT_STEM_DICT : CharArrayDictionary<string>.Empty)
        {
            // historically, this ctor never the stem dict!!!!!
            // so we populate it only for >= 3.6
        }

        public DutchAnalyzer(LuceneVersion matchVersion, CharArraySet stopwords, CharArraySet stemExclusionTable)
              : this(matchVersion, stopwords, stemExclusionTable,
#pragma warning disable 612, 618
                    matchVersion.OnOrAfter(LuceneVersion.LUCENE_36) ?
#pragma warning restore 612, 618
                    DefaultSetHolder.DEFAULT_STEM_DICT : CharArrayDictionary<string>.Empty)
        {
            // historically, this ctor never the stem dict!!!!!
            // so we populate it only for >= 3.6
        }

        public DutchAnalyzer(LuceneVersion matchVersion, CharArraySet stopwords, CharArraySet stemExclusionTable, CharArrayDictionary<string> stemOverrideDict)
        {
            this.matchVersion = matchVersion;
            this.stoptable = CharArraySet.Copy(matchVersion, stopwords).AsReadOnly();
            this.excltable = CharArraySet.Copy(matchVersion, stemExclusionTable).AsReadOnly();
#pragma warning disable 612, 618
            if (stemOverrideDict.Count == 0 || !matchVersion.OnOrAfter(LuceneVersion.LUCENE_31))
#pragma warning restore 612, 618
            {
                this.stemdict = null;
                this.origStemdict = CharArrayDictionary.Copy(matchVersion, stemOverrideDict).AsReadOnly();
            }
            else
            {
                this.origStemdict = null;
                // we don't need to ignore case here since we lowercase in this analyzer anyway
                StemmerOverrideFilter.Builder builder = new StemmerOverrideFilter.Builder(false);
                using (var iter = stemOverrideDict.GetEnumerator())
                {
                    CharsRef spare = new CharsRef();
                    while (iter.MoveNext())
                    {
                        char[] nextKey = iter.CurrentKey;
                        spare.CopyChars(nextKey, 0, nextKey.Length);
                        builder.Add(spare.Chars, iter.CurrentValue);
                    }
                }
                try
                {
                    this.stemdict = builder.Build();
                }
                catch (Exception ex) when (ex.IsIOException())
                {
                    throw RuntimeException.Create("can not build stem dict", ex);
                }
            }
        }

        /// <summary>
        /// Returns a (possibly reused) <see cref="TokenStream"/> which tokenizes all the 
        /// text in the provided <see cref="TextReader"/>.
        /// </summary>
        /// <returns> A <see cref="TokenStream"/> built from a <see cref="StandardTokenizer"/>
        ///   filtered with <see cref="StandardFilter"/>, <see cref="LowerCaseFilter"/>, 
        ///   <see cref="StopFilter"/>, <see cref="SetKeywordMarkerFilter"/> if a stem exclusion set is provided,
        ///   <see cref="StemmerOverrideFilter"/>, and <see cref="SnowballFilter"/> </returns>
        protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader aReader)
        {
#pragma warning disable 612, 618
            if (matchVersion.OnOrAfter(LuceneVersion.LUCENE_31))
#pragma warning restore 612, 618
            {
                Tokenizer source = new StandardTokenizer(matchVersion, aReader);
                TokenStream result = new StandardFilter(matchVersion, source);
                result = new LowerCaseFilter(matchVersion, result);
                result = new StopFilter(matchVersion, result, stoptable);
                if (excltable.Count > 0)
                {
                    result = new SetKeywordMarkerFilter(result, excltable);
                }
                if (stemdict != null)
                {
                    result = new StemmerOverrideFilter(result, stemdict);
                }
                result = new SnowballFilter(result, new Tartarus.Snowball.Ext.DutchStemmer());
                return new TokenStreamComponents(source, result);
            }
            else
            {
                Tokenizer source = new StandardTokenizer(matchVersion, aReader);
                TokenStream result = new StandardFilter(matchVersion, source);
                result = new StopFilter(matchVersion, result, stoptable);
                if (excltable.Count > 0)
                {
                    result = new SetKeywordMarkerFilter(result, excltable);
                }
#pragma warning disable 612, 618
                result = new DutchStemFilter(result, origStemdict);
#pragma warning restore 612, 618
                return new TokenStreamComponents(source, result);
            }
        }
    }
}