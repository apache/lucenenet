using System;

namespace org.apache.lucene.analysis.cjk
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
	using StandardTokenizer = org.apache.lucene.analysis.standard.StandardTokenizer;
	using CharArraySet = org.apache.lucene.analysis.util.CharArraySet;
	using StopwordAnalyzerBase = org.apache.lucene.analysis.util.StopwordAnalyzerBase;
	using Version = org.apache.lucene.util.Version;

	/// <summary>
	/// An <seealso cref="Analyzer"/> that tokenizes text with <seealso cref="StandardTokenizer"/>,
	/// normalizes content with <seealso cref="CJKWidthFilter"/>, folds case with
	/// <seealso cref="LowerCaseFilter"/>, forms bigrams of CJK with <seealso cref="CJKBigramFilter"/>,
	/// and filters stopwords with <seealso cref="StopFilter"/>
	/// </summary>
	public sealed class CJKAnalyzer : StopwordAnalyzerBase
	{
	  /// <summary>
	  /// File containing default CJK stopwords.
	  /// <p/>
	  /// Currently it contains some common English words that are not usually
	  /// useful for searching and some double-byte interpunctions.
	  /// </summary>
	  public const string DEFAULT_STOPWORD_FILE = "stopwords.txt";

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

		static DefaultSetHolder()
		{
		  try
		  {
			DEFAULT_STOP_SET = loadStopwordSet(false, typeof(CJKAnalyzer), DEFAULT_STOPWORD_FILE, "#");
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
	  /// Builds an analyzer which removes words in <seealso cref="#getDefaultStopSet()"/>.
	  /// </summary>
	  public CJKAnalyzer(Version matchVersion) : this(matchVersion, DefaultSetHolder.DEFAULT_STOP_SET)
	  {
	  }

	  /// <summary>
	  /// Builds an analyzer with the given stop words
	  /// </summary>
	  /// <param name="matchVersion">
	  ///          lucene compatibility version </param>
	  /// <param name="stopwords">
	  ///          a stopword set </param>
	  public CJKAnalyzer(Version matchVersion, CharArraySet stopwords) : base(matchVersion, stopwords)
	  {
	  }

	  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
	  {
		if (matchVersion.onOrAfter(Version.LUCENE_36))
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.analysis.Tokenizer source = new org.apache.lucene.analysis.standard.StandardTokenizer(matchVersion, reader);
		  Tokenizer source = new StandardTokenizer(matchVersion, reader);
		  // run the widthfilter first before bigramming, it sometimes combines characters.
		  TokenStream result = new CJKWidthFilter(source);
		  result = new LowerCaseFilter(matchVersion, result);
		  result = new CJKBigramFilter(result);
		  return new TokenStreamComponents(source, new StopFilter(matchVersion, result, stopwords));
		}
		else
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.analysis.Tokenizer source = new CJKTokenizer(reader);
		  Tokenizer source = new CJKTokenizer(reader);
		  return new TokenStreamComponents(source, new StopFilter(matchVersion, source, stopwords));
		}
	  }
	}

}