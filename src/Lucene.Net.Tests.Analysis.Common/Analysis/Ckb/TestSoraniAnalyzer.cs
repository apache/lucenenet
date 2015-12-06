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

	using CharArraySet = org.apache.lucene.analysis.util.CharArraySet;

	/// <summary>
	/// Test the Sorani analyzer
	/// </summary>
	public class TestSoraniAnalyzer : BaseTokenStreamTestCase
	{

	  /// <summary>
	  /// This test fails with NPE when the stopwords file is missing in classpath
	  /// </summary>
	  public virtual void testResourcesAvailable()
	  {
		new SoraniAnalyzer(TEST_VERSION_CURRENT);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testStopwords() throws java.io.IOException
	  public virtual void testStopwords()
	  {
		Analyzer a = new SoraniAnalyzer(TEST_VERSION_CURRENT);
		assertAnalyzesTo(a, "ئەم پیاوە", new string[] {"پیاو"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testCustomStopwords() throws java.io.IOException
	  public virtual void testCustomStopwords()
	  {
		Analyzer a = new SoraniAnalyzer(TEST_VERSION_CURRENT, CharArraySet.EMPTY_SET);
		assertAnalyzesTo(a, "ئەم پیاوە", new string[] {"ئەم", "پیاو"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testReusableTokenStream() throws java.io.IOException
	  public virtual void testReusableTokenStream()
	  {
		Analyzer a = new SoraniAnalyzer(TEST_VERSION_CURRENT);
		assertAnalyzesTo(a, "پیاوە", new string[] {"پیاو"});
		assertAnalyzesTo(a, "پیاو", new string[] {"پیاو"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testWithStemExclusionSet() throws java.io.IOException
	  public virtual void testWithStemExclusionSet()
	  {
		CharArraySet set = new CharArraySet(TEST_VERSION_CURRENT, 1, true);
		set.add("پیاوە");
		Analyzer a = new SoraniAnalyzer(TEST_VERSION_CURRENT, CharArraySet.EMPTY_SET, set);
		assertAnalyzesTo(a, "پیاوە", new string[] {"پیاوە"});
	  }

	  /// <summary>
	  /// blast some random strings through the analyzer </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandomStrings() throws Exception
	  public virtual void testRandomStrings()
	  {
		checkRandomData(random(), new SoraniAnalyzer(TEST_VERSION_CURRENT), 1000 * RANDOM_MULTIPLIER);
	  }
	}

}