namespace org.apache.lucene.analysis.ar
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
	/// Test the Arabic Normalization Filter
	/// 
	/// </summary>
	public class TestArabicStemFilter : BaseTokenStreamTestCase
	{

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testAlPrefix() throws java.io.IOException
	  public virtual void testAlPrefix()
	  {
		check("الحسن", "حسن");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testWalPrefix() throws java.io.IOException
	  public virtual void testWalPrefix()
	  {
		check("والحسن", "حسن");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBalPrefix() throws java.io.IOException
	  public virtual void testBalPrefix()
	  {
		check("بالحسن", "حسن");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testKalPrefix() throws java.io.IOException
	  public virtual void testKalPrefix()
	  {
		check("كالحسن", "حسن");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testFalPrefix() throws java.io.IOException
	  public virtual void testFalPrefix()
	  {
		check("فالحسن", "حسن");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testLlPrefix() throws java.io.IOException
	  public virtual void testLlPrefix()
	  {
		check("للاخر", "اخر");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testWaPrefix() throws java.io.IOException
	  public virtual void testWaPrefix()
	  {
		check("وحسن", "حسن");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testAhSuffix() throws java.io.IOException
	  public virtual void testAhSuffix()
	  {
		check("زوجها", "زوج");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testAnSuffix() throws java.io.IOException
	  public virtual void testAnSuffix()
	  {
		check("ساهدان", "ساهد");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testAtSuffix() throws java.io.IOException
	  public virtual void testAtSuffix()
	  {
		check("ساهدات", "ساهد");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testWnSuffix() throws java.io.IOException
	  public virtual void testWnSuffix()
	  {
		check("ساهدون", "ساهد");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testYnSuffix() throws java.io.IOException
	  public virtual void testYnSuffix()
	  {
		check("ساهدين", "ساهد");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testYhSuffix() throws java.io.IOException
	  public virtual void testYhSuffix()
	  {
		check("ساهديه", "ساهد");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testYpSuffix() throws java.io.IOException
	  public virtual void testYpSuffix()
	  {
		check("ساهدية", "ساهد");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testHSuffix() throws java.io.IOException
	  public virtual void testHSuffix()
	  {
		check("ساهده", "ساهد");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testPSuffix() throws java.io.IOException
	  public virtual void testPSuffix()
	  {
		check("ساهدة", "ساهد");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testYSuffix() throws java.io.IOException
	  public virtual void testYSuffix()
	  {
		check("ساهدي", "ساهد");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testComboPrefSuf() throws java.io.IOException
	  public virtual void testComboPrefSuf()
	  {
		check("وساهدون", "ساهد");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testComboSuf() throws java.io.IOException
	  public virtual void testComboSuf()
	  {
		check("ساهدهات", "ساهد");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testShouldntStem() throws java.io.IOException
	  public virtual void testShouldntStem()
	  {
		check("الو", "الو");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testNonArabic() throws java.io.IOException
	  public virtual void testNonArabic()
	  {
		check("English", "English");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testWithKeywordAttribute() throws java.io.IOException
	  public virtual void testWithKeywordAttribute()
	  {
		CharArraySet set = new CharArraySet(TEST_VERSION_CURRENT, 1, true);
		set.add("ساهدهات");
		ArabicLetterTokenizer tokenStream = new ArabicLetterTokenizer(TEST_VERSION_CURRENT, new StringReader("ساهدهات"));

		ArabicStemFilter filter = new ArabicStemFilter(new SetKeywordMarkerFilter(tokenStream, set));
		assertTokenStreamContents(filter, new string[]{"ساهدهات"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void check(final String input, final String expected) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
	  private void check(string input, string expected)
	  {
		ArabicLetterTokenizer tokenStream = new ArabicLetterTokenizer(TEST_VERSION_CURRENT, new StringReader(input));
		ArabicStemFilter filter = new ArabicStemFilter(tokenStream);
		assertTokenStreamContents(filter, new string[]{expected});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testEmptyTerm() throws java.io.IOException
	  public virtual void testEmptyTerm()
	  {
		Analyzer a = new AnalyzerAnonymousInnerClassHelper(this);
		checkOneTerm(a, "", "");
	  }

	  private class AnalyzerAnonymousInnerClassHelper : Analyzer
	  {
		  private readonly TestArabicStemFilter outerInstance;

		  public AnalyzerAnonymousInnerClassHelper(TestArabicStemFilter outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new KeywordTokenizer(reader);
			return new TokenStreamComponents(tokenizer, new ArabicStemFilter(tokenizer));
		  }
	  }
	}

}