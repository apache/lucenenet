using System;

namespace org.apache.lucene.analysis.th
{

	/// <summary>
	/// Copyright 2006 The Apache Software Foundation
	/// 
	/// Licensed under the Apache License, Version 2.0 (the "License");
	/// you may not use this file except in compliance with the License.
	/// You may obtain a copy of the License at
	/// 
	///     http://www.apache.org/licenses/LICENSE-2.0
	/// 
	/// Unless required by applicable law or agreed to in writing, software
	/// distributed under the License is distributed on an "AS IS" BASIS,
	/// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	/// See the License for the specific language governing permissions and
	/// limitations under the License.
	/// </summary>


	using LowerCaseFilter = org.apache.lucene.analysis.core.LowerCaseFilter;
	using StopAnalyzer = org.apache.lucene.analysis.core.StopAnalyzer;
	using StopFilter = org.apache.lucene.analysis.core.StopFilter;
	using StandardFilter = org.apache.lucene.analysis.standard.StandardFilter;
	using StandardTokenizer = org.apache.lucene.analysis.standard.StandardTokenizer;
	using CharArraySet = org.apache.lucene.analysis.util.CharArraySet;
	using StopwordAnalyzerBase = org.apache.lucene.analysis.util.StopwordAnalyzerBase;
	using Version = org.apache.lucene.util.Version;

	/// <summary>
	/// <seealso cref="Analyzer"/> for Thai language. It uses <seealso cref="java.text.BreakIterator"/> to break words.
	/// <para>
	/// <a name="version"/>
	/// </para>
	/// <para>You must specify the required <seealso cref="Version"/>
	/// compatibility when creating ThaiAnalyzer:
	/// <ul>
	///   <li> As of 3.6, a set of Thai stopwords is used by default
	/// </ul>
	/// </para>
	/// </summary>
	public sealed class ThaiAnalyzer : StopwordAnalyzerBase
	{

	  /// <summary>
	  /// File containing default Thai stopwords. </summary>
	  public const string DEFAULT_STOPWORD_FILE = "stopwords.txt";
	  /// <summary>
	  /// The comment character in the stopwords file.  
	  /// All lines prefixed with this will be ignored.
	  /// </summary>
	  private const string STOPWORDS_COMMENT = "#";

	  /// <summary>
	  /// Returns an unmodifiable instance of the default stop words set. </summary>
	  /// <returns> default stop words set. </returns>
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
			DEFAULT_STOP_SET = loadStopwordSet(false, typeof(ThaiAnalyzer), DEFAULT_STOPWORD_FILE, STOPWORDS_COMMENT);
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
	  /// Builds an analyzer with the default stop words.
	  /// </summary>
	  /// <param name="matchVersion"> lucene compatibility version </param>
	  public ThaiAnalyzer(Version matchVersion) : this(matchVersion, matchVersion.onOrAfter(Version.LUCENE_36) ? DefaultSetHolder.DEFAULT_STOP_SET : StopAnalyzer.ENGLISH_STOP_WORDS_SET)
	  {
	  }

	  /// <summary>
	  /// Builds an analyzer with the given stop words.
	  /// </summary>
	  /// <param name="matchVersion"> lucene compatibility version </param>
	  /// <param name="stopwords"> a stopword set </param>
	  public ThaiAnalyzer(Version matchVersion, CharArraySet stopwords) : base(matchVersion, stopwords)
	  {
	  }

	  /// <summary>
	  /// Creates
	  /// <seealso cref="org.apache.lucene.analysis.Analyzer.TokenStreamComponents"/>
	  /// used to tokenize all the text in the provided <seealso cref="Reader"/>.
	  /// </summary>
	  /// <returns> <seealso cref="org.apache.lucene.analysis.Analyzer.TokenStreamComponents"/>
	  ///         built from a <seealso cref="StandardTokenizer"/> filtered with
	  ///         <seealso cref="StandardFilter"/>, <seealso cref="LowerCaseFilter"/>, <seealso cref="ThaiWordFilter"/>, and
	  ///         <seealso cref="StopFilter"/> </returns>
	  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
	  {
		if (matchVersion.onOrAfter(Version.LUCENE_48))
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.analysis.Tokenizer source = new ThaiTokenizer(reader);
		  Tokenizer source = new ThaiTokenizer(reader);
		  TokenStream result = new LowerCaseFilter(matchVersion, source);
		  result = new StopFilter(matchVersion, result, stopwords);
		  return new TokenStreamComponents(source, result);
		}
		else
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.analysis.Tokenizer source = new org.apache.lucene.analysis.standard.StandardTokenizer(matchVersion, reader);
		  Tokenizer source = new StandardTokenizer(matchVersion, reader);
		  TokenStream result = new StandardFilter(matchVersion, source);
		  if (matchVersion.onOrAfter(Version.LUCENE_31))
		  {
			result = new LowerCaseFilter(matchVersion, result);
		  }
		  result = new ThaiWordFilter(matchVersion, result);
		  return new TokenStreamComponents(source, new StopFilter(matchVersion, result, stopwords));
		}
	  }
	}

}