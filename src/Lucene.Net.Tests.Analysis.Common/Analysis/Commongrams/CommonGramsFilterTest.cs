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
namespace org.apache.lucene.analysis.commongrams
{


	using org.apache.lucene.analysis;
	using WhitespaceTokenizer = org.apache.lucene.analysis.core.WhitespaceTokenizer;
	using CharTermAttribute = org.apache.lucene.analysis.tokenattributes.CharTermAttribute;
	using CharArraySet = org.apache.lucene.analysis.util.CharArraySet;

	/// <summary>
	/// Tests CommonGrams(Query)Filter
	/// </summary>
	public class CommonGramsFilterTest : BaseTokenStreamTestCase
	{
	  private static readonly CharArraySet commonWords = new CharArraySet(TEST_VERSION_CURRENT, Arrays.asList("s", "a", "b", "c", "d", "the", "of"), false);

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testReset() throws Exception
	  public virtual void testReset()
	  {
		const string input = "How the s a brown s cow d like A B thing?";
		WhitespaceTokenizer wt = new WhitespaceTokenizer(TEST_VERSION_CURRENT, new StringReader(input));
		CommonGramsFilter cgf = new CommonGramsFilter(TEST_VERSION_CURRENT, wt, commonWords);

		CharTermAttribute term = cgf.addAttribute(typeof(CharTermAttribute));
		cgf.reset();
		assertTrue(cgf.incrementToken());
		assertEquals("How", term.ToString());
		assertTrue(cgf.incrementToken());
		assertEquals("How_the", term.ToString());
		assertTrue(cgf.incrementToken());
		assertEquals("the", term.ToString());
		assertTrue(cgf.incrementToken());
		assertEquals("the_s", term.ToString());
		cgf.close();

		wt.Reader = new StringReader(input);
		cgf.reset();
		assertTrue(cgf.incrementToken());
		assertEquals("How", term.ToString());
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testQueryReset() throws Exception
	  public virtual void testQueryReset()
	  {
		const string input = "How the s a brown s cow d like A B thing?";
		WhitespaceTokenizer wt = new WhitespaceTokenizer(TEST_VERSION_CURRENT, new StringReader(input));
		CommonGramsFilter cgf = new CommonGramsFilter(TEST_VERSION_CURRENT, wt, commonWords);
		CommonGramsQueryFilter nsf = new CommonGramsQueryFilter(cgf);

		CharTermAttribute term = wt.addAttribute(typeof(CharTermAttribute));
		nsf.reset();
		assertTrue(nsf.incrementToken());
		assertEquals("How_the", term.ToString());
		assertTrue(nsf.incrementToken());
		assertEquals("the_s", term.ToString());
		nsf.close();

		wt.Reader = new StringReader(input);
		nsf.reset();
		assertTrue(nsf.incrementToken());
		assertEquals("How_the", term.ToString());
	  }

	  /// <summary>
	  /// This is for testing CommonGramsQueryFilter which outputs a set of tokens
	  /// optimized for querying with only one token at each position, either a
	  /// unigram or a bigram It also will not return a token for the final position
	  /// if the final word is already in the preceding bigram Example:(three
	  /// tokens/positions in)
	  /// "foo bar the"=>"foo:1|bar:2,bar-the:2|the:3=> "foo" "bar-the" (2 tokens
	  /// out)
	  /// 
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testCommonGramsQueryFilter() throws Exception
	  public virtual void testCommonGramsQueryFilter()
	  {
		Analyzer a = new AnalyzerAnonymousInnerClassHelper(this);

		// Stop words used below are "of" "the" and "s"

		// two word queries
		assertAnalyzesTo(a, "brown fox", new string[] {"brown", "fox"});
		assertAnalyzesTo(a, "the fox", new string[] {"the_fox"});
		assertAnalyzesTo(a, "fox of", new string[] {"fox_of"});
		assertAnalyzesTo(a, "of the", new string[] {"of_the"});

		// one word queries
		assertAnalyzesTo(a, "the", new string[] {"the"});
		assertAnalyzesTo(a, "foo", new string[] {"foo"});

		// 3 word combinations s=stopword/common word n=not a stop word
		assertAnalyzesTo(a, "n n n", new string[] {"n", "n", "n"});
		assertAnalyzesTo(a, "quick brown fox", new string[] {"quick", "brown", "fox"});

		assertAnalyzesTo(a, "n n s", new string[] {"n", "n_s"});
		assertAnalyzesTo(a, "quick brown the", new string[] {"quick", "brown_the"});

		assertAnalyzesTo(a, "n s n", new string[] {"n_s", "s_n"});
		assertAnalyzesTo(a, "quick the brown", new string[] {"quick_the", "the_brown"});

		assertAnalyzesTo(a, "n s s", new string[] {"n_s", "s_s"});
		assertAnalyzesTo(a, "fox of the", new string[] {"fox_of", "of_the"});

		assertAnalyzesTo(a, "s n n", new string[] {"s_n", "n", "n"});
		assertAnalyzesTo(a, "the quick brown", new string[] {"the_quick", "quick", "brown"});

		assertAnalyzesTo(a, "s n s", new string[] {"s_n", "n_s"});
		assertAnalyzesTo(a, "the fox of", new string[] {"the_fox", "fox_of"});

		assertAnalyzesTo(a, "s s n", new string[] {"s_s", "s_n"});
		assertAnalyzesTo(a, "of the fox", new string[] {"of_the", "the_fox"});

		assertAnalyzesTo(a, "s s s", new string[] {"s_s", "s_s"});
		assertAnalyzesTo(a, "of the of", new string[] {"of_the", "the_of"});
	  }

	  private class AnalyzerAnonymousInnerClassHelper : Analyzer
	  {
		  private readonly CommonGramsFilterTest outerInstance;

		  public AnalyzerAnonymousInnerClassHelper(CommonGramsFilterTest outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  public override TokenStreamComponents createComponents(string field, Reader @in)
		  {
			Tokenizer tokenizer = new MockTokenizer(@in, MockTokenizer.WHITESPACE, false);
			return new TokenStreamComponents(tokenizer, new CommonGramsQueryFilter(new CommonGramsFilter(TEST_VERSION_CURRENT, tokenizer, commonWords)));
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testCommonGramsFilter() throws Exception
	  public virtual void testCommonGramsFilter()
	  {
		Analyzer a = new AnalyzerAnonymousInnerClassHelper2(this);

		// Stop words used below are "of" "the" and "s"
		// one word queries
		assertAnalyzesTo(a, "the", new string[] {"the"});
		assertAnalyzesTo(a, "foo", new string[] {"foo"});

		// two word queries
		assertAnalyzesTo(a, "brown fox", new string[] {"brown", "fox"}, new int[] {1, 1});
		assertAnalyzesTo(a, "the fox", new string[] {"the", "the_fox", "fox"}, new int[] {1, 0, 1});
		assertAnalyzesTo(a, "fox of", new string[] {"fox", "fox_of", "of"}, new int[] {1, 0, 1});
		assertAnalyzesTo(a, "of the", new string[] {"of", "of_the", "the"}, new int[] {1, 0, 1});

		// 3 word combinations s=stopword/common word n=not a stop word
		assertAnalyzesTo(a, "n n n", new string[] {"n", "n", "n"}, new int[] {1, 1, 1});
		assertAnalyzesTo(a, "quick brown fox", new string[] {"quick", "brown", "fox"}, new int[] {1, 1, 1});

		assertAnalyzesTo(a, "n n s", new string[] {"n", "n", "n_s", "s"}, new int[] {1, 1, 0, 1});
		assertAnalyzesTo(a, "quick brown the", new string[] {"quick", "brown", "brown_the", "the"}, new int[] {1, 1, 0, 1});

		assertAnalyzesTo(a, "n s n", new string[] {"n", "n_s", "s", "s_n", "n"}, new int[] {1, 0, 1, 0, 1});
		assertAnalyzesTo(a, "quick the fox", new string[] {"quick", "quick_the", "the", "the_fox", "fox"}, new int[] {1, 0, 1, 0, 1});

		assertAnalyzesTo(a, "n s s", new string[] {"n", "n_s", "s", "s_s", "s"}, new int[] {1, 0, 1, 0, 1});
		assertAnalyzesTo(a, "fox of the", new string[] {"fox", "fox_of", "of", "of_the", "the"}, new int[] {1, 0, 1, 0, 1});

		assertAnalyzesTo(a, "s n n", new string[] {"s", "s_n", "n", "n"}, new int[] {1, 0, 1, 1});
		assertAnalyzesTo(a, "the quick brown", new string[] {"the", "the_quick", "quick", "brown"}, new int[] {1, 0, 1, 1});

		assertAnalyzesTo(a, "s n s", new string[] {"s", "s_n", "n", "n_s", "s"}, new int[] {1, 0, 1, 0, 1});
		assertAnalyzesTo(a, "the fox of", new string[] {"the", "the_fox", "fox", "fox_of", "of"}, new int[] {1, 0, 1, 0, 1});

		assertAnalyzesTo(a, "s s n", new string[] {"s", "s_s", "s", "s_n", "n"}, new int[] {1, 0, 1, 0, 1});
		assertAnalyzesTo(a, "of the fox", new string[] {"of", "of_the", "the", "the_fox", "fox"}, new int[] {1, 0, 1, 0, 1});

		assertAnalyzesTo(a, "s s s", new string[] {"s", "s_s", "s", "s_s", "s"}, new int[] {1, 0, 1, 0, 1});
		assertAnalyzesTo(a, "of the of", new string[] {"of", "of_the", "the", "the_of", "of"}, new int[] {1, 0, 1, 0, 1});
	  }

	  private class AnalyzerAnonymousInnerClassHelper2 : Analyzer
	  {
		  private readonly CommonGramsFilterTest outerInstance;

		  public AnalyzerAnonymousInnerClassHelper2(CommonGramsFilterTest outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  public override TokenStreamComponents createComponents(string field, Reader @in)
		  {
			Tokenizer tokenizer = new MockTokenizer(@in, MockTokenizer.WHITESPACE, false);
			return new TokenStreamComponents(tokenizer, new CommonGramsFilter(TEST_VERSION_CURRENT, tokenizer, commonWords));
		  }
	  }

	  /// <summary>
	  /// Test that CommonGramsFilter works correctly in case-insensitive mode
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testCaseSensitive() throws Exception
	  public virtual void testCaseSensitive()
	  {
		const string input = "How The s a brown s cow d like A B thing?";
		MockTokenizer wt = new MockTokenizer(new StringReader(input), MockTokenizer.WHITESPACE, false);
		TokenFilter cgf = new CommonGramsFilter(TEST_VERSION_CURRENT, wt, commonWords);
		assertTokenStreamContents(cgf, new string[] {"How", "The", "The_s", "s", "s_a", "a", "a_brown", "brown", "brown_s", "s", "s_cow", "cow", "cow_d", "d", "d_like", "like", "A", "B", "thing?"});
	  }

	  /// <summary>
	  /// Test CommonGramsQueryFilter in the case that the last word is a stopword
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testLastWordisStopWord() throws Exception
	  public virtual void testLastWordisStopWord()
	  {
		const string input = "dog the";
		MockTokenizer wt = new MockTokenizer(new StringReader(input), MockTokenizer.WHITESPACE, false);
		CommonGramsFilter cgf = new CommonGramsFilter(TEST_VERSION_CURRENT, wt, commonWords);
		TokenFilter nsf = new CommonGramsQueryFilter(cgf);
		assertTokenStreamContents(nsf, new string[] {"dog_the"});
	  }

	  /// <summary>
	  /// Test CommonGramsQueryFilter in the case that the first word is a stopword
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testFirstWordisStopWord() throws Exception
	  public virtual void testFirstWordisStopWord()
	  {
		const string input = "the dog";
		MockTokenizer wt = new MockTokenizer(new StringReader(input), MockTokenizer.WHITESPACE, false);
		CommonGramsFilter cgf = new CommonGramsFilter(TEST_VERSION_CURRENT, wt, commonWords);
		TokenFilter nsf = new CommonGramsQueryFilter(cgf);
		assertTokenStreamContents(nsf, new string[] {"the_dog"});
	  }

	  /// <summary>
	  /// Test CommonGramsQueryFilter in the case of a single (stop)word query
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testOneWordQueryStopWord() throws Exception
	  public virtual void testOneWordQueryStopWord()
	  {
		const string input = "the";
		MockTokenizer wt = new MockTokenizer(new StringReader(input), MockTokenizer.WHITESPACE, false);
		CommonGramsFilter cgf = new CommonGramsFilter(TEST_VERSION_CURRENT, wt, commonWords);
		TokenFilter nsf = new CommonGramsQueryFilter(cgf);
		assertTokenStreamContents(nsf, new string[] {"the"});
	  }

	  /// <summary>
	  /// Test CommonGramsQueryFilter in the case of a single word query
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testOneWordQuery() throws Exception
	  public virtual void testOneWordQuery()
	  {
		const string input = "monster";
		MockTokenizer wt = new MockTokenizer(new StringReader(input), MockTokenizer.WHITESPACE, false);
		CommonGramsFilter cgf = new CommonGramsFilter(TEST_VERSION_CURRENT, wt, commonWords);
		TokenFilter nsf = new CommonGramsQueryFilter(cgf);
		assertTokenStreamContents(nsf, new string[] {"monster"});
	  }

	  /// <summary>
	  /// Test CommonGramsQueryFilter when first and last words are stopwords.
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void TestFirstAndLastStopWord() throws Exception
	  public virtual void TestFirstAndLastStopWord()
	  {
		const string input = "the of";
		MockTokenizer wt = new MockTokenizer(new StringReader(input), MockTokenizer.WHITESPACE, false);
		CommonGramsFilter cgf = new CommonGramsFilter(TEST_VERSION_CURRENT, wt, commonWords);
		TokenFilter nsf = new CommonGramsQueryFilter(cgf);
		assertTokenStreamContents(nsf, new string[] {"the_of"});
	  }

	  /// <summary>
	  /// blast some random strings through the analyzer </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandomStrings() throws Exception
	  public virtual void testRandomStrings()
	  {
		Analyzer a = new AnalyzerAnonymousInnerClassHelper3(this);

		checkRandomData(random(), a, 1000 * RANDOM_MULTIPLIER);

		Analyzer b = new AnalyzerAnonymousInnerClassHelper4(this);

		checkRandomData(random(), b, 1000 * RANDOM_MULTIPLIER);
	  }

	  private class AnalyzerAnonymousInnerClassHelper3 : Analyzer
	  {
		  private readonly CommonGramsFilterTest outerInstance;

		  public AnalyzerAnonymousInnerClassHelper3(CommonGramsFilterTest outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }


		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer t = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
			CommonGramsFilter cgf = new CommonGramsFilter(TEST_VERSION_CURRENT, t, commonWords);
			return new TokenStreamComponents(t, cgf);
		  }
	  }

	  private class AnalyzerAnonymousInnerClassHelper4 : Analyzer
	  {
		  private readonly CommonGramsFilterTest outerInstance;

		  public AnalyzerAnonymousInnerClassHelper4(CommonGramsFilterTest outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }


		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer t = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
			CommonGramsFilter cgf = new CommonGramsFilter(TEST_VERSION_CURRENT, t, commonWords);
			return new TokenStreamComponents(t, new CommonGramsQueryFilter(cgf));
		  }
	  }
	}

}