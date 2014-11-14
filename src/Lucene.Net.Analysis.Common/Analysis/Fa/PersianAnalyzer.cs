using System;

namespace org.apache.lucene.analysis.fa
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


	using ArabicLetterTokenizer = org.apache.lucene.analysis.ar.ArabicLetterTokenizer;
	using ArabicNormalizationFilter = org.apache.lucene.analysis.ar.ArabicNormalizationFilter;
	using LowerCaseFilter = org.apache.lucene.analysis.core.LowerCaseFilter;
	using StopFilter = org.apache.lucene.analysis.core.StopFilter;
	using StandardTokenizer = org.apache.lucene.analysis.standard.StandardTokenizer;
	using CharArraySet = org.apache.lucene.analysis.util.CharArraySet;
	using StopwordAnalyzerBase = org.apache.lucene.analysis.util.StopwordAnalyzerBase;
	using Version = org.apache.lucene.util.Version;

	/// <summary>
	/// <seealso cref="Analyzer"/> for Persian.
	/// <para>
	/// This Analyzer uses <seealso cref="PersianCharFilter"/> which implies tokenizing around
	/// zero-width non-joiner in addition to whitespace. Some persian-specific variant forms (such as farsi
	/// yeh and keheh) are standardized. "Stemming" is accomplished via stopwords.
	/// </para>
	/// </summary>
	public sealed class PersianAnalyzer : StopwordAnalyzerBase
	{

	  /// <summary>
	  /// File containing default Persian stopwords.
	  /// 
	  /// Default stopword list is from
	  /// http://members.unine.ch/jacques.savoy/clef/index.html The stopword list is
	  /// BSD-Licensed.
	  /// 
	  /// </summary>
	  public const string DEFAULT_STOPWORD_FILE = "stopwords.txt";

	  /// <summary>
	  /// The comment character in the stopwords file. All lines prefixed with this
	  /// will be ignored
	  /// </summary>
	  public const string STOPWORDS_COMMENT = "#";

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
			DEFAULT_STOP_SET = loadStopwordSet(false, typeof(PersianAnalyzer), DEFAULT_STOPWORD_FILE, STOPWORDS_COMMENT);
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
	  /// Builds an analyzer with the default stop words:
	  /// <seealso cref="#DEFAULT_STOPWORD_FILE"/>.
	  /// </summary>
	  public PersianAnalyzer(Version matchVersion) : this(matchVersion, DefaultSetHolder.DEFAULT_STOP_SET)
	  {
	  }

	  /// <summary>
	  /// Builds an analyzer with the given stop words 
	  /// </summary>
	  /// <param name="matchVersion">
	  ///          lucene compatibility version </param>
	  /// <param name="stopwords">
	  ///          a stopword set </param>
	  public PersianAnalyzer(Version matchVersion, CharArraySet stopwords) : base(matchVersion, stopwords)
	  {
	  }

	  /// <summary>
	  /// Creates
	  /// <seealso cref="org.apache.lucene.analysis.Analyzer.TokenStreamComponents"/>
	  /// used to tokenize all the text in the provided <seealso cref="Reader"/>.
	  /// </summary>
	  /// <returns> <seealso cref="org.apache.lucene.analysis.Analyzer.TokenStreamComponents"/>
	  ///         built from a <seealso cref="StandardTokenizer"/> filtered with
	  ///         <seealso cref="LowerCaseFilter"/>, <seealso cref="ArabicNormalizationFilter"/>,
	  ///         <seealso cref="PersianNormalizationFilter"/> and Persian Stop words </returns>
	  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.analysis.Tokenizer source;
		Tokenizer source;
		if (matchVersion.onOrAfter(Version.LUCENE_31))
		{
		  source = new StandardTokenizer(matchVersion, reader);
		}
		else
		{
		  source = new ArabicLetterTokenizer(matchVersion, reader);
		}
		TokenStream result = new LowerCaseFilter(matchVersion, source);
		result = new ArabicNormalizationFilter(result);
		/* additional persian-specific normalization */
		result = new PersianNormalizationFilter(result);
		/*
		 * the order here is important: the stopword list is normalized with the
		 * above!
		 */
		return new TokenStreamComponents(source, new StopFilter(matchVersion, result, stopwords));
	  }

	  /// <summary>
	  /// Wraps the Reader with <seealso cref="PersianCharFilter"/>
	  /// </summary>
	  protected internal override Reader initReader(string fieldName, Reader reader)
	  {
		return matchVersion.onOrAfter(Version.LUCENE_31) ? new PersianCharFilter(reader) : reader;
	  }
	}

}