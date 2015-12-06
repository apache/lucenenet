namespace org.apache.lucene.analysis.ckb
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

//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to C#:
//	import static org.apache.lucene.analysis.VocabularyAssert.assertVocabulary;


	using KeywordTokenizer = org.apache.lucene.analysis.core.KeywordTokenizer;

	/// <summary>
	/// Test the Sorani Stemmer.
	/// </summary>
	public class TestSoraniStemFilter : BaseTokenStreamTestCase
	{
	  internal SoraniAnalyzer a = new SoraniAnalyzer(TEST_VERSION_CURRENT);

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testIndefiniteSingular() throws Exception
	  public virtual void testIndefiniteSingular()
	  {
		checkOneTerm(a, "پیاوێک", "پیاو"); // -ek
		checkOneTerm(a, "دەرگایەک", "دەرگا"); // -yek
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testDefiniteSingular() throws Exception
	  public virtual void testDefiniteSingular()
	  {
		checkOneTerm(a, "پیاوەكە", "پیاو"); // -aka
		checkOneTerm(a, "دەرگاكە", "دەرگا"); // -ka
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testDemonstrativeSingular() throws Exception
	  public virtual void testDemonstrativeSingular()
	  {
		checkOneTerm(a, "کتاویە", "کتاوی"); // -a
		checkOneTerm(a, "دەرگایە", "دەرگا"); // -ya
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testIndefinitePlural() throws Exception
	  public virtual void testIndefinitePlural()
	  {
		checkOneTerm(a, "پیاوان", "پیاو"); // -An
		checkOneTerm(a, "دەرگایان", "دەرگا"); // -yAn
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testDefinitePlural() throws Exception
	  public virtual void testDefinitePlural()
	  {
		checkOneTerm(a, "پیاوەکان", "پیاو"); // -akAn
		checkOneTerm(a, "دەرگاکان", "دەرگا"); // -kAn
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testDemonstrativePlural() throws Exception
	  public virtual void testDemonstrativePlural()
	  {
		checkOneTerm(a, "پیاوانە", "پیاو"); // -Ana
		checkOneTerm(a, "دەرگایانە", "دەرگا"); // -yAna
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testEzafe() throws Exception
	  public virtual void testEzafe()
	  {
		checkOneTerm(a, "هۆتیلی", "هۆتیل"); // singular
		checkOneTerm(a, "هۆتیلێکی", "هۆتیل"); // indefinite
		checkOneTerm(a, "هۆتیلانی", "هۆتیل"); // plural
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testPostpositions() throws Exception
	  public virtual void testPostpositions()
	  {
		checkOneTerm(a, "دوورەوە", "دوور"); // -awa
		checkOneTerm(a, "نیوەشەودا", "نیوەشەو"); // -dA
		checkOneTerm(a, "سۆرانا", "سۆران"); // -A
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testPossessives() throws Exception
	  public virtual void testPossessives()
	  {
		checkOneTerm(a, "پارەمان", "پارە"); // -mAn
		checkOneTerm(a, "پارەتان", "پارە"); // -tAn
		checkOneTerm(a, "پارەیان", "پارە"); // -yAn
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
		  private readonly TestSoraniStemFilter outerInstance;

		  public AnalyzerAnonymousInnerClassHelper(TestSoraniStemFilter outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new KeywordTokenizer(reader);
			return new TokenStreamComponents(tokenizer, new SoraniStemFilter(tokenizer));
		  }
	  }

	  /// <summary>
	  /// test against a basic vocabulary file </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testVocabulary() throws Exception
	  public virtual void testVocabulary()
	  {
		// top 8k words or so: freq > 1000
		assertVocabulary(a, getDataFile("ckbtestdata.zip"), "testdata.txt");
	  }
	}

}