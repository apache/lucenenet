using System;

namespace org.apache.lucene.analysis.nl
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

	using CharArrayMap = org.apache.lucene.analysis.util.CharArrayMap;
	using CharArraySet = org.apache.lucene.analysis.util.CharArraySet;
	using Version = org.apache.lucene.util.Version;

	/// <summary>
	/// Test the Dutch Stem Filter, which only modifies the term text.
	/// 
	/// The code states that it uses the snowball algorithm, but tests reveal some differences.
	/// 
	/// </summary>
	public class TestDutchStemmer : BaseTokenStreamTestCase
	{

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testWithSnowballExamples() throws Exception
	  public virtual void testWithSnowballExamples()
	  {
	   check("lichaamsziek", "lichaamsziek");
	   check("lichamelijk", "licham");
	   check("lichamelijke", "licham");
	   check("lichamelijkheden", "licham");
	   check("lichamen", "licham");
	   check("lichere", "licher");
	   check("licht", "licht");
	   check("lichtbeeld", "lichtbeeld");
	   check("lichtbruin", "lichtbruin");
	   check("lichtdoorlatende", "lichtdoorlat");
	   check("lichte", "licht");
	   check("lichten", "licht");
	   check("lichtende", "lichtend");
	   check("lichtenvoorde", "lichtenvoord");
	   check("lichter", "lichter");
	   check("lichtere", "lichter");
	   check("lichters", "lichter");
	   check("lichtgevoeligheid", "lichtgevoel");
	   check("lichtgewicht", "lichtgewicht");
	   check("lichtgrijs", "lichtgrijs");
	   check("lichthoeveelheid", "lichthoevel");
	   check("lichtintensiteit", "lichtintensiteit");
	   check("lichtje", "lichtj");
	   check("lichtjes", "lichtjes");
	   check("lichtkranten", "lichtkrant");
	   check("lichtkring", "lichtkring");
	   check("lichtkringen", "lichtkring");
	   check("lichtregelsystemen", "lichtregelsystem");
	   check("lichtste", "lichtst");
	   check("lichtstromende", "lichtstrom");
	   check("lichtte", "licht");
	   check("lichtten", "licht");
	   check("lichttoetreding", "lichttoetred");
	   check("lichtverontreinigde", "lichtverontreinigd");
	   check("lichtzinnige", "lichtzinn");
	   check("lid", "lid");
	   check("lidia", "lidia");
	   check("lidmaatschap", "lidmaatschap");
	   check("lidstaten", "lidstat");
	   check("lidvereniging", "lidveren");
	   check("opgingen", "opging");
	   check("opglanzing", "opglanz");
	   check("opglanzingen", "opglanz");
	   check("opglimlachten", "opglimlacht");
	   check("opglimpen", "opglimp");
	   check("opglimpende", "opglimp");
	   check("opglimping", "opglimp");
	   check("opglimpingen", "opglimp");
	   check("opgraven", "opgrav");
	   check("opgrijnzen", "opgrijnz");
	   check("opgrijzende", "opgrijz");
	   check("opgroeien", "opgroei");
	   check("opgroeiende", "opgroei");
	   check("opgroeiplaats", "opgroeiplat");
	   check("ophaal", "ophal");
	   check("ophaaldienst", "ophaaldienst");
	   check("ophaalkosten", "ophaalkost");
	   check("ophaalsystemen", "ophaalsystem");
	   check("ophaalt", "ophaalt");
	   check("ophaaltruck", "ophaaltruck");
	   check("ophalen", "ophal");
	   check("ophalend", "ophal");
	   check("ophalers", "ophaler");
	   check("ophef", "ophef");
	   check("opheldering", "ophelder");
	   check("ophemelde", "ophemeld");
	   check("ophemelen", "ophemel");
	   check("opheusden", "opheusd");
	   check("ophief", "ophief");
	   check("ophield", "ophield");
	   check("ophieven", "ophiev");
	   check("ophoepelt", "ophoepelt");
	   check("ophoog", "ophog");
	   check("ophoogzand", "ophoogzand");
	   check("ophopen", "ophop");
	   check("ophoping", "ophop");
	   check("ophouden", "ophoud");
	  }

	  /// @deprecated (3.1) remove this test in Lucene 5.0 
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Deprecated("(3.1) remove this test in Lucene 5.0") public void testOldBuggyStemmer() throws Exception
	  [Obsolete("(3.1) remove this test in Lucene 5.0")]
	  public virtual void testOldBuggyStemmer()
	  {
		Analyzer a = new DutchAnalyzer(Version.LUCENE_30);
		checkOneTerm(a, "opheffen", "ophef"); // versus snowball 'opheff'
		checkOneTerm(a, "opheffende", "ophef"); // versus snowball 'opheff'
		checkOneTerm(a, "opheffing", "ophef"); // versus snowball 'opheff'
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testSnowballCorrectness() throws Exception
	  public virtual void testSnowballCorrectness()
	  {
		Analyzer a = new DutchAnalyzer(TEST_VERSION_CURRENT);
		checkOneTerm(a, "opheffen", "opheff");
		checkOneTerm(a, "opheffende", "opheff");
		checkOneTerm(a, "opheffing", "opheff");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testReusableTokenStream() throws Exception
	  public virtual void testReusableTokenStream()
	  {
		Analyzer a = new DutchAnalyzer(TEST_VERSION_CURRENT);
		checkOneTerm(a, "lichaamsziek", "lichaamsziek");
		checkOneTerm(a, "lichamelijk", "licham");
		checkOneTerm(a, "lichamelijke", "licham");
		checkOneTerm(a, "lichamelijkheden", "licham");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testExclusionTableViaCtor() throws java.io.IOException
	  public virtual void testExclusionTableViaCtor()
	  {
		CharArraySet set = new CharArraySet(Version.LUCENE_30, 1, true);
		set.add("lichamelijk");
		DutchAnalyzer a = new DutchAnalyzer(TEST_VERSION_CURRENT, CharArraySet.EMPTY_SET, set);
		assertAnalyzesTo(a, "lichamelijk lichamelijke", new string[] {"lichamelijk", "licham"});

		a = new DutchAnalyzer(TEST_VERSION_CURRENT, CharArraySet.EMPTY_SET, set);
		assertAnalyzesTo(a, "lichamelijk lichamelijke", new string[] {"lichamelijk", "licham"});

	  }

	  /// <summary>
	  /// check that the default stem overrides are used
	  /// even if you use a non-default ctor.
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testStemOverrides() throws java.io.IOException
	  public virtual void testStemOverrides()
	  {
		DutchAnalyzer a = new DutchAnalyzer(TEST_VERSION_CURRENT, CharArraySet.EMPTY_SET);
		checkOneTerm(a, "fiets", "fiets");
	  }
	  /// <summary>
	  /// 3.0 still uses the chararraymap internally check if that works as well </summary>
	  /// @deprecated (4.3) remove this test in Lucene 5.0 
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Deprecated("(4.3) remove this test in Lucene 5.0") public void test30StemOverrides() throws java.io.IOException
	  [Obsolete("(4.3) remove this test in Lucene 5.0")]
	  public virtual void test30StemOverrides()
	  {
		DutchAnalyzer a = new DutchAnalyzer(Version.LUCENE_30);
		checkOneTerm(a, "fiets", "fiets");
		a = new DutchAnalyzer(Version.LUCENE_30, CharArraySet.EMPTY_SET);
		checkOneTerm(a, "fiets", "fiet"); // only the default ctor populates the dict
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testEmptyStemDictionary() throws java.io.IOException
	  public virtual void testEmptyStemDictionary()
	  {
		DutchAnalyzer a = new DutchAnalyzer(TEST_VERSION_CURRENT, CharArraySet.EMPTY_SET, CharArraySet.EMPTY_SET, CharArrayMap.emptyMap<string>());
		checkOneTerm(a, "fiets", "fiet");
	  }

	  /// <summary>
	  /// prior to 3.6, this confusingly did not happen if 
	  /// you specified your own stoplist!!!! </summary>
	  /// @deprecated (3.6) Remove this test in Lucene 5.0 
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Deprecated("(3.6) Remove this test in Lucene 5.0") public void testBuggyStemOverrides() throws java.io.IOException
	  [Obsolete("(3.6) Remove this test in Lucene 5.0")]
	  public virtual void testBuggyStemOverrides()
	  {
		DutchAnalyzer a = new DutchAnalyzer(Version.LUCENE_35, CharArraySet.EMPTY_SET);
		checkOneTerm(a, "fiets", "fiet");
	  }

	  /// <summary>
	  /// Prior to 3.1, this analyzer had no lowercase filter.
	  /// stopwords were case sensitive. Preserve this for back compat. </summary>
	  /// @deprecated (3.1) Remove this test in Lucene 5.0 
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Deprecated("(3.1) Remove this test in Lucene 5.0") public void testBuggyStopwordsCasing() throws java.io.IOException
	  [Obsolete("(3.1) Remove this test in Lucene 5.0")]
	  public virtual void testBuggyStopwordsCasing()
	  {
		DutchAnalyzer a = new DutchAnalyzer(Version.LUCENE_30);
		assertAnalyzesTo(a, "Zelf", new string[] {"zelf"});
	  }

	  /// <summary>
	  /// Test that stopwords are not case sensitive
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testStopwordsCasing() throws java.io.IOException
	  public virtual void testStopwordsCasing()
	  {
		DutchAnalyzer a = new DutchAnalyzer(Version.LUCENE_31);
		assertAnalyzesTo(a, "Zelf", new string[] { });
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void check(final String input, final String expected) throws Exception
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
	  private void check(string input, string expected)
	  {
		checkOneTerm(new DutchAnalyzer(TEST_VERSION_CURRENT), input, expected);
	  }

	  /// <summary>
	  /// blast some random strings through the analyzer </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandomStrings() throws Exception
	  public virtual void testRandomStrings()
	  {
		checkRandomData(random(), new DutchAnalyzer(TEST_VERSION_CURRENT), 1000 * RANDOM_MULTIPLIER);
	  }

	}

}