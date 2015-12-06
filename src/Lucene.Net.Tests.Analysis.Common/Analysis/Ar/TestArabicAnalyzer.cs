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

	using CharArraySet = org.apache.lucene.analysis.util.CharArraySet;

	/// <summary>
	/// Test the Arabic Analyzer
	/// 
	/// </summary>
	public class TestArabicAnalyzer : BaseTokenStreamTestCase
	{

	  /// <summary>
	  /// This test fails with NPE when the 
	  /// stopwords file is missing in classpath 
	  /// </summary>
	  public virtual void testResourcesAvailable()
	  {
		new ArabicAnalyzer(TEST_VERSION_CURRENT);
	  }

	  /// <summary>
	  /// Some simple tests showing some features of the analyzer, how some regular forms will conflate
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBasicFeatures() throws Exception
	  public virtual void testBasicFeatures()
	  {
		ArabicAnalyzer a = new ArabicAnalyzer(TEST_VERSION_CURRENT);
		assertAnalyzesTo(a, "كبير", new string[] {"كبير"});
		assertAnalyzesTo(a, "كبيرة", new string[] {"كبير"}); // feminine marker

		assertAnalyzesTo(a, "مشروب", new string[] {"مشروب"});
		assertAnalyzesTo(a, "مشروبات", new string[] {"مشروب"}); // plural -at

		assertAnalyzesTo(a, "أمريكيين", new string[] {"امريك"}); // plural -in
		assertAnalyzesTo(a, "امريكي", new string[] {"امريك"}); // singular with bare alif

		assertAnalyzesTo(a, "كتاب", new string[] {"كتاب"});
		assertAnalyzesTo(a, "الكتاب", new string[] {"كتاب"}); // definite article

		assertAnalyzesTo(a, "ما ملكت أيمانكم", new string[] {"ملكت", "ايمانكم"});
		assertAnalyzesTo(a, "الذين ملكت أيمانكم", new string[] {"ملكت", "ايمانكم"}); // stopwords
	  }

	  /// <summary>
	  /// Simple tests to show things are getting reset correctly, etc.
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testReusableTokenStream() throws Exception
	  public virtual void testReusableTokenStream()
	  {
		ArabicAnalyzer a = new ArabicAnalyzer(TEST_VERSION_CURRENT);
		assertAnalyzesTo(a, "كبير", new string[] {"كبير"});
		assertAnalyzesTo(a, "كبيرة", new string[] {"كبير"}); // feminine marker
	  }

	  /// <summary>
	  /// Non-arabic text gets treated in a similar way as SimpleAnalyzer.
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testEnglishInput() throws Exception
	  public virtual void testEnglishInput()
	  {
		assertAnalyzesTo(new ArabicAnalyzer(TEST_VERSION_CURRENT), "English text.", new string[] {"english", "text"});
	  }

	  /// <summary>
	  /// Test that custom stopwords work, and are not case-sensitive.
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testCustomStopwords() throws Exception
	  public virtual void testCustomStopwords()
	  {
		CharArraySet set = new CharArraySet(TEST_VERSION_CURRENT, asSet("the", "and", "a"), false);
		ArabicAnalyzer a = new ArabicAnalyzer(TEST_VERSION_CURRENT, set);
		assertAnalyzesTo(a, "The quick brown fox.", new string[] {"quick", "brown", "fox"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testWithStemExclusionSet() throws java.io.IOException
	  public virtual void testWithStemExclusionSet()
	  {
		CharArraySet set = new CharArraySet(TEST_VERSION_CURRENT, asSet("ساهدهات"), false);
		ArabicAnalyzer a = new ArabicAnalyzer(TEST_VERSION_CURRENT, CharArraySet.EMPTY_SET, set);
		assertAnalyzesTo(a, "كبيرة the quick ساهدهات", new string[] {"كبير","the", "quick", "ساهدهات"});
		assertAnalyzesTo(a, "كبيرة the quick ساهدهات", new string[] {"كبير","the", "quick", "ساهدهات"});


		a = new ArabicAnalyzer(TEST_VERSION_CURRENT, CharArraySet.EMPTY_SET, CharArraySet.EMPTY_SET);
		assertAnalyzesTo(a, "كبيرة the quick ساهدهات", new string[] {"كبير","the", "quick", "ساهد"});
		assertAnalyzesTo(a, "كبيرة the quick ساهدهات", new string[] {"كبير","the", "quick", "ساهد"});
	  }

	  /// <summary>
	  /// blast some random strings through the analyzer </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandomStrings() throws Exception
	  public virtual void testRandomStrings()
	  {
		checkRandomData(random(), new ArabicAnalyzer(TEST_VERSION_CURRENT), 1000 * RANDOM_MULTIPLIER);
	  }
	}

}