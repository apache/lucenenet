using System;

namespace org.apache.lucene.analysis.ngram
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
	using LetterTokenizer = org.apache.lucene.analysis.core.LetterTokenizer;
	using WhitespaceTokenizer = org.apache.lucene.analysis.core.WhitespaceTokenizer;
	using ASCIIFoldingFilter = org.apache.lucene.analysis.miscellaneous.ASCIIFoldingFilter;
	using ShingleFilter = org.apache.lucene.analysis.shingle.ShingleFilter;
	using CharTermAttribute = org.apache.lucene.analysis.tokenattributes.CharTermAttribute;
	using OffsetAttribute = org.apache.lucene.analysis.tokenattributes.OffsetAttribute;
	using PositionIncrementAttribute = org.apache.lucene.analysis.tokenattributes.PositionIncrementAttribute;
	using Version = org.apache.lucene.util.Version;
	using TestUtil = org.apache.lucene.util.TestUtil;

	/// <summary>
	/// Tests <seealso cref="EdgeNGramTokenFilter"/> for correctness.
	/// </summary>
	public class EdgeNGramTokenFilterTest : BaseTokenStreamTestCase
	{
	  private TokenStream input;

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void setUp() throws Exception
	  public override void setUp()
	  {
		base.setUp();
		input = new MockTokenizer(new StringReader("abcde"), MockTokenizer.WHITESPACE, false);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testInvalidInput() throws Exception
	  public virtual void testInvalidInput()
	  {
		bool gotException = false;
		try
		{
		  new EdgeNGramTokenFilter(TEST_VERSION_CURRENT, input, EdgeNGramTokenFilter.Side.FRONT, 0, 0);
		}
		catch (System.ArgumentException)
		{
		  gotException = true;
		}
		assertTrue(gotException);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testInvalidInput2() throws Exception
	  public virtual void testInvalidInput2()
	  {
		bool gotException = false;
		try
		{
		  new EdgeNGramTokenFilter(TEST_VERSION_CURRENT, input, EdgeNGramTokenFilter.Side.FRONT, 2, 1);
		}
		catch (System.ArgumentException)
		{
		  gotException = true;
		}
		assertTrue(gotException);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testInvalidInput3() throws Exception
	  public virtual void testInvalidInput3()
	  {
		bool gotException = false;
		try
		{
		  new EdgeNGramTokenFilter(TEST_VERSION_CURRENT, input, EdgeNGramTokenFilter.Side.FRONT, -1, 2);
		}
		catch (System.ArgumentException)
		{
		  gotException = true;
		}
		assertTrue(gotException);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testFrontUnigram() throws Exception
	  public virtual void testFrontUnigram()
	  {
		EdgeNGramTokenFilter tokenizer = new EdgeNGramTokenFilter(TEST_VERSION_CURRENT, input, EdgeNGramTokenFilter.Side.FRONT, 1, 1);
		assertTokenStreamContents(tokenizer, new string[]{"a"}, new int[]{0}, new int[]{5});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBackUnigram() throws Exception
	  public virtual void testBackUnigram()
	  {
		EdgeNGramTokenFilter tokenizer = new EdgeNGramTokenFilter(Version.LUCENE_43, input, EdgeNGramTokenFilter.Side.BACK, 1, 1);
		assertTokenStreamContents(tokenizer, new string[]{"e"}, new int[]{4}, new int[]{5});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testOversizedNgrams() throws Exception
	  public virtual void testOversizedNgrams()
	  {
		EdgeNGramTokenFilter tokenizer = new EdgeNGramTokenFilter(TEST_VERSION_CURRENT, input, EdgeNGramTokenFilter.Side.FRONT, 6, 6);
		assertTokenStreamContents(tokenizer, new string[0], new int[0], new int[0]);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testFrontRangeOfNgrams() throws Exception
	  public virtual void testFrontRangeOfNgrams()
	  {
		EdgeNGramTokenFilter tokenizer = new EdgeNGramTokenFilter(TEST_VERSION_CURRENT, input, EdgeNGramTokenFilter.Side.FRONT, 1, 3);
		assertTokenStreamContents(tokenizer, new string[]{"a","ab","abc"}, new int[]{0,0,0}, new int[]{5,5,5});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBackRangeOfNgrams() throws Exception
	  public virtual void testBackRangeOfNgrams()
	  {
		EdgeNGramTokenFilter tokenizer = new EdgeNGramTokenFilter(Version.LUCENE_43, input, EdgeNGramTokenFilter.Side.BACK, 1, 3);
		assertTokenStreamContents(tokenizer, new string[]{"e","de","cde"}, new int[]{4,3,2}, new int[]{5,5,5}, null, null, null, null, false);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testFilterPositions() throws Exception
	  public virtual void testFilterPositions()
	  {
		TokenStream ts = new MockTokenizer(new StringReader("abcde vwxyz"), MockTokenizer.WHITESPACE, false);
		EdgeNGramTokenFilter tokenizer = new EdgeNGramTokenFilter(TEST_VERSION_CURRENT, ts, EdgeNGramTokenFilter.Side.FRONT, 1, 3);
		assertTokenStreamContents(tokenizer, new string[]{"a","ab","abc","v","vw","vwx"}, new int[]{0,0,0,6,6,6}, new int[]{5,5,5,11,11,11}, null, new int[]{1,0,0,1,0,0}, null, null, false);
	  }

	  private class PositionFilter : TokenFilter
	  {

		internal readonly PositionIncrementAttribute posIncrAtt = addAttribute(typeof(PositionIncrementAttribute));
		internal bool started;

//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
//ORIGINAL LINE: PositionFilter(final org.apache.lucene.analysis.TokenStream input)
		internal PositionFilter(TokenStream input) : base(input)
		{
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public final boolean incrementToken() throws java.io.IOException
		public override bool incrementToken()
		{
		  if (outerInstance.input.incrementToken())
		  {
			if (started)
			{
			  posIncrAtt.PositionIncrement = 0;
			}
			else
			{
			  started = true;
			}
			return true;
		  }
		  else
		  {
			return false;
		  }
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void reset() throws java.io.IOException
		public override void reset()
		{
		  base.reset();
		  started = false;
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testFirstTokenPositionIncrement() throws Exception
	  public virtual void testFirstTokenPositionIncrement()
	  {
		TokenStream ts = new MockTokenizer(new StringReader("a abc"), MockTokenizer.WHITESPACE, false);
		ts = new PositionFilter(ts); // All but first token will get 0 position increment
		EdgeNGramTokenFilter filter = new EdgeNGramTokenFilter(TEST_VERSION_CURRENT, ts, EdgeNGramTokenFilter.Side.FRONT, 2, 3);
		// The first token "a" will not be output, since it's smaller than the mingram size of 2.
		// The second token on input to EdgeNGramTokenFilter will have position increment of 0,
		// which should be increased to 1, since this is the first output token in the stream.
		assertTokenStreamContents(filter, new string[] {"ab", "abc"}, new int[] {2, 2}, new int[] {5, 5}, new int[] {1, 0});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testSmallTokenInStream() throws Exception
	  public virtual void testSmallTokenInStream()
	  {
		input = new MockTokenizer(new StringReader("abc de fgh"), MockTokenizer.WHITESPACE, false);
		EdgeNGramTokenFilter tokenizer = new EdgeNGramTokenFilter(TEST_VERSION_CURRENT, input, EdgeNGramTokenFilter.Side.FRONT, 3, 3);
		assertTokenStreamContents(tokenizer, new string[]{"abc","fgh"}, new int[]{0,7}, new int[]{3,10});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testReset() throws Exception
	  public virtual void testReset()
	  {
		WhitespaceTokenizer tokenizer = new WhitespaceTokenizer(TEST_VERSION_CURRENT, new StringReader("abcde"));
		EdgeNGramTokenFilter filter = new EdgeNGramTokenFilter(TEST_VERSION_CURRENT, tokenizer, EdgeNGramTokenFilter.Side.FRONT, 1, 3);
		assertTokenStreamContents(filter, new string[]{"a","ab","abc"}, new int[]{0,0,0}, new int[]{5,5,5});
		tokenizer.Reader = new StringReader("abcde");
		assertTokenStreamContents(filter, new string[]{"a","ab","abc"}, new int[]{0,0,0}, new int[]{5,5,5});
	  }

	  // LUCENE-3642
	  // EdgeNgram blindly adds term length to offset, but this can take things out of bounds
	  // wrt original text if a previous filter increases the length of the word (in this case æ -> ae)
	  // so in this case we behave like WDF, and preserve any modified offsets
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testInvalidOffsets() throws Exception
	  public virtual void testInvalidOffsets()
	  {
		Analyzer analyzer = new AnalyzerAnonymousInnerClassHelper(this);
		assertAnalyzesTo(analyzer, "mosfellsbær", new string[] {"mo", "mos", "mosf", "mosfe", "mosfel", "mosfell", "mosfells", "mosfellsb", "mosfellsba", "mosfellsbae", "mosfellsbaer"}, new int[] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}, new int[] {11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11});
	  }

	  private class AnalyzerAnonymousInnerClassHelper : Analyzer
	  {
		  private readonly EdgeNGramTokenFilterTest outerInstance;

		  public AnalyzerAnonymousInnerClassHelper(EdgeNGramTokenFilterTest outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
			TokenFilter filters = new ASCIIFoldingFilter(tokenizer);
			filters = new EdgeNGramTokenFilter(Version.LUCENE_43, filters, EdgeNGramTokenFilter.Side.FRONT, 2, 15);
			return new TokenStreamComponents(tokenizer, filters);
		  }
	  }

	  /// <summary>
	  /// blast some random strings through the analyzer </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandomStrings() throws Exception
	  public virtual void testRandomStrings()
	  {
		for (int i = 0; i < 10; i++)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int min = org.apache.lucene.util.TestUtil.nextInt(random(), 2, 10);
		  int min = TestUtil.Next(random(), 2, 10);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int max = org.apache.lucene.util.TestUtil.nextInt(random(), min, 20);
		  int max = TestUtil.Next(random(), min, 20);

		  Analyzer a = new AnalyzerAnonymousInnerClassHelper2(this, min, max);
		  checkRandomData(random(), a, 100 * RANDOM_MULTIPLIER);
		}

		Analyzer b = new AnalyzerAnonymousInnerClassHelper3(this);
		checkRandomData(random(), b, 1000 * RANDOM_MULTIPLIER, 20, false, false);
	  }

	  private class AnalyzerAnonymousInnerClassHelper2 : Analyzer
	  {
		  private readonly EdgeNGramTokenFilterTest outerInstance;

		  private int min;
		  private int max;

		  public AnalyzerAnonymousInnerClassHelper2(EdgeNGramTokenFilterTest outerInstance, int min, int max)
		  {
			  this.outerInstance = outerInstance;
			  this.min = min;
			  this.max = max;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
			return new TokenStreamComponents(tokenizer, new EdgeNGramTokenFilter(TEST_VERSION_CURRENT, tokenizer, min, max));
		  }
	  }

	  private class AnalyzerAnonymousInnerClassHelper3 : Analyzer
	  {
		  private readonly EdgeNGramTokenFilterTest outerInstance;

		  public AnalyzerAnonymousInnerClassHelper3(EdgeNGramTokenFilterTest outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
			return new TokenStreamComponents(tokenizer, new EdgeNGramTokenFilter(Version.LUCENE_43, tokenizer, EdgeNGramTokenFilter.Side.BACK, 2, 4));
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testEmptyTerm() throws Exception
	  public virtual void testEmptyTerm()
	  {
		Random random = random();
		Analyzer a = new AnalyzerAnonymousInnerClassHelper4(this);
		checkAnalysisConsistency(random, a, random.nextBoolean(), "");

		Analyzer b = new AnalyzerAnonymousInnerClassHelper5(this);
		checkAnalysisConsistency(random, b, random.nextBoolean(), "");
	  }

	  private class AnalyzerAnonymousInnerClassHelper4 : Analyzer
	  {
		  private readonly EdgeNGramTokenFilterTest outerInstance;

		  public AnalyzerAnonymousInnerClassHelper4(EdgeNGramTokenFilterTest outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new KeywordTokenizer(reader);
			return new TokenStreamComponents(tokenizer, new EdgeNGramTokenFilter(TEST_VERSION_CURRENT, tokenizer, EdgeNGramTokenFilter.Side.FRONT, 2, 15));
		  }
	  }

	  private class AnalyzerAnonymousInnerClassHelper5 : Analyzer
	  {
		  private readonly EdgeNGramTokenFilterTest outerInstance;

		  public AnalyzerAnonymousInnerClassHelper5(EdgeNGramTokenFilterTest outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new KeywordTokenizer(reader);
			return new TokenStreamComponents(tokenizer, new EdgeNGramTokenFilter(Version.LUCENE_43, tokenizer, EdgeNGramTokenFilter.Side.BACK, 2, 15));
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testGraphs() throws java.io.IOException
	  public virtual void testGraphs()
	  {
		TokenStream tk = new LetterTokenizer(TEST_VERSION_CURRENT, new StringReader("abc d efgh ij klmno p q"));
		tk = new ShingleFilter(tk);
		tk = new EdgeNGramTokenFilter(TEST_VERSION_CURRENT, tk, 7, 10);
		assertTokenStreamContents(tk, new string[] {"efgh ij", "ij klmn", "ij klmno", "klmno p"}, new int[] {6,11,11,14}, new int[] {13,19,19,21}, new int[] {3,1,0,1}, new int[] {2,2,2,2}, 23);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testSupplementaryCharacters() throws java.io.IOException
	  public virtual void testSupplementaryCharacters()
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String s = org.apache.lucene.util.TestUtil.randomUnicodeString(random(), 10);
		string s = TestUtil.randomUnicodeString(random(), 10);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int codePointCount = s.codePointCount(0, s.length());
		int codePointCount = s.codePointCount(0, s.Length);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int minGram = org.apache.lucene.util.TestUtil.nextInt(random(), 1, 3);
		int minGram = TestUtil.Next(random(), 1, 3);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int maxGram = org.apache.lucene.util.TestUtil.nextInt(random(), minGram, 10);
		int maxGram = TestUtil.Next(random(), minGram, 10);
		TokenStream tk = new KeywordTokenizer(new StringReader(s));
		tk = new EdgeNGramTokenFilter(TEST_VERSION_CURRENT, tk, minGram, maxGram);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.analysis.tokenattributes.CharTermAttribute termAtt = tk.addAttribute(org.apache.lucene.analysis.tokenattributes.CharTermAttribute.class);
		CharTermAttribute termAtt = tk.addAttribute(typeof(CharTermAttribute));
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.analysis.tokenattributes.OffsetAttribute offsetAtt = tk.addAttribute(org.apache.lucene.analysis.tokenattributes.OffsetAttribute.class);
		OffsetAttribute offsetAtt = tk.addAttribute(typeof(OffsetAttribute));
		tk.reset();
		for (int i = minGram; i <= Math.Min(codePointCount, maxGram); ++i)
		{
		  assertTrue(tk.incrementToken());
		  assertEquals(0, offsetAtt.startOffset());
		  assertEquals(s.Length, offsetAtt.endOffset());
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int end = Character.offsetByCodePoints(s, 0, i);
		  int end = char.offsetByCodePoints(s, 0, i);
		  assertEquals(s.Substring(0, end), termAtt.ToString());
		}
		assertFalse(tk.incrementToken());
	  }

	}

}