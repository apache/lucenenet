namespace org.apache.lucene.analysis.gl
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
	using SetKeywordMarkerFilter = org.apache.lucene.analysis.miscellaneous.SetKeywordMarkerFilter;
	using CharArraySet = org.apache.lucene.analysis.util.CharArraySet;

	/// <summary>
	/// Simple tests for <seealso cref="GalicianMinimalStemmer"/>
	/// </summary>
	public class TestGalicianMinimalStemFilter : BaseTokenStreamTestCase
	{
	  internal Analyzer a = new AnalyzerAnonymousInnerClassHelper();

	  private class AnalyzerAnonymousInnerClassHelper : Analyzer
	  {
		  public AnalyzerAnonymousInnerClassHelper()
		  {
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
			return new TokenStreamComponents(tokenizer, new GalicianMinimalStemFilter(tokenizer));
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testPlural() throws Exception
	  public virtual void testPlural()
	  {
		checkOneTerm(a, "elefantes", "elefante");
		checkOneTerm(a, "elefante", "elefante");
		checkOneTerm(a, "kalóres", "kalór");
		checkOneTerm(a, "kalór", "kalór");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testExceptions() throws Exception
	  public virtual void testExceptions()
	  {
		checkOneTerm(a, "mas", "mas");
		checkOneTerm(a, "barcelonês", "barcelonês");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testKeyword() throws java.io.IOException
	  public virtual void testKeyword()
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.analysis.util.CharArraySet exclusionSet = new org.apache.lucene.analysis.util.CharArraySet(TEST_VERSION_CURRENT, asSet("elefantes"), false);
		CharArraySet exclusionSet = new CharArraySet(TEST_VERSION_CURRENT, asSet("elefantes"), false);
		Analyzer a = new AnalyzerAnonymousInnerClassHelper2(this, exclusionSet);
		checkOneTerm(a, "elefantes", "elefantes");
	  }

	  private class AnalyzerAnonymousInnerClassHelper2 : Analyzer
	  {
		  private readonly TestGalicianMinimalStemFilter outerInstance;

		  private CharArraySet exclusionSet;

		  public AnalyzerAnonymousInnerClassHelper2(TestGalicianMinimalStemFilter outerInstance, CharArraySet exclusionSet)
		  {
			  this.outerInstance = outerInstance;
			  this.exclusionSet = exclusionSet;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer source = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
			TokenStream sink = new SetKeywordMarkerFilter(source, exclusionSet);
			return new TokenStreamComponents(source, new GalicianMinimalStemFilter(sink));
		  }
	  }

	  /// <summary>
	  /// blast some random strings through the analyzer </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandomStrings() throws Exception
	  public virtual void testRandomStrings()
	  {
		checkRandomData(random(), a, 1000 * RANDOM_MULTIPLIER);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testEmptyTerm() throws java.io.IOException
	  public virtual void testEmptyTerm()
	  {
		Analyzer a = new AnalyzerAnonymousInnerClassHelper3(this);
		checkOneTerm(a, "", "");
	  }

	  private class AnalyzerAnonymousInnerClassHelper3 : Analyzer
	  {
		  private readonly TestGalicianMinimalStemFilter outerInstance;

		  public AnalyzerAnonymousInnerClassHelper3(TestGalicianMinimalStemFilter outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new KeywordTokenizer(reader);
			return new TokenStreamComponents(tokenizer, new GalicianMinimalStemFilter(tokenizer));
		  }
	  }
	}

}