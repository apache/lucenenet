namespace org.apache.lucene.analysis.cjk
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


	using KeywordTokenizer = org.apache.lucene.analysis.core.KeywordTokenizer;

	/// <summary>
	/// Tests for <seealso cref="CJKWidthFilter"/>
	/// </summary>
	public class TestCJKWidthFilter : BaseTokenStreamTestCase
	{
	  private Analyzer analyzer = new AnalyzerAnonymousInnerClassHelper();

	  private class AnalyzerAnonymousInnerClassHelper : Analyzer
	  {
		  public AnalyzerAnonymousInnerClassHelper()
		  {
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer source = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
			return new TokenStreamComponents(source, new CJKWidthFilter(source));
		  }
	  }

	  /// <summary>
	  /// Full-width ASCII forms normalized to half-width (basic latin)
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testFullWidthASCII() throws java.io.IOException
	  public virtual void testFullWidthASCII()
	  {
		assertAnalyzesTo(analyzer, "Ｔｅｓｔ １２３４", new string[] {"Test", "1234"}, new int[] {0, 5}, new int[] {4, 9});
	  }

	  /// <summary>
	  /// Half-width katakana forms normalized to standard katakana.
	  /// A bit trickier in some cases, since half-width forms are decomposed
	  /// and voice marks need to be recombined with a preceding base form. 
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testHalfWidthKana() throws java.io.IOException
	  public virtual void testHalfWidthKana()
	  {
		assertAnalyzesTo(analyzer, "ｶﾀｶﾅ", new string[] {"カタカナ"});
		assertAnalyzesTo(analyzer, "ｳﾞｨｯﾂ", new string[] {"ヴィッツ"});
		assertAnalyzesTo(analyzer, "ﾊﾟﾅｿﾆｯｸ", new string[] {"パナソニック"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandomData() throws java.io.IOException
	  public virtual void testRandomData()
	  {
		checkRandomData(random(), analyzer, 1000 * RANDOM_MULTIPLIER);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testEmptyTerm() throws java.io.IOException
	  public virtual void testEmptyTerm()
	  {
		Analyzer a = new AnalyzerAnonymousInnerClassHelper2(this);
		checkOneTerm(a, "", "");
	  }

	  private class AnalyzerAnonymousInnerClassHelper2 : Analyzer
	  {
		  private readonly TestCJKWidthFilter outerInstance;

		  public AnalyzerAnonymousInnerClassHelper2(TestCJKWidthFilter outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new KeywordTokenizer(reader);
			return new TokenStreamComponents(tokenizer, new CJKWidthFilter(tokenizer));
		  }
	  }
	}

}