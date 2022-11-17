// Lucene version compatibility level 8.2.0
using J2N;
using Lucene.Net.Analysis.CharFilters;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Morfologik;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using Morfologik.Stemming;
using System;
using System.IO;
using System.Text;

namespace Lucene.Net.Analysis.Uk
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
    /// A dictionary-based <see cref="Analyzer"/> for Ukrainian.
    /// </summary>
    /// <since>6.2.0</since>
    public sealed class UkrainianMorfologikAnalyzer : StopwordAnalyzerBase
    {
        private readonly CharArraySet stemExclusionSet;

        /// <summary>File containing default Ukrainian stopwords.</summary>
        public const string DEFAULT_STOPWORD_FILE = "stopwords.txt";

        /// <summary>
        /// Returns an unmodifiable instance of the default stop words set.
        /// </summary>
        /// <returns>Default stop words set.</returns>
        public static CharArraySet DefaultStopSet => DefaultSetHolder.DEFAULT_STOP_SET;

        /// <summary>
        /// Atomically loads the <see cref="DEFAULT_STOP_SET"/> in a lazy fashion once the outer class
        /// accesses the static final set the first time.
        /// </summary>
        private static class DefaultSetHolder
        {
            internal static readonly CharArraySet DEFAULT_STOP_SET = LoadDefaultSet(); // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)

            private static CharArraySet LoadDefaultSet()
            {
                try
                {
                    return WordlistLoader.GetSnowballWordSet(IOUtils.GetDecodingReader(typeof(UkrainianMorfologikAnalyzer),
                        DEFAULT_STOPWORD_FILE, Encoding.UTF8),
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
        }


        /// <summary>
        /// Builds an analyzer with the default stop words: <see cref="DEFAULT_STOPWORD_FILE"/>.
        /// </summary>
        /// <param name="matchVersion"><see cref="LuceneVersion"/> to match.</param>
        public UkrainianMorfologikAnalyzer(LuceneVersion matchVersion)
            : this(matchVersion, DefaultSetHolder.DEFAULT_STOP_SET)
        {
        }

        /// <summary>
        /// Builds an analyzer with the given stop words.
        /// </summary>
        /// <param name="matchVersion"><see cref="LuceneVersion"/> to match.</param>
        /// <param name="stopwords">A stopword set.</param>
        public UkrainianMorfologikAnalyzer(LuceneVersion matchVersion, CharArraySet stopwords)
            : this(matchVersion, stopwords, CharArraySet.Empty)
        {
        }

        /// <summary>
        /// Builds an analyzer with the given stop words. If a non-empty stem exclusion set is
        /// provided this analyzer will add a <see cref="SetKeywordMarkerFilter"/> before
        /// stemming.
        /// </summary>
        /// <param name="matchVersion"><see cref="LuceneVersion"/> to match.</param>
        /// <param name="stopwords">A stopword set.</param>
        /// <param name="stemExclusionSet">A set of terms not to be stemmed.</param>
        public UkrainianMorfologikAnalyzer(LuceneVersion matchVersion, CharArraySet stopwords, CharArraySet stemExclusionSet)
                    : base(matchVersion, stopwords)
        {
            this.stemExclusionSet = CharArraySet.Copy(matchVersion, stemExclusionSet).AsReadOnly();
        }

        protected internal override TextReader InitReader(string fieldName, TextReader reader)
        {
            NormalizeCharMap.Builder builder = new NormalizeCharMap.Builder();
            // different apostrophes
            builder.Add("\u2019", "'");
            builder.Add("\u2018", "'");
            builder.Add("\u02BC", "'");
            builder.Add("`", "'");
            builder.Add("´", "'");
            // ignored characters
            builder.Add("\u0301", "");
            builder.Add("\u00AD", "");
            builder.Add("ґ", "г");
            builder.Add("Ґ", "Г");

            NormalizeCharMap normMap = builder.Build();
            reader = new MappingCharFilter(normMap, reader);
            return reader;
        }

        /// <summary>
        /// Creates a <see cref="TokenStreamComponents"/>
        /// which tokenizes all the text in the provided <see cref="TextReader"/>.
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="reader"></param>
        /// <returns>A <see cref="TokenStreamComponents"/> built from a <see cref="StandardTokenizer"/>
        /// filtered with <see cref="LowerCaseFilter"/>, <see cref="StopFilter"/>, <see cref="SetKeywordMarkerFilter"/>
        /// if a stem exclusion set is provided and <see cref="MorfologikFilter"/> on the Ukrainian dictionary.</returns>
        protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
            Tokenizer source = new StandardTokenizer(m_matchVersion, reader);
            TokenStream result = new LowerCaseFilter(m_matchVersion, source);
            result = new StopFilter(m_matchVersion, result, m_stopwords);

            if (stemExclusionSet.Count > 0)
            {
                result = new SetKeywordMarkerFilter(result, stemExclusionSet);
            }

            result = new MorfologikFilter(result, GetDictionary());
            return new TokenStreamComponents(source, result);
        }

        private static Dictionary GetDictionary()
        {
            try
            {
                Type type = typeof(UkrainianMorfologikAnalyzer);
                // LUCENENET NOTE: In Lucene, this was downloaded from Maven as a dependency
                // (see https://search.maven.org/search?q=a:morfologik-ukrainian-search). However, we are embedding the file in .NET.
                // Since it doesn't appear to be updated frequently, this should be okay.
                string dictFile = "ukrainian.dict";
                using var dictStream = type.FindAndGetManifestResourceStream(dictFile);
                using var metadataStream = type.FindAndGetManifestResourceStream(DictionaryMetadata.GetExpectedMetadataFileName(dictFile));
                return Dictionary.Read(dictStream, metadataStream);
            }
            catch (Exception e) when (e.IsIOException())
            {
                throw RuntimeException.Create(e);
            }
        }
    }
}
