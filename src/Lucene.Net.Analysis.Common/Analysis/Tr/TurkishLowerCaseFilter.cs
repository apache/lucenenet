using System;

namespace org.apache.lucene.analysis.tr
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

	/// <summary>
	/// Normalizes Turkish token text to lower case.
	/// <para>
	/// Turkish and Azeri have unique casing behavior for some characters. This
	/// filter applies Turkish lowercase rules. For more information, see <a
	/// href="http://en.wikipedia.org/wiki/Turkish_dotted_and_dotless_I"
	/// >http://en.wikipedia.org/wiki/Turkish_dotted_and_dotless_I</a>
	/// </para>
	/// </summary>
	public sealed class TurkishLowerCaseFilter : TokenFilter
	{
	  private const int LATIN_CAPITAL_LETTER_I = '\u0049';
	  private const int LATIN_SMALL_LETTER_I = '\u0069';
	  private const int LATIN_SMALL_LETTER_DOTLESS_I = '\u0131';
	  private const int COMBINING_DOT_ABOVE = '\u0307';
	  private readonly CharTermAttribute termAtt = addAttribute(typeof(CharTermAttribute));

	  /// <summary>
	  /// Create a new TurkishLowerCaseFilter, that normalizes Turkish token text 
	  /// to lower case.
	  /// </summary>
	  /// <param name="in"> TokenStream to filter </param>
	  public TurkishLowerCaseFilter(TokenStream @in) : base(@in)
	  {
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public final boolean incrementToken() throws java.io.IOException
	  public override bool incrementToken()
	  {
		bool iOrAfter = false;

		if (input.incrementToken())
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final char[] buffer = termAtt.buffer();
		  char[] buffer = termAtt.buffer();
		  int length = termAtt.length();
		  for (int i = 0; i < length;)
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int ch = Character.codePointAt(buffer, i, length);
			int ch = char.codePointAt(buffer, i, length);

			iOrAfter = (ch == LATIN_CAPITAL_LETTER_I || (iOrAfter && char.getType(ch) == char.NON_SPACING_MARK));

			if (iOrAfter) // all the special I turkish handling happens here.
			{
			  switch (ch)
			  {
				// remove COMBINING_DOT_ABOVE to mimic composed lowercase
				case COMBINING_DOT_ABOVE:
				  length = delete(buffer, i, length);
				  continue;
				// i itself, it depends if it is followed by COMBINING_DOT_ABOVE
				// if it is, we will make it small i and later remove the dot
				case LATIN_CAPITAL_LETTER_I:
				  if (isBeforeDot(buffer, i + 1, length))
				  {
					buffer[i] = (char)LATIN_SMALL_LETTER_I;
				  }
				  else
				  {
					buffer[i] = (char)LATIN_SMALL_LETTER_DOTLESS_I;
					// below is an optimization. no COMBINING_DOT_ABOVE follows,
					// so don't waste time calculating Character.getType(), etc
					iOrAfter = false;
				  }
				  i++;
				  continue;
			  }
			}

			i += char.toChars(char.ToLower(ch), buffer, i);
		  }

		  termAtt.Length = length;
		  return true;
		}
		else
		{
		  return false;
		}
	  }


	  /// <summary>
	  /// lookahead for a combining dot above.
	  /// other NSMs may be in between.
	  /// </summary>
	  private bool isBeforeDot(char[] s, int pos, int len)
	  {
		for (int i = pos; i < len;)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int ch = Character.codePointAt(s, i, len);
		  int ch = char.codePointAt(s, i, len);
		  if (char.getType(ch) != char.NON_SPACING_MARK)
		  {
			return false;
		  }
		  if (ch == COMBINING_DOT_ABOVE)
		  {
			return true;
		  }
		  i += char.charCount(ch);
		}

		return false;
	  }

	  /// <summary>
	  /// delete a character in-place.
	  /// rarely happens, only if COMBINING_DOT_ABOVE is found after an i
	  /// </summary>
	  private int delete(char[] s, int pos, int len)
	  {
		if (pos < len)
		{
		  Array.Copy(s, pos + 1, s, pos, len - pos - 1);
		}

		return len - 1;
	  }
	}

}