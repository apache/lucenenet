namespace org.apache.lucene.analysis.hi
{

	using CharArraySet = org.apache.lucene.analysis.util.CharArraySet;

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

	/// <summary>
	/// Tests the HindiAnalyzer
	/// </summary>
	public class TestHindiAnalyzer : BaseTokenStreamTestCase
	{
	  /// <summary>
	  /// This test fails with NPE when the 
	  /// stopwords file is missing in classpath 
	  /// </summary>
	  public virtual void testResourcesAvailable()
	  {
		new HindiAnalyzer(TEST_VERSION_CURRENT);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBasics() throws Exception
	  public virtual void testBasics()
	  {
		Analyzer a = new HindiAnalyzer(TEST_VERSION_CURRENT);
		// two ways to write 'hindi' itself.
		checkOneTerm(a, "हिन्दी", "हिंद");
		checkOneTerm(a, "हिंदी", "हिंद");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testExclusionSet() throws Exception
	  public virtual void testExclusionSet()
	  {
		CharArraySet exclusionSet = new CharArraySet(TEST_VERSION_CURRENT, asSet("हिंदी"), false);
		Analyzer a = new HindiAnalyzer(TEST_VERSION_CURRENT, HindiAnalyzer.DefaultStopSet, exclusionSet);
		checkOneTerm(a, "हिंदी", "हिंदी");
	  }

	  /// <summary>
	  /// blast some random strings through the analyzer </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandomStrings() throws Exception
	  public virtual void testRandomStrings()
	  {
		checkRandomData(random(), new HindiAnalyzer(TEST_VERSION_CURRENT), 1000 * RANDOM_MULTIPLIER);
	  }
	}

}