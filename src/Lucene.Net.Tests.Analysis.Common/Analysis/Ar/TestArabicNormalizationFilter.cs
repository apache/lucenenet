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

	/// <summary>
	/// Test the Arabic Normalization Filter
	/// </summary>
	public class TestArabicNormalizationFilter : BaseTokenStreamTestCase
	{

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testAlifMadda() throws java.io.IOException
	  public virtual void testAlifMadda()
	  {
		check("آجن", "اجن");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testAlifHamzaAbove() throws java.io.IOException
	  public virtual void testAlifHamzaAbove()
	  {
		check("أحمد", "احمد");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testAlifHamzaBelow() throws java.io.IOException
	  public virtual void testAlifHamzaBelow()
	  {
		check("إعاذ", "اعاذ");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testAlifMaksura() throws java.io.IOException
	  public virtual void testAlifMaksura()
	  {
		check("بنى", "بني");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testTehMarbuta() throws java.io.IOException
	  public virtual void testTehMarbuta()
	  {
		check("فاطمة", "فاطمه");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testTatweel() throws java.io.IOException
	  public virtual void testTatweel()
	  {
		check("روبرـــــت", "روبرت");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testFatha() throws java.io.IOException
	  public virtual void testFatha()
	  {
		check("مَبنا", "مبنا");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testKasra() throws java.io.IOException
	  public virtual void testKasra()
	  {
		check("علِي", "علي");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testDamma() throws java.io.IOException
	  public virtual void testDamma()
	  {
		check("بُوات", "بوات");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testFathatan() throws java.io.IOException
	  public virtual void testFathatan()
	  {
		check("ولداً", "ولدا");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testKasratan() throws java.io.IOException
	  public virtual void testKasratan()
	  {
		check("ولدٍ", "ولد");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testDammatan() throws java.io.IOException
	  public virtual void testDammatan()
	  {
		check("ولدٌ", "ولد");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testSukun() throws java.io.IOException
	  public virtual void testSukun()
	  {
		check("نلْسون", "نلسون");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testShaddah() throws java.io.IOException
	  public virtual void testShaddah()
	  {
		check("هتميّ", "هتمي");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void check(final String input, final String expected) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
	  private void check(string input, string expected)
	  {
		ArabicLetterTokenizer tokenStream = new ArabicLetterTokenizer(TEST_VERSION_CURRENT, new StringReader(input));
		ArabicNormalizationFilter filter = new ArabicNormalizationFilter(tokenStream);
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
		  private readonly TestArabicNormalizationFilter outerInstance;

		  public AnalyzerAnonymousInnerClassHelper(TestArabicNormalizationFilter outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new KeywordTokenizer(reader);
			return new TokenStreamComponents(tokenizer, new ArabicNormalizationFilter(tokenizer));
		  }
	  }

	}

}