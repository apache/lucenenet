namespace org.apache.lucene.analysis.it
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

	public class TestItalianAnalyzer : BaseTokenStreamTestCase
	{
	  /// <summary>
	  /// This test fails with NPE when the 
	  /// stopwords file is missing in classpath 
	  /// </summary>
	  public virtual void testResourcesAvailable()
	  {
		new ItalianAnalyzer(TEST_VERSION_CURRENT);
	  }

	  /// <summary>
	  /// test stopwords and stemming </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBasics() throws java.io.IOException
	  public virtual void testBasics()
	  {
		Analyzer a = new ItalianAnalyzer(TEST_VERSION_CURRENT);
		// stemming
		checkOneTerm(a, "abbandonata", "abbandonat");
		checkOneTerm(a, "abbandonati", "abbandonat");
		// stopword
		assertAnalyzesTo(a, "dallo", new string[] {});
	  }

	  /// <summary>
	  /// test use of exclusion set </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testExclude() throws java.io.IOException
	  public virtual void testExclude()
	  {
		CharArraySet exclusionSet = new CharArraySet(TEST_VERSION_CURRENT, asSet("abbandonata"), false);
		Analyzer a = new ItalianAnalyzer(TEST_VERSION_CURRENT, ItalianAnalyzer.DefaultStopSet, exclusionSet);
		checkOneTerm(a, "abbandonata", "abbandonata");
		checkOneTerm(a, "abbandonati", "abbandonat");
	  }

	  /// <summary>
	  /// blast some random strings through the analyzer </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandomStrings() throws Exception
	  public virtual void testRandomStrings()
	  {
		checkRandomData(random(), new ItalianAnalyzer(TEST_VERSION_CURRENT), 1000 * RANDOM_MULTIPLIER);
	  }

	  /// <summary>
	  /// test that the elisionfilter is working </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testContractions() throws java.io.IOException
	  public virtual void testContractions()
	  {
		Analyzer a = new ItalianAnalyzer(TEST_VERSION_CURRENT);
		assertAnalyzesTo(a, "dell'Italia", new string[] {"ital"});
		assertAnalyzesTo(a, "l'Italiano", new string[] {"italian"});
	  }

	  /// <summary>
	  /// test that we don't enable this before 3.2 </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testContractionsBackwards() throws java.io.IOException
	  public virtual void testContractionsBackwards()
	  {
		Analyzer a = new ItalianAnalyzer(Version.LUCENE_31);
		assertAnalyzesTo(a, "dell'Italia", new string[] {"dell'ital"});
		assertAnalyzesTo(a, "l'Italiano", new string[] {"l'ital"});
	  }
	}

}