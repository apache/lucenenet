namespace org.apache.lucene.analysis.br
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
	using LowerCaseTokenizer = org.apache.lucene.analysis.core.LowerCaseTokenizer;
	using SetKeywordMarkerFilter = org.apache.lucene.analysis.miscellaneous.SetKeywordMarkerFilter;
	using CharArraySet = org.apache.lucene.analysis.util.CharArraySet;

	/// <summary>
	/// Test the Brazilian Stem Filter, which only modifies the term text.
	/// 
	/// It is very similar to the snowball portuguese algorithm but not exactly the same.
	/// 
	/// </summary>
	public class TestBrazilianStemmer : BaseTokenStreamTestCase
	{

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testWithSnowballExamples() throws Exception
	  public virtual void testWithSnowballExamples()
	  {
	   check("boa", "boa");
	   check("boainain", "boainain");
	   check("boas", "boas");
	   check("bôas", "boas"); // removes diacritic: different from snowball portugese
	   check("boassu", "boassu");
	   check("boataria", "boat");
	   check("boate", "boat");
	   check("boates", "boat");
	   check("boatos", "boat");
	   check("bob", "bob");
	   check("boba", "bob");
	   check("bobagem", "bobag");
	   check("bobagens", "bobagens");
	   check("bobalhões", "bobalho"); // removes diacritic: different from snowball portugese
	   check("bobear", "bob");
	   check("bobeira", "bobeir");
	   check("bobinho", "bobinh");
	   check("bobinhos", "bobinh");
	   check("bobo", "bob");
	   check("bobs", "bobs");
	   check("boca", "boc");
	   check("bocadas", "boc");
	   check("bocadinho", "bocadinh");
	   check("bocado", "boc");
	   check("bocaiúva", "bocaiuv"); // removes diacritic: different from snowball portuguese
	   check("boçal", "bocal"); // removes diacritic: different from snowball portuguese
	   check("bocarra", "bocarr");
	   check("bocas", "boc");
	   check("bode", "bod");
	   check("bodoque", "bodoqu");
	   check("body", "body");
	   check("boeing", "boeing");
	   check("boem", "boem");
	   check("boemia", "boem");
	   check("boêmio", "boemi"); // removes diacritic: different from snowball portuguese
	   check("bogotá", "bogot");
	   check("boi", "boi");
	   check("bóia", "boi"); // removes diacritic: different from snowball portuguese
	   check("boiando", "boi");
	   check("quiabo", "quiab");
	   check("quicaram", "quic");
	   check("quickly", "quickly");
	   check("quieto", "quiet");
	   check("quietos", "quiet");
	   check("quilate", "quilat");
	   check("quilates", "quilat");
	   check("quilinhos", "quilinh");
	   check("quilo", "quil");
	   check("quilombo", "quilomb");
	   check("quilométricas", "quilometr"); // removes diacritic: different from snowball portuguese
	   check("quilométricos", "quilometr"); // removes diacritic: different from snowball portuguese
	   check("quilômetro", "quilometr"); // removes diacritic: different from snowball portoguese
	   check("quilômetros", "quilometr"); // removes diacritic: different from snowball portoguese
	   check("quilos", "quil");
	   check("quimica", "quimic");
	   check("quilos", "quil");
	   check("quimica", "quimic");
	   check("quimicas", "quimic");
	   check("quimico", "quimic");
	   check("quimicos", "quimic");
	   check("quimioterapia", "quimioterap");
	   check("quimioterápicos", "quimioterap"); // removes diacritic: different from snowball portoguese
	   check("quimono", "quimon");
	   check("quincas", "quinc");
	   check("quinhão", "quinha"); // removes diacritic: different from snowball portoguese
	   check("quinhentos", "quinhent");
	   check("quinn", "quinn");
	   check("quino", "quin");
	   check("quinta", "quint");
	   check("quintal", "quintal");
	   check("quintana", "quintan");
	   check("quintanilha", "quintanilh");
	   check("quintão", "quinta"); // removes diacritic: different from snowball portoguese
	   check("quintessência", "quintessente"); // versus snowball portuguese 'quintessent'
	   check("quintino", "quintin");
	   check("quinto", "quint");
	   check("quintos", "quint");
	   check("quintuplicou", "quintuplic");
	   check("quinze", "quinz");
	   check("quinzena", "quinzen");
	   check("quiosque", "quiosqu");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testNormalization() throws Exception
	  public virtual void testNormalization()
	  {
		check("Brasil", "brasil"); // lowercase by default
		check("Brasília", "brasil"); // remove diacritics
		check("quimio5terápicos", "quimio5terapicos"); // contains non-letter, diacritic will still be removed
		check("áá", "áá"); // token is too short: diacritics are not removed
		check("ááá", "aaa"); // normally, diacritics are removed
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testReusableTokenStream() throws Exception
	  public virtual void testReusableTokenStream()
	  {
		Analyzer a = new BrazilianAnalyzer(TEST_VERSION_CURRENT);
		checkReuse(a, "boa", "boa");
		checkReuse(a, "boainain", "boainain");
		checkReuse(a, "boas", "boas");
		checkReuse(a, "bôas", "boas"); // removes diacritic: different from snowball portugese
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testStemExclusionTable() throws Exception
	  public virtual void testStemExclusionTable()
	  {
		BrazilianAnalyzer a = new BrazilianAnalyzer(TEST_VERSION_CURRENT, CharArraySet.EMPTY_SET, new CharArraySet(TEST_VERSION_CURRENT, asSet("quintessência"), false));
		checkReuse(a, "quintessência", "quintessência"); // excluded words will be completely unchanged.
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testWithKeywordAttribute() throws java.io.IOException
	  public virtual void testWithKeywordAttribute()
	  {
		CharArraySet set = new CharArraySet(TEST_VERSION_CURRENT, 1, true);
		set.add("Brasília");
		BrazilianStemFilter filter = new BrazilianStemFilter(new SetKeywordMarkerFilter(new LowerCaseTokenizer(TEST_VERSION_CURRENT, new StringReader("Brasília Brasilia")), set));
		assertTokenStreamContents(filter, new string[] {"brasília", "brasil"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void check(final String input, final String expected) throws Exception
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
	  private void check(string input, string expected)
	  {
		checkOneTerm(new BrazilianAnalyzer(TEST_VERSION_CURRENT), input, expected);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void checkReuse(org.apache.lucene.analysis.Analyzer a, String input, String expected) throws Exception
	  private void checkReuse(Analyzer a, string input, string expected)
	  {
		checkOneTerm(a, input, expected);
	  }

	  /// <summary>
	  /// blast some random strings through the analyzer </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandomStrings() throws Exception
	  public virtual void testRandomStrings()
	  {
		checkRandomData(random(), new BrazilianAnalyzer(TEST_VERSION_CURRENT), 1000 * RANDOM_MULTIPLIER);
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
		  private readonly TestBrazilianStemmer outerInstance;

		  public AnalyzerAnonymousInnerClassHelper(TestBrazilianStemmer outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new KeywordTokenizer(reader);
			return new TokenStreamComponents(tokenizer, new BrazilianStemFilter(tokenizer));
		  }
	  }
	}

}