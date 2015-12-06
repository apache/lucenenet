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


	public class TestScandinavianNormalizationFilter : BaseTokenStreamTestCase
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
//ORIGINAL LINE: final org.apache.lucene.analysis.TokenStream stream = new ScandinavianNormalizationFilter(tokenizer);
			TokenStream stream = new ScandinavianNormalizationFilter(tokenizer);
			return new TokenStreamComponents(tokenizer, stream);
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void test() throws Exception
	  public virtual void test()
	  {

		checkOneTerm(analyzer, "aeäaeeea", "æææeea"); // should not cause ArrayIndexOutOfBoundsException

		checkOneTerm(analyzer, "aeäaeeeae", "æææeeæ");
		checkOneTerm(analyzer, "aeaeeeae", "ææeeæ");

		checkOneTerm(analyzer, "bøen", "bøen");
		checkOneTerm(analyzer, "bOEen", "bØen");
		checkOneTerm(analyzer, "åene", "åene");


		checkOneTerm(analyzer, "blåbærsyltetøj", "blåbærsyltetøj");
		checkOneTerm(analyzer, "blaabaersyltetöj", "blåbærsyltetøj");
		checkOneTerm(analyzer, "räksmörgås", "ræksmørgås");
		checkOneTerm(analyzer, "raeksmörgaos", "ræksmørgås");
		checkOneTerm(analyzer, "raeksmörgaas", "ræksmørgås");
		checkOneTerm(analyzer, "raeksmoergås", "ræksmørgås");


		checkOneTerm(analyzer, "ab", "ab");
		checkOneTerm(analyzer, "ob", "ob");
		checkOneTerm(analyzer, "Ab", "Ab");
		checkOneTerm(analyzer, "Ob", "Ob");

		checkOneTerm(analyzer, "å", "å");

		checkOneTerm(analyzer, "aa", "å");
		checkOneTerm(analyzer, "aA", "å");
		checkOneTerm(analyzer, "ao", "å");
		checkOneTerm(analyzer, "aO", "å");

		checkOneTerm(analyzer, "AA", "Å");
		checkOneTerm(analyzer, "Aa", "Å");
		checkOneTerm(analyzer, "Ao", "Å");
		checkOneTerm(analyzer, "AO", "Å");

		checkOneTerm(analyzer, "æ", "æ");
		checkOneTerm(analyzer, "ä", "æ");

		checkOneTerm(analyzer, "Æ", "Æ");
		checkOneTerm(analyzer, "Ä", "Æ");

		checkOneTerm(analyzer, "ae", "æ");
		checkOneTerm(analyzer, "aE", "æ");

		checkOneTerm(analyzer, "Ae", "Æ");
		checkOneTerm(analyzer, "AE", "Æ");


		checkOneTerm(analyzer, "ö", "ø");
		checkOneTerm(analyzer, "ø", "ø");
		checkOneTerm(analyzer, "Ö", "Ø");
		checkOneTerm(analyzer, "Ø", "Ø");


		checkOneTerm(analyzer, "oo", "ø");
		checkOneTerm(analyzer, "oe", "ø");
		checkOneTerm(analyzer, "oO", "ø");
		checkOneTerm(analyzer, "oE", "ø");

		checkOneTerm(analyzer, "Oo", "Ø");
		checkOneTerm(analyzer, "Oe", "Ø");
		checkOneTerm(analyzer, "OO", "Ø");
		checkOneTerm(analyzer, "OE", "Ø");
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
		  private readonly TestScandinavianNormalizationFilter outerInstance;

		  public AnalyzerAnonymousInnerClassHelper2(TestScandinavianNormalizationFilter outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new KeywordTokenizer(reader);
			return new TokenStreamComponents(tokenizer, new ScandinavianNormalizationFilter(tokenizer));
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