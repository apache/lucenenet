namespace org.apache.lucene.analysis.standard
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
	using TypeAttribute = org.apache.lucene.analysis.tokenattributes.TypeAttribute;

	/// <summary>
	/// Normalizes tokens extracted with <seealso cref="ClassicTokenizer"/>. </summary>

	public class ClassicFilter : TokenFilter
	{

	  /// <summary>
	  /// Construct filtering <i>in</i>. </summary>
	  public ClassicFilter(TokenStream @in) : base(@in)
	  {
	  }

	  private static readonly string APOSTROPHE_TYPE = ClassicTokenizer.TOKEN_TYPES[ClassicTokenizer.APOSTROPHE];
	  private static readonly string ACRONYM_TYPE = ClassicTokenizer.TOKEN_TYPES[ClassicTokenizer.ACRONYM];

	  // this filters uses attribute type
	  private readonly TypeAttribute typeAtt = addAttribute(typeof(TypeAttribute));
	  private readonly CharTermAttribute termAtt = addAttribute(typeof(CharTermAttribute));

	  /// <summary>
	  /// Returns the next token in the stream, or null at EOS.
	  /// <para>Removes <tt>'s</tt> from the end of words.
	  /// </para>
	  /// <para>Removes dots from acronyms.
	  /// </para>
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public final boolean incrementToken() throws java.io.IOException
	  public override bool incrementToken()
	  {
		if (!input.incrementToken())
		{
		  return false;
		}

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final char[] buffer = termAtt.buffer();
		char[] buffer = termAtt.buffer();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int bufferLength = termAtt.length();
		int bufferLength = termAtt.length();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String type = typeAtt.type();
		string type = typeAtt.type();

		if (type == APOSTROPHE_TYPE && bufferLength >= 2 && buffer[bufferLength - 2] == '\'' && (buffer[bufferLength - 1] == 's' || buffer[bufferLength - 1] == 'S')) // remove 's
		{
		  // Strip last 2 characters off
		  termAtt.Length = bufferLength - 2;
		} // remove dots
		else if (type == ACRONYM_TYPE)
		{
		  int upto = 0;
		  for (int i = 0;i < bufferLength;i++)
		  {
			char c = buffer[i];
			if (c != '.')
			{
			  buffer[upto++] = c;
			}
		  }
		  termAtt.Length = upto;
		}

		return true;
	  }
	}

}