namespace org.apache.lucene.analysis.hunspell
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
	using IOUtils = org.apache.lucene.util.IOUtils;
	using AfterClass = org.junit.AfterClass;
	using BeforeClass = org.junit.BeforeClass;

	public class TestHunspellStemFilter : BaseTokenStreamTestCase
	{
	  private static Dictionary dictionary;

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @BeforeClass public static void beforeClass() throws Exception
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
	  public static void beforeClass()
	  {
		System.IO.Stream affixStream = typeof(TestStemmer).getResourceAsStream("simple.aff");
		System.IO.Stream dictStream = typeof(TestStemmer).getResourceAsStream("simple.dic");
		try
		{
		  dictionary = new Dictionary(affixStream, dictStream);
		}
		finally
		{
		  IOUtils.closeWhileHandlingException(affixStream, dictStream);
		}
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @AfterClass public static void afterClass()
	  public static void afterClass()
	  {
		dictionary = null;
	  }

	  /// <summary>
	  /// Simple test for KeywordAttribute </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testKeywordAttribute() throws java.io.IOException
	  public virtual void testKeywordAttribute()
	  {
		MockTokenizer tokenizer = new MockTokenizer(new StringReader("lucene is awesome"));
		tokenizer.EnableChecks = true;
		HunspellStemFilter filter = new HunspellStemFilter(tokenizer, dictionary);
		assertTokenStreamContents(filter, new string[]{"lucene", "lucen", "is", "awesome"}, new int[] {1, 0, 1, 1});

		// assert with keyword marker
		tokenizer = new MockTokenizer(new StringReader("lucene is awesome"));
		CharArraySet set = new CharArraySet(TEST_VERSION_CURRENT, Arrays.asList("Lucene"), true);
		filter = new HunspellStemFilter(new SetKeywordMarkerFilter(tokenizer, set), dictionary);
		assertTokenStreamContents(filter, new string[]{"lucene", "is", "awesome"}, new int[] {1, 1, 1});
	  }

	  /// <summary>
	  /// simple test for longestOnly option </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testLongestOnly() throws java.io.IOException
	  public virtual void testLongestOnly()
	  {
		MockTokenizer tokenizer = new MockTokenizer(new StringReader("lucene is awesome"));
		tokenizer.EnableChecks = true;
		HunspellStemFilter filter = new HunspellStemFilter(tokenizer, dictionary, true, true);
		assertTokenStreamContents(filter, new string[]{"lucene", "is", "awesome"}, new int[] {1, 1, 1});
	  }

	  /// <summary>
	  /// blast some random strings through the analyzer </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandomStrings() throws Exception
	  public virtual void testRandomStrings()
	  {
		Analyzer analyzer = new AnalyzerAnonymousInnerClassHelper(this);
		checkRandomData(random(), analyzer, 1000 * RANDOM_MULTIPLIER);
	  }

	  private class AnalyzerAnonymousInnerClassHelper : Analyzer
	  {
		  private readonly TestHunspellStemFilter outerInstance;

		  public AnalyzerAnonymousInnerClassHelper(TestHunspellStemFilter outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
			return new TokenStreamComponents(tokenizer, new HunspellStemFilter(tokenizer, dictionary));
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testEmptyTerm() throws java.io.IOException
	  public virtual void testEmptyTerm()
	  {
		Analyzer a = new AnalyzerAnonymousInnerClassHelper2(this);
		checkOneTerm(a, "", "");
	  }

	  private class AnalyzerAnonymousInnerClassHelper2 : Analyzer
	  {
		  private readonly TestHunspellStemFilter outerInstance;

		  public AnalyzerAnonymousInnerClassHelper2(TestHunspellStemFilter outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new KeywordTokenizer(reader);
			return new TokenStreamComponents(tokenizer, new HunspellStemFilter(tokenizer, dictionary));
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testIgnoreCaseNoSideEffects() throws Exception
	  public virtual void testIgnoreCaseNoSideEffects()
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.analysis.hunspell.Dictionary d;
		Dictionary d;
		System.IO.Stream affixStream = typeof(TestStemmer).getResourceAsStream("simple.aff");
		System.IO.Stream dictStream = typeof(TestStemmer).getResourceAsStream("simple.dic");
		try
		{
		  d = new Dictionary(affixStream, Collections.singletonList(dictStream), true);
		}
		finally
		{
		  IOUtils.closeWhileHandlingException(affixStream, dictStream);
		}
		Analyzer a = new AnalyzerAnonymousInnerClassHelper3(this, d);
		checkOneTerm(a, "NoChAnGy", "NoChAnGy");
	  }

	  private class AnalyzerAnonymousInnerClassHelper3 : Analyzer
	  {
		  private readonly TestHunspellStemFilter outerInstance;

		  private Dictionary d;

		  public AnalyzerAnonymousInnerClassHelper3(TestHunspellStemFilter outerInstance, Dictionary d)
		  {
			  this.outerInstance = outerInstance;
			  this.d = d;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new KeywordTokenizer(reader);
			return new TokenStreamComponents(tokenizer, new HunspellStemFilter(tokenizer, d));
		  }
	  }
	}

}