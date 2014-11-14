using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Util;
using StopwordAnalyzerBase = Lucene.Net.Analysis.Util.StopwordAnalyzerBase;

namespace org.apache.lucene.analysis.standard
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

	using org.apache.lucene.analysis;
	using LowerCaseFilter = LowerCaseFilter;
	using StopAnalyzer = StopAnalyzer;
	using StopFilter = StopFilter;
	using CharArraySet = CharArraySet;
	using StopwordAnalyzerBase = StopwordAnalyzerBase;
	using WordlistLoader = WordlistLoader;
	using Version = org.apache.lucene.util.Version;


	/// <summary>
	/// Filters <seealso cref="ClassicTokenizer"/> with <seealso cref="ClassicFilter"/>, {@link
	/// LowerCaseFilter} and <seealso cref="StopFilter"/>, using a list of
	/// English stop words.
	/// 
	/// <a name="version"/>
	/// <para>You must specify the required <seealso cref="Version"/>
	/// compatibility when creating ClassicAnalyzer:
	/// <ul>
	///   <li> As of 3.1, StopFilter correctly handles Unicode 4.0
	///         supplementary characters in stopwords
	///   <li> As of 2.9, StopFilter preserves position
	///        increments
	///   <li> As of 2.4, Tokens incorrectly identified as acronyms
	///        are corrected (see <a href="https://issues.apache.org/jira/browse/LUCENE-1068">LUCENE-1068</a>)
	/// </ul>
	/// 
	/// ClassicAnalyzer was named StandardAnalyzer in Lucene versions prior to 3.1. 
	/// As of 3.1, <seealso cref="StandardAnalyzer"/> implements Unicode text segmentation,
	/// as specified by UAX#29.
	/// </para>
	/// </summary>
	public sealed class ClassicAnalyzer : StopwordAnalyzerBase
	{

	  /// <summary>
	  /// Default maximum allowed token length </summary>
	  public const int DEFAULT_MAX_TOKEN_LENGTH = 255;

	  private int maxTokenLength = DEFAULT_MAX_TOKEN_LENGTH;

	  /// <summary>
	  /// An unmodifiable set containing some common English words that are usually not
	  /// useful for searching. 
	  /// </summary>
	  public static readonly CharArraySet STOP_WORDS_SET = StopAnalyzer.ENGLISH_STOP_WORDS_SET;

	  /// <summary>
	  /// Builds an analyzer with the given stop words. </summary>
	  /// <param name="matchVersion"> Lucene version to match See {@link
	  /// <a href="#version">above</a>} </param>
	  /// <param name="stopWords"> stop words  </param>
	  public ClassicAnalyzer(Version matchVersion, CharArraySet stopWords) : base(matchVersion, stopWords)
	  {
	  }

	  /// <summary>
	  /// Builds an analyzer with the default stop words ({@link
	  /// #STOP_WORDS_SET}). </summary>
	  /// <param name="matchVersion"> Lucene version to match See {@link
	  /// <a href="#version">above</a>} </param>
	  public ClassicAnalyzer(Version matchVersion) : this(matchVersion, STOP_WORDS_SET)
	  {
	  }

	  /// <summary>
	  /// Builds an analyzer with the stop words from the given reader. </summary>
	  /// <seealso cref= WordlistLoader#getWordSet(Reader, Version) </seealso>
	  /// <param name="matchVersion"> Lucene version to match See {@link
	  /// <a href="#version">above</a>} </param>
	  /// <param name="stopwords"> Reader to read stop words from  </param>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public ClassicAnalyzer(org.apache.lucene.util.Version matchVersion, java.io.Reader stopwords) throws java.io.IOException
	  public ClassicAnalyzer(Version matchVersion, Reader stopwords) : this(matchVersion, loadStopwordSet(stopwords, matchVersion))
	  {
	  }

	  /// <summary>
	  /// Set maximum allowed token length.  If a token is seen
	  /// that exceeds this length then it is discarded.  This
	  /// setting only takes effect the next time tokenStream or
	  /// tokenStream is called.
	  /// </summary>
	  public int MaxTokenLength
	  {
		  set
		  {
			maxTokenLength = value;
		  }
		  get
		  {
			return maxTokenLength;
		  }
	  }


//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
//ORIGINAL LINE: @Override protected TokenStreamComponents createComponents(final String fieldName, final java.io.Reader reader)
	  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final ClassicTokenizer src = new ClassicTokenizer(matchVersion, reader);
		ClassicTokenizer src = new ClassicTokenizer(matchVersion, reader);
		src.MaxTokenLength = maxTokenLength;
		TokenStream tok = new ClassicFilter(src);
		tok = new LowerCaseFilter(matchVersion, tok);
		tok = new StopFilter(matchVersion, tok, stopwords);
		return new TokenStreamComponentsAnonymousInnerClassHelper(this, src, tok, reader);
	  }

	  private class TokenStreamComponentsAnonymousInnerClassHelper : TokenStreamComponents
	  {
		  private readonly ClassicAnalyzer outerInstance;

		  private Reader reader;
		  private org.apache.lucene.analysis.standard.ClassicTokenizer src;

		  public TokenStreamComponentsAnonymousInnerClassHelper(ClassicAnalyzer outerInstance, org.apache.lucene.analysis.standard.ClassicTokenizer src, TokenStream tok, Reader reader) : base(src, tok)
		  {
			  this.outerInstance = outerInstance;
			  this.reader = reader;
			  this.src = src;
		  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override protected void setReader(final java.io.Reader reader) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
		  protected internal override Reader Reader
		  {
			  set
			  {
				src.MaxTokenLength = outerInstance.maxTokenLength;
				base.Reader = value;
			  }
		  }
	  }
	}

}