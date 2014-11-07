namespace org.apache.lucene.analysis.de
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

	using SetKeywordMarkerFilter = org.apache.lucene.analysis.miscellaneous.SetKeywordMarkerFilter;
	using CharTermAttribute = org.apache.lucene.analysis.tokenattributes.CharTermAttribute;
	using KeywordAttribute = org.apache.lucene.analysis.tokenattributes.KeywordAttribute;

	/// <summary>
	/// A <seealso cref="TokenFilter"/> that stems German words. 
	/// <para>
	/// It supports a table of words that should
	/// not be stemmed at all. The stemmer used can be changed at runtime after the
	/// filter object is created (as long as it is a <seealso cref="GermanStemmer"/>).
	/// </para>
	/// <para>
	/// To prevent terms from being stemmed use an instance of
	/// <seealso cref="SetKeywordMarkerFilter"/> or a custom <seealso cref="TokenFilter"/> that sets
	/// the <seealso cref="KeywordAttribute"/> before this <seealso cref="TokenStream"/>.
	/// </para> </summary>
	/// <seealso cref= SetKeywordMarkerFilter </seealso>
	public sealed class GermanStemFilter : TokenFilter
	{
		/// <summary>
		/// The actual token in the input stream.
		/// </summary>
		private GermanStemmer stemmer = new GermanStemmer();

		private readonly CharTermAttribute termAtt = addAttribute(typeof(CharTermAttribute));
		private readonly KeywordAttribute keywordAttr = addAttribute(typeof(KeywordAttribute));

		/// <summary>
		/// Creates a <seealso cref="GermanStemFilter"/> instance </summary>
		/// <param name="in"> the source <seealso cref="TokenStream"/>  </param>
		public GermanStemFilter(TokenStream @in) : base(@in)
		{
		}

		/// <returns>  Returns true for next token in the stream, or false at EOS </returns>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public boolean incrementToken() throws java.io.IOException
		public override bool incrementToken()
		{
		  if (input.incrementToken())
		  {
			string term = termAtt.ToString();

			if (!keywordAttr.Keyword)
			{
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
		/// Set a alternative/custom <seealso cref="GermanStemmer"/> for this filter.
		/// </summary>
		public GermanStemmer Stemmer
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