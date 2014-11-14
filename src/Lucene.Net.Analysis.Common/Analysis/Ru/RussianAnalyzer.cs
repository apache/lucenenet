using System;

namespace org.apache.lucene.analysis.ru
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


	using SnowballFilter = org.apache.lucene.analysis.snowball.SnowballFilter;
	using StandardFilter = org.apache.lucene.analysis.standard.StandardFilter;
	using StandardTokenizer = org.apache.lucene.analysis.standard.StandardTokenizer;
	using CharArraySet = org.apache.lucene.analysis.util.CharArraySet;
	using StopwordAnalyzerBase = org.apache.lucene.analysis.util.StopwordAnalyzerBase;
	using WordlistLoader = org.apache.lucene.analysis.util.WordlistLoader;
	using LowerCaseFilter = org.apache.lucene.analysis.core.LowerCaseFilter;
	using StopFilter = org.apache.lucene.analysis.core.StopFilter;
	using SetKeywordMarkerFilter = org.apache.lucene.analysis.miscellaneous.SetKeywordMarkerFilter;
	using IOUtils = org.apache.lucene.util.IOUtils;
	using Version = org.apache.lucene.util.Version;

	/// <summary>
	/// <seealso cref="Analyzer"/> for Russian language. 
	/// <para>
	/// Supports an external list of stopwords (words that
	/// will not be indexed at all).
	/// A default set of stopwords is used unless an alternative list is specified.
	/// </para>
	/// <a name="version"/>
	/// <para>You must specify the required <seealso cref="Version"/>
	/// compatibility when creating RussianAnalyzer:
	/// <ul>
	///   <li> As of 3.1, StandardTokenizer is used, Snowball stemming is done with
	///        SnowballFilter, and Snowball stopwords are used by default.
	/// </ul>
	/// </para>
	/// </summary>
	public sealed class RussianAnalyzer : StopwordAnalyzerBase
	{
		/// <summary>
		/// List of typical Russian stopwords. (for backwards compatibility) </summary>
		/// @deprecated (3.1) Remove this for LUCENE 5.0 
		[Obsolete("(3.1) Remove this for LUCENE 5.0")]
		private static readonly string[] RUSSIAN_STOP_WORDS_30 = new string[] {"а", "без", "более", "бы", "был", "была", "были", "было", "быть", "в", "вам", "вас", "весь", "во", "вот", "все", "всего", "всех", "вы", "где", "да", "даже", "для", "до", "его", "ее", "ей", "ею", "если", "есть", "еще", "же", "за", "здесь", "и", "из", "или", "им", "их", "к", "как", "ко", "когда", "кто", "ли", "либо", "мне", "может", "мы", "на", "надо", "наш", "не", "него", "нее", "нет", "ни", "них", "но", "ну", "о", "об", "однако", "он", "она", "они", "оно", "от", "очень", "по", "под", "при", "с", "со", "так", "также", "такой", "там", "те", "тем", "то", "того", "тоже", "той", "только", "том", "ты", "у", "уже", "хотя", "чего", "чей", "чем", "что", "чтобы", "чье", "чья", "эта", "эти", "это", "я"};

		/// <summary>
		/// File containing default Russian stopwords. </summary>
		public const string DEFAULT_STOPWORD_FILE = "russian_stop.txt";

		private class DefaultSetHolder
		{
		  /// @deprecated (3.1) remove this for Lucene 5.0 
		  [Obsolete("(3.1) remove this for Lucene 5.0")]
		  internal static readonly CharArraySet DEFAULT_STOP_SET_30 = CharArraySet.unmodifiableSet(new CharArraySet(Version.LUCENE_CURRENT, Arrays.asList(RUSSIAN_STOP_WORDS_30), false));
		  internal static readonly CharArraySet DEFAULT_STOP_SET;

		  static DefaultSetHolder()
		  {
			try
			{
			  DEFAULT_STOP_SET = WordlistLoader.getSnowballWordSet(IOUtils.getDecodingReader(typeof(SnowballFilter), DEFAULT_STOPWORD_FILE, StandardCharsets.UTF_8), Version.LUCENE_CURRENT);
			}
			catch (IOException ex)
			{
			  // default set should always be present as it is part of the
			  // distribution (JAR)
			  throw new Exception("Unable to load default stopword set", ex);
			}
		  }
		}

		private readonly CharArraySet stemExclusionSet;

		/// <summary>
		/// Returns an unmodifiable instance of the default stop-words set.
		/// </summary>
		/// <returns> an unmodifiable instance of the default stop-words set. </returns>
		public static CharArraySet DefaultStopSet
		{
			get
			{
			  return DefaultSetHolder.DEFAULT_STOP_SET;
			}
		}

		public RussianAnalyzer(Version matchVersion) : this(matchVersion, matchVersion.onOrAfter(Version.LUCENE_31) ? DefaultSetHolder.DEFAULT_STOP_SET : DefaultSetHolder.DEFAULT_STOP_SET_30)
		{
		}

		/// <summary>
		/// Builds an analyzer with the given stop words
		/// </summary>
		/// <param name="matchVersion">
		///          lucene compatibility version </param>
		/// <param name="stopwords">
		///          a stopword set </param>
		public RussianAnalyzer(Version matchVersion, CharArraySet stopwords) : this(matchVersion, stopwords, CharArraySet.EMPTY_SET)
		{
		}

		/// <summary>
		/// Builds an analyzer with the given stop words
		/// </summary>
		/// <param name="matchVersion">
		///          lucene compatibility version </param>
		/// <param name="stopwords">
		///          a stopword set </param>
		/// <param name="stemExclusionSet"> a set of words not to be stemmed </param>
		public RussianAnalyzer(Version matchVersion, CharArraySet stopwords, CharArraySet stemExclusionSet) : base(matchVersion, stopwords)
		{
		  this.stemExclusionSet = CharArraySet.unmodifiableSet(CharArraySet.copy(matchVersion, stemExclusionSet));
		}

	  /// <summary>
	  /// Creates
	  /// <seealso cref="org.apache.lucene.analysis.Analyzer.TokenStreamComponents"/>
	  /// used to tokenize all the text in the provided <seealso cref="Reader"/>.
	  /// </summary>
	  /// <returns> <seealso cref="org.apache.lucene.analysis.Analyzer.TokenStreamComponents"/>
	  ///         built from a <seealso cref="StandardTokenizer"/> filtered with
	  ///         <seealso cref="StandardFilter"/>, <seealso cref="LowerCaseFilter"/>, <seealso cref="StopFilter"/>
	  ///         , <seealso cref="SetKeywordMarkerFilter"/> if a stem exclusion set is
	  ///         provided, and <seealso cref="SnowballFilter"/> </returns>
		protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		{
		  if (matchVersion.onOrAfter(Version.LUCENE_31))
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.analysis.Tokenizer source = new org.apache.lucene.analysis.standard.StandardTokenizer(matchVersion, reader);
			Tokenizer source = new StandardTokenizer(matchVersion, reader);
			TokenStream result = new StandardFilter(matchVersion, source);
			result = new LowerCaseFilter(matchVersion, result);
			result = new StopFilter(matchVersion, result, stopwords);
			if (!stemExclusionSet.Empty)
			{
				result = new SetKeywordMarkerFilter(result, stemExclusionSet);
			}
			result = new SnowballFilter(result, new org.tartarus.snowball.ext.RussianStemmer());
			return new TokenStreamComponents(source, result);
		  }
		  else
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.analysis.Tokenizer source = new RussianLetterTokenizer(matchVersion, reader);
			Tokenizer source = new RussianLetterTokenizer(matchVersion, reader);
			TokenStream result = new LowerCaseFilter(matchVersion, source);
			result = new StopFilter(matchVersion, result, stopwords);
			if (!stemExclusionSet.Empty)
			{
				result = new SetKeywordMarkerFilter(result, stemExclusionSet);
			}
			result = new SnowballFilter(result, new org.tartarus.snowball.ext.RussianStemmer());
			return new TokenStreamComponents(source, result);
		  }
		}
	}

}