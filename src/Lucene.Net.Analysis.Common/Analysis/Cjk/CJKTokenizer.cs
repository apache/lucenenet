using System;

namespace org.apache.lucene.analysis.cjk
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


	using OffsetAttribute = org.apache.lucene.analysis.tokenattributes.OffsetAttribute;
	using CharTermAttribute = org.apache.lucene.analysis.tokenattributes.CharTermAttribute;
	using TypeAttribute = org.apache.lucene.analysis.tokenattributes.TypeAttribute;

	/// <summary>
	/// CJKTokenizer is designed for Chinese, Japanese, and Korean languages.
	/// <para>  
	/// The tokens returned are every two adjacent characters with overlap match.
	/// </para>
	/// <para>
	/// Example: "java C1C2C3C4" will be segmented to: "java" "C1C2" "C2C3" "C3C4".
	/// </para>
	/// Additionally, the following is applied to Latin text (such as English):
	/// <ul>
	/// <li>Text is converted to lowercase.
	/// <li>Numeric digits, '+', '#', and '_' are tokenized as letters.
	/// <li>Full-width forms are converted to half-width forms.
	/// </ul>
	/// For more info on Asian language (Chinese, Japanese, and Korean) text segmentation:
	/// please search  <a
	/// href="http://www.google.com/search?q=word+chinese+segment">google</a>
	/// </summary>
	/// @deprecated Use StandardTokenizer, CJKWidthFilter, CJKBigramFilter, and LowerCaseFilter instead. 
	[Obsolete("Use StandardTokenizer, CJKWidthFilter, CJKBigramFilter, and LowerCaseFilter instead.")]
	public sealed class CJKTokenizer : Tokenizer
	{
		//~ Static fields/initializers ---------------------------------------------
		/// <summary>
		/// Word token type </summary>
		internal const int WORD_TYPE = 0;

		/// <summary>
		/// Single byte token type </summary>
		internal const int SINGLE_TOKEN_TYPE = 1;

		/// <summary>
		/// Double byte token type </summary>
		internal const int DOUBLE_TOKEN_TYPE = 2;

		/// <summary>
		/// Names for token types </summary>
		internal static readonly string[] TOKEN_TYPE_NAMES = new string[] {"word", "single", "double"};

		/// <summary>
		/// Max word length </summary>
		private const int MAX_WORD_LEN = 255;

		/// <summary>
		/// buffer size: </summary>
		private const int IO_BUFFER_SIZE = 256;

		//~ Instance fields --------------------------------------------------------

		/// <summary>
		/// word offset, used to imply which character(in ) is parsed </summary>
		private int offset = 0;

		/// <summary>
		/// the index used only for ioBuffer </summary>
		private int bufferIndex = 0;

		/// <summary>
		/// data length </summary>
		private int dataLen = 0;

		/// <summary>
		/// character buffer, store the characters which are used to compose <br>
		/// the returned Token
		/// </summary>
		private readonly char[] buffer = new char[MAX_WORD_LEN];

		/// <summary>
		/// I/O buffer, used to store the content of the input(one of the <br>
		/// members of Tokenizer)
		/// </summary>
		private readonly char[] ioBuffer = new char[IO_BUFFER_SIZE];

		/// <summary>
		/// word type: single=>ASCII  double=>non-ASCII word=>default </summary>
		private int tokenType = WORD_TYPE;

		/// <summary>
		/// tag: previous character is a cached double-byte character  "C1C2C3C4"
		/// ----(set the C1 isTokened) C1C2 "C2C3C4" ----(set the C2 isTokened)
		/// C1C2 C2C3 "C3C4" ----(set the C3 isTokened) "C1C2 C2C3 C3C4"
		/// </summary>
		private bool preIsTokened = false;

		private readonly CharTermAttribute termAtt = addAttribute(typeof(CharTermAttribute));
		private readonly OffsetAttribute offsetAtt = addAttribute(typeof(OffsetAttribute));
		private readonly TypeAttribute typeAtt = addAttribute(typeof(TypeAttribute));

		//~ Constructors -----------------------------------------------------------

		/// <summary>
		/// Construct a token stream processing the given input.
		/// </summary>
		/// <param name="in"> I/O reader </param>
		public CJKTokenizer(Reader @in) : base(@in)
		{
		}

		public CJKTokenizer(AttributeFactory factory, Reader @in) : base(factory, @in)
		{
		}

		//~ Methods ----------------------------------------------------------------

		/// <summary>
		/// Returns true for the next token in the stream, or false at EOS.
		/// See http://java.sun.com/j2se/1.3/docs/api/java/lang/Character.UnicodeBlock.html
		/// for detail.
		/// </summary>
		/// <returns> false for end of stream, true otherwise
		/// </returns>
		/// <exception cref="java.io.IOException"> - throw IOException when read error <br>
		///         happened in the InputStream
		///  </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public boolean incrementToken() throws java.io.IOException
		public override bool incrementToken()
		{
			clearAttributes();
			/// <summary>
			/// how many character(s) has been stored in buffer </summary>

			while (true) // loop until we find a non-empty token
			{

			  int length = 0;

			  /// <summary>
			  /// the position used to create Token </summary>
			  int start = offset;

			  while (true) // loop until we've found a full token
			  {
				/// <summary>
				/// current character </summary>
				char c;

				/// <summary>
				/// unicode block of current character for detail </summary>
				char.UnicodeBlock ub;

				offset++;

				if (bufferIndex >= dataLen)
				{
					dataLen = input.read(ioBuffer);
					bufferIndex = 0;
				}

				if (dataLen == -1)
				{
					if (length > 0)
					{
						if (preIsTokened == true)
						{
							length = 0;
							preIsTokened = false;
						}
						else
						{
						  offset--;
						}

						break;
					}
					else
					{
						offset--;
						return false;
					}
				}
				else
				{
					//get current character
					c = ioBuffer[bufferIndex++];

					//get the UnicodeBlock of the current character
					ub = char.UnicodeBlock.of(c);
				}

				//if the current character is ASCII or Extend ASCII
				if ((ub == char.UnicodeBlock.BASIC_LATIN) || (ub == char.UnicodeBlock.HALFWIDTH_AND_FULLWIDTH_FORMS))
				{
					if (ub == char.UnicodeBlock.HALFWIDTH_AND_FULLWIDTH_FORMS)
					{
					  int i = (int) c;
					  if (i >= 65281 && i <= 65374)
					  {
						// convert certain HALFWIDTH_AND_FULLWIDTH_FORMS to BASIC_LATIN
						i = i - 65248;
						c = (char) i;
					  }
					}

					// if the current character is a letter or "_" "+" "#"
					if (char.IsLetterOrDigit(c) || ((c == '_') || (c == '+') || (c == '#')))
					{
						if (length == 0)
						{
							// "javaC1C2C3C4linux" <br>
							//      ^--: the current character begin to token the ASCII
							// letter
							start = offset - 1;
						}
						else if (tokenType == DOUBLE_TOKEN_TYPE)
						{
							// "javaC1C2C3C4linux" <br>
							//              ^--: the previous non-ASCII
							// : the current character
							offset--;
							bufferIndex--;

							if (preIsTokened == true)
							{
								// there is only one non-ASCII has been stored
								length = 0;
								preIsTokened = false;
								break;
							}
							else
							{
								break;
							}
						}

						// store the LowerCase(c) in the buffer
						buffer[length++] = char.ToLower(c);
						tokenType = SINGLE_TOKEN_TYPE;

						// break the procedure if buffer overflowed!
						if (length == MAX_WORD_LEN)
						{
							break;
						}
					}
					else if (length > 0)
					{
						if (preIsTokened == true)
						{
							length = 0;
							preIsTokened = false;
						}
						else
						{
							break;
						}
					}
				}
				else
				{
					// non-ASCII letter, e.g."C1C2C3C4"
					if (char.IsLetter(c))
					{
						if (length == 0)
						{
							start = offset - 1;
							buffer[length++] = c;
							tokenType = DOUBLE_TOKEN_TYPE;
						}
						else
						{
						  if (tokenType == SINGLE_TOKEN_TYPE)
						  {
								offset--;
								bufferIndex--;

								//return the previous ASCII characters
								break;
						  }
							else
							{
								buffer[length++] = c;
								tokenType = DOUBLE_TOKEN_TYPE;

								if (length == 2)
								{
									offset--;
									bufferIndex--;
									preIsTokened = true;

									break;
								}
							}
						}
					}
					else if (length > 0)
					{
						if (preIsTokened == true)
						{
							// empty the buffer
							length = 0;
							preIsTokened = false;
						}
						else
						{
							break;
						}
					}
				}
			  }

			if (length > 0)
			{
			  termAtt.copyBuffer(buffer, 0, length);
			  offsetAtt.setOffset(correctOffset(start), correctOffset(start + length));
			  typeAtt.Type = TOKEN_TYPE_NAMES[tokenType];
			  return true;
			}
			else if (dataLen == -1)
			{
			  offset--;
			  return false;
			}

			// Cycle back and try for the next token (don't
			// return an empty string)
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
		  preIsTokened = false;
		  tokenType = WORD_TYPE;
		}
	}

}