using System;
using System.Collections.Generic;

namespace org.apache.lucene.analysis.nl
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
	/// A <seealso cref="TokenFilter"/> that stems Dutch words. 
	/// <para>
	/// It supports a table of words that should
	/// not be stemmed at all. The stemmer used can be changed at runtime after the
	/// filter object is created (as long as it is a <seealso cref="DutchStemmer"/>).
	/// </para>
	/// <para>
	/// To prevent terms from being stemmed use an instance of
	/// <seealso cref="KeywordMarkerFilter"/> or a custom <seealso cref="TokenFilter"/> that sets
	/// the <seealso cref="KeywordAttribute"/> before this <seealso cref="TokenStream"/>.
	/// </para> </summary>
	/// <seealso cref= KeywordMarkerFilter </seealso>
	/// @deprecated (3.1) Use <seealso cref="SnowballFilter"/> with 
	/// <seealso cref="org.tartarus.snowball.ext.DutchStemmer"/> instead, which has the
	/// same functionality. This filter will be removed in Lucene 5.0 
	[Obsolete("(3.1) Use <seealso cref="SnowballFilter"/> with")]
	public sealed class DutchStemFilter : TokenFilter
	{
	  /// <summary>
	  /// The actual token in the input stream.
	  /// </summary>
	  private DutchStemmer stemmer = new DutchStemmer();

	  private readonly CharTermAttribute termAtt = addAttribute(typeof(CharTermAttribute));
	  private readonly KeywordAttribute keywordAttr = addAttribute(typeof(KeywordAttribute));

	  public DutchStemFilter(TokenStream _in) : base(_in)
	  {
	  }

	  /// <param name="stemdictionary"> Dictionary of word stem pairs, that overrule the algorithm </param>
	  public DutchStemFilter<T1>(TokenStream _in, IDictionary<T1> stemdictionary) : this(_in)
	  {
		stemmer.StemDictionary = stemdictionary;
	  }

	  /// <summary>
	  /// Returns the next token in the stream, or null at EOS
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public boolean incrementToken() throws java.io.IOException
	  public override bool incrementToken()
	  {
		if (input.incrementToken())
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String term = termAtt.toString();
		  string term = termAtt.ToString();

		  // Check the exclusion table.
		  if (!keywordAttr.Keyword)
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String s = stemmer.stem(term);
			string s = stemmer.stem(term);
			// If not stemmed, don't waste the time adjusting the token.
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
	  /// Set a alternative/custom <seealso cref="DutchStemmer"/> for this filter.
	  /// </summary>
	  public DutchStemmer Stemmer
	  {
		  set
		  {
			if (value != null)
			{
			  this.stemmer = value;
			}
		  }
	  }

	  /// <summary>
	  /// Set dictionary for stemming, this dictionary overrules the algorithm,
	  /// so you can correct for a particular unwanted word-stem pair.
	  /// </summary>
	  public Dictionary<T1> StemDictionary<T1>
	  {
		  set
		  {
			if (stemmer != null)
			{
			  stemmer.StemDictionary = value;
			}
		  }
	  }
	}
}