using System;
using System.Diagnostics;

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


	using CharTermAttribute = org.apache.lucene.analysis.tokenattributes.CharTermAttribute;
	using OffsetAttribute = org.apache.lucene.analysis.tokenattributes.OffsetAttribute;
	using PositionIncrementAttribute = org.apache.lucene.analysis.tokenattributes.PositionIncrementAttribute;
	using PositionLengthAttribute = org.apache.lucene.analysis.tokenattributes.PositionLengthAttribute;
	using CharacterUtils = org.apache.lucene.analysis.util.CharacterUtils;
	using Version = org.apache.lucene.util.Version;

	/// <summary>
	/// Tokenizes the input into n-grams of the given size(s).
	/// <para>On the contrary to <seealso cref="NGramTokenFilter"/>, this class sets offsets so
	/// that characters between startOffset and endOffset in the original stream are
	/// the same as the term chars.
	/// </para>
	/// <para>For example, "abcde" would be tokenized as (minGram=2, maxGram=3):
	/// <table>
	/// <tr><th>Term</th><td>ab</td><td>abc</td><td>bc</td><td>bcd</td><td>cd</td><td>cde</td><td>de</td></tr>
	/// <tr><th>Position increment</th><td>1</td><td>1</td><td>1</td><td>1</td><td>1</td><td>1</td><td>1</td></tr>
	/// <tr><th>Position length</th><td>1</td><td>1</td><td>1</td><td>1</td><td>1</td><td>1</td><td>1</td></tr>
	/// <tr><th>Offsets</th><td>[0,2[</td><td>[0,3[</td><td>[1,3[</td><td>[1,4[</td><td>[2,4[</td><td>[2,5[</td><td>[3,5[</td></tr>
	/// </table>
	/// <a name="version"/>
	/// </para>
	/// <para>This tokenizer changed a lot in Lucene 4.4 in order to:<ul>
	/// <li>tokenize in a streaming fashion to support streams which are larger
	/// than 1024 chars (limit of the previous version),
	/// <li>count grams based on unicode code points instead of java chars (and
	/// never split in the middle of surrogate pairs),
	/// <li>give the ability to <seealso cref="#isTokenChar(int) pre-tokenize"/> the stream
	/// before computing n-grams.</ul>
	/// </para>
	/// <para>Additionally, this class doesn't trim trailing whitespaces and emits
	/// tokens in a different order, tokens are now emitted by increasing start
	/// offsets while they used to be emitted by increasing lengths (which prevented
	/// from supporting large input streams).
	/// </para>
	/// <para>Although <b style="color:red">highly</b> discouraged, it is still possible
	/// to use the old behavior through <seealso cref="Lucene43NGramTokenizer"/>.
	/// </para>
	/// </summary>
	// non-final to allow for overriding isTokenChar, but all other methods should be final
	public class NGramTokenizer : Tokenizer
	{
	  public const int DEFAULT_MIN_NGRAM_SIZE = 1;
	  public const int DEFAULT_MAX_NGRAM_SIZE = 2;

	  private CharacterUtils charUtils;
	  private CharacterUtils.CharacterBuffer charBuffer;
	  private int[] buffer; // like charBuffer, but converted to code points
	  private int bufferStart, bufferEnd; // remaining slice in buffer
	  private int offset;
	  private int gramSize;
	  private int minGram, maxGram;
	  private bool exhausted;
	  private int lastCheckedChar; // last offset in the buffer that we checked
	  private int lastNonTokenChar; // last offset that we found to not be a token char
	  private bool edgesOnly; // leading edges n-grams only

	  private readonly CharTermAttribute termAtt = addAttribute(typeof(CharTermAttribute));
	  private readonly PositionIncrementAttribute posIncAtt = addAttribute(typeof(PositionIncrementAttribute));
	  private readonly PositionLengthAttribute posLenAtt = addAttribute(typeof(PositionLengthAttribute));
	  private readonly OffsetAttribute offsetAtt = addAttribute(typeof(OffsetAttribute));

	  internal NGramTokenizer(Version version, Reader input, int minGram, int maxGram, bool edgesOnly) : base(input)
	  {
		init(version, minGram, maxGram, edgesOnly);
	  }

	  /// <summary>
	  /// Creates NGramTokenizer with given min and max n-grams. </summary>
	  /// <param name="version"> the lucene compatibility <a href="#version">version</a> </param>
	  /// <param name="input"> <seealso cref="Reader"/> holding the input to be tokenized </param>
	  /// <param name="minGram"> the smallest n-gram to generate </param>
	  /// <param name="maxGram"> the largest n-gram to generate </param>
	  public NGramTokenizer(Version version, Reader input, int minGram, int maxGram) : this(version, input, minGram, maxGram, false)
	  {
	  }

	  internal NGramTokenizer(Version version, AttributeFactory factory, Reader input, int minGram, int maxGram, bool edgesOnly) : base(factory, input)
	  {
		init(version, minGram, maxGram, edgesOnly);
	  }

	  /// <summary>
	  /// Creates NGramTokenizer with given min and max n-grams. </summary>
	  /// <param name="version"> the lucene compatibility <a href="#version">version</a> </param>
	  /// <param name="factory"> <seealso cref="org.apache.lucene.util.AttributeSource.AttributeFactory"/> to use </param>
	  /// <param name="input"> <seealso cref="Reader"/> holding the input to be tokenized </param>
	  /// <param name="minGram"> the smallest n-gram to generate </param>
	  /// <param name="maxGram"> the largest n-gram to generate </param>
	  public NGramTokenizer(Version version, AttributeFactory factory, Reader input, int minGram, int maxGram) : this(version, factory, input, minGram, maxGram, false)
	  {
	  }

	  /// <summary>
	  /// Creates NGramTokenizer with default min and max n-grams. </summary>
	  /// <param name="version"> the lucene compatibility <a href="#version">version</a> </param>
	  /// <param name="input"> <seealso cref="Reader"/> holding the input to be tokenized </param>
	  public NGramTokenizer(Version version, Reader input) : this(version, input, DEFAULT_MIN_NGRAM_SIZE, DEFAULT_MAX_NGRAM_SIZE)
	  {
	  }

	  private void init(Version version, int minGram, int maxGram, bool edgesOnly)
	  {
		if (!version.onOrAfter(Version.LUCENE_44))
		{
		  throw new System.ArgumentException("This class only works with Lucene 4.4+. To emulate the old (broken) behavior of NGramTokenizer, use Lucene43NGramTokenizer/Lucene43EdgeNGramTokenizer");
		}
		charUtils = version.onOrAfter(Version.LUCENE_44) ? CharacterUtils.getInstance(version) : CharacterUtils.Java4Instance;
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
		this.edgesOnly = edgesOnly;
		charBuffer = CharacterUtils.newCharacterBuffer(2 * maxGram + 1024); // 2 * maxGram in case all code points require 2 chars and + 1024 for buffering to not keep polling the Reader
		buffer = new int[charBuffer.Buffer.Length];
		// Make the term att large enough
		termAtt.resizeBuffer(2 * maxGram);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public final boolean incrementToken() throws java.io.IOException
	  public override bool incrementToken()
	  {
		clearAttributes();

		// termination of this loop is guaranteed by the fact that every iteration
		// either advances the buffer (calls consumes()) or increases gramSize
		while (true)
		{
		  // compact
		  if (bufferStart >= bufferEnd - maxGram - 1 && !exhausted)
		  {
			Array.Copy(buffer, bufferStart, buffer, 0, bufferEnd - bufferStart);
			bufferEnd -= bufferStart;
			lastCheckedChar -= bufferStart;
			lastNonTokenChar -= bufferStart;
			bufferStart = 0;

			// fill in remaining space
			exhausted = !charUtils.fill(charBuffer, input, buffer.Length - bufferEnd);
			// convert to code points
			bufferEnd += charUtils.toCodePoints(charBuffer.Buffer, 0, charBuffer.Length, buffer, bufferEnd);
		  }

		  // should we go to the next offset?
		  if (gramSize > maxGram || (bufferStart + gramSize) > bufferEnd)
		  {
			if (bufferStart + 1 + minGram > bufferEnd)
			{
			  Debug.Assert(exhausted);
			  return false;
			}
			consume();
			gramSize = minGram;
		  }

		  updateLastNonTokenChar();

		  // retry if the token to be emitted was going to not only contain token chars
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final boolean termContainsNonTokenChar = lastNonTokenChar >= bufferStart && lastNonTokenChar < (bufferStart + gramSize);
		  bool termContainsNonTokenChar = lastNonTokenChar >= bufferStart && lastNonTokenChar < (bufferStart + gramSize);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final boolean isEdgeAndPreviousCharIsTokenChar = edgesOnly && lastNonTokenChar != bufferStart - 1;
		  bool isEdgeAndPreviousCharIsTokenChar = edgesOnly && lastNonTokenChar != bufferStart - 1;
		  if (termContainsNonTokenChar || isEdgeAndPreviousCharIsTokenChar)
		  {
			consume();
			gramSize = minGram;
			continue;
		  }

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int length = charUtils.toChars(buffer, bufferStart, gramSize, termAtt.buffer(), 0);
		  int length = charUtils.toChars(buffer, bufferStart, gramSize, termAtt.buffer(), 0);
		  termAtt.Length = length;
		  posIncAtt.PositionIncrement = 1;
		  posLenAtt.PositionLength = 1;
		  offsetAtt.setOffset(correctOffset(offset), correctOffset(offset + length));
		  ++gramSize;
		  return true;
		}
	  }

	  private void updateLastNonTokenChar()
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int termEnd = bufferStart + gramSize - 1;
		int termEnd = bufferStart + gramSize - 1;
		if (termEnd > lastCheckedChar)
		{
		  for (int i = termEnd; i > lastCheckedChar; --i)
		  {
			if (!isTokenChar(buffer[i]))
			{
			  lastNonTokenChar = i;
			  break;
			}
		  }
		  lastCheckedChar = termEnd;
		}
	  }

	  /// <summary>
	  /// Consume one code point. </summary>
	  private void consume()
	  {
		offset += char.charCount(buffer[bufferStart++]);
	  }

	  /// <summary>
	  /// Only collect characters which satisfy this condition. </summary>
	  protected internal virtual bool isTokenChar(int chr)
	  {
		return true;
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public final void end() throws java.io.IOException
	  public override void end()
	  {
		base.end();
		Debug.Assert(bufferStart <= bufferEnd);
		int endOffset = offset;
		for (int i = bufferStart; i < bufferEnd; ++i)
		{
		  endOffset += char.charCount(buffer[i]);
		}
		endOffset = correctOffset(endOffset);
		// set final offset
		offsetAtt.setOffset(endOffset, endOffset);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public final void reset() throws java.io.IOException
	  public override void reset()
	  {
		base.reset();
		bufferStart = bufferEnd = buffer.Length;
		lastNonTokenChar = lastCheckedChar = bufferStart - 1;
		offset = 0;
		gramSize = minGram;
		exhausted = false;
		charBuffer.reset();
	  }
	}

}