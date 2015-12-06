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
	using WhitespaceTokenizer = org.apache.lucene.analysis.core.WhitespaceTokenizer;
	using ASCIIFoldingFilter = org.apache.lucene.analysis.miscellaneous.ASCIIFoldingFilter;
	using CharTermAttribute = org.apache.lucene.analysis.tokenattributes.CharTermAttribute;
	using OffsetAttribute = org.apache.lucene.analysis.tokenattributes.OffsetAttribute;
	using TestUtil = org.apache.lucene.util.TestUtil;
	using Version = org.apache.lucene.util.Version;


	/// <summary>
	/// Tests <seealso cref="NGramTokenFilter"/> for correctness.
	/// </summary>
	public class NGramTokenFilterTest : BaseTokenStreamTestCase
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
		  new NGramTokenFilter(TEST_VERSION_CURRENT, input, 2, 1);
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
		  new NGramTokenFilter(TEST_VERSION_CURRENT, input, 0, 1);
		}
		catch (System.ArgumentException)
		{
		  gotException = true;
		}
		assertTrue(gotException);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testUnigrams() throws Exception
	  public virtual void testUnigrams()
	  {
		NGramTokenFilter filter = new NGramTokenFilter(TEST_VERSION_CURRENT, input, 1, 1);
		assertTokenStreamContents(filter, new string[]{"a","b","c","d","e"}, new int[]{0,0,0,0,0}, new int[]{5,5,5,5,5}, new int[]{1,0,0,0,0});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBigrams() throws Exception
	  public virtual void testBigrams()
	  {
		NGramTokenFilter filter = new NGramTokenFilter(TEST_VERSION_CURRENT, input, 2, 2);
		assertTokenStreamContents(filter, new string[]{"ab","bc","cd","de"}, new int[]{0,0,0,0}, new int[]{5,5,5,5}, new int[]{1,0,0,0});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testNgrams() throws Exception
	  public virtual void testNgrams()
	  {
		NGramTokenFilter filter = new NGramTokenFilter(TEST_VERSION_CURRENT, input, 1, 3);
		assertTokenStreamContents(filter, new string[]{"a","ab","abc","b","bc","bcd","c","cd","cde","d","de","e"}, new int[]{0,0,0,0,0,0,0,0,0,0,0,0}, new int[]{5,5,5,5,5,5,5,5,5,5,5,5}, null, new int[]{1,0,0,0,0,0,0,0,0,0,0,0}, null, null, false);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testNgramsNoIncrement() throws Exception
	  public virtual void testNgramsNoIncrement()
	  {
		NGramTokenFilter filter = new NGramTokenFilter(TEST_VERSION_CURRENT, input, 1, 3);
		assertTokenStreamContents(filter, new string[]{"a","ab","abc","b","bc","bcd","c","cd","cde","d","de","e"}, new int[]{0,0,0,0,0,0,0,0,0,0,0,0}, new int[]{5,5,5,5,5,5,5,5,5,5,5,5}, null, new int[]{1,0,0,0,0,0,0,0,0,0,0,0}, null, null, false);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testOversizedNgrams() throws Exception
	  public virtual void testOversizedNgrams()
	  {
		NGramTokenFilter filter = new NGramTokenFilter(TEST_VERSION_CURRENT, input, 6, 7);
		assertTokenStreamContents(filter, new string[0], new int[0], new int[0]);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testSmallTokenInStream() throws Exception
	  public virtual void testSmallTokenInStream()
	  {
		input = new MockTokenizer(new StringReader("abc de fgh"), MockTokenizer.WHITESPACE, false);
		NGramTokenFilter filter = new NGramTokenFilter(TEST_VERSION_CURRENT, input, 3, 3);
		assertTokenStreamContents(filter, new string[]{"abc","fgh"}, new int[]{0,7}, new int[]{3,10}, new int[] {1, 2});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testReset() throws Exception
	  public virtual void testReset()
	  {
		WhitespaceTokenizer tokenizer = new WhitespaceTokenizer(TEST_VERSION_CURRENT, new StringReader("abcde"));
		NGramTokenFilter filter = new NGramTokenFilter(TEST_VERSION_CURRENT, tokenizer, 1, 1);
		assertTokenStreamContents(filter, new string[]{"a","b","c","d","e"}, new int[]{0,0,0,0,0}, new int[]{5,5,5,5,5}, new int[]{1,0,0,0,0});
		tokenizer.Reader = new StringReader("abcde");
		assertTokenStreamContents(filter, new string[]{"a","b","c","d","e"}, new int[]{0,0,0,0,0}, new int[]{5,5,5,5,5}, new int[]{1,0,0,0,0});
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
		assertAnalyzesTo(analyzer, "mosfellsbær", new string[] {"mo", "os", "sf", "fe", "el", "ll", "ls", "sb", "ba", "ae", "er"}, new int[] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}, new int[] {11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11}, new int[] {1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0});
	  }

	  private class AnalyzerAnonymousInnerClassHelper : Analyzer
	  {
		  private readonly NGramTokenFilterTest outerInstance;

		  public AnalyzerAnonymousInnerClassHelper(NGramTokenFilterTest outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
			TokenFilter filters = new ASCIIFoldingFilter(tokenizer);
			filters = new NGramTokenFilter(TEST_VERSION_CURRENT, filters, 2, 2);
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
		  checkRandomData(random(), a, 200 * RANDOM_MULTIPLIER, 20);
		}
	  }

	  private class AnalyzerAnonymousInnerClassHelper2 : Analyzer
	  {
		  private readonly NGramTokenFilterTest outerInstance;

		  private int min;
		  private int max;

		  public AnalyzerAnonymousInnerClassHelper2(NGramTokenFilterTest outerInstance, int min, int max)
		  {
			  this.outerInstance = outerInstance;
			  this.min = min;
			  this.max = max;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
			return new TokenStreamComponents(tokenizer, new NGramTokenFilter(TEST_VERSION_CURRENT, tokenizer, min, max));
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testEmptyTerm() throws Exception
	  public virtual void testEmptyTerm()
	  {
		Random random = random();
		Analyzer a = new AnalyzerAnonymousInnerClassHelper3(this);
		checkAnalysisConsistency(random, a, random.nextBoolean(), "");
	  }

	  private class AnalyzerAnonymousInnerClassHelper3 : Analyzer
	  {
		  private readonly NGramTokenFilterTest outerInstance;

		  public AnalyzerAnonymousInnerClassHelper3(NGramTokenFilterTest outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new KeywordTokenizer(reader);
			return new TokenStreamComponents(tokenizer, new NGramTokenFilter(TEST_VERSION_CURRENT, tokenizer, 2, 15));
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testLucene43() throws java.io.IOException
	  public virtual void testLucene43()
	  {
		NGramTokenFilter filter = new NGramTokenFilter(Version.LUCENE_43, input, 2, 3);
		assertTokenStreamContents(filter, new string[]{"ab","bc","cd","de","abc","bcd","cde"}, new int[]{0,1,2,3,0,1,2}, new int[]{2,3,4,5,3,4,5}, null, new int[]{1,1,1,1,1,1,1}, null, null, false);
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
		tk = new NGramTokenFilter(TEST_VERSION_CURRENT, tk, minGram, maxGram);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.analysis.tokenattributes.CharTermAttribute termAtt = tk.addAttribute(org.apache.lucene.analysis.tokenattributes.CharTermAttribute.class);
		CharTermAttribute termAtt = tk.addAttribute(typeof(CharTermAttribute));
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.analysis.tokenattributes.OffsetAttribute offsetAtt = tk.addAttribute(org.apache.lucene.analysis.tokenattributes.OffsetAttribute.class);
		OffsetAttribute offsetAtt = tk.addAttribute(typeof(OffsetAttribute));
		tk.reset();
		for (int start = 0; start < codePointCount; ++start)
		{
		  for (int end = start + minGram; end <= Math.Min(codePointCount, start + maxGram); ++end)
		  {
			assertTrue(tk.incrementToken());
			assertEquals(0, offsetAtt.startOffset());
			assertEquals(s.Length, offsetAtt.endOffset());
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int startIndex = Character.offsetByCodePoints(s, 0, start);
			int startIndex = char.offsetByCodePoints(s, 0, start);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int endIndex = Character.offsetByCodePoints(s, 0, end);
			int endIndex = char.offsetByCodePoints(s, 0, end);
			assertEquals(s.Substring(startIndex, endIndex - startIndex), termAtt.ToString());
		  }
		}
		assertFalse(tk.incrementToken());
	  }

	}

}