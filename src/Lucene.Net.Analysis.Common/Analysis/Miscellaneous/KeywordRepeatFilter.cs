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

	using KeywordAttribute = org.apache.lucene.analysis.tokenattributes.KeywordAttribute;
	using PositionIncrementAttribute = org.apache.lucene.analysis.tokenattributes.PositionIncrementAttribute;


	/// <summary>
	/// This TokenFilter emits each incoming token twice once as keyword and once non-keyword, in other words once with
	/// <seealso cref="KeywordAttribute#setKeyword(boolean)"/> set to <code>true</code> and once set to <code>false</code>.
	/// This is useful if used with a stem filter that respects the <seealso cref="KeywordAttribute"/> to index the stemmed and the
	/// un-stemmed version of a term into the same field.
	/// </summary>
	public sealed class KeywordRepeatFilter : TokenFilter
	{

	  private readonly KeywordAttribute keywordAttribute = addAttribute(typeof(KeywordAttribute));
	  private readonly PositionIncrementAttribute posIncAttr = addAttribute(typeof(PositionIncrementAttribute));
	  private State state;

	  /// <summary>
	  /// Construct a token stream filtering the given input.
	  /// </summary>
	  public KeywordRepeatFilter(TokenStream input) : base(input)
	  {
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public boolean incrementToken() throws java.io.IOException
	  public override bool incrementToken()
	  {
		if (state != null)
		{
		  restoreState(state);
		  posIncAttr.PositionIncrement = 0;
		  keywordAttribute.Keyword = false;
		  state = null;
		  return true;
		}
		if (input.incrementToken())
		{
		  state = captureState();
		  keywordAttribute.Keyword = true;
		  return true;
		}
		return false;
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void reset() throws java.io.IOException
	  public override void reset()
	  {
		base.reset();
		state = null;
	  }
	}

}