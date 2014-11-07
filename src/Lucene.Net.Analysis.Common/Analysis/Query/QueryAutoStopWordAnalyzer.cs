using System.Collections.Generic;
using Lucene.Net.Analysis.Core;

namespace org.apache.lucene.analysis.query
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


	using StopFilter = StopFilter;
	using CharArraySet = org.apache.lucene.analysis.util.CharArraySet;
	using IndexReader = org.apache.lucene.index.IndexReader;
	using MultiFields = org.apache.lucene.index.MultiFields;
	using Term = org.apache.lucene.index.Term;
	using Terms = org.apache.lucene.index.Terms;
	using TermsEnum = org.apache.lucene.index.TermsEnum;
	using BytesRef = org.apache.lucene.util.BytesRef;
	using CharsRef = org.apache.lucene.util.CharsRef;
	using UnicodeUtil = org.apache.lucene.util.UnicodeUtil;
	using Version = org.apache.lucene.util.Version;

	/// <summary>
	/// An <seealso cref="Analyzer"/> used primarily at query time to wrap another analyzer and provide a layer of protection
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
	  private readonly IDictionary<string, HashSet<string>> stopWordsPerField = new Dictionary<string, HashSet<string>>();
	  //The default maximum percentage (40%) of index documents which
	  //can contain a term, after which the term is considered to be a stop word.
	  public const float defaultMaxDocFreqPercent = 0.4f;
	  private readonly Version matchVersion;

	  /// <summary>
	  /// Creates a new QueryAutoStopWordAnalyzer with stopwords calculated for all
	  /// indexed fields from terms with a document frequency percentage greater than
	  /// <seealso cref="#defaultMaxDocFreqPercent"/>
	  /// </summary>
	  /// <param name="matchVersion"> Version to be used in <seealso cref="StopFilter"/> </param>
	  /// <param name="delegate"> Analyzer whose TokenStream will be filtered </param>
	  /// <param name="indexReader"> IndexReader to identify the stopwords from </param>
	  /// <exception cref="IOException"> Can be thrown while reading from the IndexReader </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public QueryAutoStopWordAnalyzer(org.apache.lucene.util.Version matchVersion, org.apache.lucene.analysis.Analyzer delegate, org.apache.lucene.index.IndexReader indexReader) throws java.io.IOException
	  public QueryAutoStopWordAnalyzer(Version matchVersion, Analyzer @delegate, IndexReader indexReader) : this(matchVersion, @delegate, indexReader, defaultMaxDocFreqPercent)
	  {
	  }

	  /// <summary>
	  /// Creates a new QueryAutoStopWordAnalyzer with stopwords calculated for all
	  /// indexed fields from terms with a document frequency greater than the given
	  /// maxDocFreq
	  /// </summary>
	  /// <param name="matchVersion"> Version to be used in <seealso cref="StopFilter"/> </param>
	  /// <param name="delegate"> Analyzer whose TokenStream will be filtered </param>
	  /// <param name="indexReader"> IndexReader to identify the stopwords from </param>
	  /// <param name="maxDocFreq"> Document frequency terms should be above in order to be stopwords </param>
	  /// <exception cref="IOException"> Can be thrown while reading from the IndexReader </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public QueryAutoStopWordAnalyzer(org.apache.lucene.util.Version matchVersion, org.apache.lucene.analysis.Analyzer delegate, org.apache.lucene.index.IndexReader indexReader, int maxDocFreq) throws java.io.IOException
	  public QueryAutoStopWordAnalyzer(Version matchVersion, Analyzer @delegate, IndexReader indexReader, int maxDocFreq) : this(matchVersion, @delegate, indexReader, MultiFields.getIndexedFields(indexReader), maxDocFreq)
	  {
	  }

	  /// <summary>
	  /// Creates a new QueryAutoStopWordAnalyzer with stopwords calculated for all
	  /// indexed fields from terms with a document frequency percentage greater than
	  /// the given maxPercentDocs
	  /// </summary>
	  /// <param name="matchVersion"> Version to be used in <seealso cref="StopFilter"/> </param>
	  /// <param name="delegate"> Analyzer whose TokenStream will be filtered </param>
	  /// <param name="indexReader"> IndexReader to identify the stopwords from </param>
	  /// <param name="maxPercentDocs"> The maximum percentage (between 0.0 and 1.0) of index documents which
	  ///                      contain a term, after which the word is considered to be a stop word </param>
	  /// <exception cref="IOException"> Can be thrown while reading from the IndexReader </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public QueryAutoStopWordAnalyzer(org.apache.lucene.util.Version matchVersion, org.apache.lucene.analysis.Analyzer delegate, org.apache.lucene.index.IndexReader indexReader, float maxPercentDocs) throws java.io.IOException
	  public QueryAutoStopWordAnalyzer(Version matchVersion, Analyzer @delegate, IndexReader indexReader, float maxPercentDocs) : this(matchVersion, @delegate, indexReader, MultiFields.getIndexedFields(indexReader), maxPercentDocs)
	  {
	  }

	  /// <summary>
	  /// Creates a new QueryAutoStopWordAnalyzer with stopwords calculated for the
	  /// given selection of fields from terms with a document frequency percentage
	  /// greater than the given maxPercentDocs
	  /// </summary>
	  /// <param name="matchVersion"> Version to be used in <seealso cref="StopFilter"/> </param>
	  /// <param name="delegate"> Analyzer whose TokenStream will be filtered </param>
	  /// <param name="indexReader"> IndexReader to identify the stopwords from </param>
	  /// <param name="fields"> Selection of fields to calculate stopwords for </param>
	  /// <param name="maxPercentDocs"> The maximum percentage (between 0.0 and 1.0) of index documents which
	  ///                      contain a term, after which the word is considered to be a stop word </param>
	  /// <exception cref="IOException"> Can be thrown while reading from the IndexReader </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public QueryAutoStopWordAnalyzer(org.apache.lucene.util.Version matchVersion, org.apache.lucene.analysis.Analyzer delegate, org.apache.lucene.index.IndexReader indexReader, Collection<String> fields, float maxPercentDocs) throws java.io.IOException
	  public QueryAutoStopWordAnalyzer(Version matchVersion, Analyzer @delegate, IndexReader indexReader, ICollection<string> fields, float maxPercentDocs) : this(matchVersion, @delegate, indexReader, fields, (int)(indexReader.numDocs() * maxPercentDocs))
	  {
	  }

	  /// <summary>
	  /// Creates a new QueryAutoStopWordAnalyzer with stopwords calculated for the
	  /// given selection of fields from terms with a document frequency greater than
	  /// the given maxDocFreq
	  /// </summary>
	  /// <param name="matchVersion"> Version to be used in <seealso cref="StopFilter"/> </param>
	  /// <param name="delegate"> Analyzer whose TokenStream will be filtered </param>
	  /// <param name="indexReader"> IndexReader to identify the stopwords from </param>
	  /// <param name="fields"> Selection of fields to calculate stopwords for </param>
	  /// <param name="maxDocFreq"> Document frequency terms should be above in order to be stopwords </param>
	  /// <exception cref="IOException"> Can be thrown while reading from the IndexReader </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public QueryAutoStopWordAnalyzer(org.apache.lucene.util.Version matchVersion, org.apache.lucene.analysis.Analyzer delegate, org.apache.lucene.index.IndexReader indexReader, Collection<String> fields, int maxDocFreq) throws java.io.IOException
	  public QueryAutoStopWordAnalyzer(Version matchVersion, Analyzer @delegate, IndexReader indexReader, ICollection<string> fields, int maxDocFreq) : base(@delegate.ReuseStrategy)
	  {
		this.matchVersion = matchVersion;
		this.@delegate = @delegate;

		foreach (string field in fields)
		{
		  HashSet<string> stopWords = new HashSet<string>();
		  Terms terms = MultiFields.getTerms(indexReader, field);
		  CharsRef spare = new CharsRef();
		  if (terms != null)
		  {
			TermsEnum te = terms.iterator(null);
			BytesRef text;
			while ((text = te.next()) != null)
			{
			  if (te.docFreq() > maxDocFreq)
			  {
				UnicodeUtil.UTF8toUTF16(text, spare);
				stopWords.Add(spare.ToString());
			  }
			}
		  }
		  stopWordsPerField[field] = stopWords;
		}
	  }

	  protected internal override Analyzer getWrappedAnalyzer(string fieldName)
	  {
		return @delegate;
	  }

	  protected internal override TokenStreamComponents wrapComponents(string fieldName, TokenStreamComponents components)
	  {
		HashSet<string> stopWords = stopWordsPerField[fieldName];
		if (stopWords == null)
		{
		  return components;
		}
		StopFilter stopFilter = new StopFilter(matchVersion, components.TokenStream, new CharArraySet(matchVersion, stopWords, false));
		return new TokenStreamComponents(components.Tokenizer, stopFilter);
	  }

	  /// <summary>
	  /// Provides information on which stop words have been identified for a field
	  /// </summary>
	  /// <param name="fieldName"> The field for which stop words identified in "addStopWords"
	  ///                  method calls will be returned </param>
	  /// <returns> the stop words identified for a field </returns>
	  public string[] getStopWords(string fieldName)
	  {
		HashSet<string> stopWords = stopWordsPerField[fieldName];
		return stopWords != null ? stopWords.toArray(new string[stopWords.Count]) : new string[0];
	  }

	  /// <summary>
	  /// Provides information on which stop words have been identified for all fields
	  /// </summary>
	  /// <returns> the stop words (as terms) </returns>
	  public Term[] StopWords
	  {
		  get
		  {
			IList<Term> allStopWords = new List<Term>();
			foreach (string fieldName in stopWordsPerField.Keys)
			{
			  HashSet<string> stopWords = stopWordsPerField[fieldName];
			  foreach (string text in stopWords)
			  {
				allStopWords.Add(new Term(fieldName, text));
			  }
			}
			return allStopWords.ToArray();
		  }
	  }

	}

}