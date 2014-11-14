namespace org.apache.lucene.analysis.el
{

	/// <summary>
	/// Copyright 2005 The Apache Software Foundation
	/// 
	/// Licensed under the Apache License, Version 2.0 (the "License");
	/// you may not use this file except in compliance with the License.
	/// You may obtain a copy of the License at
	/// 
	///     http://www.apache.org/licenses/LICENSE-2.0
	/// 
	/// Unless required by applicable law or agreed to in writing, software
	/// distributed under the License is distributed on an "AS IS" BASIS,
	/// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	/// See the License for the specific language governing permissions and
	/// limitations under the License.
	/// </summary>

	using CharTermAttribute = org.apache.lucene.analysis.tokenattributes.CharTermAttribute;
	using CharacterUtils = org.apache.lucene.analysis.util.CharacterUtils;
	using Version = org.apache.lucene.util.Version;

	/// <summary>
	/// Normalizes token text to lower case, removes some Greek diacritics,
	/// and standardizes final sigma to sigma. 
	/// <a name="version"/>
	/// <para>You must specify the required <seealso cref="Version"/>
	/// compatibility when creating GreekLowerCaseFilter:
	/// <ul>
	///   <li> As of 3.1, supplementary characters are properly lowercased.
	/// </ul>
	/// </para>
	/// </summary>
	public sealed class GreekLowerCaseFilter : TokenFilter
	{
	  private readonly CharTermAttribute termAtt = addAttribute(typeof(CharTermAttribute));
	  private readonly CharacterUtils charUtils;

	  /// <summary>
	  /// Create a GreekLowerCaseFilter that normalizes Greek token text.
	  /// </summary>
	  /// <param name="matchVersion"> Lucene compatibility version, 
	  ///   See <a href="#version">above</a> </param>
	  /// <param name="in"> TokenStream to filter </param>
	  public GreekLowerCaseFilter(Version matchVersion, TokenStream @in) : base(@in)
	  {
		this.charUtils = CharacterUtils.getInstance(matchVersion);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public boolean incrementToken() throws java.io.IOException
	  public override bool incrementToken()
	  {
		if (input.incrementToken())
		{
		  char[] chArray = termAtt.buffer();
		  int chLen = termAtt.length();
		  for (int i = 0; i < chLen;)
		  {
			i += char.toChars(lowerCase(charUtils.codePointAt(chArray, i, chLen)), chArray, i);
		  }
		  return true;
		}
		else
		{
		  return false;
		}
	  }

	  private int lowerCase(int codepoint)
	  {
		switch (codepoint)
		{
		  /* There are two lowercase forms of sigma:
		   *   U+03C2: small final sigma (end of word)
		   *   U+03C3: small sigma (otherwise)
		   *   
		   * Standardize both to U+03C3
		   */
		  case '\u03C2': // small final sigma
			return '\u03C3'; // small sigma

		  /* Some greek characters contain diacritics.
		   * This filter removes these, converting to the lowercase base form.
		   */

		  case '\u0386': // capital alpha with tonos
		  case '\u03AC': // small alpha with tonos
			return '\u03B1'; // small alpha

		  case '\u0388': // capital epsilon with tonos
		  case '\u03AD': // small epsilon with tonos
			return '\u03B5'; // small epsilon

		  case '\u0389': // capital eta with tonos
		  case '\u03AE': // small eta with tonos
			return '\u03B7'; // small eta

		  case '\u038A': // capital iota with tonos
		  case '\u03AA': // capital iota with dialytika
		  case '\u03AF': // small iota with tonos
		  case '\u03CA': // small iota with dialytika
		  case '\u0390': // small iota with dialytika and tonos
			return '\u03B9'; // small iota

		  case '\u038E': // capital upsilon with tonos
		  case '\u03AB': // capital upsilon with dialytika
		  case '\u03CD': // small upsilon with tonos
		  case '\u03CB': // small upsilon with dialytika
		  case '\u03B0': // small upsilon with dialytika and tonos
			return '\u03C5'; // small upsilon

		  case '\u038C': // capital omicron with tonos
		  case '\u03CC': // small omicron with tonos
			return '\u03BF'; // small omicron

		  case '\u038F': // capital omega with tonos
		  case '\u03CE': // small omega with tonos
			return '\u03C9'; // small omega

		  /* The previous implementation did the conversion below.
		   * Only implemented for backwards compatibility with old indexes.
		   */

		  case '\u03A2': // reserved
			return '\u03C2'; // small final sigma

		  default:
			return char.ToLower(codepoint);
		}
	  }
	}

}