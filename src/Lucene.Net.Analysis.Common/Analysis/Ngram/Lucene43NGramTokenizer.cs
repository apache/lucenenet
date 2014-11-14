using System;

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

	/// <summary>
	/// Old broken version of <seealso cref="NGramTokenizer"/>.
	/// </summary>
	[Obsolete]
	public sealed class Lucene43NGramTokenizer : Tokenizer
	{
	  public const int DEFAULT_MIN_NGRAM_SIZE = 1;
	  public const int DEFAULT_MAX_NGRAM_SIZE = 2;

	  private int minGram, maxGram;
	  private int gramSize;
	  private int pos;
	  private int inLen; // length of the input AFTER trim()
	  private int charsRead; // length of the input
	  private string inStr;
	  private bool started;

	  private readonly CharTermAttribute termAtt = addAttribute(typeof(CharTermAttribute));
	  private readonly OffsetAttribute offsetAtt = addAttribute(typeof(OffsetAttribute));

	  /// <summary>
	  /// Creates NGramTokenizer with given min and max n-grams. </summary>
	  /// <param name="input"> <seealso cref="Reader"/> holding the input to be tokenized </param>
	  /// <param name="minGram"> the smallest n-gram to generate </param>
	  /// <param name="maxGram"> the largest n-gram to generate </param>
	  public Lucene43NGramTokenizer(Reader input, int minGram, int maxGram) : base(input)
	  {
		init(minGram, maxGram);
	  }

	  /// <summary>
	  /// Creates NGramTokenizer with given min and max n-grams. </summary>
	  /// <param name="factory"> <seealso cref="org.apache.lucene.util.AttributeSource.AttributeFactory"/> to use </param>
	  /// <param name="input"> <seealso cref="Reader"/> holding the input to be tokenized </param>
	  /// <param name="minGram"> the smallest n-gram to generate </param>
	  /// <param name="maxGram"> the largest n-gram to generate </param>
	  public Lucene43NGramTokenizer(AttributeFactory factory, Reader input, int minGram, int maxGram) : base(factory, input)
	  {
		init(minGram, maxGram);
	  }

	  /// <summary>
	  /// Creates NGramTokenizer with default min and max n-grams. </summary>
	  /// <param name="input"> <seealso cref="Reader"/> holding the input to be tokenized </param>
	  public Lucene43NGramTokenizer(Reader input) : this(input, DEFAULT_MIN_NGRAM_SIZE, DEFAULT_MAX_NGRAM_SIZE)
	  {
	  }

	  private void init(int minGram, int maxGram)
	  {
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
	  }

	  /// <summary>
	  /// Returns the next token in the stream, or null at EOS. </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public boolean incrementToken() throws java.io.IOException
	  public override bool incrementToken()
	  {
		clearAttributes();
		if (!started)
		{
		  started = true;
		  gramSize = minGram;
		  char[] chars = new char[1024];
		  charsRead = 0;
		  // TODO: refactor to a shared readFully somewhere:
		  while (charsRead < chars.Length)
		  {
			int inc = input.read(chars, charsRead, chars.Length - charsRead);
			if (inc == -1)
			{
			  break;
			}
			charsRead += inc;
		  }
		  inStr = (new string(chars, 0, charsRead)).Trim(); // remove any trailing empty strings

		  if (charsRead == chars.Length)
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

		  inLen = inStr.Length;
		  if (inLen == 0)
		  {
			return false;
		  }
		}

		if (pos + gramSize > inLen) // if we hit the end of the string
		{
		  pos = 0; // reset to beginning of string
		  gramSize++; // increase n-gram size
		  if (gramSize > maxGram) // we are done
		  {
			return false;
		  }
		  if (pos + gramSize > inLen)
		  {
			return false;
		  }
		}

		int oldPos = pos;
		pos++;
		termAtt.setEmpty().append(inStr, oldPos, oldPos + gramSize);
		offsetAtt.setOffset(correctOffset(oldPos), correctOffset(oldPos + gramSize));
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
		pos = 0;
	  }
	}

}