using System;

namespace org.apache.lucene.analysis.cn
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



	using StandardTokenizer = org.apache.lucene.analysis.standard.StandardTokenizer;
	using CharTermAttribute = org.apache.lucene.analysis.tokenattributes.CharTermAttribute;
	using OffsetAttribute = org.apache.lucene.analysis.tokenattributes.OffsetAttribute;


	/// <summary>
	/// Tokenize Chinese text as individual chinese characters.
	/// 
	/// <para>
	/// The difference between ChineseTokenizer and
	/// CJKTokenizer is that they have different
	/// token parsing logic.
	/// </para>
	/// <para>
	/// For example, if the Chinese text
	/// "C1C2C3C4" is to be indexed:
	/// <ul>
	/// <li>The tokens returned from ChineseTokenizer are C1, C2, C3, C4. 
	/// <li>The tokens returned from the CJKTokenizer are C1C2, C2C3, C3C4.
	/// </ul>
	/// </para>
	/// <para>
	/// Therefore the index created by CJKTokenizer is much larger.
	/// </para>
	/// <para>
	/// The problem is that when searching for C1, C1C2, C1C3,
	/// C4C2, C1C2C3 ... the ChineseTokenizer works, but the
	/// CJKTokenizer will not work.
	/// </para> </summary>
	/// @deprecated (3.1) Use <seealso cref="StandardTokenizer"/> instead, which has the same functionality.
	/// This filter will be removed in Lucene 5.0 
	[Obsolete("(3.1) Use <seealso cref="StandardTokenizer"/> instead, which has the same functionality.")]
	public sealed class ChineseTokenizer : Tokenizer
	{


		public ChineseTokenizer(Reader @in) : base(@in)
		{
		}

		public ChineseTokenizer(AttributeFactory factory, Reader @in) : base(factory, @in)
		{
		}

		private int offset = 0, bufferIndex = 0, dataLen = 0;
		private const int MAX_WORD_LEN = 255;
		private const int IO_BUFFER_SIZE = 1024;
		private readonly char[] buffer = new char[MAX_WORD_LEN];
		private readonly char[] ioBuffer = new char[IO_BUFFER_SIZE];


		private int length;
		private int start;

		private readonly CharTermAttribute termAtt = addAttribute(typeof(CharTermAttribute));
		private readonly OffsetAttribute offsetAtt = addAttribute(typeof(OffsetAttribute));

		private void push(char c)
		{

			if (length == 0) // start of token
			{
				start = offset - 1;
			}
			buffer[length++] = char.ToLower(c); // buffer it

		}

		private bool flush()
		{

			if (length > 0)
			{
				//System.out.println(new String(buffer, 0,
				//length));
			  termAtt.copyBuffer(buffer, 0, length);
			  offsetAtt.setOffset(correctOffset(start), correctOffset(start + length));
			  return true;
			}
			else
			{
				return false;
			}
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public boolean incrementToken() throws java.io.IOException
		public override bool incrementToken()
		{
			clearAttributes();

			length = 0;
			start = offset;


			while (true)
			{

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final char c;
				char c;
				offset++;

				if (bufferIndex >= dataLen)
				{
					dataLen = input.read(ioBuffer);
					bufferIndex = 0;
				}

				if (dataLen == -1)
				{
				  offset--;
				  return flush();
				}
				else
				{
					c = ioBuffer[bufferIndex++];
				}


				switch (char.getType(c))
				{

				case char.DECIMAL_DIGIT_NUMBER:
				case char.LOWERCASE_LETTER:
				case char.UPPERCASE_LETTER:
					push(c);
					if (length == MAX_WORD_LEN)
					{
						return flush();
					}
					break;

				case char.OTHER_LETTER:
					if (length > 0)
					{
						bufferIndex--;
						offset--;
						return flush();
					}
					push(c);
					return flush();

				default:
					if (length > 0)
					{
						return flush();
					}
					break;
				}
			}
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public final void end() throws java.io.IOException
		public override void end()
		{
		  base.end();
		  // set final offset
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int finalOffset = correctOffset(offset);
		  int finalOffset = correctOffset(offset);
		  this.offsetAtt.setOffset(finalOffset, finalOffset);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void reset() throws java.io.IOException
		public override void reset()
		{
		  base.reset();
		  offset = bufferIndex = dataLen = 0;
		}
	}

}