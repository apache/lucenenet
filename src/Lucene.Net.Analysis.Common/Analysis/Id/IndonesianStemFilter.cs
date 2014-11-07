namespace org.apache.lucene.analysis.id
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
	/// A <seealso cref="TokenFilter"/> that applies <seealso cref="IndonesianStemmer"/> to stem Indonesian words.
	/// </summary>
	public sealed class IndonesianStemFilter : TokenFilter
	{
	  private readonly CharTermAttribute termAtt = addAttribute(typeof(CharTermAttribute));
	  private readonly KeywordAttribute keywordAtt = addAttribute(typeof(KeywordAttribute));
	  private readonly IndonesianStemmer stemmer = new IndonesianStemmer();
	  private readonly bool stemDerivational;

	  /// <summary>
	  /// Calls <seealso cref="#IndonesianStemFilter(TokenStream, boolean) IndonesianStemFilter(input, true)"/>
	  /// </summary>
	  public IndonesianStemFilter(TokenStream input) : this(input, true)
	  {
	  }

	  /// <summary>
	  /// Create a new IndonesianStemFilter.
	  /// <para>
	  /// If <code>stemDerivational</code> is false, 
	  /// only inflectional suffixes (particles and possessive pronouns) are stemmed.
	  /// </para>
	  /// </summary>
	  public IndonesianStemFilter(TokenStream input, bool stemDerivational) : base(input)
	  {
		this.stemDerivational = stemDerivational;
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public boolean incrementToken() throws java.io.IOException
	  public override bool incrementToken()
	  {
		if (input.incrementToken())
		{
		  if (!keywordAtt.Keyword)
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int newlen = stemmer.stem(termAtt.buffer(), termAtt.length(), stemDerivational);
			int newlen = stemmer.stem(termAtt.buffer(), termAtt.length(), stemDerivational);
			termAtt.Length = newlen;
		  }
		  return true;
		}
		else
		{
		  return false;
		}
	  }
	}

}