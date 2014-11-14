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

	using UnicodeBlock = Character.UnicodeBlock;

	using LowerCaseFilter = org.apache.lucene.analysis.core.LowerCaseFilter;
	using CharTermAttribute = org.apache.lucene.analysis.tokenattributes.CharTermAttribute;
	using OffsetAttribute = org.apache.lucene.analysis.tokenattributes.OffsetAttribute;
	using PositionIncrementAttribute = org.apache.lucene.analysis.tokenattributes.PositionIncrementAttribute;
	using CharArrayIterator = org.apache.lucene.analysis.util.CharArrayIterator;
	using AttributeSource = org.apache.lucene.util.AttributeSource;
	using Version = org.apache.lucene.util.Version;

	/// <summary>
	/// <seealso cref="TokenFilter"/> that use <seealso cref="java.text.BreakIterator"/> to break each 
	/// Token that is Thai into separate Token(s) for each Thai word.
	/// <para>Please note: Since matchVersion 3.1 on, this filter no longer lowercases non-thai text.
	/// <seealso cref="ThaiAnalyzer"/> will insert a <seealso cref="LowerCaseFilter"/> before this filter
	/// so the behaviour of the Analyzer does not change. With version 3.1, the filter handles
	/// position increments correctly.
	/// </para>
	/// <para>WARNING: this filter may not be supported by all JREs.
	///    It is known to work with Sun/Oracle and Harmony JREs.
	///    If your application needs to be fully portable, consider using ICUTokenizer instead,
	///    which uses an ICU Thai BreakIterator that will always be available.
	/// </para>
	/// </summary>
	/// @deprecated Use <seealso cref="ThaiTokenizer"/> instead. 
	[Obsolete("Use <seealso cref="ThaiTokenizer"/> instead.")]
	public sealed class ThaiWordFilter : TokenFilter
	{
	  /// <summary>
	  /// True if the JRE supports a working dictionary-based breakiterator for Thai.
	  /// If this is false, this filter will not work at all!
	  /// </summary>
	  public static readonly bool DBBI_AVAILABLE = ThaiTokenizer.DBBI_AVAILABLE;
	  private static readonly BreakIterator proto = BreakIterator.getWordInstance(new Locale("th"));
	  private readonly BreakIterator breaker = (BreakIterator) proto.clone();
	  private readonly CharArrayIterator charIterator = CharArrayIterator.newWordInstance();

	  private readonly bool handlePosIncr;

	  private readonly CharTermAttribute termAtt = addAttribute(typeof(CharTermAttribute));
	  private readonly OffsetAttribute offsetAtt = addAttribute(typeof(OffsetAttribute));
	  private readonly PositionIncrementAttribute posAtt = addAttribute(typeof(PositionIncrementAttribute));

	  private AttributeSource clonedToken = null;
	  private CharTermAttribute clonedTermAtt = null;
	  private OffsetAttribute clonedOffsetAtt = null;
	  private bool hasMoreTokensInClone = false;
	  private bool hasIllegalOffsets = false; // only if the length changed before this filter

	  /// <summary>
	  /// Creates a new ThaiWordFilter with the specified match version. </summary>
	  public ThaiWordFilter(Version matchVersion, TokenStream input) : base(matchVersion.onOrAfter(Version.LUCENE_31) ? input : new LowerCaseFilter(matchVersion, input))
	  {
		if (!DBBI_AVAILABLE)
		{
		  throw new System.NotSupportedException("This JRE does not have support for Thai segmentation");
		}
		handlePosIncr = matchVersion.onOrAfter(Version.LUCENE_31);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public boolean incrementToken() throws java.io.IOException
	  public override bool incrementToken()
	  {
		if (hasMoreTokensInClone)
		{
		  int start = breaker.current();
		  int end = breaker.next();
		  if (end != BreakIterator.DONE)
		  {
			clonedToken.copyTo(this);
			termAtt.copyBuffer(clonedTermAtt.buffer(), start, end - start);
			if (hasIllegalOffsets)
			{
			  offsetAtt.setOffset(clonedOffsetAtt.startOffset(), clonedOffsetAtt.endOffset());
			}
			else
			{
			  offsetAtt.setOffset(clonedOffsetAtt.startOffset() + start, clonedOffsetAtt.startOffset() + end);
			}
			if (handlePosIncr)
			{
				posAtt.PositionIncrement = 1;
			}
			return true;
		  }
		  hasMoreTokensInClone = false;
		}

		if (!input.incrementToken())
		{
		  return false;
		}

		if (termAtt.length() == 0 || char.UnicodeBlock.of(termAtt.charAt(0)) != char.UnicodeBlock.THAI)
		{
		  return true;
		}

		hasMoreTokensInClone = true;

		// if length by start + end offsets doesn't match the term text then assume
		// this is a synonym and don't adjust the offsets.
		hasIllegalOffsets = offsetAtt.endOffset() - offsetAtt.startOffset() != termAtt.length();

		// we lazy init the cloned token, as in ctor not all attributes may be added
		if (clonedToken == null)
		{
		  clonedToken = cloneAttributes();
		  clonedTermAtt = clonedToken.getAttribute(typeof(CharTermAttribute));
		  clonedOffsetAtt = clonedToken.getAttribute(typeof(OffsetAttribute));
		}
		else
		{
		  this.copyTo(clonedToken);
		}

		// reinit CharacterIterator
		charIterator.setText(clonedTermAtt.buffer(), 0, clonedTermAtt.length());
		breaker.Text = charIterator;
		int end = breaker.next();
		if (end != BreakIterator.DONE)
		{
		  termAtt.Length = end;
		  if (hasIllegalOffsets)
		  {
			offsetAtt.setOffset(clonedOffsetAtt.startOffset(), clonedOffsetAtt.endOffset());
		  }
		  else
		  {
			offsetAtt.setOffset(clonedOffsetAtt.startOffset(), clonedOffsetAtt.startOffset() + end);
		  }
		  // position increment keeps as it is for first token
		  return true;
		}
		return false;
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void reset() throws java.io.IOException
	  public override void reset()
	  {
		base.reset();
		hasMoreTokensInClone = false;
		clonedToken = null;
		clonedTermAtt = null;
		clonedOffsetAtt = null;
	  }
	}

}