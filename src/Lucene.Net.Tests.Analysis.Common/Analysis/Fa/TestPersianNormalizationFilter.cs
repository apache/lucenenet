namespace org.apache.lucene.analysis.fa
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


	using ArabicLetterTokenizer = org.apache.lucene.analysis.ar.ArabicLetterTokenizer;
	using KeywordTokenizer = org.apache.lucene.analysis.core.KeywordTokenizer;

	/// <summary>
	/// Test the Persian Normalization Filter
	/// 
	/// </summary>
	public class TestPersianNormalizationFilter : BaseTokenStreamTestCase
	{

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testFarsiYeh() throws java.io.IOException
	  public virtual void testFarsiYeh()
	  {
		check("های", "هاي");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testYehBarree() throws java.io.IOException
	  public virtual void testYehBarree()
	  {
		check("هاے", "هاي");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testKeheh() throws java.io.IOException
	  public virtual void testKeheh()
	  {
		check("کشاندن", "كشاندن");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testHehYeh() throws java.io.IOException
	  public virtual void testHehYeh()
	  {
		check("كتابۀ", "كتابه");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testHehHamzaAbove() throws java.io.IOException
	  public virtual void testHehHamzaAbove()
	  {
		check("كتابهٔ", "كتابه");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testHehGoal() throws java.io.IOException
	  public virtual void testHehGoal()
	  {
		check("زادہ", "زاده");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void check(final String input, final String expected) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
	  private void check(string input, string expected)
	  {
		ArabicLetterTokenizer tokenStream = new ArabicLetterTokenizer(TEST_VERSION_CURRENT, new StringReader(input));
		PersianNormalizationFilter filter = new PersianNormalizationFilter(tokenStream);
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
		  private readonly TestPersianNormalizationFilter outerInstance;

		  public AnalyzerAnonymousInnerClassHelper(TestPersianNormalizationFilter outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new KeywordTokenizer(reader);
			return new TokenStreamComponents(tokenizer, new PersianNormalizationFilter(tokenizer));
		  }
	  }

	}

}