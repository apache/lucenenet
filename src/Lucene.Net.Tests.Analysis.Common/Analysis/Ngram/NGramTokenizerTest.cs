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

//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to C#:
//	import static org.apache.lucene.analysis.ngram.NGramTokenizerTest.isTokenChar;


	using CharTermAttribute = org.apache.lucene.analysis.tokenattributes.CharTermAttribute;
	using OffsetAttribute = org.apache.lucene.analysis.tokenattributes.OffsetAttribute;
	using PositionIncrementAttribute = org.apache.lucene.analysis.tokenattributes.PositionIncrementAttribute;
	using PositionLengthAttribute = org.apache.lucene.analysis.tokenattributes.PositionLengthAttribute;
	using TestUtil = org.apache.lucene.util.TestUtil;

	using RandomStrings = com.carrotsearch.randomizedtesting.generators.RandomStrings;

	/// <summary>
	/// Tests <seealso cref="NGramTokenizer"/> for correctness.
	/// </summary>
	public class NGramTokenizerTest : BaseTokenStreamTestCase
	{
	  private StringReader input;

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void setUp() throws Exception
	  public override void setUp()
	  {
		base.setUp();
		input = new StringReader("abcde");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testInvalidInput() throws Exception
	  public virtual void testInvalidInput()
	  {
		bool gotException = false;
		try
		{
		  new NGramTokenizer(TEST_VERSION_CURRENT, input, 2, 1);
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
		  new NGramTokenizer(TEST_VERSION_CURRENT, input, 0, 1);
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
		NGramTokenizer tokenizer = new NGramTokenizer(TEST_VERSION_CURRENT, input, 1, 1);
		assertTokenStreamContents(tokenizer, new string[]{"a","b","c","d","e"}, new int[]{0,1,2,3,4}, new int[]{1,2,3,4,5}, 5); // abcde
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBigrams() throws Exception
	  public virtual void testBigrams()
	  {
		NGramTokenizer tokenizer = new NGramTokenizer(TEST_VERSION_CURRENT, input, 2, 2);
		assertTokenStreamContents(tokenizer, new string[]{"ab","bc","cd","de"}, new int[]{0,1,2,3}, new int[]{2,3,4,5}, 5); // abcde
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testNgrams() throws Exception
	  public virtual void testNgrams()
	  {
		NGramTokenizer tokenizer = new NGramTokenizer(TEST_VERSION_CURRENT, input, 1, 3);
		assertTokenStreamContents(tokenizer, new string[]{"a","ab", "abc", "b", "bc", "bcd", "c", "cd", "cde", "d", "de", "e"}, new int[]{0,0,0,1,1,1,2,2,2,3,3,4}, new int[]{1,2,3,2,3,4,3,4,5,4,5,5}, null, null, null, 5, false); // abcde
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testOversizedNgrams() throws Exception
	  public virtual void testOversizedNgrams()
	  {
		NGramTokenizer tokenizer = new NGramTokenizer(TEST_VERSION_CURRENT, input, 6, 7);
		assertTokenStreamContents(tokenizer, new string[0], new int[0], new int[0], 5); // abcde
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testReset() throws Exception
	  public virtual void testReset()
	  {
		NGramTokenizer tokenizer = new NGramTokenizer(TEST_VERSION_CURRENT, input, 1, 1);
		assertTokenStreamContents(tokenizer, new string[]{"a","b","c","d","e"}, new int[]{0,1,2,3,4}, new int[]{1,2,3,4,5}, 5); // abcde
		tokenizer.Reader = new StringReader("abcde");
		assertTokenStreamContents(tokenizer, new string[]{"a","b","c","d","e"}, new int[]{0,1,2,3,4}, new int[]{1,2,3,4,5}, 5); // abcde
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
		  Analyzer a = new AnalyzerAnonymousInnerClassHelper(this, min, max);
		  checkRandomData(random(), a, 200 * RANDOM_MULTIPLIER, 20);
		  checkRandomData(random(), a, 10 * RANDOM_MULTIPLIER, 1027);
		}
	  }

	  private class AnalyzerAnonymousInnerClassHelper : Analyzer
	  {
		  private readonly NGramTokenizerTest outerInstance;

		  private int min;
		  private int max;

		  public AnalyzerAnonymousInnerClassHelper(NGramTokenizerTest outerInstance, int min, int max)
		  {
			  this.outerInstance = outerInstance;
			  this.min = min;
			  this.max = max;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new NGramTokenizer(TEST_VERSION_CURRENT, reader, min, max);
			return new TokenStreamComponents(tokenizer, tokenizer);
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private static void testNGrams(int minGram, int maxGram, int length, final String nonTokenChars) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
	  private static void testNGrams(int minGram, int maxGram, int length, string nonTokenChars)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String s = com.carrotsearch.randomizedtesting.generators.RandomStrings.randomAsciiOfLength(random(), length);
		string s = RandomStrings.randomAsciiOfLength(random(), length);
		testNGrams(minGram, maxGram, s, nonTokenChars);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private static void testNGrams(int minGram, int maxGram, String s, String nonTokenChars) throws java.io.IOException
	  private static void testNGrams(int minGram, int maxGram, string s, string nonTokenChars)
	  {
		testNGrams(minGram, maxGram, s, nonTokenChars, false);
	  }

	  internal static int[] toCodePoints(CharSequence s)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int[] codePoints = new int[Character.codePointCount(s, 0, s.length())];
		int[] codePoints = new int[char.codePointCount(s, 0, s.length())];
		for (int i = 0, j = 0; i < s.length(); ++j)
		{
		  codePoints[j] = char.codePointAt(s, i);
		  i += char.charCount(codePoints[j]);
		}
		return codePoints;
	  }

	  internal static bool isTokenChar(string nonTokenChars, int codePoint)
	  {
		for (int i = 0; i < nonTokenChars.Length;)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int cp = nonTokenChars.codePointAt(i);
		  int cp = char.ConvertToUtf32(nonTokenChars, i);
		  if (cp == codePoint)
		  {
			return false;
		  }
		  i += char.charCount(cp);
		}
		return true;
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: static void testNGrams(int minGram, int maxGram, String s, final String nonTokenChars, boolean edgesOnly) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
	  internal static void testNGrams(int minGram, int maxGram, string s, string nonTokenChars, bool edgesOnly)
	  {
		// convert the string to code points
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int[] codePoints = toCodePoints(s);
		int[] codePoints = toCodePoints(s);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int[] offsets = new int[codePoints.length + 1];
		int[] offsets = new int[codePoints.Length + 1];
		for (int i = 0; i < codePoints.Length; ++i)
		{
		  offsets[i + 1] = offsets[i] + char.charCount(codePoints[i]);
		}
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.analysis.TokenStream grams = new NGramTokenizer(TEST_VERSION_CURRENT, new java.io.StringReader(s), minGram, maxGram, edgesOnly)
		TokenStream grams = new NGramTokenizerAnonymousInnerClassHelper(TEST_VERSION_CURRENT, new StringReader(s), minGram, maxGram, edgesOnly, nonTokenChars);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.analysis.tokenattributes.CharTermAttribute termAtt = grams.addAttribute(org.apache.lucene.analysis.tokenattributes.CharTermAttribute.class);
		CharTermAttribute termAtt = grams.addAttribute(typeof(CharTermAttribute));
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.analysis.tokenattributes.PositionIncrementAttribute posIncAtt = grams.addAttribute(org.apache.lucene.analysis.tokenattributes.PositionIncrementAttribute.class);
		PositionIncrementAttribute posIncAtt = grams.addAttribute(typeof(PositionIncrementAttribute));
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.analysis.tokenattributes.PositionLengthAttribute posLenAtt = grams.addAttribute(org.apache.lucene.analysis.tokenattributes.PositionLengthAttribute.class);
		PositionLengthAttribute posLenAtt = grams.addAttribute(typeof(PositionLengthAttribute));
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.analysis.tokenattributes.OffsetAttribute offsetAtt = grams.addAttribute(org.apache.lucene.analysis.tokenattributes.OffsetAttribute.class);
		OffsetAttribute offsetAtt = grams.addAttribute(typeof(OffsetAttribute));
		grams.reset();
		for (int start = 0; start < codePoints.Length; ++start)
		{
		  for (int end = start + minGram; end <= start + maxGram && end <= codePoints.Length; ++end)
		  {
			if (edgesOnly && start > 0 && isTokenChar(nonTokenChars, codePoints[start - 1]))
			{
			  // not on an edge
			  goto nextGramContinue;
			}
			for (int j = start; j < end; ++j)
			{
			  if (!isTokenChar(nonTokenChars, codePoints[j]))
			  {
				goto nextGramContinue;
			  }
			}
			assertTrue(grams.incrementToken());
			assertArrayEquals(Arrays.copyOfRange(codePoints, start, end), toCodePoints(termAtt));
			assertEquals(1, posIncAtt.PositionIncrement);
			assertEquals(1, posLenAtt.PositionLength);
			assertEquals(offsets[start], offsetAtt.startOffset());
			assertEquals(offsets[end], offsetAtt.endOffset());
			  nextGramContinue:;
		  }
		  nextGramBreak:;
		}
		assertFalse(grams.incrementToken());
		grams.end();
		assertEquals(s.Length, offsetAtt.startOffset());
		assertEquals(s.Length, offsetAtt.endOffset());
	  }

	  private class NGramTokenizerAnonymousInnerClassHelper : NGramTokenizer
	  {
		  private string nonTokenChars;

		  public NGramTokenizerAnonymousInnerClassHelper(UnknownType TEST_VERSION_CURRENT, StringReader java, int minGram, int maxGram, bool edgesOnly, string nonTokenChars) : base(TEST_VERSION_CURRENT, StringReader, minGram, maxGram, edgesOnly)
		  {
			  this.nonTokenChars = nonTokenChars;
		  }

		  protected internal override bool isTokenChar(int chr)
		  {
			return nonTokenChars.IndexOf(chr) < 0;
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testLargeInput() throws java.io.IOException
	  public virtual void testLargeInput()
	  {
		// test sliding
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int minGram = org.apache.lucene.util.TestUtil.nextInt(random(), 1, 100);
		int minGram = TestUtil.Next(random(), 1, 100);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int maxGram = org.apache.lucene.util.TestUtil.nextInt(random(), minGram, 100);
		int maxGram = TestUtil.Next(random(), minGram, 100);
		testNGrams(minGram, maxGram, TestUtil.Next(random(), 3 * 1024, 4 * 1024), "");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testLargeMaxGram() throws java.io.IOException
	  public virtual void testLargeMaxGram()
	  {
		// test sliding with maxGram > 1024
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int minGram = org.apache.lucene.util.TestUtil.nextInt(random(), 1290, 1300);
		int minGram = TestUtil.Next(random(), 1290, 1300);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int maxGram = org.apache.lucene.util.TestUtil.nextInt(random(), minGram, 1300);
		int maxGram = TestUtil.Next(random(), minGram, 1300);
		testNGrams(minGram, maxGram, TestUtil.Next(random(), 3 * 1024, 4 * 1024), "");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testPreTokenization() throws java.io.IOException
	  public virtual void testPreTokenization()
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int minGram = org.apache.lucene.util.TestUtil.nextInt(random(), 1, 100);
		int minGram = TestUtil.Next(random(), 1, 100);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int maxGram = org.apache.lucene.util.TestUtil.nextInt(random(), minGram, 100);
		int maxGram = TestUtil.Next(random(), minGram, 100);
		testNGrams(minGram, maxGram, TestUtil.Next(random(), 0, 4 * 1024), "a");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testHeavyPreTokenization() throws java.io.IOException
	  public virtual void testHeavyPreTokenization()
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int minGram = org.apache.lucene.util.TestUtil.nextInt(random(), 1, 100);
		int minGram = TestUtil.Next(random(), 1, 100);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int maxGram = org.apache.lucene.util.TestUtil.nextInt(random(), minGram, 100);
		int maxGram = TestUtil.Next(random(), minGram, 100);
		testNGrams(minGram, maxGram, TestUtil.Next(random(), 0, 4 * 1024), "abcdef");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testFewTokenChars() throws java.io.IOException
	  public virtual void testFewTokenChars()
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final char[] chrs = new char[org.apache.lucene.util.TestUtil.nextInt(random(), 4000, 5000)];
		char[] chrs = new char[TestUtil.Next(random(), 4000, 5000)];
		Arrays.fill(chrs, ' ');
		for (int i = 0; i < chrs.Length; ++i)
		{
		  if (random().nextFloat() < 0.1)
		  {
			chrs[i] = 'a';
		  }
		}
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int minGram = org.apache.lucene.util.TestUtil.nextInt(random(), 1, 2);
		int minGram = TestUtil.Next(random(), 1, 2);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int maxGram = org.apache.lucene.util.TestUtil.nextInt(random(), minGram, 2);
		int maxGram = TestUtil.Next(random(), minGram, 2);
		testNGrams(minGram, maxGram, new string(chrs), " ");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testFullUTF8Range() throws java.io.IOException
	  public virtual void testFullUTF8Range()
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int minGram = org.apache.lucene.util.TestUtil.nextInt(random(), 1, 100);
		int minGram = TestUtil.Next(random(), 1, 100);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int maxGram = org.apache.lucene.util.TestUtil.nextInt(random(), minGram, 100);
		int maxGram = TestUtil.Next(random(), minGram, 100);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String s = org.apache.lucene.util.TestUtil.randomUnicodeString(random(), 4 * 1024);
		string s = TestUtil.randomUnicodeString(random(), 4 * 1024);
		testNGrams(minGram, maxGram, s, "");
		testNGrams(minGram, maxGram, s, "abcdef");
	  }

	}

}