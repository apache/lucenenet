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


	using KeywordTokenizer = org.apache.lucene.analysis.core.KeywordTokenizer;

	/// <summary>
	/// Tests <seealso cref="GermanNormalizationFilter"/>
	/// </summary>
	public class TestGermanNormalizationFilter : BaseTokenStreamTestCase
	{
	  private Analyzer analyzer = new AnalyzerAnonymousInnerClassHelper();

	  private class AnalyzerAnonymousInnerClassHelper : Analyzer
	  {
		  public AnalyzerAnonymousInnerClassHelper()
		  {
		  }

		  protected internal override TokenStreamComponents createComponents(string field, Reader reader)
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.analysis.Tokenizer tokenizer = new org.apache.lucene.analysis.MockTokenizer(reader, org.apache.lucene.analysis.MockTokenizer.WHITESPACE, false);
			Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.analysis.TokenStream stream = new GermanNormalizationFilter(tokenizer);
			TokenStream stream = new GermanNormalizationFilter(tokenizer);
			return new TokenStreamComponents(tokenizer, stream);
		  }
	  }

	  /// <summary>
	  /// Tests that a/o/u + e is equivalent to the umlaut form
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBasicExamples() throws java.io.IOException
	  public virtual void testBasicExamples()
	  {
		checkOneTerm(analyzer, "Schaltflächen", "Schaltflachen");
		checkOneTerm(analyzer, "Schaltflaechen", "Schaltflachen");
	  }

	  /// <summary>
	  /// Tests the specific heuristic that ue is not folded after a vowel or q.
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testUHeuristic() throws java.io.IOException
	  public virtual void testUHeuristic()
	  {
		checkOneTerm(analyzer, "dauer", "dauer");
	  }

	  /// <summary>
	  /// Tests german specific folding of sharp-s
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testSpecialFolding() throws java.io.IOException
	  public virtual void testSpecialFolding()
	  {
		checkOneTerm(analyzer, "weißbier", "weissbier");
	  }

	  /// <summary>
	  /// blast some random strings through the analyzer </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandomStrings() throws Exception
	  public virtual void testRandomStrings()
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
		  private readonly TestGermanNormalizationFilter outerInstance;

		  public AnalyzerAnonymousInnerClassHelper2(TestGermanNormalizationFilter outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new KeywordTokenizer(reader);
			return new TokenStreamComponents(tokenizer, new GermanNormalizationFilter(tokenizer));
		  }
	  }
	}

}