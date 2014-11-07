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
	/// Tokenizer for path-like hierarchies.
	/// <para>
	/// Take something like:
	/// 
	/// <pre>
	///  /something/something/else
	/// </pre>
	/// 
	/// and make:
	/// 
	/// <pre>
	///  /something
	///  /something/something
	///  /something/something/else
	/// </pre>
	/// </para>
	/// </summary>
	public class PathHierarchyTokenizer : Tokenizer
	{

	  public PathHierarchyTokenizer(Reader input) : this(input, DEFAULT_BUFFER_SIZE, DEFAULT_DELIMITER, DEFAULT_DELIMITER, DEFAULT_SKIP)
	  {
	  }

	  public PathHierarchyTokenizer(Reader input, int skip) : this(input, DEFAULT_BUFFER_SIZE, DEFAULT_DELIMITER, DEFAULT_DELIMITER, skip)
	  {
	  }

	  public PathHierarchyTokenizer(Reader input, int bufferSize, char delimiter) : this(input, bufferSize, delimiter, delimiter, DEFAULT_SKIP)
	  {
	  }

	  public PathHierarchyTokenizer(Reader input, char delimiter, char replacement) : this(input, DEFAULT_BUFFER_SIZE, delimiter, replacement, DEFAULT_SKIP)
	  {
	  }

	  public PathHierarchyTokenizer(Reader input, char delimiter, char replacement, int skip) : this(input, DEFAULT_BUFFER_SIZE, delimiter, replacement, skip)
	  {
	  }

	  public PathHierarchyTokenizer(AttributeFactory factory, Reader input, char delimiter, char replacement, int skip) : this(factory, input, DEFAULT_BUFFER_SIZE, delimiter, replacement, skip)
	  {
	  }

	  public PathHierarchyTokenizer(Reader input, int bufferSize, char delimiter, char replacement, int skip) : this(AttributeFactory.DEFAULT_ATTRIBUTE_FACTORY, input, bufferSize, delimiter, replacement, skip)
	  {
	  }

	  public PathHierarchyTokenizer(AttributeFactory factory, Reader input, int bufferSize, char delimiter, char replacement, int skip) : base(factory, input)
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
	  private int startPosition = 0;
	  private int skipped = 0;
	  private bool endDelimiter = false;
	  private StringBuilder resultToken;

	  private int charsRead = 0;


//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public final boolean incrementToken() throws java.io.IOException
	  public override bool incrementToken()
	  {
		clearAttributes();
		termAtt.append(resultToken);
		if (resultToken.Length == 0)
		{
		  posAtt.PositionIncrement = 1;
		}
		else
		{
		  posAtt.PositionIncrement = 0;
		}
		int length = 0;
		bool added = false;
		if (endDelimiter)
		{
		  termAtt.append(replacement);
		  length++;
		  endDelimiter = false;
		  added = true;
		}

		while (true)
		{
		  int c = input.read();
		  if (c >= 0)
		  {
			charsRead++;
		  }
		  else
		  {
			if (skipped > skip)
			{
			  length += resultToken.Length;
			  termAtt.Length = length;
			   offsetAtt.setOffset(correctOffset(startPosition), correctOffset(startPosition + length));
			  if (added)
			  {
				resultToken.Length = 0;
				resultToken.Append(termAtt.buffer(), 0, length);
			  }
			  return added;
			}
			else
			{
			  return false;
			}
		  }
		  if (!added)
		  {
			added = true;
			skipped++;
			if (skipped > skip)
			{
			  termAtt.append(c == delimiter ? replacement : (char)c);
			  length++;
			}
			else
			{
			  startPosition++;
			}
		  }
		  else
		  {
			if (c == delimiter)
			{
			  if (skipped > skip)
			  {
				endDelimiter = true;
				break;
			  }
			  skipped++;
			  if (skipped > skip)
			  {
				termAtt.append(replacement);
				length++;
			  }
			  else
			  {
				startPosition++;
			  }
			}
			else
			{
			  if (skipped > skip)
			  {
				termAtt.append((char)c);
				length++;
			  }
			  else
			  {
				startPosition++;
			  }
			}
		  }
		}
		length += resultToken.Length;
		termAtt.Length = length;
		offsetAtt.setOffset(correctOffset(startPosition), correctOffset(startPosition + length));
		resultToken.Length = 0;
		resultToken.Append(termAtt.buffer(), 0, length);
		return true;
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public final void end() throws java.io.IOException
	  public override void end()
	  {
		base.end();
		// set final offset
		int finalOffset = correctOffset(charsRead);
		offsetAtt.setOffset(finalOffset, finalOffset);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void reset() throws java.io.IOException
	  public override void reset()
	  {
		base.reset();
		resultToken.Length = 0;
		charsRead = 0;
		endDelimiter = false;
		skipped = 0;
		startPosition = 0;
	  }
	}

}