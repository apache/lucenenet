using Lucene.Net.Analysis.Core;

namespace org.apache.lucene.analysis.core
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
	using CharacterUtils = org.apache.lucene.analysis.util.CharacterUtils;
	using Version = org.apache.lucene.util.Version;

	/// <summary>
	/// Normalizes token text to UPPER CASE.
	/// <a name="version"/>
	/// <para>You must specify the required <seealso cref="Version"/>
	/// compatibility when creating UpperCaseFilter
	/// 
	/// </para>
	/// <para><b>NOTE:</b> In Unicode, this transformation may lose information when the
	/// upper case character represents more than one lower case character. Use this filter
	/// when you require uppercase tokens.  Use the <seealso cref="LowerCaseFilter"/> for 
	/// general search matching
	/// </para>
	/// </summary>
	public sealed class UpperCaseFilter : TokenFilter
	{
	  private readonly CharacterUtils charUtils;
	  private readonly CharTermAttribute termAtt = addAttribute(typeof(CharTermAttribute));

	  /// <summary>
	  /// Create a new UpperCaseFilter, that normalizes token text to upper case.
	  /// </summary>
	  /// <param name="matchVersion"> See <a href="#version">above</a> </param>
	  /// <param name="in"> TokenStream to filter </param>
	  public UpperCaseFilter(Version matchVersion, TokenStream @in) : base(@in)
	  {
		charUtils = CharacterUtils.getInstance(matchVersion);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public final boolean incrementToken() throws java.io.IOException
	  public override bool incrementToken()
	  {
		if (input.incrementToken())
		{
		  charUtils.ToUpper(termAtt.buffer(), 0, termAtt.length());
		  return true;
		}
		else
		{
		  return false;
		}
	  }
	}

}