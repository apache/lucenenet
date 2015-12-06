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

	using KeywordTokenizer = org.apache.lucene.analysis.core.KeywordTokenizer;

	public class TestScandinavianFoldingFilter : BaseTokenStreamTestCase
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
//ORIGINAL LINE: final org.apache.lucene.analysis.TokenStream stream = new ScandinavianFoldingFilter(tokenizer);
			TokenStream stream = new ScandinavianFoldingFilter(tokenizer);
			return new TokenStreamComponents(tokenizer, stream);
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void test() throws Exception
	  public virtual void test()
	  {

		checkOneTerm(analyzer, "aeäaeeea", "aaaeea"); // should not cause ArrayOutOfBoundsException

		checkOneTerm(analyzer, "aeäaeeeae", "aaaeea");
		checkOneTerm(analyzer, "aeaeeeae", "aaeea");

		checkOneTerm(analyzer, "bøen", "boen");
		checkOneTerm(analyzer, "åene", "aene");


		checkOneTerm(analyzer, "blåbærsyltetøj", "blabarsyltetoj");
		checkOneTerm(analyzer, "blaabaarsyltetoej", "blabarsyltetoj");
		checkOneTerm(analyzer, "blåbärsyltetöj", "blabarsyltetoj");

		checkOneTerm(analyzer, "raksmorgas", "raksmorgas");
		checkOneTerm(analyzer, "räksmörgås", "raksmorgas");
		checkOneTerm(analyzer, "ræksmørgås", "raksmorgas");
		checkOneTerm(analyzer, "raeksmoergaas", "raksmorgas");
		checkOneTerm(analyzer, "ræksmörgaos", "raksmorgas");


		checkOneTerm(analyzer, "ab", "ab");
		checkOneTerm(analyzer, "ob", "ob");
		checkOneTerm(analyzer, "Ab", "Ab");
		checkOneTerm(analyzer, "Ob", "Ob");

		checkOneTerm(analyzer, "å", "a");

		checkOneTerm(analyzer, "aa", "a");
		checkOneTerm(analyzer, "aA", "a");
		checkOneTerm(analyzer, "ao", "a");
		checkOneTerm(analyzer, "aO", "a");

		checkOneTerm(analyzer, "AA", "A");
		checkOneTerm(analyzer, "Aa", "A");
		checkOneTerm(analyzer, "Ao", "A");
		checkOneTerm(analyzer, "AO", "A");

		checkOneTerm(analyzer, "æ", "a");
		checkOneTerm(analyzer, "ä", "a");

		checkOneTerm(analyzer, "Æ", "A");
		checkOneTerm(analyzer, "Ä", "A");

		checkOneTerm(analyzer, "ae", "a");
		checkOneTerm(analyzer, "aE", "a");

		checkOneTerm(analyzer, "Ae", "A");
		checkOneTerm(analyzer, "AE", "A");


		checkOneTerm(analyzer, "ö", "o");
		checkOneTerm(analyzer, "ø", "o");
		checkOneTerm(analyzer, "Ö", "O");
		checkOneTerm(analyzer, "Ø", "O");


		checkOneTerm(analyzer, "oo", "o");
		checkOneTerm(analyzer, "oe", "o");
		checkOneTerm(analyzer, "oO", "o");
		checkOneTerm(analyzer, "oE", "o");

		checkOneTerm(analyzer, "Oo", "O");
		checkOneTerm(analyzer, "Oe", "O");
		checkOneTerm(analyzer, "OO", "O");
		checkOneTerm(analyzer, "OE", "O");
	  }

	  /// <summary>
	  /// check that the empty string doesn't cause issues </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testEmptyTerm() throws Exception
	  public virtual void testEmptyTerm()
	  {
		Analyzer a = new AnalyzerAnonymousInnerClassHelper2(this);
		checkOneTerm(a, "", "");
	  }

	  private class AnalyzerAnonymousInnerClassHelper2 : Analyzer
	  {
		  private readonly TestScandinavianFoldingFilter outerInstance;

		  public AnalyzerAnonymousInnerClassHelper2(TestScandinavianFoldingFilter outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new KeywordTokenizer(reader);
			return new TokenStreamComponents(tokenizer, new ScandinavianFoldingFilter(tokenizer));
		  }
	  }

	  /// <summary>
	  /// blast some random strings through the analyzer </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandomData() throws Exception
	  public virtual void testRandomData()
	  {
		checkRandomData(random(), analyzer, 1000 * RANDOM_MULTIPLIER);
	  }
	}

}