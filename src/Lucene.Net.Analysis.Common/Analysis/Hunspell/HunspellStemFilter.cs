using System.Collections.Generic;

namespace org.apache.lucene.analysis.hunspell
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


	using CharTermAttribute = org.apache.lucene.analysis.tokenattributes.CharTermAttribute;
	using KeywordAttribute = org.apache.lucene.analysis.tokenattributes.KeywordAttribute;
	using PositionIncrementAttribute = org.apache.lucene.analysis.tokenattributes.PositionIncrementAttribute;
	using CharsRef = org.apache.lucene.util.CharsRef;

	/// <summary>
	/// TokenFilter that uses hunspell affix rules and words to stem tokens.  Since hunspell supports a word having multiple
	/// stems, this filter can emit multiple tokens for each consumed token
	/// 
	/// <para>
	/// Note: This filter is aware of the <seealso cref="KeywordAttribute"/>. To prevent
	/// certain terms from being passed to the stemmer
	/// <seealso cref="KeywordAttribute#isKeyword()"/> should be set to <code>true</code>
	/// in a previous <seealso cref="TokenStream"/>.
	/// 
	/// Note: For including the original term as well as the stemmed version, see
	/// <seealso cref="org.apache.lucene.analysis.miscellaneous.KeywordRepeatFilterFactory"/>
	/// </para>
	/// 
	/// @lucene.experimental
	/// </summary>
	public sealed class HunspellStemFilter : TokenFilter
	{

	  private readonly CharTermAttribute termAtt = addAttribute(typeof(CharTermAttribute));
	  private readonly PositionIncrementAttribute posIncAtt = addAttribute(typeof(PositionIncrementAttribute));
	  private readonly KeywordAttribute keywordAtt = addAttribute(typeof(KeywordAttribute));
	  private readonly Stemmer stemmer;

	  private IList<CharsRef> buffer;
	  private State savedState;

	  private readonly bool dedup;
	  private readonly bool longestOnly;

	  /// <summary>
	  /// Create a <seealso cref="HunspellStemFilter"/> outputting all possible stems. </summary>
	  ///  <seealso cref= #HunspellStemFilter(TokenStream, Dictionary, boolean)  </seealso>
	  public HunspellStemFilter(TokenStream input, Dictionary dictionary) : this(input, dictionary, true)
	  {
	  }

	  /// <summary>
	  /// Create a <seealso cref="HunspellStemFilter"/> outputting all possible stems. </summary>
	  ///  <seealso cref= #HunspellStemFilter(TokenStream, Dictionary, boolean, boolean)  </seealso>
	  public HunspellStemFilter(TokenStream input, Dictionary dictionary, bool dedup) : this(input, dictionary, dedup, false)
	  {
	  }

	  /// <summary>
	  /// Creates a new HunspellStemFilter that will stem tokens from the given TokenStream using affix rules in the provided
	  /// Dictionary
	  /// </summary>
	  /// <param name="input"> TokenStream whose tokens will be stemmed </param>
	  /// <param name="dictionary"> HunspellDictionary containing the affix rules and words that will be used to stem the tokens </param>
	  /// <param name="longestOnly"> true if only the longest term should be output. </param>
	  public HunspellStemFilter(TokenStream input, Dictionary dictionary, bool dedup, bool longestOnly) : base(input)
	  {
		this.dedup = dedup && longestOnly == false; // don't waste time deduping if longestOnly is set
		this.stemmer = new Stemmer(dictionary);
		this.longestOnly = longestOnly;
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public boolean incrementToken() throws java.io.IOException
	  public override bool incrementToken()
	  {
		if (buffer != null && buffer.Count > 0)
		{
		  CharsRef nextStem = buffer.Remove(0);
		  restoreState(savedState);
		  posIncAtt.PositionIncrement = 0;
		  termAtt.setEmpty().append(nextStem);
		  return true;
		}

		if (!input.incrementToken())
		{
		  return false;
		}

		if (keywordAtt.Keyword)
		{
		  return true;
		}

		buffer = dedup ? stemmer.uniqueStems(termAtt.buffer(), termAtt.length()) : stemmer.stem(termAtt.buffer(), termAtt.length());

		if (buffer.Count == 0) // we do not know this word, return it unchanged
		{
		  return true;
		}

		if (longestOnly && buffer.Count > 1)
		{
		  buffer.Sort(lengthComparator);
		}

		CharsRef stem = buffer.Remove(0);
		termAtt.setEmpty().append(stem);

		if (longestOnly)
		{
		  buffer.Clear();
		}
		else
		{
		  if (buffer.Count > 0)
		  {
			savedState = captureState();
		  }
		}

		return true;
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void reset() throws java.io.IOException
	  public override void reset()
	  {
		base.reset();
		buffer = null;
	  }

	  internal static readonly IComparer<CharsRef> lengthComparator = new ComparatorAnonymousInnerClassHelper();

	  private class ComparatorAnonymousInnerClassHelper : IComparer<CharsRef>
	  {
		  public ComparatorAnonymousInnerClassHelper()
		  {
		  }

		  public virtual int Compare(CharsRef o1, CharsRef o2)
		  {
			if (o2.length == o1.length)
			{
			  // tie break on text
			  return o2.compareTo(o1);
			}
			else
			{
			  return o2.length < o1.length ? - 1 : 1;
			}
		  }
	  }
	}

}