namespace org.apache.lucene.analysis.cz
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
	using SetKeywordMarkerFilter = org.apache.lucene.analysis.miscellaneous.SetKeywordMarkerFilter;
	using CharArraySet = org.apache.lucene.analysis.util.CharArraySet;

	/// <summary>
	/// Test the Czech Stemmer.
	/// 
	/// Note: its algorithmic, so some stems are nonsense
	/// 
	/// </summary>
	public class TestCzechStemmer : BaseTokenStreamTestCase
	{

	  /// <summary>
	  /// Test showing how masculine noun forms conflate
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testMasculineNouns() throws java.io.IOException
	  public virtual void testMasculineNouns()
	  {
		CzechAnalyzer cz = new CzechAnalyzer(TEST_VERSION_CURRENT);

		/* animate ending with a hard consonant */
		assertAnalyzesTo(cz, "pán", new string[] {"pán"});
		assertAnalyzesTo(cz, "páni", new string[] {"pán"});
		assertAnalyzesTo(cz, "pánové", new string[] {"pán"});
		assertAnalyzesTo(cz, "pána", new string[] {"pán"});
		assertAnalyzesTo(cz, "pánů", new string[] {"pán"});
		assertAnalyzesTo(cz, "pánovi", new string[] {"pán"});
		assertAnalyzesTo(cz, "pánům", new string[] {"pán"});
		assertAnalyzesTo(cz, "pány", new string[] {"pán"});
		assertAnalyzesTo(cz, "páne", new string[] {"pán"});
		assertAnalyzesTo(cz, "pánech", new string[] {"pán"});
		assertAnalyzesTo(cz, "pánem", new string[] {"pán"});

		/* inanimate ending with hard consonant */
		assertAnalyzesTo(cz, "hrad", new string[] {"hrad"});
		assertAnalyzesTo(cz, "hradu", new string[] {"hrad"});
		assertAnalyzesTo(cz, "hrade", new string[] {"hrad"});
		assertAnalyzesTo(cz, "hradem", new string[] {"hrad"});
		assertAnalyzesTo(cz, "hrady", new string[] {"hrad"});
		assertAnalyzesTo(cz, "hradech", new string[] {"hrad"});
		assertAnalyzesTo(cz, "hradům", new string[] {"hrad"});
		assertAnalyzesTo(cz, "hradů", new string[] {"hrad"});

		/* animate ending with a soft consonant */
		assertAnalyzesTo(cz, "muž", new string[] {"muh"});
		assertAnalyzesTo(cz, "muži", new string[] {"muh"});
		assertAnalyzesTo(cz, "muže", new string[] {"muh"});
		assertAnalyzesTo(cz, "mužů", new string[] {"muh"});
		assertAnalyzesTo(cz, "mužům", new string[] {"muh"});
		assertAnalyzesTo(cz, "mužích", new string[] {"muh"});
		assertAnalyzesTo(cz, "mužem", new string[] {"muh"});

		/* inanimate ending with a soft consonant */
		assertAnalyzesTo(cz, "stroj", new string[] {"stroj"});
		assertAnalyzesTo(cz, "stroje", new string[] {"stroj"});
		assertAnalyzesTo(cz, "strojů", new string[] {"stroj"});
		assertAnalyzesTo(cz, "stroji", new string[] {"stroj"});
		assertAnalyzesTo(cz, "strojům", new string[] {"stroj"});
		assertAnalyzesTo(cz, "strojích", new string[] {"stroj"});
		assertAnalyzesTo(cz, "strojem", new string[] {"stroj"});

		/* ending with a */
		assertAnalyzesTo(cz, "předseda", new string[] {"předsd"});
		assertAnalyzesTo(cz, "předsedové", new string[] {"předsd"});
		assertAnalyzesTo(cz, "předsedy", new string[] {"předsd"});
		assertAnalyzesTo(cz, "předsedů", new string[] {"předsd"});
		assertAnalyzesTo(cz, "předsedovi", new string[] {"předsd"});
		assertAnalyzesTo(cz, "předsedům", new string[] {"předsd"});
		assertAnalyzesTo(cz, "předsedu", new string[] {"předsd"});
		assertAnalyzesTo(cz, "předsedo", new string[] {"předsd"});
		assertAnalyzesTo(cz, "předsedech", new string[] {"předsd"});
		assertAnalyzesTo(cz, "předsedou", new string[] {"předsd"});

		/* ending with e */
		assertAnalyzesTo(cz, "soudce", new string[] {"soudk"});
		assertAnalyzesTo(cz, "soudci", new string[] {"soudk"});
		assertAnalyzesTo(cz, "soudců", new string[] {"soudk"});
		assertAnalyzesTo(cz, "soudcům", new string[] {"soudk"});
		assertAnalyzesTo(cz, "soudcích", new string[] {"soudk"});
		assertAnalyzesTo(cz, "soudcem", new string[] {"soudk"});
	  }

	  /// <summary>
	  /// Test showing how feminine noun forms conflate
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testFeminineNouns() throws java.io.IOException
	  public virtual void testFeminineNouns()
	  {
		CzechAnalyzer cz = new CzechAnalyzer(TEST_VERSION_CURRENT);

		/* ending with hard consonant */
		assertAnalyzesTo(cz, "kost", new string[] {"kost"});
		assertAnalyzesTo(cz, "kosti", new string[] {"kost"});
		assertAnalyzesTo(cz, "kostí", new string[] {"kost"});
		assertAnalyzesTo(cz, "kostem", new string[] {"kost"});
		assertAnalyzesTo(cz, "kostech", new string[] {"kost"});
		assertAnalyzesTo(cz, "kostmi", new string[] {"kost"});

		/* ending with a soft consonant */
		// note: in this example sing nom. and sing acc. don't conflate w/ the rest
		assertAnalyzesTo(cz, "píseň", new string[] {"písň"});
		assertAnalyzesTo(cz, "písně", new string[] {"písn"});
		assertAnalyzesTo(cz, "písni", new string[] {"písn"});
		assertAnalyzesTo(cz, "písněmi", new string[] {"písn"});
		assertAnalyzesTo(cz, "písních", new string[] {"písn"});
		assertAnalyzesTo(cz, "písním", new string[] {"písn"});

		/* ending with e */
		assertAnalyzesTo(cz, "růže", new string[] {"růh"});
		assertAnalyzesTo(cz, "růží", new string[] {"růh"});
		assertAnalyzesTo(cz, "růžím", new string[] {"růh"});
		assertAnalyzesTo(cz, "růžích", new string[] {"růh"});
		assertAnalyzesTo(cz, "růžemi", new string[] {"růh"});
		assertAnalyzesTo(cz, "růži", new string[] {"růh"});

		/* ending with a */
		assertAnalyzesTo(cz, "žena", new string[] {"žn"});
		assertAnalyzesTo(cz, "ženy", new string[] {"žn"});
		assertAnalyzesTo(cz, "žen", new string[] {"žn"});
		assertAnalyzesTo(cz, "ženě", new string[] {"žn"});
		assertAnalyzesTo(cz, "ženám", new string[] {"žn"});
		assertAnalyzesTo(cz, "ženu", new string[] {"žn"});
		assertAnalyzesTo(cz, "ženo", new string[] {"žn"});
		assertAnalyzesTo(cz, "ženách", new string[] {"žn"});
		assertAnalyzesTo(cz, "ženou", new string[] {"žn"});
		assertAnalyzesTo(cz, "ženami", new string[] {"žn"});
	  }

	  /// <summary>
	  /// Test showing how neuter noun forms conflate
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testNeuterNouns() throws java.io.IOException
	  public virtual void testNeuterNouns()
	  {
		CzechAnalyzer cz = new CzechAnalyzer(TEST_VERSION_CURRENT);

		/* ending with o */
		assertAnalyzesTo(cz, "město", new string[] {"měst"});
		assertAnalyzesTo(cz, "města", new string[] {"měst"});
		assertAnalyzesTo(cz, "měst", new string[] {"měst"});
		assertAnalyzesTo(cz, "městu", new string[] {"měst"});
		assertAnalyzesTo(cz, "městům", new string[] {"měst"});
		assertAnalyzesTo(cz, "městě", new string[] {"měst"});
		assertAnalyzesTo(cz, "městech", new string[] {"měst"});
		assertAnalyzesTo(cz, "městem", new string[] {"měst"});
		assertAnalyzesTo(cz, "městy", new string[] {"měst"});

		/* ending with e */
		assertAnalyzesTo(cz, "moře", new string[] {"moř"});
		assertAnalyzesTo(cz, "moří", new string[] {"moř"});
		assertAnalyzesTo(cz, "mořím", new string[] {"moř"});
		assertAnalyzesTo(cz, "moři", new string[] {"moř"});
		assertAnalyzesTo(cz, "mořích", new string[] {"moř"});
		assertAnalyzesTo(cz, "mořem", new string[] {"moř"});

		/* ending with ě */
		assertAnalyzesTo(cz, "kuře", new string[] {"kuř"});
		assertAnalyzesTo(cz, "kuřata", new string[] {"kuř"});
		assertAnalyzesTo(cz, "kuřete", new string[] {"kuř"});
		assertAnalyzesTo(cz, "kuřat", new string[] {"kuř"});
		assertAnalyzesTo(cz, "kuřeti", new string[] {"kuř"});
		assertAnalyzesTo(cz, "kuřatům", new string[] {"kuř"});
		assertAnalyzesTo(cz, "kuřatech", new string[] {"kuř"});
		assertAnalyzesTo(cz, "kuřetem", new string[] {"kuř"});
		assertAnalyzesTo(cz, "kuřaty", new string[] {"kuř"});

		/* ending with í */
		assertAnalyzesTo(cz, "stavení", new string[] {"stavn"});
		assertAnalyzesTo(cz, "stavením", new string[] {"stavn"});
		assertAnalyzesTo(cz, "staveních", new string[] {"stavn"});
		assertAnalyzesTo(cz, "staveními", new string[] {"stavn"});
	  }

	  /// <summary>
	  /// Test showing how adjectival forms conflate
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testAdjectives() throws java.io.IOException
	  public virtual void testAdjectives()
	  {
		CzechAnalyzer cz = new CzechAnalyzer(TEST_VERSION_CURRENT);

		/* ending with ý/á/é */
		assertAnalyzesTo(cz, "mladý", new string[] {"mlad"});
		assertAnalyzesTo(cz, "mladí", new string[] {"mlad"});
		assertAnalyzesTo(cz, "mladého", new string[] {"mlad"});
		assertAnalyzesTo(cz, "mladých", new string[] {"mlad"});
		assertAnalyzesTo(cz, "mladému", new string[] {"mlad"});
		assertAnalyzesTo(cz, "mladým", new string[] {"mlad"});
		assertAnalyzesTo(cz, "mladé", new string[] {"mlad"});
		assertAnalyzesTo(cz, "mladém", new string[] {"mlad"});
		assertAnalyzesTo(cz, "mladými", new string[] {"mlad"});
		assertAnalyzesTo(cz, "mladá", new string[] {"mlad"});
		assertAnalyzesTo(cz, "mladou", new string[] {"mlad"});

		/* ending with í */
		assertAnalyzesTo(cz, "jarní", new string[] {"jarn"});
		assertAnalyzesTo(cz, "jarního", new string[] {"jarn"});
		assertAnalyzesTo(cz, "jarních", new string[] {"jarn"});
		assertAnalyzesTo(cz, "jarnímu", new string[] {"jarn"});
		assertAnalyzesTo(cz, "jarním", new string[] {"jarn"});
		assertAnalyzesTo(cz, "jarními", new string[] {"jarn"});
	  }

	  /// <summary>
	  /// Test some possessive suffixes
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testPossessive() throws java.io.IOException
	  public virtual void testPossessive()
	  {
		CzechAnalyzer cz = new CzechAnalyzer(TEST_VERSION_CURRENT);
		assertAnalyzesTo(cz, "Karlův", new string[] {"karl"});
		assertAnalyzesTo(cz, "jazykový", new string[] {"jazyk"});
	  }

	  /// <summary>
	  /// Test some exceptional rules, implemented as rewrites.
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testExceptions() throws java.io.IOException
	  public virtual void testExceptions()
	  {
		CzechAnalyzer cz = new CzechAnalyzer(TEST_VERSION_CURRENT);

		/* rewrite of št -> sk */
		assertAnalyzesTo(cz, "český", new string[] {"česk"});
		assertAnalyzesTo(cz, "čeští", new string[] {"česk"});

		/* rewrite of čt -> ck */
		assertAnalyzesTo(cz, "anglický", new string[] {"anglick"});
		assertAnalyzesTo(cz, "angličtí", new string[] {"anglick"});

		/* rewrite of z -> h */
		assertAnalyzesTo(cz, "kniha", new string[] {"knih"});
		assertAnalyzesTo(cz, "knize", new string[] {"knih"});

		/* rewrite of ž -> h */
		assertAnalyzesTo(cz, "mazat", new string[] {"mah"});
		assertAnalyzesTo(cz, "mažu", new string[] {"mah"});

		/* rewrite of c -> k */
		assertAnalyzesTo(cz, "kluk", new string[] {"kluk"});
		assertAnalyzesTo(cz, "kluci", new string[] {"kluk"});
		assertAnalyzesTo(cz, "klucích", new string[] {"kluk"});

		/* rewrite of č -> k */
		assertAnalyzesTo(cz, "hezký", new string[] {"hezk"});
		assertAnalyzesTo(cz, "hezčí", new string[] {"hezk"});

		/* rewrite of *ů* -> *o* */
		assertAnalyzesTo(cz, "hůl", new string[] {"hol"});
		assertAnalyzesTo(cz, "hole", new string[] {"hol"});

		/* rewrite of e* -> * */
		assertAnalyzesTo(cz, "deska", new string[] {"desk"});
		assertAnalyzesTo(cz, "desek", new string[] {"desk"});
	  }

	  /// <summary>
	  /// Test that very short words are not stemmed.
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testDontStem() throws java.io.IOException
	  public virtual void testDontStem()
	  {
		CzechAnalyzer cz = new CzechAnalyzer(TEST_VERSION_CURRENT);
		assertAnalyzesTo(cz, "e", new string[] {"e"});
		assertAnalyzesTo(cz, "zi", new string[] {"zi"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testWithKeywordAttribute() throws java.io.IOException
	  public virtual void testWithKeywordAttribute()
	  {
		CharArraySet set = new CharArraySet(TEST_VERSION_CURRENT, 1, true);
		set.add("hole");
		CzechStemFilter filter = new CzechStemFilter(new SetKeywordMarkerFilter(new MockTokenizer(new StringReader("hole desek"), MockTokenizer.WHITESPACE, false), set));
		assertTokenStreamContents(filter, new string[] {"hole", "desk"});
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
		  private readonly TestCzechStemmer outerInstance;

		  public AnalyzerAnonymousInnerClassHelper(TestCzechStemmer outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new KeywordTokenizer(reader);
			return new TokenStreamComponents(tokenizer, new CzechStemFilter(tokenizer));
		  }
	  }

	}

}