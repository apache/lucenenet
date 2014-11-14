using System;

namespace org.apache.lucene.analysis.fr
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

	using KeywordMarkerFilter = org.apache.lucene.analysis.miscellaneous.KeywordMarkerFilter; // for javadoc
	using SnowballFilter = org.apache.lucene.analysis.snowball.SnowballFilter;
	using KeywordAttribute = org.apache.lucene.analysis.tokenattributes.KeywordAttribute;
	using CharTermAttribute = org.apache.lucene.analysis.tokenattributes.CharTermAttribute;

	/// <summary>
	/// A <seealso cref="TokenFilter"/> that stems french words. 
	/// <para>
	/// The used stemmer can be changed at runtime after the
	/// filter object is created (as long as it is a <seealso cref="FrenchStemmer"/>).
	/// </para>
	/// <para>
	/// To prevent terms from being stemmed use an instance of
	/// <seealso cref="KeywordMarkerFilter"/> or a custom <seealso cref="TokenFilter"/> that sets
	/// the <seealso cref="KeywordAttribute"/> before this <seealso cref="TokenStream"/>.
	/// </para> </summary>
	/// <seealso cref= KeywordMarkerFilter </seealso>
	/// @deprecated (3.1) Use <seealso cref="SnowballFilter"/> with 
	/// <seealso cref="org.tartarus.snowball.ext.FrenchStemmer"/> instead, which has the
	/// same functionality. This filter will be removed in Lucene 5.0 
	[Obsolete("(3.1) Use <seealso cref="SnowballFilter"/> with")]
	public sealed class FrenchStemFilter : TokenFilter
	{

	  /// <summary>
	  /// The actual token in the input stream.
	  /// </summary>
	  private FrenchStemmer stemmer = new FrenchStemmer();

	  private readonly CharTermAttribute termAtt = addAttribute(typeof(CharTermAttribute));
	  private readonly KeywordAttribute keywordAttr = addAttribute(typeof(KeywordAttribute));

	  public FrenchStemFilter(TokenStream @in) : base(@in)
	  {
	  }

	  /// <returns>  Returns true for the next token in the stream, or false at EOS </returns>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public boolean incrementToken() throws java.io.IOException
	  public override bool incrementToken()
	  {
		if (input.incrementToken())
		{
		  string term = termAtt.ToString();

		  // Check the exclusion table
		  if (!keywordAttr.Keyword)
		  {
			string s = stemmer.stem(term);
			// If not stemmed, don't waste the time  adjusting the token.
			if ((s != null) && !s.Equals(term))
			{
			  termAtt.setEmpty().append(s);
			}
		  }
		  return true;
		}
		else
		{
		  return false;
		}
	  }
	  /// <summary>
	  /// Set a alternative/custom <seealso cref="FrenchStemmer"/> for this filter.
	  /// </summary>
	  public FrenchStemmer Stemmer
	  {
		  set
		  {
			if (value != null)
			{
			  this.stemmer = value;
			}
		  }
	  }
	}



}