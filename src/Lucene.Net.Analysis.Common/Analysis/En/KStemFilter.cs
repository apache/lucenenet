namespace org.apache.lucene.analysis.en
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
	/// A high-performance kstem filter for english.
	/// <p/>
	/// See <a href="http://ciir.cs.umass.edu/pubfiles/ir-35.pdf">
	/// "Viewing Morphology as an Inference Process"</a>
	/// (Krovetz, R., Proceedings of the Sixteenth Annual International ACM SIGIR
	/// Conference on Research and Development in Information Retrieval, 191-203, 1993).
	/// <p/>
	/// All terms must already be lowercased for this filter to work correctly.
	/// 
	/// <para>
	/// Note: This filter is aware of the <seealso cref="KeywordAttribute"/>. To prevent
	/// certain terms from being passed to the stemmer
	/// <seealso cref="KeywordAttribute#isKeyword()"/> should be set to <code>true</code>
	/// in a previous <seealso cref="TokenStream"/>.
	/// 
	/// Note: For including the original term as well as the stemmed version, see
	/// <seealso cref="org.apache.lucene.analysis.miscellaneous.KeywordRepeatFilterFactory"/>
	/// </para>
	/// 
	/// 
	/// </summary>

	public sealed class KStemFilter : TokenFilter
	{
	  private readonly KStemmer stemmer = new KStemmer();
	  private readonly CharTermAttribute termAttribute = addAttribute(typeof(CharTermAttribute));
	  private readonly KeywordAttribute keywordAtt = addAttribute(typeof(KeywordAttribute));

	  public KStemFilter(TokenStream @in) : base(@in)
	  {
	  }

	  /// <summary>
	  /// Returns the next, stemmed, input Token. </summary>
	  ///  <returns> The stemmed form of a token. </returns>
	  ///  <exception cref="IOException"> If there is a low-level I/O error. </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public boolean incrementToken() throws java.io.IOException
	  public override bool incrementToken()
	  {
		if (!input.incrementToken())
		{
		  return false;
		}

		char[] term = termAttribute.buffer();
		int len = termAttribute.length();
		if ((!keywordAtt.Keyword) && stemmer.stem(term, len))
		{
		  termAttribute.setEmpty().append(stemmer.asCharSequence());
		}

		return true;
	  }
	}

}