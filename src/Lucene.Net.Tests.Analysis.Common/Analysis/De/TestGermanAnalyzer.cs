namespace org.apache.lucene.analysis.de
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


	using LowerCaseTokenizer = org.apache.lucene.analysis.core.LowerCaseTokenizer;
	using SetKeywordMarkerFilter = org.apache.lucene.analysis.miscellaneous.SetKeywordMarkerFilter;
	using CharArraySet = org.apache.lucene.analysis.util.CharArraySet;
	using Version = org.apache.lucene.util.Version;

	public class TestGermanAnalyzer : BaseTokenStreamTestCase
	{
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testReusableTokenStream() throws Exception
	  public virtual void testReusableTokenStream()
	  {
		Analyzer a = new GermanAnalyzer(TEST_VERSION_CURRENT);
		checkOneTerm(a, "Tisch", "tisch");
		checkOneTerm(a, "Tische", "tisch");
		checkOneTerm(a, "Tischen", "tisch");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testWithKeywordAttribute() throws java.io.IOException
	  public virtual void testWithKeywordAttribute()
	  {
		CharArraySet set = new CharArraySet(TEST_VERSION_CURRENT, 1, true);
		set.add("fischen");
		GermanStemFilter filter = new GermanStemFilter(new SetKeywordMarkerFilter(new LowerCaseTokenizer(TEST_VERSION_CURRENT, new StringReader("Fischen Trinken")), set));
		assertTokenStreamContents(filter, new string[] {"fischen", "trink"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testStemExclusionTable() throws Exception
	  public virtual void testStemExclusionTable()
	  {
		GermanAnalyzer a = new GermanAnalyzer(TEST_VERSION_CURRENT, CharArraySet.EMPTY_SET, new CharArraySet(TEST_VERSION_CURRENT, asSet("tischen"), false));
		checkOneTerm(a, "tischen", "tischen");
	  }

	  /// <summary>
	  /// test some features of the new snowball filter
	  /// these only pass with LUCENE_CURRENT, not if you use o.a.l.a.de.GermanStemmer
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testGermanSpecials() throws Exception
	  public virtual void testGermanSpecials()
	  {
		GermanAnalyzer a = new GermanAnalyzer(TEST_VERSION_CURRENT);
		// a/o/u + e is equivalent to the umlaut form
		checkOneTerm(a, "Schaltflächen", "schaltflach");
		checkOneTerm(a, "Schaltflaechen", "schaltflach");
		// here they are with the old stemmer
		a = new GermanAnalyzer(Version.LUCENE_30);
		checkOneTerm(a, "Schaltflächen", "schaltflach");
		checkOneTerm(a, "Schaltflaechen", "schaltflaech");
	  }

	  /// <summary>
	  /// blast some random strings through the analyzer </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandomStrings() throws Exception
	  public virtual void testRandomStrings()
	  {
		checkRandomData(random(), new GermanAnalyzer(TEST_VERSION_CURRENT), 1000 * RANDOM_MULTIPLIER);
	  }
	}

}