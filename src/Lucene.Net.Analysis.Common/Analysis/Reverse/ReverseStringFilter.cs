using System;

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

namespace org.apache.lucene.analysis.reverse
{

	using CharTermAttribute = org.apache.lucene.analysis.tokenattributes.CharTermAttribute;
	using Version = org.apache.lucene.util.Version;

	/// <summary>
	/// Reverse token string, for example "country" => "yrtnuoc".
	/// <para>
	/// If <code>marker</code> is supplied, then tokens will be also prepended by
	/// that character. For example, with a marker of &#x5C;u0001, "country" =>
	/// "&#x5C;u0001yrtnuoc". This is useful when implementing efficient leading
	/// wildcards search.
	/// </para>
	/// <a name="version"/>
	/// <para>You must specify the required <seealso cref="Version"/>
	/// compatibility when creating ReverseStringFilter, or when using any of
	/// its static methods:
	/// <ul>
	///   <li> As of 3.1, supplementary characters are handled correctly
	/// </ul>
	/// </para>
	/// </summary>
	public sealed class ReverseStringFilter : TokenFilter
	{

	  private readonly CharTermAttribute termAtt = addAttribute(typeof(CharTermAttribute));
	  private readonly char marker;
	  private readonly Version matchVersion;
	  private const char NOMARKER = '\uFFFF';

	  /// <summary>
	  /// Example marker character: U+0001 (START OF HEADING) 
	  /// </summary>
	  public const char START_OF_HEADING_MARKER = '\u0001';

	  /// <summary>
	  /// Example marker character: U+001F (INFORMATION SEPARATOR ONE)
	  /// </summary>
	  public const char INFORMATION_SEPARATOR_MARKER = '\u001F';

	  /// <summary>
	  /// Example marker character: U+EC00 (PRIVATE USE AREA: EC00) 
	  /// </summary>
	  public const char PUA_EC00_MARKER = '\uEC00';

	  /// <summary>
	  /// Example marker character: U+200F (RIGHT-TO-LEFT MARK)
	  /// </summary>
	  public const char RTL_DIRECTION_MARKER = '\u200F';

	  /// <summary>
	  /// Create a new ReverseStringFilter that reverses all tokens in the 
	  /// supplied <seealso cref="TokenStream"/>.
	  /// <para>
	  /// The reversed tokens will not be marked. 
	  /// </para>
	  /// </summary>
	  /// <param name="matchVersion"> See <a href="#version">above</a> </param>
	  /// <param name="in"> <seealso cref="TokenStream"/> to filter </param>
	  public ReverseStringFilter(Version matchVersion, TokenStream @in) : this(matchVersion, @in, NOMARKER)
	  {
	  }

	  /// <summary>
	  /// Create a new ReverseStringFilter that reverses and marks all tokens in the
	  /// supplied <seealso cref="TokenStream"/>.
	  /// <para>
	  /// The reversed tokens will be prepended (marked) by the <code>marker</code>
	  /// character.
	  /// </para>
	  /// </summary>
	  /// <param name="matchVersion"> See <a href="#version">above</a> </param>
	  /// <param name="in"> <seealso cref="TokenStream"/> to filter </param>
	  /// <param name="marker"> A character used to mark reversed tokens </param>
	  public ReverseStringFilter(Version matchVersion, TokenStream @in, char marker) : base(@in)
	  {
		this.matchVersion = matchVersion;
		this.marker = marker;
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public boolean incrementToken() throws java.io.IOException
	  public override bool incrementToken()
	  {
		if (input.incrementToken())
		{
		  int len = termAtt.length();
		  if (marker != NOMARKER)
		  {
			len++;
			termAtt.resizeBuffer(len);
			termAtt.buffer()[len - 1] = marker;
		  }
		  reverse(matchVersion, termAtt.buffer(), 0, len);
		  termAtt.Length = len;
		  return true;
		}
		else
		{
		  return false;
		}
	  }

	  /// <summary>
	  /// Reverses the given input string
	  /// </summary>
	  /// <param name="matchVersion"> See <a href="#version">above</a> </param>
	  /// <param name="input"> the string to reverse </param>
	  /// <returns> the given input string in reversed order </returns>
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
//ORIGINAL LINE: public static String reverse(org.apache.lucene.util.Version matchVersion, final String input)
	  public static string reverse(Version matchVersion, string input)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final char[] charInput = input.toCharArray();
		char[] charInput = input.ToCharArray();
		reverse(matchVersion, charInput, 0, charInput.Length);
		return new string(charInput);
	  }

	  /// <summary>
	  /// Reverses the given input buffer in-place </summary>
	  /// <param name="matchVersion"> See <a href="#version">above</a> </param>
	  /// <param name="buffer"> the input char array to reverse </param>
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
//ORIGINAL LINE: public static void reverse(org.apache.lucene.util.Version matchVersion, final char[] buffer)
	  public static void reverse(Version matchVersion, char[] buffer)
	  {
		reverse(matchVersion, buffer, 0, buffer.Length);
	  }

	  /// <summary>
	  /// Partially reverses the given input buffer in-place from offset 0
	  /// up to the given length. </summary>
	  /// <param name="matchVersion"> See <a href="#version">above</a> </param>
	  /// <param name="buffer"> the input char array to reverse </param>
	  /// <param name="len"> the length in the buffer up to where the
	  ///        buffer should be reversed </param>
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
//ORIGINAL LINE: public static void reverse(org.apache.lucene.util.Version matchVersion, final char[] buffer, final int len)
	  public static void reverse(Version matchVersion, char[] buffer, int len)
	  {
		reverse(matchVersion, buffer, 0, len);
	  }

	  /// @deprecated (3.1) Remove this when support for 3.0 indexes is no longer needed. 
	  [Obsolete("(3.1) Remove this when support for 3.0 indexes is no longer needed.")]
	  private static void reverseUnicode3(char[] buffer, int start, int len)
	  {
		if (len <= 1)
		{
			return;
		}
		int num = len >> 1;
		for (int i = start; i < (start + num); i++)
		{
		  char c = buffer[i];
		  buffer[i] = buffer[start * 2 + len - i - 1];
		  buffer[start * 2 + len - i - 1] = c;
		}
	  }

	  /// <summary>
	  /// Partially reverses the given input buffer in-place from the given offset
	  /// up to the given length. </summary>
	  /// <param name="matchVersion"> See <a href="#version">above</a> </param>
	  /// <param name="buffer"> the input char array to reverse </param>
	  /// <param name="start"> the offset from where to reverse the buffer </param>
	  /// <param name="len"> the length in the buffer up to where the
	  ///        buffer should be reversed </param>
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
//ORIGINAL LINE: public static void reverse(org.apache.lucene.util.Version matchVersion, final char[] buffer, final int start, final int len)
	  public static void reverse(Version matchVersion, char[] buffer, int start, int len)
	  {
		if (!matchVersion.onOrAfter(Version.LUCENE_31))
		{
		  reverseUnicode3(buffer, start, len);
		  return;
		}
		/* modified version of Apache Harmony AbstractStringBuilder reverse0() */
		if (len < 2)
		{
		  return;
		}
		int end = (start + len) - 1;
		char frontHigh = buffer[start];
		char endLow = buffer[end];
		bool allowFrontSur = true, allowEndSur = true;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int mid = start + (len >> 1);
		int mid = start + (len >> 1);
		for (int i = start; i < mid; ++i, --end)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final char frontLow = buffer[i + 1];
		  char frontLow = buffer[i + 1];
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final char endHigh = buffer[end - 1];
		  char endHigh = buffer[end - 1];
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final boolean surAtFront = allowFrontSur && Character.isSurrogatePair(frontHigh, frontLow);
		  bool surAtFront = allowFrontSur && char.IsSurrogatePair(frontHigh, frontLow);
		  if (surAtFront && (len < 3))
		  {
			// nothing to do since surAtFront is allowed and 1 char left
			return;
		  }
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final boolean surAtEnd = allowEndSur && Character.isSurrogatePair(endHigh, endLow);
		  bool surAtEnd = allowEndSur && char.IsSurrogatePair(endHigh, endLow);
		  allowFrontSur = allowEndSur = true;
		  if (surAtFront == surAtEnd)
		  {
			if (surAtFront)
			{
			  // both surrogates
			  buffer[end] = frontLow;
			  buffer[--end] = frontHigh;
			  buffer[i] = endHigh;
			  buffer[++i] = endLow;
			  frontHigh = buffer[i + 1];
			  endLow = buffer[end - 1];
			}
			else
			{
			  // neither surrogates
			  buffer[end] = frontHigh;
			  buffer[i] = endLow;
			  frontHigh = frontLow;
			  endLow = endHigh;
			}
		  }
		  else
		  {
			if (surAtFront)
			{
			  // surrogate only at the front
			  buffer[end] = frontLow;
			  buffer[i] = endLow;
			  endLow = endHigh;
			  allowFrontSur = false;
			}
			else
			{
			  // surrogate only at the end
			  buffer[end] = frontHigh;
			  buffer[i] = endHigh;
			  frontHigh = frontLow;
			  allowEndSur = false;
			}
		  }
		}
		if ((len & 0x01) == 1 && !(allowFrontSur && allowEndSur))
		{
		  // only if odd length
		  buffer[end] = allowFrontSur ? endLow : frontHigh;
		}
	  }
	}

}