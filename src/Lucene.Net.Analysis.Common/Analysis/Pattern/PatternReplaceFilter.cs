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

namespace org.apache.lucene.analysis.pattern
{

	using CharTermAttribute = org.apache.lucene.analysis.tokenattributes.CharTermAttribute;


	/// <summary>
	/// A TokenFilter which applies a Pattern to each token in the stream,
	/// replacing match occurances with the specified replacement string.
	/// 
	/// <para>
	/// <b>Note:</b> Depending on the input and the pattern used and the input
	/// TokenStream, this TokenFilter may produce Tokens whose text is the empty
	/// string.
	/// </para>
	/// </summary>
	/// <seealso cref= Pattern </seealso>
	public sealed class PatternReplaceFilter : TokenFilter
	{
	  private readonly string replacement;
	  private readonly bool all;
	  private readonly CharTermAttribute termAtt = addAttribute(typeof(CharTermAttribute));
	  private readonly Matcher m;

	  /// <summary>
	  /// Constructs an instance to replace either the first, or all occurances
	  /// </summary>
	  /// <param name="in"> the TokenStream to process </param>
	  /// <param name="p"> the patterm to apply to each Token </param>
	  /// <param name="replacement"> the "replacement string" to substitute, if null a
	  ///        blank string will be used. Note that this is not the literal
	  ///        string that will be used, '$' and '\' have special meaning. </param>
	  /// <param name="all"> if true, all matches will be replaced otherwise just the first match. </param>
	  /// <seealso cref= Matcher#quoteReplacement </seealso>
	  public PatternReplaceFilter(TokenStream @in, Pattern p, string replacement, bool all) : base(@in)
	  {
		this.replacement = (null == replacement) ? "" : replacement;
		this.all = all;
		this.m = p.matcher(termAtt);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public boolean incrementToken() throws java.io.IOException
	  public override bool incrementToken()
	  {
		if (!input.incrementToken())
		{
			return false;
		}

		m.reset();
		if (m.find())
		{
		  // replaceAll/replaceFirst will reset() this previous find.
		  string transformed = all ? m.replaceAll(replacement) : m.replaceFirst(replacement);
		  termAtt.setEmpty().append(transformed);
		}

		return true;
	  }

	}

}