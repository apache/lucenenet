using System;
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
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Util;

namespace org.apache.lucene.analysis.miscellaneous
{

	using WhitespaceTokenizer = WhitespaceTokenizer;
	using StandardTokenizer = org.apache.lucene.analysis.standard.StandardTokenizer;
	using OffsetAttribute = org.apache.lucene.analysis.tokenattributes.OffsetAttribute;
	using PositionIncrementAttribute = org.apache.lucene.analysis.tokenattributes.PositionIncrementAttribute;
	using CharTermAttribute = org.apache.lucene.analysis.tokenattributes.CharTermAttribute;
	using TypeAttribute = org.apache.lucene.analysis.tokenattributes.TypeAttribute;
	using CharArraySet = CharArraySet;
	using ArrayUtil = org.apache.lucene.util.ArrayUtil;
	using RamUsageEstimator = org.apache.lucene.util.RamUsageEstimator;

	/// <summary>
	/// Old Broken version of <seealso cref="WordDelimiterFilter"/>
	/// </summary>
	[Obsolete]
	public sealed class Lucene47WordDelimiterFilter : TokenFilter
	{
		private bool InstanceFieldsInitialized = false;

		private void InitializeInstanceFields()
		{
			concat = new WordDelimiterConcatenation(this);
			concatAll = new WordDelimiterConcatenation(this);
		}


	  public const int LOWER = 0x01;
	  public const int UPPER = 0x02;
	  public const int DIGIT = 0x04;
	  public const int SUBWORD_DELIM = 0x08;

	  // combinations: for testing, not for setting bits
	  public const int ALPHA = 0x03;
	  public const int ALPHANUM = 0x07;

	  /// <summary>
	  /// Causes parts of words to be generated:
	  /// <p/>
	  /// "PowerShot" => "Power" "Shot"
	  /// </summary>
	  public const int GENERATE_WORD_PARTS = 1;

	  /// <summary>
	  /// Causes number subwords to be generated:
	  /// <p/>
	  /// "500-42" => "500" "42"
	  /// </summary>
	  public const int GENERATE_NUMBER_PARTS = 2;

	  /// <summary>
	  /// Causes maximum runs of word parts to be catenated:
	  /// <p/>
	  /// "wi-fi" => "wifi"
	  /// </summary>
	  public const int CATENATE_WORDS = 4;

	  /// <summary>
	  /// Causes maximum runs of word parts to be catenated:
	  /// <p/>
	  /// "wi-fi" => "wifi"
	  /// </summary>
	  public const int CATENATE_NUMBERS = 8;

	  /// <summary>
	  /// Causes all subword parts to be catenated:
	  /// <p/>
	  /// "wi-fi-4000" => "wifi4000"
	  /// </summary>
	  public const int CATENATE_ALL = 16;

	  /// <summary>
	  /// Causes original words are preserved and added to the subword list (Defaults to false)
	  /// <p/>
	  /// "500-42" => "500" "42" "500-42"
	  /// </summary>
	  public const int PRESERVE_ORIGINAL = 32;

	  /// <summary>
	  /// If not set, causes case changes to be ignored (subwords will only be generated
	  /// given SUBWORD_DELIM tokens)
	  /// </summary>
	  public const int SPLIT_ON_CASE_CHANGE = 64;

	  /// <summary>
	  /// If not set, causes numeric changes to be ignored (subwords will only be generated
	  /// given SUBWORD_DELIM tokens).
	  /// </summary>
	  public const int SPLIT_ON_NUMERICS = 128;

	  /// <summary>
	  /// Causes trailing "'s" to be removed for each subword
	  /// <p/>
	  /// "O'Neil's" => "O", "Neil"
	  /// </summary>
	  public const int STEM_ENGLISH_POSSESSIVE = 256;

	  /// <summary>
	  /// If not null is the set of tokens to protect from being delimited
	  /// 
	  /// </summary>
	  internal readonly CharArraySet protWords;

	  private readonly int flags;

	  private readonly CharTermAttribute termAttribute = addAttribute(typeof(CharTermAttribute));
	  private readonly OffsetAttribute offsetAttribute = addAttribute(typeof(OffsetAttribute));
	  private readonly PositionIncrementAttribute posIncAttribute = addAttribute(typeof(PositionIncrementAttribute));
	  private readonly TypeAttribute typeAttribute = addAttribute(typeof(TypeAttribute));

	  // used for iterating word delimiter breaks
	  private readonly WordDelimiterIterator iterator;

	  // used for concatenating runs of similar typed subwords (word,number)
	  private WordDelimiterConcatenation concat;
	  // number of subwords last output by concat.
	  private int lastConcatCount = 0;

	  // used for catenate all
	  private WordDelimiterConcatenation concatAll;

	  // used for accumulating position increment gaps
	  private int accumPosInc = 0;

	  private char[] savedBuffer = new char[1024];
	  private int savedStartOffset;
	  private int savedEndOffset;
	  private string savedType;
	  private bool hasSavedState = false;
	  // if length by start + end offsets doesn't match the term text then assume
	  // this is a synonym and don't adjust the offsets.
	  private bool hasIllegalOffsets = false;

	  // for a run of the same subword type within a word, have we output anything?
	  private bool hasOutputToken = false;
	  // when preserve original is on, have we output any token following it?
	  // this token must have posInc=0!
	  private bool hasOutputFollowingOriginal = false;

	  /// <summary>
	  /// Creates a new WordDelimiterFilter
	  /// </summary>
	  /// <param name="in"> TokenStream to be filtered </param>
	  /// <param name="charTypeTable"> table containing character types </param>
	  /// <param name="configurationFlags"> Flags configuring the filter </param>
	  /// <param name="protWords"> If not null is the set of tokens to protect from being delimited </param>
	  public Lucene47WordDelimiterFilter(TokenStream @in, sbyte[] charTypeTable, int configurationFlags, CharArraySet protWords) : base(@in)
	  {
		  if (!InstanceFieldsInitialized)
		  {
			  InitializeInstanceFields();
			  InstanceFieldsInitialized = true;
		  }
		this.flags = configurationFlags;
		this.protWords = protWords;
		this.iterator = new WordDelimiterIterator(charTypeTable, has(SPLIT_ON_CASE_CHANGE), has(SPLIT_ON_NUMERICS), has(STEM_ENGLISH_POSSESSIVE));
	  }

	  /// <summary>
	  /// Creates a new WordDelimiterFilter using <seealso cref="WordDelimiterIterator#DEFAULT_WORD_DELIM_TABLE"/>
	  /// as its charTypeTable
	  /// </summary>
	  /// <param name="in"> TokenStream to be filtered </param>
	  /// <param name="configurationFlags"> Flags configuring the filter </param>
	  /// <param name="protWords"> If not null is the set of tokens to protect from being delimited </param>
	  public Lucene47WordDelimiterFilter(TokenStream @in, int configurationFlags, CharArraySet protWords) : this(@in, WordDelimiterIterator.DEFAULT_WORD_DELIM_TABLE, configurationFlags, protWords)
	  {
		  if (!InstanceFieldsInitialized)
		  {
			  InitializeInstanceFields();
			  InstanceFieldsInitialized = true;
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public boolean incrementToken() throws java.io.IOException
	  public override bool incrementToken()
	  {
		while (true)
		{
		  if (!hasSavedState)
		  {
			// process a new input word
			if (!input.incrementToken())
			{
			  return false;
			}

			int termLength = termAttribute.length();
			char[] termBuffer = termAttribute.buffer();

			accumPosInc += posIncAttribute.PositionIncrement;

			iterator.setText(termBuffer, termLength);
			iterator.next();

			// word of no delimiters, or protected word: just return it
			if ((iterator.current == 0 && iterator.end == termLength) || (protWords != null && protWords.contains(termBuffer, 0, termLength)))
			{
			  posIncAttribute.PositionIncrement = accumPosInc;
			  accumPosInc = 0;
			  return true;
			}

			// word of simply delimiters
			if (iterator.end == WordDelimiterIterator.DONE && !has(PRESERVE_ORIGINAL))
			{
			  // if the posInc is 1, simply ignore it in the accumulation
			  if (posIncAttribute.PositionIncrement == 1)
			  {
				accumPosInc--;
			  }
			  continue;
			}

			saveState();

			hasOutputToken = false;
			hasOutputFollowingOriginal = !has(PRESERVE_ORIGINAL);
			lastConcatCount = 0;

			if (has(PRESERVE_ORIGINAL))
			{
			  posIncAttribute.PositionIncrement = accumPosInc;
			  accumPosInc = 0;
			  return true;
			}
		  }

		  // at the end of the string, output any concatenations
		  if (iterator.end == WordDelimiterIterator.DONE)
		  {
			if (!concat.Empty)
			{
			  if (flushConcatenation(concat))
			  {
				return true;
			  }
			}

			if (!concatAll.Empty)
			{
			  // only if we haven't output this same combo above!
			  if (concatAll.subwordCount > lastConcatCount)
			  {
				concatAll.writeAndClear();
				return true;
			  }
			  concatAll.clear();
			}

			// no saved concatenations, on to the next input word
			hasSavedState = false;
			continue;
		  }

		  // word surrounded by delimiters: always output
		  if (iterator.SingleWord)
		  {
			generatePart(true);
			iterator.next();
			return true;
		  }

		  int wordType = iterator.type();

		  // do we already have queued up incompatible concatenations?
		  if (!concat.Empty && (concat.type & wordType) == 0)
		  {
			if (flushConcatenation(concat))
			{
			  hasOutputToken = false;
			  return true;
			}
			hasOutputToken = false;
		  }

		  // add subwords depending upon options
		  if (shouldConcatenate(wordType))
		  {
			if (concat.Empty)
			{
			  concat.type = wordType;
			}
			concatenate(concat);
		  }

		  // add all subwords (catenateAll)
		  if (has(CATENATE_ALL))
		  {
			concatenate(concatAll);
		  }

		  // if we should output the word or number part
		  if (shouldGenerateParts(wordType))
		  {
			generatePart(false);
			iterator.next();
			return true;
		  }

		  iterator.next();
		}
	  }

	  /// <summary>
	  /// {@inheritDoc}
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void reset() throws java.io.IOException
	  public override void reset()
	  {
		base.reset();
		hasSavedState = false;
		concat.clear();
		concatAll.clear();
		accumPosInc = 0;
	  }

	  // ================================================= Helper Methods ================================================

	  /// <summary>
	  /// Saves the existing attribute states
	  /// </summary>
	  private void saveState()
	  {
		// otherwise, we have delimiters, save state
		savedStartOffset = offsetAttribute.startOffset();
		savedEndOffset = offsetAttribute.endOffset();
		// if length by start + end offsets doesn't match the term text then assume this is a synonym and don't adjust the offsets.
		hasIllegalOffsets = (savedEndOffset - savedStartOffset != termAttribute.length());
		savedType = typeAttribute.type();

		if (savedBuffer.Length < termAttribute.length())
		{
		  savedBuffer = new char[ArrayUtil.oversize(termAttribute.length(), RamUsageEstimator.NUM_BYTES_CHAR)];
		}

		Array.Copy(termAttribute.buffer(), 0, savedBuffer, 0, termAttribute.length());
		iterator.text = savedBuffer;

		hasSavedState = true;
	  }

	  /// <summary>
	  /// Flushes the given WordDelimiterConcatenation by either writing its concat and then clearing, or just clearing.
	  /// </summary>
	  /// <param name="concatenation"> WordDelimiterConcatenation that will be flushed </param>
	  /// <returns> {@code true} if the concatenation was written before it was cleared, {@code false} otherwise </returns>
	  private bool flushConcatenation(WordDelimiterConcatenation concatenation)
	  {
		lastConcatCount = concatenation.subwordCount;
		if (concatenation.subwordCount != 1 || !shouldGenerateParts(concatenation.type))
		{
		  concatenation.writeAndClear();
		  return true;
		}
		concatenation.clear();
		return false;
	  }

	  /// <summary>
	  /// Determines whether to concatenate a word or number if the current word is the given type
	  /// </summary>
	  /// <param name="wordType"> Type of the current word used to determine if it should be concatenated </param>
	  /// <returns> {@code true} if concatenation should occur, {@code false} otherwise </returns>
	  private bool shouldConcatenate(int wordType)
	  {
		return (has(CATENATE_WORDS) && isAlpha(wordType)) || (has(CATENATE_NUMBERS) && isDigit(wordType));
	  }

	  /// <summary>
	  /// Determines whether a word/number part should be generated for a word of the given type
	  /// </summary>
	  /// <param name="wordType"> Type of the word used to determine if a word/number part should be generated </param>
	  /// <returns> {@code true} if a word/number part should be generated, {@code false} otherwise </returns>
	  private bool shouldGenerateParts(int wordType)
	  {
		return (has(GENERATE_WORD_PARTS) && isAlpha(wordType)) || (has(GENERATE_NUMBER_PARTS) && isDigit(wordType));
	  }

	  /// <summary>
	  /// Concatenates the saved buffer to the given WordDelimiterConcatenation
	  /// </summary>
	  /// <param name="concatenation"> WordDelimiterConcatenation to concatenate the buffer to </param>
	  private void concatenate(WordDelimiterConcatenation concatenation)
	  {
		if (concatenation.Empty)
		{
		  concatenation.startOffset = savedStartOffset + iterator.current;
		}
		concatenation.append(savedBuffer, iterator.current, iterator.end - iterator.current);
		concatenation.endOffset = savedStartOffset + iterator.end;
	  }

	  /// <summary>
	  /// Generates a word/number part, updating the appropriate attributes
	  /// </summary>
	  /// <param name="isSingleWord"> {@code true} if the generation is occurring from a single word, {@code false} otherwise </param>
	  private void generatePart(bool isSingleWord)
	  {
		clearAttributes();
		termAttribute.copyBuffer(savedBuffer, iterator.current, iterator.end - iterator.current);

		int startOffset = savedStartOffset + iterator.current;
		int endOffset = savedStartOffset + iterator.end;

		if (hasIllegalOffsets)
		{
		  // historically this filter did this regardless for 'isSingleWord', 
		  // but we must do a sanity check:
		  if (isSingleWord && startOffset <= savedEndOffset)
		  {
			offsetAttribute.setOffset(startOffset, savedEndOffset);
		  }
		  else
		  {
			offsetAttribute.setOffset(savedStartOffset, savedEndOffset);
		  }
		}
		else
		{
		  offsetAttribute.setOffset(startOffset, endOffset);
		}
		posIncAttribute.PositionIncrement = position(false);
		typeAttribute.Type = savedType;
	  }

	  /// <summary>
	  /// Get the position increment gap for a subword or concatenation
	  /// </summary>
	  /// <param name="inject"> true if this token wants to be injected </param>
	  /// <returns> position increment gap </returns>
	  private int position(bool inject)
	  {
		int posInc = accumPosInc;

		if (hasOutputToken)
		{
		  accumPosInc = 0;
		  return inject ? 0 : Math.Max(1, posInc);
		}

		hasOutputToken = true;

		if (!hasOutputFollowingOriginal)
		{
		  // the first token following the original is 0 regardless
		  hasOutputFollowingOriginal = true;
		  return 0;
		}
		// clear the accumulated position increment
		accumPosInc = 0;
		return Math.Max(1, posInc);
	  }

	  /// <summary>
	  /// Checks if the given word type includes <seealso cref="#ALPHA"/>
	  /// </summary>
	  /// <param name="type"> Word type to check </param>
	  /// <returns> {@code true} if the type contains ALPHA, {@code false} otherwise </returns>
	  internal static bool isAlpha(int type)
	  {
		return (type & ALPHA) != 0;
	  }

	  /// <summary>
	  /// Checks if the given word type includes <seealso cref="#DIGIT"/>
	  /// </summary>
	  /// <param name="type"> Word type to check </param>
	  /// <returns> {@code true} if the type contains DIGIT, {@code false} otherwise </returns>
	  internal static bool isDigit(int type)
	  {
		return (type & DIGIT) != 0;
	  }

	  /// <summary>
	  /// Checks if the given word type includes <seealso cref="#SUBWORD_DELIM"/>
	  /// </summary>
	  /// <param name="type"> Word type to check </param>
	  /// <returns> {@code true} if the type contains SUBWORD_DELIM, {@code false} otherwise </returns>
	  internal static bool isSubwordDelim(int type)
	  {
		return (type & SUBWORD_DELIM) != 0;
	  }

	  /// <summary>
	  /// Checks if the given word type includes <seealso cref="#UPPER"/>
	  /// </summary>
	  /// <param name="type"> Word type to check </param>
	  /// <returns> {@code true} if the type contains UPPER, {@code false} otherwise </returns>
	  internal static bool isUpper(int type)
	  {
		return (type & UPPER) != 0;
	  }

	  /// <summary>
	  /// Determines whether the given flag is set
	  /// </summary>
	  /// <param name="flag"> Flag to see if set </param>
	  /// <returns> {@code true} if flag is set </returns>
	  private bool has(int flag)
	  {
		return (flags & flag) != 0;
	  }

	  // ================================================= Inner Classes =================================================

	  /// <summary>
	  /// A WDF concatenated 'run'
	  /// </summary>
	  internal sealed class WordDelimiterConcatenation
	  {
		  private readonly Lucene47WordDelimiterFilter outerInstance;

		  public WordDelimiterConcatenation(Lucene47WordDelimiterFilter outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		internal readonly StringBuilder buffer = new StringBuilder();
		internal int startOffset;
		internal int endOffset;
		internal int type;
		internal int subwordCount;

		/// <summary>
		/// Appends the given text of the given length, to the concetenation at the given offset
		/// </summary>
		/// <param name="text"> Text to append </param>
		/// <param name="offset"> Offset in the concetenation to add the text </param>
		/// <param name="length"> Length of the text to append </param>
		internal void append(char[] text, int offset, int length)
		{
		  buffer.Append(text, offset, length);
		  subwordCount++;
		}

		/// <summary>
		/// Writes the concatenation to the attributes
		/// </summary>
		internal void write()
		{
		  clearAttributes();
		  if (outerInstance.termAttribute.length() < buffer.Length)
		  {
			outerInstance.termAttribute.resizeBuffer(buffer.Length);
		  }
		  char[] termbuffer = outerInstance.termAttribute.buffer();

		  buffer.getChars(0, buffer.Length, termbuffer, 0);
		  outerInstance.termAttribute.Length = buffer.Length;

		  if (outerInstance.hasIllegalOffsets)
		  {
			outerInstance.offsetAttribute.setOffset(outerInstance.savedStartOffset, outerInstance.savedEndOffset);
		  }
		  else
		  {
			outerInstance.offsetAttribute.setOffset(startOffset, endOffset);
		  }
		  outerInstance.posIncAttribute.PositionIncrement = outerInstance.position(true);
		  outerInstance.typeAttribute.Type = outerInstance.savedType;
		  outerInstance.accumPosInc = 0;
		}

		/// <summary>
		/// Determines if the concatenation is empty
		/// </summary>
		/// <returns> {@code true} if the concatenation is empty, {@code false} otherwise </returns>
		internal bool Empty
		{
			get
			{
			  return buffer.Length == 0;
			}
		}

		/// <summary>
		/// Clears the concatenation and resets its state
		/// </summary>
		internal void clear()
		{
		  buffer.Length = 0;
		  startOffset = endOffset = type = subwordCount = 0;
		}

		/// <summary>
		/// Convenience method for the common scenario of having to write the concetenation and then clearing its state
		/// </summary>
		internal void writeAndClear()
		{
		  write();
		  clear();
		}
	  }
	  // questions:
	  // negative numbers?  -42 indexed as just 42?
	  // dollar sign?  $42
	  // percent sign?  33%
	  // downsides:  if source text is "powershot" then a query of "PowerShot" won't match!
	}

}