using System;

namespace org.apache.lucene.analysis.fr
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
	using SnowballFilter = org.apache.lucene.analysis.snowball.SnowballFilter;
	using StandardFilter = org.apache.lucene.analysis.standard.StandardFilter;
	using StandardTokenizer = org.apache.lucene.analysis.standard.StandardTokenizer;
	using StandardAnalyzer = org.apache.lucene.analysis.standard.StandardAnalyzer; // for javadoc
	using CharArraySet = org.apache.lucene.analysis.util.CharArraySet;
	using ElisionFilter = org.apache.lucene.analysis.util.ElisionFilter;
	using StopwordAnalyzerBase = org.apache.lucene.analysis.util.StopwordAnalyzerBase;
	using WordlistLoader = org.apache.lucene.analysis.util.WordlistLoader;
	using IOUtils = org.apache.lucene.util.IOUtils;
	using Version = org.apache.lucene.util.Version;


	/// <summary>
	/// <seealso cref="Analyzer"/> for French language. 
	/// <para>
	/// Supports an external list of stopwords (words that
	/// will not be indexed at all) and an external list of exclusions (word that will
	/// not be stemmed, but indexed).
	/// A default set of stopwords is used unless an alternative list is specified, but the
	/// exclusion list is empty by default.
	/// </para>
	/// 
	/// <a name="version"/>
	/// <para>You must specify the required <seealso cref="Version"/>
	/// compatibility when creating FrenchAnalyzer:
	/// <ul>
	///   <li> As of 3.6, FrenchLightStemFilter is used for less aggressive stemming.
	///   <li> As of 3.1, Snowball stemming is done with SnowballFilter, 
	///        LowerCaseFilter is used prior to StopFilter, and ElisionFilter and 
	///        Snowball stopwords are used by default.
	///   <li> As of 2.9, StopFilter preserves position
	///        increments
	/// </ul>
	/// 
	/// </para>
	/// <para><b>NOTE</b>: This class uses the same <seealso cref="Version"/>
	/// dependent settings as <seealso cref="StandardAnalyzer"/>.</para>
	/// </summary>
	public sealed class FrenchAnalyzer : StopwordAnalyzerBase
	{

	  /// <summary>
	  /// Extended list of typical French stopwords. </summary>
	  /// @deprecated (3.1) remove in Lucene 5.0 (index bw compat) 
	  [Obsolete("(3.1) remove in Lucene 5.0 (index bw compat)")]
	  private static readonly string[] FRENCH_STOP_WORDS = new string[] {"a", "afin", "ai", "ainsi", "après", "attendu", "au", "aujourd", "auquel", "aussi", "autre", "autres", "aux", "auxquelles", "auxquels", "avait", "avant", "avec", "avoir", "c", "car", "ce", "ceci", "cela", "celle", "celles", "celui", "cependant", "certain", "certaine", "certaines", "certains", "ces", "cet", "cette", "ceux", "chez", "ci", "combien", "comme", "comment", "concernant", "contre", "d", "dans", "de", "debout", "dedans", "dehors", "delà", "depuis", "derrière", "des", "désormais", "desquelles", "desquels", "dessous", "dessus", "devant", "devers", "devra", "divers", "diverse", "diverses", "doit", "donc", "dont", "du", "duquel", "durant", "dès", "elle", "elles", "en", "entre", "environ", "est", "et", "etc", "etre", "eu", "eux", "excepté", "hormis", "hors", "hélas", "hui", "il", "ils", "j", "je", "jusqu", "jusque", "l", "la", "laquelle", "le", "lequel", "les", "lesquelles", "lesquels", "leur", "leurs", "lorsque", "lui", "là", "ma", "mais", "malgré", "me", "merci", "mes", "mien", "mienne", "miennes", "miens", "moi", "moins", "mon", "moyennant", "même", "mêmes", "n", "ne", "ni", "non", "nos", "notre", "nous", "néanmoins", "nôtre", "nôtres", "on", "ont", "ou", "outre", "où", "par", "parmi", "partant", "pas", "passé", "pendant", "plein", "plus", "plusieurs", "pour", "pourquoi", "proche", "près", "puisque", "qu", "quand", "que", "quel", "quelle", "quelles", "quels", "qui", "quoi", "quoique", "revoici", "revoilà", "s", "sa", "sans", "sauf", "se", "selon", "seront", "ses", "si", "sien", "sienne", "siennes", "siens", "sinon", "soi", "soit", "son", "sont", "sous", "suivant", "sur", "ta", "te", "tes", "tien", "tienne", "tiennes", "tiens", "toi", "ton", "tous", "tout", "toute", "toutes", "tu", "un", "une", "va", "vers", "voici", "voilà", "vos", "votre", "vous", "vu", "vôtre", "vôtres", "y", "à", "ça", "ès", "été", "être", "ô"};

	  /// <summary>
	  /// File containing default French stopwords. </summary>
	  public const string DEFAULT_STOPWORD_FILE = "french_stop.txt";

	  /// <summary>
	  /// Default set of articles for ElisionFilter </summary>
	  public static readonly CharArraySet DEFAULT_ARTICLES = CharArraySet.unmodifiableSet(new CharArraySet(Version.LUCENE_CURRENT, Arrays.asList("l", "m", "t", "qu", "n", "s", "j", "d", "c", "jusqu", "quoiqu", "lorsqu", "puisqu"), true));

	  /// <summary>
	  /// Contains words that should be indexed but not stemmed.
	  /// </summary>
	  private readonly CharArraySet excltable;

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
		/// @deprecated (3.1) remove this in Lucene 5.0, index bw compat 
		[Obsolete("(3.1) remove this in Lucene 5.0, index bw compat")]
		internal static readonly CharArraySet DEFAULT_STOP_SET_30 = CharArraySet.unmodifiableSet(new CharArraySet(Version.LUCENE_CURRENT, Arrays.asList(FRENCH_STOP_WORDS), false));
		internal static readonly CharArraySet DEFAULT_STOP_SET;
		static DefaultSetHolder()
		{
		  try
		  {
			DEFAULT_STOP_SET = WordlistLoader.getSnowballWordSet(IOUtils.getDecodingReader(typeof(SnowballFilter), DEFAULT_STOPWORD_FILE, StandardCharsets.UTF_8), Version.LUCENE_CURRENT);
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
	  /// Builds an analyzer with the default stop words (<seealso cref="#getDefaultStopSet"/>).
	  /// </summary>
	  public FrenchAnalyzer(Version matchVersion) : this(matchVersion, matchVersion.onOrAfter(Version.LUCENE_31) ? DefaultSetHolder.DEFAULT_STOP_SET : DefaultSetHolder.DEFAULT_STOP_SET_30)
	  {
	  }

	  /// <summary>
	  /// Builds an analyzer with the given stop words
	  /// </summary>
	  /// <param name="matchVersion">
	  ///          lucene compatibility version </param>
	  /// <param name="stopwords">
	  ///          a stopword set </param>
	  public FrenchAnalyzer(Version matchVersion, CharArraySet stopwords) : this(matchVersion, stopwords, CharArraySet.EMPTY_SET)
	  {
	  }

	  /// <summary>
	  /// Builds an analyzer with the given stop words
	  /// </summary>
	  /// <param name="matchVersion">
	  ///          lucene compatibility version </param>
	  /// <param name="stopwords">
	  ///          a stopword set </param>
	  /// <param name="stemExclutionSet">
	  ///          a stemming exclusion set </param>
	  public FrenchAnalyzer(Version matchVersion, CharArraySet stopwords, CharArraySet stemExclutionSet) : base(matchVersion, stopwords)
	  {
		this.excltable = CharArraySet.unmodifiableSet(CharArraySet.copy(matchVersion, stemExclutionSet));
	  }

	  /// <summary>
	  /// Creates
	  /// <seealso cref="org.apache.lucene.analysis.Analyzer.TokenStreamComponents"/>
	  /// used to tokenize all the text in the provided <seealso cref="Reader"/>.
	  /// </summary>
	  /// <returns> <seealso cref="org.apache.lucene.analysis.Analyzer.TokenStreamComponents"/>
	  ///         built from a <seealso cref="StandardTokenizer"/> filtered with
	  ///         <seealso cref="StandardFilter"/>, <seealso cref="ElisionFilter"/>,
	  ///         <seealso cref="LowerCaseFilter"/>, <seealso cref="StopFilter"/>,
	  ///         <seealso cref="SetKeywordMarkerFilter"/> if a stem exclusion set is
	  ///         provided, and <seealso cref="FrenchLightStemFilter"/> </returns>
	  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
	  {
		if (matchVersion.onOrAfter(Version.LUCENE_31))
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.analysis.Tokenizer source = new org.apache.lucene.analysis.standard.StandardTokenizer(matchVersion, reader);
		  Tokenizer source = new StandardTokenizer(matchVersion, reader);
		  TokenStream result = new StandardFilter(matchVersion, source);
		  result = new ElisionFilter(result, DEFAULT_ARTICLES);
		  result = new LowerCaseFilter(matchVersion, result);
		  result = new StopFilter(matchVersion, result, stopwords);
		  if (!excltable.Empty)
		  {
			result = new SetKeywordMarkerFilter(result, excltable);
		  }
		  if (matchVersion.onOrAfter(Version.LUCENE_36))
		  {
			result = new FrenchLightStemFilter(result);
		  }
		  else
		  {
			result = new SnowballFilter(result, new org.tartarus.snowball.ext.FrenchStemmer());
		  }
		  return new TokenStreamComponents(source, result);
		}
		else
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.analysis.Tokenizer source = new org.apache.lucene.analysis.standard.StandardTokenizer(matchVersion, reader);
		  Tokenizer source = new StandardTokenizer(matchVersion, reader);
		  TokenStream result = new StandardFilter(matchVersion, source);
		  result = new StopFilter(matchVersion, result, stopwords);
		  if (!excltable.Empty)
		  {
			result = new SetKeywordMarkerFilter(result, excltable);
		  }
		  result = new FrenchStemFilter(result);
		  // Convert to lowercase after stemming!
		  return new TokenStreamComponents(source, new LowerCaseFilter(matchVersion, result));
		}
	  }
	}


}