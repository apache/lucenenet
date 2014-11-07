using System.Text;

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

namespace org.apache.lucene.analysis.commongrams
{

	using OffsetAttribute = org.apache.lucene.analysis.tokenattributes.OffsetAttribute;
	using PositionIncrementAttribute = org.apache.lucene.analysis.tokenattributes.PositionIncrementAttribute;
	using CharTermAttribute = org.apache.lucene.analysis.tokenattributes.CharTermAttribute;
	using PositionLengthAttribute = org.apache.lucene.analysis.tokenattributes.PositionLengthAttribute;
	using TypeAttribute = org.apache.lucene.analysis.tokenattributes.TypeAttribute;
	using CharArraySet = org.apache.lucene.analysis.util.CharArraySet;
	using Version = org.apache.lucene.util.Version;

	/*
	 * TODO: Consider implementing https://issues.apache.org/jira/browse/LUCENE-1688 changes to stop list and associated constructors 
	 */

	/// <summary>
	/// Construct bigrams for frequently occurring terms while indexing. Single terms
	/// are still indexed too, with bigrams overlaid. This is achieved through the
	/// use of <seealso cref="PositionIncrementAttribute#setPositionIncrement(int)"/>. Bigrams have a type
	/// of <seealso cref="#GRAM_TYPE"/> Example:
	/// <ul>
	/// <li>input:"the quick brown fox"</li>
	/// <li>output:|"the","the-quick"|"brown"|"fox"|</li>
	/// <li>"the-quick" has a position increment of 0 so it is in the same position
	/// as "the" "the-quick" has a term.type() of "gram"</li>
	/// 
	/// </ul>
	/// </summary>

	/*
	 * Constructors and makeCommonSet based on similar code in StopFilter
	 */
	public sealed class CommonGramsFilter : TokenFilter
	{

	  public const string GRAM_TYPE = "gram";
	  private const char SEPARATOR = '_';

	  private readonly CharArraySet commonWords;

	  private readonly StringBuilder buffer = new StringBuilder();

	  private readonly CharTermAttribute termAttribute = addAttribute(typeof(CharTermAttribute));
	  private readonly OffsetAttribute offsetAttribute = addAttribute(typeof(OffsetAttribute));
	  private readonly TypeAttribute typeAttribute = addAttribute(typeof(TypeAttribute));
	  private readonly PositionIncrementAttribute posIncAttribute = addAttribute(typeof(PositionIncrementAttribute));
	  private readonly PositionLengthAttribute posLenAttribute = addAttribute(typeof(PositionLengthAttribute));

	  private int lastStartOffset;
	  private bool lastWasCommon;
	  private State savedState;

	  /// <summary>
	  /// Construct a token stream filtering the given input using a Set of common
	  /// words to create bigrams. Outputs both unigrams with position increment and
	  /// bigrams with position increment 0 type=gram where one or both of the words
	  /// in a potential bigram are in the set of common words .
	  /// </summary>
	  /// <param name="input"> TokenStream input in filter chain </param>
	  /// <param name="commonWords"> The set of common words. </param>
	  public CommonGramsFilter(Version matchVersion, TokenStream input, CharArraySet commonWords) : base(input)
	  {
		this.commonWords = commonWords;
	  }

	  /// <summary>
	  /// Inserts bigrams for common words into a token stream. For each input token,
	  /// output the token. If the token and/or the following token are in the list
	  /// of common words also output a bigram with position increment 0 and
	  /// type="gram"
	  /// 
	  /// TODO:Consider adding an option to not emit unigram stopwords
	  /// as in CDL XTF BigramStopFilter, CommonGramsQueryFilter would need to be
	  /// changed to work with this.
	  /// 
	  /// TODO: Consider optimizing for the case of three
	  /// commongrams i.e "man of the year" normally produces 3 bigrams: "man-of",
	  /// "of-the", "the-year" but with proper management of positions we could
	  /// eliminate the middle bigram "of-the"and save a disk seek and a whole set of
	  /// position lookups.
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public boolean incrementToken() throws java.io.IOException
	  public override bool incrementToken()
	  {
		// get the next piece of input
		if (savedState != null)
		{
		  restoreState(savedState);
		  savedState = null;
		  saveTermBuffer();
		  return true;
		}
		else if (!input.incrementToken())
		{
			return false;
		}

		/* We build n-grams before and after stopwords. 
		 * When valid, the buffer always contains at least the separator.
		 * If its empty, there is nothing before this stopword.
		 */
		if (lastWasCommon || (Common && buffer.Length > 0))
		{
		  savedState = captureState();
		  gramToken();
		  return true;
		}

		saveTermBuffer();
		return true;
	  }

	  /// <summary>
	  /// {@inheritDoc}
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void reset() throws java.io.IOException
	  public override void reset()
	  {
		base.reset();
		lastWasCommon = false;
		savedState = null;
		buffer.Length = 0;
	  }

	  // ================================================= Helper Methods ================================================

	  /// <summary>
	  /// Determines if the current token is a common term
	  /// </summary>
	  /// <returns> {@code true} if the current token is a common term, {@code false} otherwise </returns>
	  private bool Common
	  {
		  get
		  {
			return commonWords != null && commonWords.contains(termAttribute.buffer(), 0, termAttribute.length());
		  }
	  }

	  /// <summary>
	  /// Saves this information to form the left part of a gram
	  /// </summary>
	  private void saveTermBuffer()
	  {
		buffer.Length = 0;
		buffer.Append(termAttribute.buffer(), 0, termAttribute.length());
		buffer.Append(SEPARATOR);
		lastStartOffset = offsetAttribute.startOffset();
		lastWasCommon = Common;
	  }

	  /// <summary>
	  /// Constructs a compound token.
	  /// </summary>
	  private void gramToken()
	  {
		buffer.Append(termAttribute.buffer(), 0, termAttribute.length());
		int endOffset = offsetAttribute.endOffset();

		clearAttributes();

		int length = buffer.Length;
		char[] termText = termAttribute.buffer();
		if (length > termText.Length)
		{
		  termText = termAttribute.resizeBuffer(length);
		}

		buffer.getChars(0, length, termText, 0);
		termAttribute.Length = length;
		posIncAttribute.PositionIncrement = 0;
		posLenAttribute.PositionLength = 2; // bigram
		offsetAttribute.setOffset(lastStartOffset, endOffset);
		typeAttribute.Type = GRAM_TYPE;
		buffer.Length = 0;
	  }
	}

}