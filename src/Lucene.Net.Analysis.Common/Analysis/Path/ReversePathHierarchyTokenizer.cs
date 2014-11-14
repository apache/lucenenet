using System.Collections.Generic;
using System.Text;

namespace org.apache.lucene.analysis.path
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

	/// <summary>
	/// Tokenizer for domain-like hierarchies.
	/// <para>
	/// Take something like:
	/// 
	/// <pre>
	/// www.site.co.uk
	/// </pre>
	/// 
	/// and make:
	/// 
	/// <pre>
	/// www.site.co.uk
	/// site.co.uk
	/// co.uk
	/// uk
	/// </pre>
	/// 
	/// </para>
	/// </summary>
	public class ReversePathHierarchyTokenizer : Tokenizer
	{

	  public ReversePathHierarchyTokenizer(Reader input) : this(input, DEFAULT_BUFFER_SIZE, DEFAULT_DELIMITER, DEFAULT_DELIMITER, DEFAULT_SKIP)
	  {
	  }

	  public ReversePathHierarchyTokenizer(Reader input, int skip) : this(input, DEFAULT_BUFFER_SIZE, DEFAULT_DELIMITER, DEFAULT_DELIMITER, skip)
	  {
	  }

	  public ReversePathHierarchyTokenizer(Reader input, int bufferSize, char delimiter) : this(input, bufferSize, delimiter, delimiter, DEFAULT_SKIP)
	  {
	  }

	  public ReversePathHierarchyTokenizer(Reader input, char delimiter, char replacement) : this(input, DEFAULT_BUFFER_SIZE, delimiter, replacement, DEFAULT_SKIP)
	  {
	  }

	  public ReversePathHierarchyTokenizer(Reader input, int bufferSize, char delimiter, char replacement) : this(input, bufferSize, delimiter, replacement, DEFAULT_SKIP)
	  {
	  }

	  public ReversePathHierarchyTokenizer(Reader input, char delimiter, int skip) : this(input, DEFAULT_BUFFER_SIZE, delimiter, delimiter, skip)
	  {
	  }

	  public ReversePathHierarchyTokenizer(Reader input, char delimiter, char replacement, int skip) : this(input, DEFAULT_BUFFER_SIZE, delimiter, replacement, skip)
	  {
	  }

	  public ReversePathHierarchyTokenizer(AttributeFactory factory, Reader input, char delimiter, char replacement, int skip) : this(factory, input, DEFAULT_BUFFER_SIZE, delimiter, replacement, skip)
	  {
	  }

	  public ReversePathHierarchyTokenizer(Reader input, int bufferSize, char delimiter, char replacement, int skip) : this(AttributeFactory.DEFAULT_ATTRIBUTE_FACTORY, input, bufferSize, delimiter, replacement, skip)
	  {
	  }
	  public ReversePathHierarchyTokenizer(AttributeFactory factory, Reader input, int bufferSize, char delimiter, char replacement, int skip) : base(factory, input)
	  {
		if (bufferSize < 0)
		{
		  throw new System.ArgumentException("bufferSize cannot be negative");
		}
		if (skip < 0)
		{
		  throw new System.ArgumentException("skip cannot be negative");
		}
		termAtt.resizeBuffer(bufferSize);
		this.delimiter = delimiter;
		this.replacement = replacement;
		this.skip = skip;
		resultToken = new StringBuilder(bufferSize);
		resultTokenBuffer = new char[bufferSize];
		delimiterPositions = new List<>(bufferSize / 10);
	  }

	  private const int DEFAULT_BUFFER_SIZE = 1024;
	  public const char DEFAULT_DELIMITER = '/';
	  public const int DEFAULT_SKIP = 0;

	  private readonly char delimiter;
	  private readonly char replacement;
	  private readonly int skip;

	  private readonly CharTermAttribute termAtt = addAttribute(typeof(CharTermAttribute));
	  private readonly OffsetAttribute offsetAtt = addAttribute(typeof(OffsetAttribute));
	  private readonly PositionIncrementAttribute posAtt = addAttribute(typeof(PositionIncrementAttribute));

	  private int endPosition = 0;
	  private int finalOffset = 0;
	  private int skipped = 0;
	  private StringBuilder resultToken;

	  private IList<int?> delimiterPositions;
	  private int delimitersCount = -1;
	  private char[] resultTokenBuffer;

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public final boolean incrementToken() throws java.io.IOException
	  public override bool incrementToken()
	  {
		clearAttributes();
		if (delimitersCount == -1)
		{
		  int length = 0;
		  delimiterPositions.Add(0);
		  while (true)
		  {
			int c = input.read();
			if (c < 0)
			{
			  break;
			}
			length++;
			if (c == delimiter)
			{
			  delimiterPositions.Add(length);
			  resultToken.Append(replacement);
			}
			else
			{
			  resultToken.Append((char)c);
			}
		  }
		  delimitersCount = delimiterPositions.Count;
		  if (delimiterPositions[delimitersCount - 1] < length)
		  {
			delimiterPositions.Add(length);
			delimitersCount++;
		  }
		  if (resultTokenBuffer.Length < resultToken.Length)
		  {
			resultTokenBuffer = new char[resultToken.Length];
		  }
		  resultToken.getChars(0, resultToken.Length, resultTokenBuffer, 0);
		  resultToken.Length = 0;
		  int idx = delimitersCount - 1 - skip;
		  if (idx >= 0)
		  {
			// otherwise its ok, because we will skip and return false
			endPosition = delimiterPositions[idx];
		  }
		  finalOffset = correctOffset(length);
		  posAtt.PositionIncrement = 1;
		}
		else
		{
		  posAtt.PositionIncrement = 0;
		}

		while (skipped < delimitersCount - skip - 1)
		{
		  int start = delimiterPositions[skipped];
		  termAtt.copyBuffer(resultTokenBuffer, start, endPosition - start);
		  offsetAtt.setOffset(correctOffset(start), correctOffset(endPosition));
		  skipped++;
		  return true;
		}

		return false;
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public final void end() throws java.io.IOException
	  public override void end()
	  {
		base.end();
		// set final offset
		offsetAtt.setOffset(finalOffset, finalOffset);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void reset() throws java.io.IOException
	  public override void reset()
	  {
		base.reset();
		resultToken.Length = 0;
		finalOffset = 0;
		endPosition = 0;
		skipped = 0;
		delimitersCount = -1;
		delimiterPositions.Clear();
	  }
	}

}