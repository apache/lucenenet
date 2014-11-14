using System;

namespace org.apache.lucene.analysis.cz
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

	using LowerCaseFilter = org.apache.lucene.analysis.core.LowerCaseFilter;
	using StopFilter = org.apache.lucene.analysis.core.StopFilter;
	using SetKeywordMarkerFilter = org.apache.lucene.analysis.miscellaneous.SetKeywordMarkerFilter;
	using StandardFilter = org.apache.lucene.analysis.standard.StandardFilter;
	using StandardTokenizer = org.apache.lucene.analysis.standard.StandardTokenizer;
	using CharArraySet = org.apache.lucene.analysis.util.CharArraySet;
	using StopwordAnalyzerBase = org.apache.lucene.analysis.util.StopwordAnalyzerBase;
	using WordlistLoader = org.apache.lucene.analysis.util.WordlistLoader;
	using IOUtils = org.apache.lucene.util.IOUtils;
	using Version = org.apache.lucene.util.Version;


	/// <summary>
	/// <seealso cref="Analyzer"/> for Czech language.
	/// <para>
	/// Supports an external list of stopwords (words that will not be indexed at
	/// all). A default set of stopwords is used unless an alternative list is
	/// specified.
	/// </para>
	/// 
	/// <a name="version"/>
	/// <para>
	/// You must specify the required <seealso cref="Version"/> compatibility when creating
	/// CzechAnalyzer:
	/// <ul>
	/// <li>As of 3.1, words are stemmed with <seealso cref="CzechStemFilter"/>
	/// <li>As of 2.9, StopFilter preserves position increments
	/// <li>As of 2.4, Tokens incorrectly identified as acronyms are corrected (see
	/// <a href="https://issues.apache.org/jira/browse/LUCENE-1068">LUCENE-1068</a>)
	/// </ul>
	/// </para>
	/// </summary>
	public sealed class CzechAnalyzer : StopwordAnalyzerBase
	{
	  /// <summary>
	  /// File containing default Czech stopwords. </summary>
	  public const string DEFAULT_STOPWORD_FILE = "stopwords.txt";

	  /// <summary>
	  /// Returns a set of default Czech-stopwords
	  /// </summary>
	  /// <returns> a set of default Czech-stopwords </returns>
	  public static CharArraySet DefaultStopSet
	  {
		  get
		  {
			return DefaultSetHolder.DEFAULT_SET;
		  }
	  }

	  private class DefaultSetHolder
	  {
		internal static readonly CharArraySet DEFAULT_SET;

		static DefaultSetHolder()
		{
		  try
		  {
			DEFAULT_SET = WordlistLoader.getWordSet(IOUtils.getDecodingReader(typeof(CzechAnalyzer), DEFAULT_STOPWORD_FILE, StandardCharsets.UTF_8), "#", Version.LUCENE_CURRENT);
		  }
		  catch (IOException)
		  {
			// default set should always be present as it is part of the
			// distribution (JAR)
			throw new Exception("Unable to load default stopword set");
		  }
		}
	  }


	  private readonly CharArraySet stemExclusionTable;

	  /// <summary>
	  /// Builds an analyzer with the default stop words (<seealso cref="#getDefaultStopSet()"/>).
	  /// </summary>
	  /// <param name="matchVersion"> Lucene version to match See
	  ///          <seealso cref="<a href="#version">above</a>"/> </param>
	  public CzechAnalyzer(Version matchVersion) : this(matchVersion, DefaultSetHolder.DEFAULT_SET)
	  {
	  }

	  /// <summary>
	  /// Builds an analyzer with the given stop words.
	  /// </summary>
	  /// <param name="matchVersion"> Lucene version to match See
	  ///          <seealso cref="<a href="#version">above</a>"/> </param>
	  /// <param name="stopwords"> a stopword set </param>
	  public CzechAnalyzer(Version matchVersion, CharArraySet stopwords) : this(matchVersion, stopwords, CharArraySet.EMPTY_SET)
	  {
	  }

	  /// <summary>
	  /// Builds an analyzer with the given stop words and a set of work to be
	  /// excluded from the <seealso cref="CzechStemFilter"/>.
	  /// </summary>
	  /// <param name="matchVersion"> Lucene version to match See
	  ///          <seealso cref="<a href="#version">above</a>"/> </param>
	  /// <param name="stopwords"> a stopword set </param>
	  /// <param name="stemExclusionTable"> a stemming exclusion set </param>
	  public CzechAnalyzer(Version matchVersion, CharArraySet stopwords, CharArraySet stemExclusionTable) : base(matchVersion, stopwords)
	  {
		this.stemExclusionTable = CharArraySet.unmodifiableSet(CharArraySet.copy(matchVersion, stemExclusionTable));
	  }

	  /// <summary>
	  /// Creates
	  /// <seealso cref="org.apache.lucene.analysis.Analyzer.TokenStreamComponents"/>
	  /// used to tokenize all the text in the provided <seealso cref="Reader"/>.
	  /// </summary>
	  /// <returns> <seealso cref="org.apache.lucene.analysis.Analyzer.TokenStreamComponents"/>
	  ///         built from a <seealso cref="StandardTokenizer"/> filtered with
	  ///         <seealso cref="StandardFilter"/>, <seealso cref="LowerCaseFilter"/>, <seealso cref="StopFilter"/>
	  ///         , and <seealso cref="CzechStemFilter"/> (only if version is >= LUCENE_31). If
	  ///         a version is >= LUCENE_31 and a stem exclusion set is provided via
	  ///         <seealso cref="#CzechAnalyzer(Version, CharArraySet, CharArraySet)"/> a
	  ///         <seealso cref="SetKeywordMarkerFilter"/> is added before
	  ///         <seealso cref="CzechStemFilter"/>. </returns>
	  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.analysis.Tokenizer source = new org.apache.lucene.analysis.standard.StandardTokenizer(matchVersion, reader);
		Tokenizer source = new StandardTokenizer(matchVersion, reader);
		TokenStream result = new StandardFilter(matchVersion, source);
		result = new LowerCaseFilter(matchVersion, result);
		result = new StopFilter(matchVersion, result, stopwords);
		if (matchVersion.onOrAfter(Version.LUCENE_31))
		{
		  if (!this.stemExclusionTable.Empty)
		  {
			result = new SetKeywordMarkerFilter(result, stemExclusionTable);
		  }
		  result = new CzechStemFilter(result);
		}
		return new TokenStreamComponents(source, result);
	  }
	}


}