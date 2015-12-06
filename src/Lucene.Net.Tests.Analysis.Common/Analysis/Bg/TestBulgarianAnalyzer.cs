namespace org.apache.lucene.analysis.bg
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
	using Version = org.apache.lucene.util.Version;

	/// <summary>
	/// Test the Bulgarian analyzer
	/// </summary>
	public class TestBulgarianAnalyzer : BaseTokenStreamTestCase
	{

	  /// <summary>
	  /// This test fails with NPE when the stopwords file is missing in classpath
	  /// </summary>
	  public virtual void testResourcesAvailable()
	  {
		new BulgarianAnalyzer(TEST_VERSION_CURRENT);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testStopwords() throws java.io.IOException
	  public virtual void testStopwords()
	  {
		Analyzer a = new BulgarianAnalyzer(TEST_VERSION_CURRENT);
		assertAnalyzesTo(a, "Как се казваш?", new string[] {"казваш"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testCustomStopwords() throws java.io.IOException
	  public virtual void testCustomStopwords()
	  {
		Analyzer a = new BulgarianAnalyzer(TEST_VERSION_CURRENT, CharArraySet.EMPTY_SET);
		assertAnalyzesTo(a, "Как се казваш?", new string[] {"как", "се", "казваш"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testReusableTokenStream() throws java.io.IOException
	  public virtual void testReusableTokenStream()
	  {
		Analyzer a = new BulgarianAnalyzer(TEST_VERSION_CURRENT);
		assertAnalyzesTo(a, "документи", new string[] {"документ"});
		assertAnalyzesTo(a, "документ", new string[] {"документ"});
	  }

	  /// <summary>
	  /// Test some examples from the paper
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBasicExamples() throws java.io.IOException
	  public virtual void testBasicExamples()
	  {
		Analyzer a = new BulgarianAnalyzer(TEST_VERSION_CURRENT);
		assertAnalyzesTo(a, "енергийни кризи", new string[] {"енергийн", "криз"});
		assertAnalyzesTo(a, "Атомната енергия", new string[] {"атомн", "енерг"});

		assertAnalyzesTo(a, "компютри", new string[] {"компютр"});
		assertAnalyzesTo(a, "компютър", new string[] {"компютр"});

		assertAnalyzesTo(a, "градове", new string[] {"град"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testWithStemExclusionSet() throws java.io.IOException
	  public virtual void testWithStemExclusionSet()
	  {
		CharArraySet set = new CharArraySet(TEST_VERSION_CURRENT, 1, true);
		set.add("строеве");
		Analyzer a = new BulgarianAnalyzer(TEST_VERSION_CURRENT, CharArraySet.EMPTY_SET, set);
		assertAnalyzesTo(a, "строевете строеве", new string[] {"строй", "строеве"});
	  }

	  /// <summary>
	  /// blast some random strings through the analyzer </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandomStrings() throws Exception
	  public virtual void testRandomStrings()
	  {
		checkRandomData(random(), new BulgarianAnalyzer(TEST_VERSION_CURRENT), 1000 * RANDOM_MULTIPLIER);
	  }
	}

}