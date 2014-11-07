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

namespace org.apache.lucene.analysis.miscellaneous
{

	using WhitespaceTokenizer = WhitespaceTokenizer;
	using StandardTokenizer = org.apache.lucene.analysis.standard.StandardTokenizer;
	using OffsetAttribute = org.apache.lucene.analysis.tokenattributes.OffsetAttribute;
	using PositionIncrementAttribute = org.apache.lucene.analysis.tokenattributes.PositionIncrementAttribute;
	using CharTermAttribute = org.apache.lucene.analysis.tokenattributes.CharTermAttribute;
	using TypeAttribute = org.apache.lucene.analysis.tokenattributes.TypeAttribute;
	using CharArraySet = org.apache.lucene.analysis.util.CharArraySet;
	using ArrayUtil = org.apache.lucene.util.ArrayUtil;
	using AttributeSource = org.apache.lucene.util.AttributeSource;
	using InPlaceMergeSorter = org.apache.lucene.util.InPlaceMergeSorter;
	using RamUsageEstimator = org.apache.lucene.util.RamUsageEstimator;
	using Version = org.apache.lucene.util.Version;


	/// <summary>
	/// Splits words into subwords and performs optional transformations on subword
	/// groups. Words are split into subwords with the following rules:
	/// <ul>
	/// <li>split on intra-word delimiters (by default, all non alpha-numeric
	/// characters): <code>"Wi-Fi"</code> &#8594; <code>"Wi", "Fi"</code></li>
	/// <li>split on case transitions: <code>"PowerShot"</code> &#8594;
	/// <code>"Power", "Shot"</code></li>
	/// <li>split on letter-number transitions: <code>"SD500"</code> &#8594;
	/// <code>"SD", "500"</code></li>
	/// <li>leading and trailing intra-word delimiters on each subword are ignored:
	/// <code>"//hello---there, 'dude'"</code> &#8594;
	/// <code>"hello", "there", "dude"</code></li>
	/// <li>trailing "'s" are removed for each subword: <code>"O'Neil's"</code>
	/// &#8594; <code>"O", "Neil"</code>
	/// <ul>
	/// <li>Note: this step isn't performed in a separate filter because of possible
	/// subword combinations.</li>
	/// </ul>
	/// </li>
	/// </ul>
	/// 
	/// The <b>combinations</b> parameter affects how subwords are combined:
	/// <ul>
	/// <li>combinations="0" causes no subword combinations: <code>"PowerShot"</code>
	/// &#8594; <code>0:"Power", 1:"Shot"</code> (0 and 1 are the token positions)</li>
	/// <li>combinations="1" means that in addition to the subwords, maximum runs of
	/// non-numeric subwords are catenated and produced at the same position of the
	/// last subword in the run:
	/// <ul>
	/// <li><code>"PowerShot"</code> &#8594;
	/// <code>0:"Power", 1:"Shot" 1:"PowerShot"</code></li>
	/// <li><code>"A's+B's&C's"</code> -gt; <code>0:"A", 1:"B", 2:"C", 2:"ABC"</code>
	/// </li>
	/// <li><code>"Super-Duper-XL500-42-AutoCoder!"</code> &#8594;
	/// <code>0:"Super", 1:"Duper", 2:"XL", 2:"SuperDuperXL", 3:"500" 4:"42", 5:"Auto", 6:"Coder", 6:"AutoCoder"</code>
	/// </li>
	/// </ul>
	/// </li>
	/// </ul>
	/// One use for <seealso cref="WordDelimiterFilter"/> is to help match words with different
	/// subword delimiters. For example, if the source text contained "wi-fi" one may
	/// want "wifi" "WiFi" "wi-fi" "wi+fi" queries to all match. One way of doing so
	/// is to specify combinations="1" in the analyzer used for indexing, and
	/// combinations="0" (the default) in the analyzer used for querying. Given that
	/// the current <seealso cref="StandardTokenizer"/> immediately removes many intra-word
	/// delimiters, it is recommended that this filter be used after a tokenizer that
	/// does not do this (such as <seealso cref="WhitespaceTokenizer"/>).
	/// </summary>
	public sealed class WordDelimiterFilter : TokenFilter
	{
		private bool InstanceFieldsInitialized = false;

		private void InitializeInstanceFields()
		{
			concat = new WordDelimiterConcatenation(this);
			concatAll = new WordDelimiterConcatenation(this);
			sorter = new OffsetSorter(this);
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
	  public WordDelimiterFilter(Version matchVersion, TokenStream @in, sbyte[] charTypeTable, int configurationFlags, CharArraySet protWords) : base(@in)
	  {
		  if (!InstanceFieldsInitialized)
		  {
			  InitializeInstanceFields();
			  InstanceFieldsInitialized = true;
		  }
		if (!matchVersion.onOrAfter(Version.LUCENE_48))
		{
		  throw new System.ArgumentException("This class only works with Lucene 4.8+. To emulate the old (broken) behavior of WordDelimiterFilter, use Lucene47WordDelimiterFilter");
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
	  public WordDelimiterFilter(Version matchVersion, TokenStream @in, int configurationFlags, CharArraySet protWords) : this(matchVersion, @in, WordDelimiterIterator.DEFAULT_WORD_DELIM_TABLE, configurationFlags, protWords)
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
			  first = false;
			  return true;
			}

			// word of simply delimiters
			if (iterator.end == WordDelimiterIterator.DONE && !has(PRESERVE_ORIGINAL))
			{
			  // if the posInc is 1, simply ignore it in the accumulation
			  // TODO: proper hole adjustment (FilteringTokenFilter-like) instead of this previous logic!
			  if (posIncAttribute.PositionIncrement == 1 && !first)
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
			  first = false;
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
				buffer();
				continue;
			  }
			}

			if (!concatAll.Empty)
			{
			  // only if we haven't output this same combo above!
			  if (concatAll.subwordCount > lastConcatCount)
			  {
				concatAll.writeAndClear();
				buffer();
				continue;
			  }
			  concatAll.clear();
			}

			if (bufferedPos < bufferedLen)
			{
			  if (bufferedPos == 0)
			  {
				sorter.sort(0, bufferedLen);
			  }
			  clearAttributes();
			  restoreState(buffered[bufferedPos++]);
			  if (first && posIncAttribute.PositionIncrement == 0)
			  {
				// can easily happen with strange combinations (e.g. not outputting numbers, but concat-all)
				posIncAttribute.PositionIncrement = 1;
			  }
			  first = false;
			  return true;
			}

			// no saved concatenations, on to the next input word
			bufferedPos = bufferedLen = 0;
			hasSavedState = false;
			continue;
		  }

		  // word surrounded by delimiters: always output
		  if (iterator.SingleWord)
		  {
			generatePart(true);
			iterator.next();
			first = false;
			return true;
		  }

		  int wordType = iterator.type();

		  // do we already have queued up incompatible concatenations?
		  if (!concat.Empty && (concat.type & wordType) == 0)
		  {
			if (flushConcatenation(concat))
			{
			  hasOutputToken = false;
			  buffer();
			  continue;
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
			buffer();
		  }

		  iterator.next();
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void reset() throws java.io.IOException
	  public override void reset()
	  {
		base.reset();
		hasSavedState = false;
		concat.clear();
		concatAll.clear();
		accumPosInc = bufferedPos = bufferedLen = 0;
		first = true;
	  }

	  // ================================================= Helper Methods ================================================


	  private AttributeSource.State[] buffered = new AttributeSource.State[8];
	  private int[] startOff = new int[8];
	  private int[] posInc = new int[8];
	  private int bufferedLen = 0;
	  private int bufferedPos = 0;
	  private bool first;

	  private class OffsetSorter : InPlaceMergeSorter
	  {
		  private readonly WordDelimiterFilter outerInstance;

		  public OffsetSorter(WordDelimiterFilter outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		protected internal override int compare(int i, int j)
		{
		  int cmp = int.compare(outerInstance.startOff[i], outerInstance.startOff[j]);
		  if (cmp == 0)
		  {
			cmp = int.compare(outerInstance.posInc[j], outerInstance.posInc[i]);
		  }
		  return cmp;
		}

		protected internal override void swap(int i, int j)
		{
		  AttributeSource.State tmp = outerInstance.buffered[i];
		  outerInstance.buffered[i] = outerInstance.buffered[j];
		  outerInstance.buffered[j] = tmp;

		  int tmp2 = outerInstance.startOff[i];
		  outerInstance.startOff[i] = outerInstance.startOff[j];
		  outerInstance.startOff[j] = tmp2;

		  tmp2 = outerInstance.posInc[i];
		  outerInstance.posInc[i] = outerInstance.posInc[j];
		  outerInstance.posInc[j] = tmp2;
		}
	  }

	  internal OffsetSorter sorter;

	  private void buffer()
	  {
		if (bufferedLen == buffered.Length)
		{
		  int newSize = ArrayUtil.oversize(bufferedLen + 1, 8);
		  buffered = Arrays.copyOf(buffered, newSize);
		  startOff = Arrays.copyOf(startOff, newSize);
		  posInc = Arrays.copyOf(posInc, newSize);
		}
		startOff[bufferedLen] = offsetAttribute.startOffset();
		posInc[bufferedLen] = posIncAttribute.PositionIncrement;
		buffered[bufferedLen] = captureState();
		bufferedLen++;
	  }

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
		  private readonly WordDelimiterFilter outerInstance;

		  public WordDelimiterConcatenation(WordDelimiterFilter outerInstance)
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