namespace org.apache.lucene.analysis.ngram
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

	using CodepointCountFilter = org.apache.lucene.analysis.miscellaneous.CodepointCountFilter;
	using CharTermAttribute = org.apache.lucene.analysis.tokenattributes.CharTermAttribute;
	using OffsetAttribute = org.apache.lucene.analysis.tokenattributes.OffsetAttribute;
	using PositionIncrementAttribute = org.apache.lucene.analysis.tokenattributes.PositionIncrementAttribute;
	using PositionLengthAttribute = org.apache.lucene.analysis.tokenattributes.PositionLengthAttribute;
	using CharacterUtils = org.apache.lucene.analysis.util.CharacterUtils;
	using Version = org.apache.lucene.util.Version;

	/// <summary>
	/// Tokenizes the input into n-grams of the given size(s).
	/// <a name="version"/>
	/// <para>You must specify the required <seealso cref="Version"/> compatibility when
	/// creating a <seealso cref="NGramTokenFilter"/>. As of Lucene 4.4, this token filters:<ul>
	/// <li>handles supplementary characters correctly,</li>
	/// <li>emits all n-grams for the same token at the same position,</li>
	/// <li>does not modify offsets,</li>
	/// <li>sorts n-grams by their offset in the original token first, then
	/// increasing length (meaning that "abc" will give "a", "ab", "abc", "b", "bc",
	/// "c").</li></ul>
	/// </para>
	/// <para>You can make this filter use the old behavior by providing a version &lt;
	/// <seealso cref="Version#LUCENE_44"/> in the constructor but this is not recommended as
	/// it will lead to broken <seealso cref="TokenStream"/>s that will cause highlighting
	/// bugs.
	/// </para>
	/// <para>If you were using this <seealso cref="TokenFilter"/> to perform partial highlighting,
	/// this won't work anymore since this filter doesn't update offsets. You should
	/// modify your analysis chain to use <seealso cref="NGramTokenizer"/>, and potentially
	/// override <seealso cref="NGramTokenizer#isTokenChar(int)"/> to perform pre-tokenization.
	/// </para>
	/// </summary>
	public sealed class NGramTokenFilter : TokenFilter
	{
	  public const int DEFAULT_MIN_NGRAM_SIZE = 1;
	  public const int DEFAULT_MAX_NGRAM_SIZE = 2;

	  private readonly int minGram, maxGram;

	  private char[] curTermBuffer;
	  private int curTermLength;
	  private int curCodePointCount;
	  private int curGramSize;
	  private int curPos;
	  private int curPosInc, curPosLen;
	  private int tokStart;
	  private int tokEnd;
	  private bool hasIllegalOffsets; // only if the length changed before this filter

	  private readonly Version version;
	  private readonly CharacterUtils charUtils;
	  private readonly CharTermAttribute termAtt = addAttribute(typeof(CharTermAttribute));
	  private readonly PositionIncrementAttribute posIncAtt;
	  private readonly PositionLengthAttribute posLenAtt;
	  private readonly OffsetAttribute offsetAtt = addAttribute(typeof(OffsetAttribute));

	  /// <summary>
	  /// Creates NGramTokenFilter with given min and max n-grams. </summary>
	  /// <param name="version"> Lucene version to enable correct position increments.
	  ///                See <a href="#version">above</a> for details. </param>
	  /// <param name="input"> <seealso cref="TokenStream"/> holding the input to be tokenized </param>
	  /// <param name="minGram"> the smallest n-gram to generate </param>
	  /// <param name="maxGram"> the largest n-gram to generate </param>
	  public NGramTokenFilter(Version version, TokenStream input, int minGram, int maxGram) : base(new CodepointCountFilter(version, input, minGram, int.MaxValue))
	  {
		this.version = version;
		this.charUtils = version.onOrAfter(Version.LUCENE_44) ? CharacterUtils.getInstance(version) : CharacterUtils.Java4Instance;
		if (minGram < 1)
		{
		  throw new System.ArgumentException("minGram must be greater than zero");
		}
		if (minGram > maxGram)
		{
		  throw new System.ArgumentException("minGram must not be greater than maxGram");
		}
		this.minGram = minGram;
		this.maxGram = maxGram;
		if (version.onOrAfter(Version.LUCENE_44))
		{
		  posIncAtt = addAttribute(typeof(PositionIncrementAttribute));
		  posLenAtt = addAttribute(typeof(PositionLengthAttribute));
		}
		else
		{
		  posIncAtt = new PositionIncrementAttributeAnonymousInnerClassHelper(this);
		  posLenAtt = new PositionLengthAttributeAnonymousInnerClassHelper(this);
		}
	  }

	  private class PositionIncrementAttributeAnonymousInnerClassHelper : PositionIncrementAttribute
	  {
		  private readonly NGramTokenFilter outerInstance;

		  public PositionIncrementAttributeAnonymousInnerClassHelper(NGramTokenFilter outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  public override int PositionIncrement
		  {
			  set
			  {
			  }
			  get
			  {
				return 0;
			  }
		  }
	  }

	  private class PositionLengthAttributeAnonymousInnerClassHelper : PositionLengthAttribute
	  {
		  private readonly NGramTokenFilter outerInstance;

		  public PositionLengthAttributeAnonymousInnerClassHelper(NGramTokenFilter outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  public override int PositionLength
		  {
			  set
			  {
			  }
			  get
			  {
				return 0;
			  }
		  }
	  }

	  /// <summary>
	  /// Creates NGramTokenFilter with default min and max n-grams. </summary>
	  /// <param name="version"> Lucene version to enable correct position increments.
	  ///                See <a href="#version">above</a> for details. </param>
	  /// <param name="input"> <seealso cref="TokenStream"/> holding the input to be tokenized </param>
	  public NGramTokenFilter(Version version, TokenStream input) : this(version, input, DEFAULT_MIN_NGRAM_SIZE, DEFAULT_MAX_NGRAM_SIZE)
	  {
	  }

	  /// <summary>
	  /// Returns the next token in the stream, or null at EOS. </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public final boolean incrementToken() throws java.io.IOException
	  public override bool incrementToken()
	  {
		while (true)
		{
		  if (curTermBuffer == null)
		  {
			if (!input.incrementToken())
			{
			  return false;
			}
			else
			{
			  curTermBuffer = termAtt.buffer().clone();
			  curTermLength = termAtt.length();
			  curCodePointCount = charUtils.codePointCount(termAtt);
			  curGramSize = minGram;
			  curPos = 0;
			  curPosInc = posIncAtt.PositionIncrement;
			  curPosLen = posLenAtt.PositionLength;
			  tokStart = offsetAtt.startOffset();
			  tokEnd = offsetAtt.endOffset();
			  // if length by start + end offsets doesn't match the term text then assume
			  // this is a synonym and don't adjust the offsets.
			  hasIllegalOffsets = (tokStart + curTermLength) != tokEnd;
			}
		  }
		  if (version.onOrAfter(Version.LUCENE_44))
		  {
			if (curGramSize > maxGram || (curPos + curGramSize) > curCodePointCount)
			{
			  ++curPos;
			  curGramSize = minGram;
			}
			if ((curPos + curGramSize) <= curCodePointCount)
			{
			  clearAttributes();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int start = charUtils.offsetByCodePoints(curTermBuffer, 0, curTermLength, 0, curPos);
			  int start = charUtils.offsetByCodePoints(curTermBuffer, 0, curTermLength, 0, curPos);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int end = charUtils.offsetByCodePoints(curTermBuffer, 0, curTermLength, start, curGramSize);
			  int end = charUtils.offsetByCodePoints(curTermBuffer, 0, curTermLength, start, curGramSize);
			  termAtt.copyBuffer(curTermBuffer, start, end - start);
			  posIncAtt.PositionIncrement = curPosInc;
			  curPosInc = 0;
			  posLenAtt.PositionLength = curPosLen;
			  offsetAtt.setOffset(tokStart, tokEnd);
			  curGramSize++;
			  return true;
			}
		  }
		  else
		  {
			while (curGramSize <= maxGram)
			{
			  while (curPos + curGramSize <= curTermLength) // while there is input
			  {
				clearAttributes();
				termAtt.copyBuffer(curTermBuffer, curPos, curGramSize);
				if (hasIllegalOffsets)
				{
				  offsetAtt.setOffset(tokStart, tokEnd);
				}
				else
				{
				  offsetAtt.setOffset(tokStart + curPos, tokStart + curPos + curGramSize);
				}
				curPos++;
				return true;
			  }
			  curGramSize++; // increase n-gram size
			  curPos = 0;
			}
		  }
		  curTermBuffer = null;
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void reset() throws java.io.IOException
	  public override void reset()
	  {
		base.reset();
		curTermBuffer = null;
	  }
	}

}