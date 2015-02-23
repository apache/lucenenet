using System;
using Lucene.Net.Analysis.Tokenattributes;
using Reader = System.IO.TextReader;
using Version = Lucene.Net.Util.LuceneVersion;

namespace Lucene.Net.Analysis.Ngram
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
    /// <summary>
	/// Old version of <seealso cref="EdgeNGramTokenizer"/> which doesn't handle correctly
	/// supplementary characters.
	/// </summary>
	[Obsolete]
	public sealed class Lucene43EdgeNGramTokenizer : Tokenizer
	{
	  public const Side DEFAULT_SIDE = Side.FRONT;
	  public const int DEFAULT_MAX_GRAM_SIZE = 1;
	  public const int DEFAULT_MIN_GRAM_SIZE = 1;

	  private readonly CharTermAttribute termAtt = addAttribute(typeof(CharTermAttribute));
	  private readonly OffsetAttribute offsetAtt = addAttribute(typeof(OffsetAttribute));
	  private readonly PositionIncrementAttribute posIncrAtt = addAttribute(typeof(PositionIncrementAttribute));

	  /// <summary>
	  /// Specifies which side of the input the n-gram should be generated from </summary>
	  public enum Side
	  {

		/// <summary>
		/// Get the n-gram from the front of the input </summary>
//JAVA TO C# CONVERTER TODO TASK: The following line could not be converted:
		FRONT
		{
		  public String getLabel() { return "front"
		}
	  },

		/// <summary>
		/// Get the n-gram from the end of the input </summary>
//JAVA TO C# CONVERTER TODO TASK: The following line could not be converted:
		BACK
		{
		  public String getLabel()
		  {
			  return "back";
		  }
		}

		public = 

		// Get the appropriate Side from a string
		public static Side getSide(String sideName)
		{
//JAVA TO C# CONVERTER TODO TASK: The following line could not be converted:
		  if (FRONT.getLabel().equals(sideName))
		  {
			return FRONT;
		  }
//JAVA TO C# CONVERTER TODO TASK: The following line could not be converted:
		  if (BACK.getLabel().equals(sideName))
		  {
			return BACK;
		  }
		  return null;
		}
	}

	  private int minGram;
	  private int maxGram;
	  private int gramSize;
	  private Side side;
	  private bool started;
	  private int inLen; // length of the input AFTER trim()
	  private int charsRead; // length of the input
	  private string inStr;


	  /// <summary>
	  /// Creates EdgeNGramTokenizer that can generate n-grams in the sizes of the given range
	  /// </summary>
	  /// <param name="version"> the <a href="#version">Lucene match version</a> </param>
	  /// <param name="input"> <seealso cref="Reader"/> holding the input to be tokenized </param>
	  /// <param name="side"> the <seealso cref="Side"/> from which to chop off an n-gram </param>
	  /// <param name="minGram"> the smallest n-gram to generate </param>
	  /// <param name="maxGram"> the largest n-gram to generate </param>
	  [Obsolete]
	  public Lucene43EdgeNGramTokenizer(Version version, Reader input, Side side, int minGram, int maxGram) : base(input)
	  {
		init(version, side, minGram, maxGram);
	  }

	  /// <summary>
	  /// Creates EdgeNGramTokenizer that can generate n-grams in the sizes of the given range
	  /// </summary>
	  /// <param name="version"> the <a href="#version">Lucene match version</a> </param>
	  /// <param name="factory"> <seealso cref="org.apache.lucene.util.AttributeSource.AttributeFactory"/> to use </param>
	  /// <param name="input"> <seealso cref="Reader"/> holding the input to be tokenized </param>
	  /// <param name="side"> the <seealso cref="Side"/> from which to chop off an n-gram </param>
	  /// <param name="minGram"> the smallest n-gram to generate </param>
	  /// <param name="maxGram"> the largest n-gram to generate </param>
	  [Obsolete]
	  public Lucene43EdgeNGramTokenizer(Version version, AttributeFactory factory, Reader input, Side side, int minGram, int maxGram) : base(factory, input)
	  {
		init(version, side, minGram, maxGram);
	  }

	  /// <summary>
	  /// Creates EdgeNGramTokenizer that can generate n-grams in the sizes of the given range
	  /// </summary>
	  /// <param name="version"> the <a href="#version">Lucene match version</a> </param>
	  /// <param name="input"> <seealso cref="Reader"/> holding the input to be tokenized </param>
	  /// <param name="sideLabel"> the name of the <seealso cref="Side"/> from which to chop off an n-gram </param>
	  /// <param name="minGram"> the smallest n-gram to generate </param>
	  /// <param name="maxGram"> the largest n-gram to generate </param>
	  [Obsolete]
	  public Lucene43EdgeNGramTokenizer(Version version, Reader input, string sideLabel, int minGram, int maxGram) : this(version, input, Side.getSide(sideLabel), minGram, maxGram)
	  {
	  }

	  /// <summary>
	  /// Creates EdgeNGramTokenizer that can generate n-grams in the sizes of the given range
	  /// </summary>
	  /// <param name="version"> the <a href="#version">Lucene match version</a> </param>
	  /// <param name="factory"> <seealso cref="org.apache.lucene.util.AttributeSource.AttributeFactory"/> to use </param>
	  /// <param name="input"> <seealso cref="Reader"/> holding the input to be tokenized </param>
	  /// <param name="sideLabel"> the name of the <seealso cref="Side"/> from which to chop off an n-gram </param>
	  /// <param name="minGram"> the smallest n-gram to generate </param>
	  /// <param name="maxGram"> the largest n-gram to generate </param>
	  [Obsolete]
	  public Lucene43EdgeNGramTokenizer(Version version, AttributeFactory factory, Reader input, string sideLabel, int minGram, int maxGram) : this(version, factory, input, Side.getSide(sideLabel), minGram, maxGram)
	  {
	  }

	  /// <summary>
	  /// Creates EdgeNGramTokenizer that can generate n-grams in the sizes of the given range
	  /// </summary>
	  /// <param name="version"> the <a href="#version">Lucene match version</a> </param>
	  /// <param name="input"> <seealso cref="Reader"/> holding the input to be tokenized </param>
	  /// <param name="minGram"> the smallest n-gram to generate </param>
	  /// <param name="maxGram"> the largest n-gram to generate </param>
	  public Lucene43EdgeNGramTokenizer(Version version, Reader input, int minGram, int maxGram) : this(version, input, Side.FRONT, minGram, maxGram)
	  {
	  }

	  /// <summary>
	  /// Creates EdgeNGramTokenizer that can generate n-grams in the sizes of the given range
	  /// </summary>
	  /// <param name="version"> the <a href="#version">Lucene match version</a> </param>
	  /// <param name="factory"> <seealso cref="org.apache.lucene.util.AttributeSource.AttributeFactory"/> to use </param>
	  /// <param name="input"> <seealso cref="Reader"/> holding the input to be tokenized </param>
	  /// <param name="minGram"> the smallest n-gram to generate </param>
	  /// <param name="maxGram"> the largest n-gram to generate </param>
	  public Lucene43EdgeNGramTokenizer(Version version, AttributeFactory factory, Reader input, int minGram, int maxGram) : this(version, factory, input, Side.FRONT, minGram, maxGram)
	  {
	  }

	  private void init(Version version, Side side, int minGram, int maxGram)
	  {
		if (version == null)
		{
		  throw new System.ArgumentException("version must not be null");
		}

		if (side == null)
		{
		  throw new System.ArgumentException("sideLabel must be either front or back");
		}

		if (minGram < 1)
		{
		  throw new System.ArgumentException("minGram must be greater than zero");
		}

		if (minGram > maxGram)
		{
		  throw new System.ArgumentException("minGram must not be greater than maxGram");
		}

		if (version.onOrAfter(Version.LUCENE_44))
		{
		  if (side == Side.BACK)
		  {
			throw new System.ArgumentException("Side.BACK is not supported anymore as of Lucene 4.4");
		  }
		}
		else
		{
		  maxGram = Math.Min(maxGram, 1024);
		}

		this.minGram = minGram;
		this.maxGram = maxGram;
		this.side = side;
	  }

	  /// <summary>
	  /// Returns the next token in the stream, or null at EOS. </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public boolean incrementToken() throws java.io.IOException
	  public override bool incrementToken()
	  {
		clearAttributes();
		// if we are just starting, read the whole input
		if (!started)
		{
		  started = true;
		  gramSize = minGram;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int limit = side == Side.FRONT ? maxGram : 1024;
		  int limit = side == Side.FRONT ? maxGram : 1024;
		  char[] chars = new char[Math.Min(1024, limit)];
		  charsRead = 0;
		  // TODO: refactor to a shared readFully somewhere:
		  bool exhausted = false;
		  while (charsRead < limit)
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int inc = input.read(chars, charsRead, chars.length-charsRead);
			int inc = input.read(chars, charsRead, chars.Length - charsRead);
			if (inc == -1)
			{
			  exhausted = true;
			  break;
			}
			charsRead += inc;
			if (charsRead == chars.Length && charsRead < limit)
			{
			  chars = ArrayUtil.grow(chars);
			}
		  }

		  inStr = new string(chars, 0, charsRead);
		  inStr = inStr.Trim();

		  if (!exhausted)
		  {
			// Read extra throwaway chars so that on end() we
			// report the correct offset:
			char[] throwaway = new char[1024];
			while (true)
			{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int inc = input.read(throwaway, 0, throwaway.length);
			  int inc = input.read(throwaway, 0, throwaway.Length);
			  if (inc == -1)
			  {
				break;
			  }
			  charsRead += inc;
			}
		  }

		  inLen = inStr.length();
		  if (inLen == 0)
		  {
			return false;
		  }
		  posIncrAtt.PositionIncrement = 1;
		}
		else
		{
		  posIncrAtt.PositionIncrement = 0;
		}

		// if the remaining input is too short, we can't generate any n-grams
		if (gramSize > inLen)
		{
		  return false;
		}

		// if we have hit the end of our n-gram size range, quit
		if (gramSize > maxGram || gramSize > inLen)
		{
		  return false;
		}

		// grab gramSize chars from front or back
		int start = side == Side.FRONT ? 0 : inLen - gramSize;
		int end = start + gramSize;
		termAtt.setEmpty().append(inStr, start, end);
		offsetAtt.setOffset(correctOffset(start), correctOffset(end));
		gramSize++;
		return true;
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void end() throws java.io.IOException
	  public override void end()
	  {
		base.end();
		// set final offset
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int finalOffset = correctOffset(charsRead);
		int finalOffset = correctOffset(charsRead);
		this.offsetAtt.setOffset(finalOffset, finalOffset);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void reset() throws java.io.IOException
	  public override void reset()
	  {
		base.reset();
		started = false;
	  }
}

}