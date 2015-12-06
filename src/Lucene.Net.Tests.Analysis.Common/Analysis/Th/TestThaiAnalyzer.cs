using System;

namespace org.apache.lucene.analysis.th
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
	using StopAnalyzer = org.apache.lucene.analysis.core.StopAnalyzer;
	using FlagsAttribute = org.apache.lucene.analysis.tokenattributes.FlagsAttribute;
	using CharArraySet = org.apache.lucene.analysis.util.CharArraySet;
	using Version = org.apache.lucene.util.Version;

	/// <summary>
	/// Test case for ThaiAnalyzer, modified from TestFrenchAnalyzer
	/// 
	/// </summary>

	public class TestThaiAnalyzer : BaseTokenStreamTestCase
	{

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void setUp() throws Exception
	  public override void setUp()
	  {
		base.setUp();
		assumeTrue("JRE does not support Thai dictionary-based BreakIterator", ThaiTokenizer.DBBI_AVAILABLE);
	  }
	  /*
	   * testcase for offsets
	   */
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testOffsets() throws Exception
	  public virtual void testOffsets()
	  {
		assertAnalyzesTo(new ThaiAnalyzer(TEST_VERSION_CURRENT, CharArraySet.EMPTY_SET), "การที่ได้ต้องแสดงว่างานดี", new string[] {"การ", "ที่", "ได้", "ต้อง", "แสดง", "ว่า", "งาน", "ดี"}, new int[] {0, 3, 6, 9, 13, 17, 20, 23}, new int[] {3, 6, 9, 13, 17, 20, 23, 25});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testStopWords() throws Exception
	  public virtual void testStopWords()
	  {
		assertAnalyzesTo(new ThaiAnalyzer(TEST_VERSION_CURRENT), "การที่ได้ต้องแสดงว่างานดี", new string[] {"แสดง", "งาน", "ดี"}, new int[] {13, 20, 23}, new int[] {17, 23, 25}, new int[] {5, 2, 1});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBackwardsStopWords() throws Exception
	  public virtual void testBackwardsStopWords()
	  {
		 assertAnalyzesTo(new ThaiAnalyzer(Version.LUCENE_35), "การที่ได้ต้องแสดงว่างานดี", new string[] {"การ", "ที่", "ได้", "ต้อง", "แสดง", "ว่า", "งาน", "ดี"}, new int[] {0, 3, 6, 9, 13, 17, 20, 23}, new int[] {3, 6, 9, 13, 17, 20, 23, 25});
	  }

	  /// <summary>
	  /// Thai numeric tokens were typed as <ALPHANUM> instead of <NUM>. </summary>
	  /// @deprecated (3.1) testing backwards behavior 
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Deprecated("(3.1) testing backwards behavior") public void testBuggyTokenType30() throws Exception
	  [Obsolete("(3.1) testing backwards behavior")]
	  public virtual void testBuggyTokenType30()
	  {
		assertAnalyzesTo(new ThaiAnalyzer(Version.LUCENE_30), "การที่ได้ต้องแสดงว่างานดี ๑๒๓", new string[] {"การ", "ที่", "ได้", "ต้อง", "แสดง", "ว่า", "งาน", "ดี", "๑๒๓"}, new string[] {"<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>"});
	  }

	  /// @deprecated (3.1) testing backwards behavior 
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Deprecated("(3.1) testing backwards behavior") public void testAnalyzer30() throws Exception
	  [Obsolete("(3.1) testing backwards behavior")]
	  public virtual void testAnalyzer30()
	  {
			ThaiAnalyzer analyzer = new ThaiAnalyzer(Version.LUCENE_30);

		assertAnalyzesTo(analyzer, "", new string[] {});

		assertAnalyzesTo(analyzer, "การที่ได้ต้องแสดงว่างานดี", new string[] {"การ", "ที่", "ได้", "ต้อง", "แสดง", "ว่า", "งาน", "ดี"});

		assertAnalyzesTo(analyzer, "บริษัทชื่อ XY&Z - คุยกับ xyz@demo.com", new string[] {"บริษัท", "ชื่อ", "xy&z", "คุย", "กับ", "xyz@demo.com"});

		// English stop words
		assertAnalyzesTo(analyzer, "ประโยคว่า The quick brown fox jumped over the lazy dogs", new string[] {"ประโยค", "ว่า", "quick", "brown", "fox", "jumped", "over", "lazy", "dogs"});
	  }

	  /*
	   * Test that position increments are adjusted correctly for stopwords.
	   */
	  // note this test uses stopfilter's stopset
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testPositionIncrements() throws Exception
	  public virtual void testPositionIncrements()
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final ThaiAnalyzer analyzer = new ThaiAnalyzer(TEST_VERSION_CURRENT, org.apache.lucene.analysis.core.StopAnalyzer.ENGLISH_STOP_WORDS_SET);
		ThaiAnalyzer analyzer = new ThaiAnalyzer(TEST_VERSION_CURRENT, StopAnalyzer.ENGLISH_STOP_WORDS_SET);
		assertAnalyzesTo(analyzer, "การที่ได้ต้อง the แสดงว่างานดี", new string[] {"การ", "ที่", "ได้", "ต้อง", "แสดง", "ว่า", "งาน", "ดี"}, new int[] {0, 3, 6, 9, 18, 22, 25, 28}, new int[] {3, 6, 9, 13, 22, 25, 28, 30}, new int[] {1, 1, 1, 1, 2, 1, 1, 1});

		// case that a stopword is adjacent to thai text, with no whitespace
		assertAnalyzesTo(analyzer, "การที่ได้ต้องthe แสดงว่างานดี", new string[] {"การ", "ที่", "ได้", "ต้อง", "แสดง", "ว่า", "งาน", "ดี"}, new int[] {0, 3, 6, 9, 17, 21, 24, 27}, new int[] {3, 6, 9, 13, 21, 24, 27, 29}, new int[] {1, 1, 1, 1, 2, 1, 1, 1});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testReusableTokenStream() throws Exception
	  public virtual void testReusableTokenStream()
	  {
		ThaiAnalyzer analyzer = new ThaiAnalyzer(TEST_VERSION_CURRENT, CharArraySet.EMPTY_SET);
		assertAnalyzesTo(analyzer, "", new string[] {});

		  assertAnalyzesTo(analyzer, "การที่ได้ต้องแสดงว่างานดี", new string[] {"การ", "ที่", "ได้", "ต้อง", "แสดง", "ว่า", "งาน", "ดี"});

		  assertAnalyzesTo(analyzer, "บริษัทชื่อ XY&Z - คุยกับ xyz@demo.com", new string[] {"บริษัท", "ชื่อ", "xy", "z", "คุย", "กับ", "xyz", "demo.com"});
	  }

	  /// @deprecated (3.1) for version back compat 
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Deprecated("(3.1) for version back compat") public void testReusableTokenStream30() throws Exception
	  [Obsolete("(3.1) for version back compat")]
	  public virtual void testReusableTokenStream30()
	  {
		  ThaiAnalyzer analyzer = new ThaiAnalyzer(Version.LUCENE_30);
		  assertAnalyzesTo(analyzer, "", new string[] {});

		  assertAnalyzesTo(analyzer, "การที่ได้ต้องแสดงว่างานดี", new string[] {"การ", "ที่", "ได้", "ต้อง", "แสดง", "ว่า", "งาน", "ดี"});

		  assertAnalyzesTo(analyzer, "บริษัทชื่อ XY&Z - คุยกับ xyz@demo.com", new string[] {"บริษัท", "ชื่อ", "xy&z", "คุย", "กับ", "xyz@demo.com"});
	  }

	  /// <summary>
	  /// blast some random strings through the analyzer </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandomStrings() throws Exception
	  public virtual void testRandomStrings()
	  {
		checkRandomData(random(), new ThaiAnalyzer(TEST_VERSION_CURRENT), 1000 * RANDOM_MULTIPLIER);
	  }

	  /// <summary>
	  /// blast some random large strings through the analyzer </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandomHugeStrings() throws Exception
	  public virtual void testRandomHugeStrings()
	  {
		Random random = random();
		checkRandomData(random, new ThaiAnalyzer(TEST_VERSION_CURRENT), 100 * RANDOM_MULTIPLIER, 8192);
	  }

	  // LUCENE-3044
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testAttributeReuse() throws Exception
	  public virtual void testAttributeReuse()
	  {
		ThaiAnalyzer analyzer = new ThaiAnalyzer(Version.LUCENE_30);
		// just consume
		TokenStream ts = analyzer.tokenStream("dummy", "ภาษาไทย");
		assertTokenStreamContents(ts, new string[] {"ภาษา", "ไทย"});
		// this consumer adds flagsAtt, which this analyzer does not use. 
		ts = analyzer.tokenStream("dummy", "ภาษาไทย");
		ts.addAttribute(typeof(FlagsAttribute));
		assertTokenStreamContents(ts, new string[] {"ภาษา", "ไทย"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testTwoSentences() throws Exception
	  public virtual void testTwoSentences()
	  {
		assertAnalyzesTo(new ThaiAnalyzer(TEST_VERSION_CURRENT, CharArraySet.EMPTY_SET), "This is a test. การที่ได้ต้องแสดงว่างานดี", new string[] {"this", "is", "a", "test", "การ", "ที่", "ได้", "ต้อง", "แสดง", "ว่า", "งาน", "ดี"}, new int[] {0, 5, 8, 10, 16, 19, 22, 25, 29, 33, 36, 39}, new int[] {4, 7, 9, 14, 19, 22, 25, 29, 33, 36, 39, 41});
	  }
	}

}