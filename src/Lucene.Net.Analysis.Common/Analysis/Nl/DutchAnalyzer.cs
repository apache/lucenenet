using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Snowball;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using System;
using System.IO;
using System.Reflection;
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
    /// <seealso cref="Analyzer"/> for Dutch language. 
    /// <para>
    /// Supports an external list of stopwords (words that
    /// will not be indexed at all), an external list of exclusions (word that will
    /// not be stemmed, but indexed) and an external list of word-stem pairs that overrule
    /// the algorithm (dictionary stemming).
    /// A default set of stopwords is used unless an alternative list is specified, but the
    /// exclusion list is empty by default.
    /// </para>
    /// 
    /// <a name="version"/>
    /// <para>You must specify the required <seealso cref="Version"/>
    /// compatibility when creating DutchAnalyzer:
    /// <ul>
    ///   <li> As of 3.6, <seealso cref="#DutchAnalyzer(Version, CharArraySet)"/> and
    ///        <seealso cref="#DutchAnalyzer(Version, CharArraySet, CharArraySet)"/> also populate
    ///        the default entries for the stem override dictionary
    ///   <li> As of 3.1, Snowball stemming is done with SnowballFilter, 
    ///        LowerCaseFilter is used prior to StopFilter, and Snowball 
    ///        stopwords are used by default.
    ///   <li> As of 2.9, StopFilter preserves position
    ///        increments
    /// </ul>
    /// 
    /// </para>
    /// <para><b>NOTE</b>: This class uses the same <seealso cref="Version"/>
    /// dependent settings as <seealso cref="StandardAnalyzer"/>.</para>
    /// </summary>
    public sealed class DutchAnalyzer : Analyzer
    {

        /// <summary>
        /// File containing default Dutch stopwords. </summary>
        public const string DEFAULT_STOPWORD_FILE = "dutch_stop.txt";

        /// <summary>
        /// Returns an unmodifiable instance of the default stop-words set. </summary>
        /// <returns> an unmodifiable instance of the default stop-words set. </returns>
        public static CharArraySet DefaultStopSet
        {
            get
            {
                return DefaultSetHolder.DEFAULT_STOP_SET;
            }
        }

        private class DefaultSetHolder
        {
            internal static readonly CharArraySet DEFAULT_STOP_SET;
            internal static readonly CharArrayMap<string> DEFAULT_STEM_DICT;
            static DefaultSetHolder()
            {
                try
                {
                    var resource = GetAnalysisResourceName(typeof(SnowballFilter), "Snowball", DEFAULT_STOPWORD_FILE);
                    DEFAULT_STOP_SET = WordlistLoader.GetSnowballWordSet(
                        IOUtils.GetDecodingReader(typeof(SnowballFilter), resource, Encoding.UTF8),
#pragma warning disable 612, 618
                        LuceneVersion.LUCENE_CURRENT);
#pragma warning restore 612, 618
                }
                catch (IOException)
                {
                    // default set should always be present as it is part of the
                    // distribution (JAR)
                    throw new Exception("Unable to load default stopword set");
                }
#pragma warning disable 612, 618
                DEFAULT_STEM_DICT = new CharArrayMap<string>(LuceneVersion.LUCENE_CURRENT, 4, false);
#pragma warning restore 612, 618
                DEFAULT_STEM_DICT.Put("fiets", "fiets"); //otherwise fiet
                DEFAULT_STEM_DICT.Put("bromfiets", "bromfiets"); //otherwise bromfiet
                DEFAULT_STEM_DICT.Put("ei", "eier");
                DEFAULT_STEM_DICT.Put("kind", "kinder");
            }
        }


        /// <summary>
        /// Contains the stopwords used with the StopFilter.
        /// </summary>
        private readonly CharArraySet stoptable;

        /// <summary>
        /// Contains words that should be indexed but not stemmed.
        /// </summary>
        private CharArraySet excltable = CharArraySet.EMPTY_SET;

        private readonly StemmerOverrideFilter.StemmerOverrideMap stemdict;

        // null if on 3.1 or later - only for bw compat
        private readonly CharArrayMap<string> origStemdict;
        private readonly LuceneVersion matchVersion;

        /// <summary>
        /// Builds an analyzer with the default stop words (<seealso cref="#getDefaultStopSet()"/>) 
        /// and a few default entries for the stem exclusion table.
        /// 
        /// </summary>
        public DutchAnalyzer(LuceneVersion matchVersion)
              : this(matchVersion, DefaultSetHolder.DEFAULT_STOP_SET, CharArraySet.EMPTY_SET, DefaultSetHolder.DEFAULT_STEM_DICT)
        {
            // historically, only this ctor populated the stem dict!!!!!
        }

        public DutchAnalyzer(LuceneVersion matchVersion, CharArraySet stopwords)
              : this(matchVersion, stopwords, CharArraySet.EMPTY_SET,
#pragma warning disable 612, 618
                    matchVersion.OnOrAfter(LuceneVersion.LUCENE_36) ?
#pragma warning restore 612, 618
                    DefaultSetHolder.DEFAULT_STEM_DICT : CharArrayMap<string>.EmptyMap())
        {
            // historically, this ctor never the stem dict!!!!!
            // so we populate it only for >= 3.6
        }

        public DutchAnalyzer(LuceneVersion matchVersion, CharArraySet stopwords, CharArraySet stemExclusionTable)
              : this(matchVersion, stopwords, stemExclusionTable,
#pragma warning disable 612, 618
                    matchVersion.OnOrAfter(LuceneVersion.LUCENE_36) ?
#pragma warning restore 612, 618
                    DefaultSetHolder.DEFAULT_STEM_DICT : CharArrayMap<string>.EmptyMap())
        {
            // historically, this ctor never the stem dict!!!!!
            // so we populate it only for >= 3.6
        }

        public DutchAnalyzer(LuceneVersion matchVersion, CharArraySet stopwords, CharArraySet stemExclusionTable, CharArrayMap<string> stemOverrideDict)
        {
            this.matchVersion = matchVersion;
            this.stoptable = CharArraySet.UnmodifiableSet(CharArraySet.Copy(matchVersion, stopwords));
            this.excltable = CharArraySet.UnmodifiableSet(CharArraySet.Copy(matchVersion, stemExclusionTable));
#pragma warning disable 612, 618
            if (stemOverrideDict.Count == 0 || !matchVersion.OnOrAfter(LuceneVersion.LUCENE_31))
#pragma warning restore 612, 618
            {
                this.stemdict = null;
                this.origStemdict = CharArrayMap.UnmodifiableMap(CharArrayMap.Copy(matchVersion, stemOverrideDict));
            }
            else
            {
                this.origStemdict = null;
                // we don't need to ignore case here since we lowercase in this analyzer anyway
                StemmerOverrideFilter.Builder builder = new StemmerOverrideFilter.Builder(false);
                CharArrayMap<string>.EntryIterator iter = (CharArrayMap<string>.EntryIterator)stemOverrideDict.EntrySet().GetEnumerator();
                CharsRef spare = new CharsRef();
                while (iter.HasNext)
                {
                    char[] nextKey = iter.NextKey();
                    spare.CopyChars(nextKey, 0, nextKey.Length);
                    builder.Add(new string(spare.Chars), iter.CurrentValue);
                }
                try
                {
                    this.stemdict = builder.Build();
                }
                catch (IOException ex)
                {
                    throw new Exception("can not build stem dict", ex);
                }
            }
        }

        /// <summary>
        /// Returns a (possibly reused) <seealso cref="TokenStream"/> which tokenizes all the 
        /// text in the provided <seealso cref="Reader"/>.
        /// </summary>
        /// <returns> A <seealso cref="TokenStream"/> built from a <seealso cref="StandardTokenizer"/>
        ///   filtered with <seealso cref="StandardFilter"/>, <seealso cref="LowerCaseFilter"/>, 
        ///   <seealso cref="StopFilter"/>, <seealso cref="SetKeywordMarkerFilter"/> if a stem exclusion set is provided,
        ///   <seealso cref="StemmerOverrideFilter"/>, and <seealso cref="SnowballFilter"/> </returns>
        public override TokenStreamComponents CreateComponents(string fieldName, TextReader aReader)
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

        /// <summary>
        /// LUCENENET specific: For explanation see:
        /// <see cref="StopwordAnalyzerBase.GetAnalysisResourceName(Type, string, string)"/>
        /// </summary>
        private static string GetAnalysisResourceName(Type type, string analysisSubfolder, string filename)
        {
#if FEATURE_NETCOREEMBEDDEDRESOURCE
            return string.Format("{0}.Analysis.{1}.{2}", type.GetTypeInfo().Assembly.GetName().Name, analysisSubfolder, filename);
#else
            return string.Format("{0}.{1}", type.Namespace, filename);
#endif
        }
    }
}