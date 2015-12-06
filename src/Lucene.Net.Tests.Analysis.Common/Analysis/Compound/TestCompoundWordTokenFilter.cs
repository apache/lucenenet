namespace org.apache.lucene.analysis.compound
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


	using MappingCharFilter = org.apache.lucene.analysis.charfilter.MappingCharFilter;
	using NormalizeCharMap = org.apache.lucene.analysis.charfilter.NormalizeCharMap;
	using HyphenationTree = org.apache.lucene.analysis.compound.hyphenation.HyphenationTree;
	using KeywordTokenizer = org.apache.lucene.analysis.core.KeywordTokenizer;
	using WhitespaceTokenizer = org.apache.lucene.analysis.core.WhitespaceTokenizer;
	using CharArraySet = org.apache.lucene.analysis.util.CharArraySet;
	using CharTermAttribute = org.apache.lucene.analysis.tokenattributes.CharTermAttribute;
	using Attribute = org.apache.lucene.util.Attribute;
	using AttributeImpl = org.apache.lucene.util.AttributeImpl;
	using InputSource = org.xml.sax.InputSource;

	public class TestCompoundWordTokenFilter : BaseTokenStreamTestCase
	{

	  private static CharArraySet makeDictionary(params string[] dictionary)
	  {
		return new CharArraySet(TEST_VERSION_CURRENT, Arrays.asList(dictionary), true);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testHyphenationCompoundWordsDA() throws Exception
	  public virtual void testHyphenationCompoundWordsDA()
	  {
		CharArraySet dict = makeDictionary("læse", "hest");

		InputSource @is = new InputSource(this.GetType().getResource("da_UTF8.xml").toExternalForm());
		HyphenationTree hyphenator = HyphenationCompoundWordTokenFilter.getHyphenationTree(@is);

		HyphenationCompoundWordTokenFilter tf = new HyphenationCompoundWordTokenFilter(TEST_VERSION_CURRENT, new MockTokenizer(new StringReader("min veninde som er lidt af en læsehest"), MockTokenizer.WHITESPACE, false), hyphenator, dict, CompoundWordTokenFilterBase.DEFAULT_MIN_WORD_SIZE, CompoundWordTokenFilterBase.DEFAULT_MIN_SUBWORD_SIZE, CompoundWordTokenFilterBase.DEFAULT_MAX_SUBWORD_SIZE, false);
		assertTokenStreamContents(tf, new string[] {"min", "veninde", "som", "er", "lidt", "af", "en", "læsehest", "læse", "hest"}, new int[] {1, 1, 1, 1, 1, 1, 1, 1, 0, 0});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testHyphenationCompoundWordsDELongestMatch() throws Exception
	  public virtual void testHyphenationCompoundWordsDELongestMatch()
	  {
		CharArraySet dict = makeDictionary("basketball", "basket", "ball", "kurv");

		InputSource @is = new InputSource(this.GetType().getResource("da_UTF8.xml").toExternalForm());
		HyphenationTree hyphenator = HyphenationCompoundWordTokenFilter.getHyphenationTree(@is);

		// the word basket will not be added due to the longest match option
		HyphenationCompoundWordTokenFilter tf = new HyphenationCompoundWordTokenFilter(TEST_VERSION_CURRENT, new MockTokenizer(new StringReader("basketballkurv"), MockTokenizer.WHITESPACE, false), hyphenator, dict, CompoundWordTokenFilterBase.DEFAULT_MIN_WORD_SIZE, CompoundWordTokenFilterBase.DEFAULT_MIN_SUBWORD_SIZE, 40, true);
		assertTokenStreamContents(tf, new string[] {"basketballkurv", "basketball", "ball", "kurv"}, new int[] {1, 0, 0, 0});

	  }

	  /// <summary>
	  /// With hyphenation-only, you can get a lot of nonsense tokens.
	  /// This can be controlled with the min/max subword size.
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testHyphenationOnly() throws Exception
	  public virtual void testHyphenationOnly()
	  {
		InputSource @is = new InputSource(this.GetType().getResource("da_UTF8.xml").toExternalForm());
		HyphenationTree hyphenator = HyphenationCompoundWordTokenFilter.getHyphenationTree(@is);

		HyphenationCompoundWordTokenFilter tf = new HyphenationCompoundWordTokenFilter(TEST_VERSION_CURRENT, new MockTokenizer(new StringReader("basketballkurv"), MockTokenizer.WHITESPACE, false), hyphenator, CompoundWordTokenFilterBase.DEFAULT_MIN_WORD_SIZE, 2, 4);

		// min=2, max=4
		assertTokenStreamContents(tf, new string[] {"basketballkurv", "ba", "sket", "bal", "ball", "kurv"});

		tf = new HyphenationCompoundWordTokenFilter(TEST_VERSION_CURRENT, new MockTokenizer(new StringReader("basketballkurv"), MockTokenizer.WHITESPACE, false), hyphenator, CompoundWordTokenFilterBase.DEFAULT_MIN_WORD_SIZE, 4, 6);

		// min=4, max=6
		assertTokenStreamContents(tf, new string[] {"basketballkurv", "basket", "sket", "ball", "lkurv", "kurv"});

		tf = new HyphenationCompoundWordTokenFilter(TEST_VERSION_CURRENT, new MockTokenizer(new StringReader("basketballkurv"), MockTokenizer.WHITESPACE, false), hyphenator, CompoundWordTokenFilterBase.DEFAULT_MIN_WORD_SIZE, 4, 10);

		// min=4, max=10
		assertTokenStreamContents(tf, new string[] {"basketballkurv", "basket", "basketbal", "basketball", "sket", "sketbal", "sketball", "ball", "ballkurv", "lkurv", "kurv"});

	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testDumbCompoundWordsSE() throws Exception
	  public virtual void testDumbCompoundWordsSE()
	  {
		CharArraySet dict = makeDictionary("Bil", "Dörr", "Motor", "Tak", "Borr", "Slag", "Hammar", "Pelar", "Glas", "Ögon", "Fodral", "Bas", "Fiol", "Makare", "Gesäll", "Sko", "Vind", "Rute", "Torkare", "Blad");

		DictionaryCompoundWordTokenFilter tf = new DictionaryCompoundWordTokenFilter(TEST_VERSION_CURRENT, new MockTokenizer(new StringReader("Bildörr Bilmotor Biltak Slagborr Hammarborr Pelarborr Glasögonfodral Basfiolsfodral Basfiolsfodralmakaregesäll Skomakare Vindrutetorkare Vindrutetorkarblad abba"), MockTokenizer.WHITESPACE, false), dict);

		assertTokenStreamContents(tf, new string[] {"Bildörr", "Bil", "dörr", "Bilmotor", "Bil", "motor", "Biltak", "Bil", "tak", "Slagborr", "Slag", "borr", "Hammarborr", "Hammar", "borr", "Pelarborr", "Pelar", "borr", "Glasögonfodral", "Glas", "ögon", "fodral", "Basfiolsfodral", "Bas", "fiol", "fodral", "Basfiolsfodralmakaregesäll", "Bas", "fiol", "fodral", "makare", "gesäll", "Skomakare", "Sko", "makare", "Vindrutetorkare", "Vind", "rute", "torkare", "Vindrutetorkarblad", "Vind", "rute", "blad", "abba"}, new int[] {0, 0, 0, 8, 8, 8, 17, 17, 17, 24, 24, 24, 33, 33, 33, 44, 44, 44, 54, 54, 54, 54, 69, 69, 69, 69, 84, 84, 84, 84, 84, 84, 111, 111, 111, 121, 121, 121, 121, 137, 137, 137, 137, 156}, new int[] {7, 7, 7, 16, 16, 16, 23, 23, 23, 32, 32, 32, 43, 43, 43, 53, 53, 53, 68, 68, 68, 68, 83, 83, 83, 83, 110, 110, 110, 110, 110, 110, 120, 120, 120, 136, 136, 136, 136, 155, 155, 155, 155, 160}, new int[] {1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testDumbCompoundWordsSELongestMatch() throws Exception
	  public virtual void testDumbCompoundWordsSELongestMatch()
	  {
		CharArraySet dict = makeDictionary("Bil", "Dörr", "Motor", "Tak", "Borr", "Slag", "Hammar", "Pelar", "Glas", "Ögon", "Fodral", "Bas", "Fiols", "Makare", "Gesäll", "Sko", "Vind", "Rute", "Torkare", "Blad", "Fiolsfodral");

		DictionaryCompoundWordTokenFilter tf = new DictionaryCompoundWordTokenFilter(TEST_VERSION_CURRENT, new MockTokenizer(new StringReader("Basfiolsfodralmakaregesäll"), MockTokenizer.WHITESPACE, false), dict, CompoundWordTokenFilterBase.DEFAULT_MIN_WORD_SIZE, CompoundWordTokenFilterBase.DEFAULT_MIN_SUBWORD_SIZE, CompoundWordTokenFilterBase.DEFAULT_MAX_SUBWORD_SIZE, true);

		assertTokenStreamContents(tf, new string[] {"Basfiolsfodralmakaregesäll", "Bas", "fiolsfodral", "fodral", "makare", "gesäll"}, new int[] {0, 0, 0, 0, 0, 0}, new int[] {26, 26, 26, 26, 26, 26}, new int[] {1, 0, 0, 0, 0, 0});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testTokenEndingWithWordComponentOfMinimumLength() throws Exception
	  public virtual void testTokenEndingWithWordComponentOfMinimumLength()
	  {
		CharArraySet dict = makeDictionary("ab", "cd", "ef");

		DictionaryCompoundWordTokenFilter tf = new DictionaryCompoundWordTokenFilter(TEST_VERSION_CURRENT, new WhitespaceTokenizer(TEST_VERSION_CURRENT, new StringReader("abcdef")
		   ), dict, CompoundWordTokenFilterBase.DEFAULT_MIN_WORD_SIZE, CompoundWordTokenFilterBase.DEFAULT_MIN_SUBWORD_SIZE, CompoundWordTokenFilterBase.DEFAULT_MAX_SUBWORD_SIZE, false);

		assertTokenStreamContents(tf, new string[] {"abcdef", "ab", "cd", "ef"}, new int[] {0, 0, 0, 0}, new int[] {6, 6, 6, 6}, new int[] {1, 0, 0, 0});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testWordComponentWithLessThanMinimumLength() throws Exception
	  public virtual void testWordComponentWithLessThanMinimumLength()
	  {
		CharArraySet dict = makeDictionary("abc", "d", "efg");

		DictionaryCompoundWordTokenFilter tf = new DictionaryCompoundWordTokenFilter(TEST_VERSION_CURRENT, new WhitespaceTokenizer(TEST_VERSION_CURRENT, new StringReader("abcdefg")
		   ), dict, CompoundWordTokenFilterBase.DEFAULT_MIN_WORD_SIZE, CompoundWordTokenFilterBase.DEFAULT_MIN_SUBWORD_SIZE, CompoundWordTokenFilterBase.DEFAULT_MAX_SUBWORD_SIZE, false);

	  // since "d" is shorter than the minimum subword size, it should not be added to the token stream
		assertTokenStreamContents(tf, new string[] {"abcdefg", "abc", "efg"}, new int[] {0, 0, 0}, new int[] {7, 7, 7}, new int[] {1, 0, 0});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testReset() throws Exception
	  public virtual void testReset()
	  {
		CharArraySet dict = makeDictionary("Rind", "Fleisch", "Draht", "Schere", "Gesetz", "Aufgabe", "Überwachung");

		Tokenizer wsTokenizer = new WhitespaceTokenizer(TEST_VERSION_CURRENT, new StringReader("Rindfleischüberwachungsgesetz"));
		DictionaryCompoundWordTokenFilter tf = new DictionaryCompoundWordTokenFilter(TEST_VERSION_CURRENT, wsTokenizer, dict, CompoundWordTokenFilterBase.DEFAULT_MIN_WORD_SIZE, CompoundWordTokenFilterBase.DEFAULT_MIN_SUBWORD_SIZE, CompoundWordTokenFilterBase.DEFAULT_MAX_SUBWORD_SIZE, false);

		CharTermAttribute termAtt = tf.getAttribute(typeof(CharTermAttribute));
		tf.reset();
		assertTrue(tf.incrementToken());
		assertEquals("Rindfleischüberwachungsgesetz", termAtt.ToString());
		assertTrue(tf.incrementToken());
		assertEquals("Rind", termAtt.ToString());
		tf.end();
		tf.close();
		wsTokenizer.Reader = new StringReader("Rindfleischüberwachungsgesetz");
		tf.reset();
		assertTrue(tf.incrementToken());
		assertEquals("Rindfleischüberwachungsgesetz", termAtt.ToString());
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRetainMockAttribute() throws Exception
	  public virtual void testRetainMockAttribute()
	  {
		CharArraySet dict = makeDictionary("abc", "d", "efg");
		Tokenizer tokenizer = new WhitespaceTokenizer(TEST_VERSION_CURRENT, new StringReader("abcdefg"));
		TokenStream stream = new MockRetainAttributeFilter(tokenizer);
		stream = new DictionaryCompoundWordTokenFilter(TEST_VERSION_CURRENT, stream, dict, CompoundWordTokenFilterBase.DEFAULT_MIN_WORD_SIZE, CompoundWordTokenFilterBase.DEFAULT_MIN_SUBWORD_SIZE, CompoundWordTokenFilterBase.DEFAULT_MAX_SUBWORD_SIZE, false);
		MockRetainAttribute retAtt = stream.addAttribute(typeof(MockRetainAttribute));
		stream.reset();
		while (stream.incrementToken())
		{
		  assertTrue("Custom attribute value was lost", retAtt.Retain);
		}

	  }

	  public interface MockRetainAttribute : Attribute
	  {
		bool Retain {set;get;}
	  }

	  public sealed class MockRetainAttributeImpl : AttributeImpl, MockRetainAttribute
	  {
		internal bool retain = false;
		public override void clear()
		{
		  retain = false;
		}
		public bool Retain
		{
			get
			{
			  return retain;
			}
			set
			{
			  this.retain = value;
			}
		}
		public override void copyTo(AttributeImpl target)
		{
		  MockRetainAttribute t = (MockRetainAttribute) target;
		  t.Retain = retain;
		}
	  }

	  private class MockRetainAttributeFilter : TokenFilter
	  {

		internal MockRetainAttribute retainAtt = addAttribute(typeof(MockRetainAttribute));

		internal MockRetainAttributeFilter(TokenStream input) : base(input)
		{
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public boolean incrementToken() throws java.io.IOException
		public override bool incrementToken()
		{
		  if (input.incrementToken())
		  {
			retainAtt.Retain = true;
			return true;
		  }
		  else
		  {
		  return false;
		  }
		}
	  }

	  // SOLR-2891
	  // *CompoundWordTokenFilter blindly adds term length to offset, but this can take things out of bounds
	  // wrt original text if a previous filter increases the length of the word (in this case ü -> ue)
	  // so in this case we behave like WDF, and preserve any modified offsets
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testInvalidOffsets() throws Exception
	  public virtual void testInvalidOffsets()
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.analysis.util.CharArraySet dict = makeDictionary("fall");
		CharArraySet dict = makeDictionary("fall");
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.analysis.charfilter.NormalizeCharMap.Builder builder = new org.apache.lucene.analysis.charfilter.NormalizeCharMap.Builder();
		NormalizeCharMap.Builder builder = new NormalizeCharMap.Builder();
		builder.add("ü", "ue");
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.analysis.charfilter.NormalizeCharMap normMap = builder.build();
		NormalizeCharMap normMap = builder.build();

		Analyzer analyzer = new AnalyzerAnonymousInnerClassHelper(this, dict, normMap);

		assertAnalyzesTo(analyzer, "banküberfall", new string[] {"bankueberfall", "fall"}, new int[] {0, 0}, new int[] {12, 12});
	  }

	  private class AnalyzerAnonymousInnerClassHelper : Analyzer
	  {
		  private readonly TestCompoundWordTokenFilter outerInstance;

		  private CharArraySet dict;
		  private NormalizeCharMap normMap;

		  public AnalyzerAnonymousInnerClassHelper(TestCompoundWordTokenFilter outerInstance, CharArraySet dict, NormalizeCharMap normMap)
		  {
			  this.outerInstance = outerInstance;
			  this.dict = dict;
			  this.normMap = normMap;
		  }


		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
			TokenFilter filter = new DictionaryCompoundWordTokenFilter(TEST_VERSION_CURRENT, tokenizer, dict);
			return new TokenStreamComponents(tokenizer, filter);
		  }

		  protected internal override Reader initReader(string fieldName, Reader reader)
		  {
			return new MappingCharFilter(normMap, reader);
		  }
	  }

	  /// <summary>
	  /// blast some random strings through the analyzer </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandomStrings() throws Exception
	  public virtual void testRandomStrings()
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.analysis.util.CharArraySet dict = makeDictionary("a", "e", "i", "o", "u", "y", "bc", "def");
		CharArraySet dict = makeDictionary("a", "e", "i", "o", "u", "y", "bc", "def");
		Analyzer a = new AnalyzerAnonymousInnerClassHelper2(this, dict);
		checkRandomData(random(), a, 1000 * RANDOM_MULTIPLIER);

		InputSource @is = new InputSource(this.GetType().getResource("da_UTF8.xml").toExternalForm());
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.analysis.compound.hyphenation.HyphenationTree hyphenator = HyphenationCompoundWordTokenFilter.getHyphenationTree(is);
		HyphenationTree hyphenator = HyphenationCompoundWordTokenFilter.getHyphenationTree(@is);
		Analyzer b = new AnalyzerAnonymousInnerClassHelper3(this, hyphenator);
		checkRandomData(random(), b, 1000 * RANDOM_MULTIPLIER);
	  }

	  private class AnalyzerAnonymousInnerClassHelper2 : Analyzer
	  {
		  private readonly TestCompoundWordTokenFilter outerInstance;

		  private CharArraySet dict;

		  public AnalyzerAnonymousInnerClassHelper2(TestCompoundWordTokenFilter outerInstance, CharArraySet dict)
		  {
			  this.outerInstance = outerInstance;
			  this.dict = dict;
		  }


		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
			return new TokenStreamComponents(tokenizer, new DictionaryCompoundWordTokenFilter(TEST_VERSION_CURRENT, tokenizer, dict));
		  }
	  }

	  private class AnalyzerAnonymousInnerClassHelper3 : Analyzer
	  {
		  private readonly TestCompoundWordTokenFilter outerInstance;

		  private HyphenationTree hyphenator;

		  public AnalyzerAnonymousInnerClassHelper3(TestCompoundWordTokenFilter outerInstance, HyphenationTree hyphenator)
		  {
			  this.outerInstance = outerInstance;
			  this.hyphenator = hyphenator;
		  }


		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
			TokenFilter filter = new HyphenationCompoundWordTokenFilter(TEST_VERSION_CURRENT, tokenizer, hyphenator);
			return new TokenStreamComponents(tokenizer, filter);
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testEmptyTerm() throws Exception
	  public virtual void testEmptyTerm()
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.analysis.util.CharArraySet dict = makeDictionary("a", "e", "i", "o", "u", "y", "bc", "def");
		CharArraySet dict = makeDictionary("a", "e", "i", "o", "u", "y", "bc", "def");
		Analyzer a = new AnalyzerAnonymousInnerClassHelper4(this, dict);
		checkOneTerm(a, "", "");

		InputSource @is = new InputSource(this.GetType().getResource("da_UTF8.xml").toExternalForm());
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.analysis.compound.hyphenation.HyphenationTree hyphenator = HyphenationCompoundWordTokenFilter.getHyphenationTree(is);
		HyphenationTree hyphenator = HyphenationCompoundWordTokenFilter.getHyphenationTree(@is);
		Analyzer b = new AnalyzerAnonymousInnerClassHelper5(this, hyphenator);
		checkOneTerm(b, "", "");
	  }

	  private class AnalyzerAnonymousInnerClassHelper4 : Analyzer
	  {
		  private readonly TestCompoundWordTokenFilter outerInstance;

		  private CharArraySet dict;

		  public AnalyzerAnonymousInnerClassHelper4(TestCompoundWordTokenFilter outerInstance, CharArraySet dict)
		  {
			  this.outerInstance = outerInstance;
			  this.dict = dict;
		  }


		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new KeywordTokenizer(reader);
			return new TokenStreamComponents(tokenizer, new DictionaryCompoundWordTokenFilter(TEST_VERSION_CURRENT, tokenizer, dict));
		  }
	  }

	  private class AnalyzerAnonymousInnerClassHelper5 : Analyzer
	  {
		  private readonly TestCompoundWordTokenFilter outerInstance;

		  private HyphenationTree hyphenator;

		  public AnalyzerAnonymousInnerClassHelper5(TestCompoundWordTokenFilter outerInstance, HyphenationTree hyphenator)
		  {
			  this.outerInstance = outerInstance;
			  this.hyphenator = hyphenator;
		  }


		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new KeywordTokenizer(reader);
			TokenFilter filter = new HyphenationCompoundWordTokenFilter(TEST_VERSION_CURRENT, tokenizer, hyphenator);
			return new TokenStreamComponents(tokenizer, filter);
		  }
	  }
	}

}