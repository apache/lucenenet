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


	using KeywordTokenizer = org.apache.lucene.analysis.core.KeywordTokenizer;
	using SetKeywordMarkerFilter = org.apache.lucene.analysis.miscellaneous.SetKeywordMarkerFilter;
	using CharArraySet = org.apache.lucene.analysis.util.CharArraySet;
	using Version = org.apache.lucene.util.Version;

	/// <summary>
	/// Test the Bulgarian Stemmer
	/// </summary>
	public class TestBulgarianStemmer : BaseTokenStreamTestCase
	{
	  /// <summary>
	  /// Test showing how masculine noun forms conflate. An example noun for each
	  /// common (and some rare) plural pattern is listed.
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testMasculineNouns() throws java.io.IOException
	  public virtual void testMasculineNouns()
	  {
		BulgarianAnalyzer a = new BulgarianAnalyzer(TEST_VERSION_CURRENT);

		// -и pattern
		assertAnalyzesTo(a, "град", new string[] {"град"});
		assertAnalyzesTo(a, "града", new string[] {"град"});
		assertAnalyzesTo(a, "градът", new string[] {"град"});
		assertAnalyzesTo(a, "градове", new string[] {"град"});
		assertAnalyzesTo(a, "градовете", new string[] {"град"});

		// -ове pattern
		assertAnalyzesTo(a, "народ", new string[] {"народ"});
		assertAnalyzesTo(a, "народа", new string[] {"народ"});
		assertAnalyzesTo(a, "народът", new string[] {"народ"});
		assertAnalyzesTo(a, "народи", new string[] {"народ"});
		assertAnalyzesTo(a, "народите", new string[] {"народ"});
		assertAnalyzesTo(a, "народе", new string[] {"народ"});

		// -ища pattern
		assertAnalyzesTo(a, "път", new string[] {"път"});
		assertAnalyzesTo(a, "пътя", new string[] {"път"});
		assertAnalyzesTo(a, "пътят", new string[] {"път"});
		assertAnalyzesTo(a, "пътища", new string[] {"път"});
		assertAnalyzesTo(a, "пътищата", new string[] {"път"});

		// -чета pattern
		assertAnalyzesTo(a, "градец", new string[] {"градец"});
		assertAnalyzesTo(a, "градеца", new string[] {"градец"});
		assertAnalyzesTo(a, "градецът", new string[] {"градец"});
		/* note the below forms conflate with each other, but not the rest */
		assertAnalyzesTo(a, "градовце", new string[] {"градовц"});
		assertAnalyzesTo(a, "градовцете", new string[] {"градовц"});

		// -овци pattern
		assertAnalyzesTo(a, "дядо", new string[] {"дяд"});
		assertAnalyzesTo(a, "дядото", new string[] {"дяд"});
		assertAnalyzesTo(a, "дядовци", new string[] {"дяд"});
		assertAnalyzesTo(a, "дядовците", new string[] {"дяд"});

		// -е pattern
		assertAnalyzesTo(a, "мъж", new string[] {"мъж"});
		assertAnalyzesTo(a, "мъжа", new string[] {"мъж"});
		assertAnalyzesTo(a, "мъже", new string[] {"мъж"});
		assertAnalyzesTo(a, "мъжете", new string[] {"мъж"});
		assertAnalyzesTo(a, "мъжо", new string[] {"мъж"});
		/* word is too short, will not remove -ът */
		assertAnalyzesTo(a, "мъжът", new string[] {"мъжът"});

		// -а pattern
		assertAnalyzesTo(a, "крак", new string[] {"крак"});
		assertAnalyzesTo(a, "крака", new string[] {"крак"});
		assertAnalyzesTo(a, "кракът", new string[] {"крак"});
		assertAnalyzesTo(a, "краката", new string[] {"крак"});

		// брат
		assertAnalyzesTo(a, "брат", new string[] {"брат"});
		assertAnalyzesTo(a, "брата", new string[] {"брат"});
		assertAnalyzesTo(a, "братът", new string[] {"брат"});
		assertAnalyzesTo(a, "братя", new string[] {"брат"});
		assertAnalyzesTo(a, "братята", new string[] {"брат"});
		assertAnalyzesTo(a, "брате", new string[] {"брат"});
	  }

	  /// <summary>
	  /// Test showing how feminine noun forms conflate
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testFeminineNouns() throws java.io.IOException
	  public virtual void testFeminineNouns()
	  {
		BulgarianAnalyzer a = new BulgarianAnalyzer(TEST_VERSION_CURRENT);

		assertAnalyzesTo(a, "вест", new string[] {"вест"});
		assertAnalyzesTo(a, "вестта", new string[] {"вест"});
		assertAnalyzesTo(a, "вести", new string[] {"вест"});
		assertAnalyzesTo(a, "вестите", new string[] {"вест"});
	  }

	  /// <summary>
	  /// Test showing how neuter noun forms conflate an example noun for each common
	  /// plural pattern is listed
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testNeuterNouns() throws java.io.IOException
	  public virtual void testNeuterNouns()
	  {
		BulgarianAnalyzer a = new BulgarianAnalyzer(TEST_VERSION_CURRENT);

		// -а pattern
		assertAnalyzesTo(a, "дърво", new string[] {"дърв"});
		assertAnalyzesTo(a, "дървото", new string[] {"дърв"});
		assertAnalyzesTo(a, "дърва", new string[] {"дърв"});
		assertAnalyzesTo(a, "дървета", new string[] {"дърв"});
		assertAnalyzesTo(a, "дървата", new string[] {"дърв"});
		assertAnalyzesTo(a, "дърветата", new string[] {"дърв"});

		// -та pattern
		assertAnalyzesTo(a, "море", new string[] {"мор"});
		assertAnalyzesTo(a, "морето", new string[] {"мор"});
		assertAnalyzesTo(a, "морета", new string[] {"мор"});
		assertAnalyzesTo(a, "моретата", new string[] {"мор"});

		// -я pattern
		assertAnalyzesTo(a, "изключение", new string[] {"изключени"});
		assertAnalyzesTo(a, "изключението", new string[] {"изключени"});
		assertAnalyzesTo(a, "изключенията", new string[] {"изключени"});
		/* note the below form in this example does not conflate with the rest */
		assertAnalyzesTo(a, "изключения", new string[] {"изключн"});
	  }

	  /// <summary>
	  /// Test showing how adjectival forms conflate
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testAdjectives() throws java.io.IOException
	  public virtual void testAdjectives()
	  {
		BulgarianAnalyzer a = new BulgarianAnalyzer(TEST_VERSION_CURRENT);
		assertAnalyzesTo(a, "красив", new string[] {"красив"});
		assertAnalyzesTo(a, "красивия", new string[] {"красив"});
		assertAnalyzesTo(a, "красивият", new string[] {"красив"});
		assertAnalyzesTo(a, "красива", new string[] {"красив"});
		assertAnalyzesTo(a, "красивата", new string[] {"красив"});
		assertAnalyzesTo(a, "красиво", new string[] {"красив"});
		assertAnalyzesTo(a, "красивото", new string[] {"красив"});
		assertAnalyzesTo(a, "красиви", new string[] {"красив"});
		assertAnalyzesTo(a, "красивите", new string[] {"красив"});
	  }

	  /// <summary>
	  /// Test some exceptional rules, implemented as rewrites.
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testExceptions() throws java.io.IOException
	  public virtual void testExceptions()
	  {
		BulgarianAnalyzer a = new BulgarianAnalyzer(TEST_VERSION_CURRENT);

		// ци -> к
		assertAnalyzesTo(a, "собственик", new string[] {"собственик"});
		assertAnalyzesTo(a, "собственика", new string[] {"собственик"});
		assertAnalyzesTo(a, "собственикът", new string[] {"собственик"});
		assertAnalyzesTo(a, "собственици", new string[] {"собственик"});
		assertAnalyzesTo(a, "собствениците", new string[] {"собственик"});

		// зи -> г
		assertAnalyzesTo(a, "подлог", new string[] {"подлог"});
		assertAnalyzesTo(a, "подлога", new string[] {"подлог"});
		assertAnalyzesTo(a, "подлогът", new string[] {"подлог"});
		assertAnalyzesTo(a, "подлози", new string[] {"подлог"});
		assertAnalyzesTo(a, "подлозите", new string[] {"подлог"});

		// си -> х
		assertAnalyzesTo(a, "кожух", new string[] {"кожух"});
		assertAnalyzesTo(a, "кожуха", new string[] {"кожух"});
		assertAnalyzesTo(a, "кожухът", new string[] {"кожух"});
		assertAnalyzesTo(a, "кожуси", new string[] {"кожух"});
		assertAnalyzesTo(a, "кожусите", new string[] {"кожух"});

		// ъ deletion
		assertAnalyzesTo(a, "център", new string[] {"центр"});
		assertAnalyzesTo(a, "центъра", new string[] {"центр"});
		assertAnalyzesTo(a, "центърът", new string[] {"центр"});
		assertAnalyzesTo(a, "центрове", new string[] {"центр"});
		assertAnalyzesTo(a, "центровете", new string[] {"центр"});

		// е*и -> я*
		assertAnalyzesTo(a, "промяна", new string[] {"промян"});
		assertAnalyzesTo(a, "промяната", new string[] {"промян"});
		assertAnalyzesTo(a, "промени", new string[] {"промян"});
		assertAnalyzesTo(a, "промените", new string[] {"промян"});

		// ен -> н
		assertAnalyzesTo(a, "песен", new string[] {"песн"});
		assertAnalyzesTo(a, "песента", new string[] {"песн"});
		assertAnalyzesTo(a, "песни", new string[] {"песн"});
		assertAnalyzesTo(a, "песните", new string[] {"песн"});

		// -еве -> й
		// note: this is the only word i think this rule works for.
		// most -еве pluralized nouns are monosyllabic,
		// and the stemmer requires length > 6...
		assertAnalyzesTo(a, "строй", new string[] {"строй"});
		assertAnalyzesTo(a, "строеве", new string[] {"строй"});
		assertAnalyzesTo(a, "строевете", new string[] {"строй"});
		/* note the below forms conflate with each other, but not the rest */
		assertAnalyzesTo(a, "строя", new string[] {"стр"});
		assertAnalyzesTo(a, "строят", new string[] {"стр"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testWithKeywordAttribute() throws java.io.IOException
	  public virtual void testWithKeywordAttribute()
	  {
		CharArraySet set = new CharArraySet(TEST_VERSION_CURRENT, 1, true);
		set.add("строеве");
		MockTokenizer tokenStream = new MockTokenizer(new StringReader("строевете строеве"), MockTokenizer.WHITESPACE, false);

		BulgarianStemFilter filter = new BulgarianStemFilter(new SetKeywordMarkerFilter(tokenStream, set));
		assertTokenStreamContents(filter, new string[] {"строй", "строеве"});
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
		  private readonly TestBulgarianStemmer outerInstance;

		  public AnalyzerAnonymousInnerClassHelper(TestBulgarianStemmer outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new KeywordTokenizer(reader);
			return new TokenStreamComponents(tokenizer, new BulgarianStemFilter(tokenizer));
		  }
	  }
	}

}