using Lucene.Net.Analysis.Miscellaneous;

namespace org.apache.lucene.analysis.miscellaneous
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
	using KeywordAttribute = org.apache.lucene.analysis.tokenattributes.KeywordAttribute;

	/// <summary>
	/// Marks terms as keywords via the <seealso cref="KeywordAttribute"/>. Each token
	/// that matches the provided pattern is marked as a keyword by setting
	/// <seealso cref="KeywordAttribute#setKeyword(boolean)"/> to <code>true</code>.
	/// </summary>
	public sealed class PatternKeywordMarkerFilter : KeywordMarkerFilter
	{
	  private readonly CharTermAttribute termAtt = addAttribute(typeof(CharTermAttribute));
	  private readonly Matcher matcher;

	  /// <summary>
	  /// Create a new <seealso cref="PatternKeywordMarkerFilter"/>, that marks the current
	  /// token as a keyword if the tokens term buffer matches the provided
	  /// <seealso cref="Pattern"/> via the <seealso cref="KeywordAttribute"/>.
	  /// </summary>
	  /// <param name="in">
	  ///          TokenStream to filter </param>
	  /// <param name="pattern">
	  ///          the pattern to apply to the incoming term buffer
	  ///  </param>
	  public PatternKeywordMarkerFilter(TokenStream @in, Pattern pattern) : base(@in)
	  {
		this.matcher = pattern.matcher("");
	  }

	  protected internal override bool Keyword
	  {
		  get
		  {
			matcher.reset(termAtt);
			return matcher.matches();
		  }
	  }

	}

}