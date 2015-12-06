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



	using Version = org.apache.lucene.util.Version;
	using TestUtil = org.apache.lucene.util.TestUtil;

	using RandomStrings = com.carrotsearch.randomizedtesting.generators.RandomStrings;

	/// <summary>
	/// Tests <seealso cref="EdgeNGramTokenizer"/> for correctness.
	/// </summary>
	public class EdgeNGramTokenizerTest : BaseTokenStreamTestCase
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
		  new EdgeNGramTokenizer(TEST_VERSION_CURRENT, input, 0, 0);
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
		  new EdgeNGramTokenizer(TEST_VERSION_CURRENT, input, 2, 1);
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
		  new EdgeNGramTokenizer(TEST_VERSION_CURRENT, input, -1, 2);
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
		EdgeNGramTokenizer tokenizer = new EdgeNGramTokenizer(TEST_VERSION_CURRENT, input, 1, 1);
		assertTokenStreamContents(tokenizer, new string[]{"a"}, new int[]{0}, new int[]{1}, 5); // abcde
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBackUnigram() throws Exception
	  public virtual void testBackUnigram()
	  {
		Tokenizer tokenizer = new Lucene43EdgeNGramTokenizer(Version.LUCENE_43, input, Lucene43EdgeNGramTokenizer.Side.BACK, 1, 1);
		assertTokenStreamContents(tokenizer, new string[]{"e"}, new int[]{4}, new int[]{5}, 5); // abcde
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testOversizedNgrams() throws Exception
	  public virtual void testOversizedNgrams()
	  {
		EdgeNGramTokenizer tokenizer = new EdgeNGramTokenizer(TEST_VERSION_CURRENT, input, 6, 6);
		assertTokenStreamContents(tokenizer, new string[0], new int[0], new int[0], 5); // abcde
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testFrontRangeOfNgrams() throws Exception
	  public virtual void testFrontRangeOfNgrams()
	  {
		EdgeNGramTokenizer tokenizer = new EdgeNGramTokenizer(TEST_VERSION_CURRENT, input, 1, 3);
		assertTokenStreamContents(tokenizer, new string[]{"a","ab","abc"}, new int[]{0,0,0}, new int[]{1,2,3}, 5); // abcde
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBackRangeOfNgrams() throws Exception
	  public virtual void testBackRangeOfNgrams()
	  {
		Tokenizer tokenizer = new Lucene43EdgeNGramTokenizer(Version.LUCENE_43, input, Lucene43EdgeNGramTokenizer.Side.BACK, 1, 3);
		assertTokenStreamContents(tokenizer, new string[]{"e","de","cde"}, new int[]{4,3,2}, new int[]{5,5,5}, null, null, null, 5, false); // abcde
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testReset() throws Exception
	  public virtual void testReset()
	  {
		EdgeNGramTokenizer tokenizer = new EdgeNGramTokenizer(TEST_VERSION_CURRENT, input, 1, 3);
		assertTokenStreamContents(tokenizer, new string[]{"a","ab","abc"}, new int[]{0,0,0}, new int[]{1,2,3}, 5); // abcde
		tokenizer.Reader = new StringReader("abcde");
		assertTokenStreamContents(tokenizer, new string[]{"a","ab","abc"}, new int[]{0,0,0}, new int[]{1,2,3}, 5); // abcde
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
		  checkRandomData(random(), a, 100 * RANDOM_MULTIPLIER, 20);
		  checkRandomData(random(), a, 10 * RANDOM_MULTIPLIER, 8192);
		}

		Analyzer b = new AnalyzerAnonymousInnerClassHelper2(this);
		checkRandomData(random(), b, 1000 * RANDOM_MULTIPLIER, 20, false, false);
		checkRandomData(random(), b, 100 * RANDOM_MULTIPLIER, 8192, false, false);
	  }

	  private class AnalyzerAnonymousInnerClassHelper : Analyzer
	  {
		  private readonly EdgeNGramTokenizerTest outerInstance;

		  private int min;
		  private int max;

		  public AnalyzerAnonymousInnerClassHelper(EdgeNGramTokenizerTest outerInstance, int min, int max)
		  {
			  this.outerInstance = outerInstance;
			  this.min = min;
			  this.max = max;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new EdgeNGramTokenizer(TEST_VERSION_CURRENT, reader, min, max);
			return new TokenStreamComponents(tokenizer, tokenizer);
		  }
	  }

	  private class AnalyzerAnonymousInnerClassHelper2 : Analyzer
	  {
		  private readonly EdgeNGramTokenizerTest outerInstance;

		  public AnalyzerAnonymousInnerClassHelper2(EdgeNGramTokenizerTest outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new Lucene43EdgeNGramTokenizer(Version.LUCENE_43, reader, Lucene43EdgeNGramTokenizer.Side.BACK, 2, 4);
			return new TokenStreamComponents(tokenizer, tokenizer);
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testTokenizerPositions() throws Exception
	  public virtual void testTokenizerPositions()
	  {
		Tokenizer tokenizer = new Lucene43EdgeNGramTokenizer(Version.LUCENE_43, input, Lucene43EdgeNGramTokenizer.Side.FRONT, 1, 3);
		assertTokenStreamContents(tokenizer, new string[]{"a","ab","abc"}, new int[]{0,0,0}, new int[]{1,2,3}, null, new int[] {1,0,0}, null, null, false);

		tokenizer = new EdgeNGramTokenizer(TEST_VERSION_CURRENT, new StringReader("abcde"), 1, 3);
		assertTokenStreamContents(tokenizer, new string[]{"a","ab","abc"}, new int[]{0,0,0}, new int[]{1,2,3}, null, new int[]{1,1,1}, null, null, false);
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
		NGramTokenizerTest.testNGrams(minGram, maxGram, s, nonTokenChars, true);
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