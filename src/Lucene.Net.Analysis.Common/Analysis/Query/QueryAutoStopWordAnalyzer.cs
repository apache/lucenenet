// Lucene version compatibility level 4.8.1
using J2N.Collections.Generic.Extensions;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Index;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System.Collections.Generic;
using System.IO;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Analysis.Query
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
    /// An <see cref="Analyzer"/> used primarily at query time to wrap another analyzer and provide a layer of protection
    /// which prevents very common words from being passed into queries. 
    /// <para>
    /// For very large indexes the cost
    /// of reading TermDocs for a very common word can be  high. This analyzer was created after experience with
    /// a 38 million doc index which had a term in around 50% of docs and was causing TermQueries for 
    /// this term to take 2 seconds.
    /// </para>
    /// </summary>
    public sealed class QueryAutoStopWordAnalyzer : AnalyzerWrapper
    {
        private readonly Analyzer @delegate;
        private readonly IDictionary<string, ISet<string>> stopWordsPerField = new Dictionary<string, ISet<string>>();
        //The default maximum percentage (40%) of index documents which
        //can contain a term, after which the term is considered to be a stop word.
        public const float defaultMaxDocFreqPercent = 0.4f;
        private readonly LuceneVersion matchVersion;

        /// <summary>
        /// Creates a new <see cref="QueryAutoStopWordAnalyzer"/> with stopwords calculated for all
        /// indexed fields from terms with a document frequency percentage greater than
        /// <see cref="defaultMaxDocFreqPercent"/>
        /// </summary>
        /// <param name="matchVersion"> Version to be used in <see cref="StopFilter"/> </param>
        /// <param name="delegate"> <see cref="Analyzer"/> whose <see cref="TokenStream"/> will be filtered </param>
        /// <param name="indexReader"> <see cref="IndexReader"/> to identify the stopwords from </param>
        /// <exception cref="IOException"> Can be thrown while reading from the <see cref="IndexReader"/> </exception>
        public QueryAutoStopWordAnalyzer(LuceneVersion matchVersion, Analyzer @delegate, IndexReader indexReader)
            : this(matchVersion, @delegate, indexReader, defaultMaxDocFreqPercent)
        {
        }

        /// <summary>
        /// Creates a new <see cref="QueryAutoStopWordAnalyzer"/> with stopwords calculated for all
        /// indexed fields from terms with a document frequency greater than the given
        /// <paramref name="maxDocFreq"/>
        /// </summary>
        /// <param name="matchVersion"> Version to be used in <see cref="StopFilter"/> </param>
        /// <param name="delegate"> <see cref="Analyzer"/> whose <see cref="TokenStream"/> will be filtered </param>
        /// <param name="indexReader"> <see cref="IndexReader"/> to identify the stopwords from </param>
        /// <param name="maxDocFreq"> Document frequency terms should be above in order to be stopwords </param>
        /// <exception cref="IOException"> Can be thrown while reading from the <see cref="IndexReader"/> </exception>
        public QueryAutoStopWordAnalyzer(LuceneVersion matchVersion, Analyzer @delegate, IndexReader indexReader, int maxDocFreq)
            : this(matchVersion, @delegate, indexReader, MultiFields.GetIndexedFields(indexReader), maxDocFreq)
        {
        }

        /// <summary>
        /// Creates a new <see cref="QueryAutoStopWordAnalyzer"/> with stopwords calculated for all
        /// indexed fields from terms with a document frequency percentage greater than
        /// the given <paramref name="maxPercentDocs"/>
        /// </summary>
        /// <param name="matchVersion"> Version to be used in <see cref="StopFilter"/> </param>
        /// <param name="delegate"> <see cref="Analyzer"/> whose <see cref="TokenStream"/> will be filtered </param>
        /// <param name="indexReader"> <see cref="IndexReader"/> to identify the stopwords from </param>
        /// <param name="maxPercentDocs"> The maximum percentage (between 0.0 and 1.0) of index documents which
        ///                      contain a term, after which the word is considered to be a stop word </param>
        /// <exception cref="IOException"> Can be thrown while reading from the <see cref="IndexReader"/> </exception>
        public QueryAutoStopWordAnalyzer(LuceneVersion matchVersion, Analyzer @delegate, IndexReader indexReader, float maxPercentDocs)
            : this(matchVersion, @delegate, indexReader, MultiFields.GetIndexedFields(indexReader), maxPercentDocs)
        {
        }

        /// <summary>
        /// Creates a new <see cref="QueryAutoStopWordAnalyzer"/> with stopwords calculated for the
        /// given selection of fields from terms with a document frequency percentage
        /// greater than the given <paramref name="maxPercentDocs"/>
        /// </summary>
        /// <param name="matchVersion"> Version to be used in <see cref="StopFilter"/> </param>
        /// <param name="delegate"> <see cref="Analyzer"/> whose <see cref="TokenStream"/> will be filtered </param>
        /// <param name="indexReader"> <see cref="IndexReader"/> to identify the stopwords from </param>
        /// <param name="fields"> Selection of fields to calculate stopwords for </param>
        /// <param name="maxPercentDocs"> The maximum percentage (between 0.0 and 1.0) of index documents which
        ///                      contain a term, after which the word is considered to be a stop word </param>
        /// <exception cref="IOException"> Can be thrown while reading from the <see cref="IndexReader"/> </exception>
        public QueryAutoStopWordAnalyzer(LuceneVersion matchVersion, Analyzer @delegate, IndexReader indexReader, ICollection<string> fields, float maxPercentDocs)
            : this(matchVersion, @delegate, indexReader, fields, (int)(indexReader.NumDocs * maxPercentDocs))
        {
        }

        /// <summary>
        /// Creates a new <see cref="QueryAutoStopWordAnalyzer"/> with stopwords calculated for the
        /// given selection of fields from terms with a document frequency greater than
        /// the given <paramref name="maxDocFreq"/>
        /// </summary>
        /// <param name="matchVersion"> Version to be used in <see cref="StopFilter"/> </param>
        /// <param name="delegate"> Analyzer whose TokenStream will be filtered </param>
        /// <param name="indexReader"> <see cref="IndexReader"/> to identify the stopwords from </param>
        /// <param name="fields"> Selection of fields to calculate stopwords for </param>
        /// <param name="maxDocFreq"> Document frequency terms should be above in order to be stopwords </param>
        /// <exception cref="IOException"> Can be thrown while reading from the <see cref="IndexReader"/> </exception>
        public QueryAutoStopWordAnalyzer(LuceneVersion matchVersion, Analyzer @delegate, IndexReader indexReader, ICollection<string> fields, int maxDocFreq)
            : base(@delegate.Strategy)
        {
            this.matchVersion = matchVersion;
            this.@delegate = @delegate;

            foreach (string field in fields)
            {
                var stopWords = new JCG.HashSet<string>();
                Terms terms = MultiFields.GetTerms(indexReader, field);
                CharsRef spare = new CharsRef();
                if (terms != null)
                {
                    TermsEnum te = terms.GetEnumerator();
                    while (te.MoveNext())
                    {
                        if (te.DocFreq > maxDocFreq)
                        {
                            UnicodeUtil.UTF8toUTF16(te.Term, spare);
                            stopWords.Add(spare.ToString());
                        }
                    }
                }
                stopWordsPerField[field] = stopWords;
            }
        }

        protected override Analyzer GetWrappedAnalyzer(string fieldName)
        {
            return @delegate;
        }

        protected override TokenStreamComponents WrapComponents(string fieldName, TokenStreamComponents components)
        {
            if (!stopWordsPerField.TryGetValue(fieldName, out ISet<string> stopWords) || stopWords is null)
            {
                return components;
            }
            var stopFilter = new StopFilter(matchVersion, components.TokenStream, new CharArraySet(matchVersion, stopWords, false));
            return new TokenStreamComponents(components.Tokenizer, stopFilter);
        }

        /// <summary>
        /// Provides information on which stop words have been identified for a field
        /// </summary>
        /// <param name="fieldName"> The field for which stop words identified in "addStopWords"
        ///                  method calls will be returned </param>
        /// <returns> the stop words identified for a field </returns>
        public string[] GetStopWords(string fieldName)
        {            
            var stopWords = stopWordsPerField[fieldName];
            return stopWords != null ? stopWords.ToArray() : Arrays.Empty<string>();
        }

        /// <summary>
        /// Provides information on which stop words have been identified for all fields
        /// </summary>
        /// <returns> the stop words (as terms) </returns>
        public Term[] GetStopWords()
        {
            IList<Term> allStopWords = new JCG.List<Term>();
            foreach (string fieldName in stopWordsPerField.Keys)
            {
                ISet<string> stopWords = stopWordsPerField[fieldName];
                foreach (string text in stopWords)
                {
                    allStopWords.Add(new Term(fieldName, text));
                }
            }
            return allStopWords.ToArray();
        }
    }
}