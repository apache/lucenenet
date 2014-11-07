using System;

namespace org.apache.lucene.analysis.hi
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


	using SetKeywordMarkerFilter = org.apache.lucene.analysis.miscellaneous.SetKeywordMarkerFilter;
	using StandardTokenizer = org.apache.lucene.analysis.standard.StandardTokenizer;
	using CharArraySet = org.apache.lucene.analysis.util.CharArraySet;
	using StopwordAnalyzerBase = org.apache.lucene.analysis.util.StopwordAnalyzerBase;
	using LowerCaseFilter = org.apache.lucene.analysis.core.LowerCaseFilter;
	using StopFilter = org.apache.lucene.analysis.core.StopFilter;
	using IndicNormalizationFilter = org.apache.lucene.analysis.@in.IndicNormalizationFilter;
	using IndicTokenizer = org.apache.lucene.analysis.@in.IndicTokenizer;
	using Version = org.apache.lucene.util.Version;

	/// <summary>
	/// Analyzer for Hindi.
	/// <para>
	/// <a name="version"/>
	/// </para>
	/// <para>You must specify the required <seealso cref="Version"/>
	/// compatibility when creating HindiAnalyzer:
	/// <ul>
	///   <li> As of 3.6, StandardTokenizer is used for tokenization
	/// </ul>
	/// </para>
	/// </summary>
	public sealed class HindiAnalyzer : StopwordAnalyzerBase
	{
	  private readonly CharArraySet stemExclusionSet;

	  /// <summary>
	  /// File containing default Hindi stopwords.
	  /// 
	  /// Default stopword list is from http://members.unine.ch/jacques.savoy/clef/index.html
	  /// The stopword list is BSD-Licensed.
	  /// </summary>
	  public const string DEFAULT_STOPWORD_FILE = "stopwords.txt";
	  private const string STOPWORDS_COMMENT = "#";

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

	  /// <summary>
	  /// Atomically loads the DEFAULT_STOP_SET in a lazy fashion once the outer class 
	  /// accesses the static final set the first time.;
	  /// </summary>
	  private class DefaultSetHolder
	  {
		internal static readonly CharArraySet DEFAULT_STOP_SET;

		static DefaultSetHolder()
		{
		  try
		  {
			DEFAULT_STOP_SET = loadStopwordSet(false, typeof(HindiAnalyzer), DEFAULT_STOPWORD_FILE, STOPWORDS_COMMENT);
		  }
		  catch (IOException)
		  {
			// default set should always be present as it is part of the
			// distribution (JAR)
			throw new Exception("Unable to load default stopword set");
		  }
		}
	  }

	  /// <summary>
	  /// Builds an analyzer with the given stop words
	  /// </summary>
	  /// <param name="version"> lucene compatibility version </param>
	  /// <param name="stopwords"> a stopword set </param>
	  /// <param name="stemExclusionSet"> a stemming exclusion set </param>
	  public HindiAnalyzer(Version version, CharArraySet stopwords, CharArraySet stemExclusionSet) : base(version, stopwords)
	  {
		this.stemExclusionSet = CharArraySet.unmodifiableSet(CharArraySet.copy(matchVersion, stemExclusionSet));
	  }

	  /// <summary>
	  /// Builds an analyzer with the given stop words 
	  /// </summary>
	  /// <param name="version"> lucene compatibility version </param>
	  /// <param name="stopwords"> a stopword set </param>
	  public HindiAnalyzer(Version version, CharArraySet stopwords) : this(version, stopwords, CharArraySet.EMPTY_SET)
	  {
	  }

	  /// <summary>
	  /// Builds an analyzer with the default stop words:
	  /// <seealso cref="#DEFAULT_STOPWORD_FILE"/>.
	  /// </summary>
	  public HindiAnalyzer(Version version) : this(version, DefaultSetHolder.DEFAULT_STOP_SET)
	  {
	  }

	  /// <summary>
	  /// Creates
	  /// <seealso cref="org.apache.lucene.analysis.Analyzer.TokenStreamComponents"/>
	  /// used to tokenize all the text in the provided <seealso cref="Reader"/>.
	  /// </summary>
	  /// <returns> <seealso cref="org.apache.lucene.analysis.Analyzer.TokenStreamComponents"/>
	  ///         built from a <seealso cref="StandardTokenizer"/> filtered with
	  ///         <seealso cref="LowerCaseFilter"/>, <seealso cref="IndicNormalizationFilter"/>,
	  ///         <seealso cref="HindiNormalizationFilter"/>, <seealso cref="SetKeywordMarkerFilter"/>
	  ///         if a stem exclusion set is provided, <seealso cref="HindiStemFilter"/>, and
	  ///         Hindi Stop words </returns>
	  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.analysis.Tokenizer source;
		Tokenizer source;
		if (matchVersion.onOrAfter(Version.LUCENE_36))
		{
		  source = new StandardTokenizer(matchVersion, reader);
		}
		else
		{
		  source = new IndicTokenizer(matchVersion, reader);
		}
		TokenStream result = new LowerCaseFilter(matchVersion, source);
		if (!stemExclusionSet.Empty)
		{
		  result = new SetKeywordMarkerFilter(result, stemExclusionSet);
		}
		result = new IndicNormalizationFilter(result);
		result = new HindiNormalizationFilter(result);
		result = new StopFilter(matchVersion, result, stopwords);
		result = new HindiStemFilter(result);
		return new TokenStreamComponents(source, result);
	  }
	}

}