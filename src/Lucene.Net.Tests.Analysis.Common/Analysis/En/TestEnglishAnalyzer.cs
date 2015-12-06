namespace org.apache.lucene.analysis.en
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

	public class TestEnglishAnalyzer : BaseTokenStreamTestCase
	{
	  /// <summary>
	  /// This test fails with NPE when the 
	  /// stopwords file is missing in classpath 
	  /// </summary>
	  public virtual void testResourcesAvailable()
	  {
		new EnglishAnalyzer(TEST_VERSION_CURRENT);
	  }

	  /// <summary>
	  /// test stopwords and stemming </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBasics() throws java.io.IOException
	  public virtual void testBasics()
	  {
		Analyzer a = new EnglishAnalyzer(TEST_VERSION_CURRENT);
		// stemming
		checkOneTerm(a, "books", "book");
		checkOneTerm(a, "book", "book");
		// stopword
		assertAnalyzesTo(a, "the", new string[] {});
		// possessive removal
		checkOneTerm(a, "steven's", "steven");
		checkOneTerm(a, "steven\u2019s", "steven");
		checkOneTerm(a, "steven\uFF07s", "steven");
	  }

	  /// <summary>
	  /// test use of exclusion set </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testExclude() throws java.io.IOException
	  public virtual void testExclude()
	  {
		CharArraySet exclusionSet = new CharArraySet(TEST_VERSION_CURRENT, asSet("books"), false);
		Analyzer a = new EnglishAnalyzer(TEST_VERSION_CURRENT, EnglishAnalyzer.DefaultStopSet, exclusionSet);
		checkOneTerm(a, "books", "books");
		checkOneTerm(a, "book", "book");
	  }

	  /// <summary>
	  /// blast some random strings through the analyzer </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandomStrings() throws Exception
	  public virtual void testRandomStrings()
	  {
		checkRandomData(random(), new EnglishAnalyzer(TEST_VERSION_CURRENT), 1000 * RANDOM_MULTIPLIER);
	  }
	}

}