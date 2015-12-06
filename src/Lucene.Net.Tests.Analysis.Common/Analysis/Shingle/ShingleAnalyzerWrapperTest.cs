namespace org.apache.lucene.analysis.shingle
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

	using StopFilter = org.apache.lucene.analysis.core.StopFilter;
	using CharTermAttribute = org.apache.lucene.analysis.tokenattributes.CharTermAttribute;
	using PositionIncrementAttribute = org.apache.lucene.analysis.tokenattributes.PositionIncrementAttribute;
	using CharArraySet = org.apache.lucene.analysis.util.CharArraySet;
	using Document = org.apache.lucene.document.Document;
	using Field = org.apache.lucene.document.Field;
	using TextField = org.apache.lucene.document.TextField;
	using DirectoryReader = org.apache.lucene.index.DirectoryReader;
	using IndexReader = org.apache.lucene.index.IndexReader;
	using IndexWriter = org.apache.lucene.index.IndexWriter;
	using IndexWriterConfig = org.apache.lucene.index.IndexWriterConfig;
	using Term = org.apache.lucene.index.Term;
	using org.apache.lucene.search;
	using Directory = org.apache.lucene.store.Directory;
	using IOUtils = org.apache.lucene.util.IOUtils;

	/// <summary>
	/// A test class for ShingleAnalyzerWrapper as regards queries and scoring.
	/// </summary>
	public class ShingleAnalyzerWrapperTest : BaseTokenStreamTestCase
	{
	  private Analyzer analyzer;
	  private IndexSearcher searcher;
	  private IndexReader reader;
	  private Directory directory;

	  /// <summary>
	  /// Set up a new index in RAM with three test phrases and the supplied Analyzer.
	  /// </summary>
	  /// <exception cref="Exception"> if an error occurs with index writer or searcher </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void setUp() throws Exception
	  public override void setUp()
	  {
		base.setUp();
		analyzer = new ShingleAnalyzerWrapper(new MockAnalyzer(random(), MockTokenizer.WHITESPACE, false), 2);
		directory = newDirectory();
		IndexWriter writer = new IndexWriter(directory, new IndexWriterConfig(TEST_VERSION_CURRENT, analyzer));

		Document doc;
		doc = new Document();
		doc.add(new TextField("content", "please divide this sentence into shingles", Field.Store.YES));
		writer.addDocument(doc);

		doc = new Document();
		doc.add(new TextField("content", "just another test sentence", Field.Store.YES));
		writer.addDocument(doc);

		doc = new Document();
		doc.add(new TextField("content", "a sentence which contains no test", Field.Store.YES));
		writer.addDocument(doc);

		writer.close();

		reader = DirectoryReader.open(directory);
		searcher = newSearcher(reader);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void tearDown() throws Exception
	  public override void tearDown()
	  {
		reader.close();
		directory.close();
		base.tearDown();
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: protected void compareRanks(ScoreDoc[] hits, int[] ranks) throws Exception
	  protected internal virtual void compareRanks(ScoreDoc[] hits, int[] ranks)
	  {
		assertEquals(ranks.Length, hits.Length);
		for (int i = 0; i < ranks.Length; i++)
		{
		  assertEquals(ranks[i], hits[i].doc);
		}
	  }

	  /*
	   * This shows how to construct a phrase query containing shingles.
	   */
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testShingleAnalyzerWrapperPhraseQuery() throws Exception
	  public virtual void testShingleAnalyzerWrapperPhraseQuery()
	  {
		PhraseQuery q = new PhraseQuery();

		TokenStream ts = analyzer.tokenStream("content", "this sentence");
		try
		{
		  int j = -1;

		  PositionIncrementAttribute posIncrAtt = ts.addAttribute(typeof(PositionIncrementAttribute));
		  CharTermAttribute termAtt = ts.addAttribute(typeof(CharTermAttribute));

		  ts.reset();
		  while (ts.incrementToken())
		  {
			j += posIncrAtt.PositionIncrement;
			string termText = termAtt.ToString();
			q.add(new Term("content", termText), j);
		  }
		  ts.end();
		}
		finally
		{
		  IOUtils.closeWhileHandlingException(ts);
		}

		ScoreDoc[] hits = searcher.search(q, null, 1000).scoreDocs;
		int[] ranks = new int[] {0};
		compareRanks(hits, ranks);
	  }

	  /*
	   * How to construct a boolean query with shingles. A query like this will
	   * implicitly score those documents higher that contain the words in the query
	   * in the right order and adjacent to each other.
	   */
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testShingleAnalyzerWrapperBooleanQuery() throws Exception
	  public virtual void testShingleAnalyzerWrapperBooleanQuery()
	  {
		BooleanQuery q = new BooleanQuery();

		TokenStream ts = analyzer.tokenStream("content", "test sentence");
		try
		{
		  CharTermAttribute termAtt = ts.addAttribute(typeof(CharTermAttribute));

		  ts.reset();
		  while (ts.incrementToken())
		  {
			string termText = termAtt.ToString();
			q.add(new TermQuery(new Term("content", termText)), BooleanClause.Occur.SHOULD);
		  }
		  ts.end();
		}
		finally
		{
		  IOUtils.closeWhileHandlingException(ts);
		}

		ScoreDoc[] hits = searcher.search(q, null, 1000).scoreDocs;
		int[] ranks = new int[] {1, 2, 0};
		compareRanks(hits, ranks);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testReusableTokenStream() throws Exception
	  public virtual void testReusableTokenStream()
	  {
		Analyzer a = new ShingleAnalyzerWrapper(new MockAnalyzer(random(), MockTokenizer.WHITESPACE, false), 2);
		assertAnalyzesTo(a, "please divide into shingles", new string[] {"please", "please divide", "divide", "divide into", "into", "into shingles", "shingles"}, new int[] {0, 0, 7, 7, 14, 14, 19}, new int[] {6, 13, 13, 18, 18, 27, 27}, new int[] {1, 0, 1, 0, 1, 0, 1});
		assertAnalyzesTo(a, "divide me up again", new string[] {"divide", "divide me", "me", "me up", "up", "up again", "again"}, new int[] {0, 0, 7, 7, 10, 10, 13}, new int[] {6, 9, 9, 12, 12, 18, 18}, new int[] {1, 0, 1, 0, 1, 0, 1});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testNonDefaultMinShingleSize() throws Exception
	  public virtual void testNonDefaultMinShingleSize()
	  {
		ShingleAnalyzerWrapper analyzer = new ShingleAnalyzerWrapper(new MockAnalyzer(random(), MockTokenizer.WHITESPACE, false), 3, 4);
		assertAnalyzesTo(analyzer, "please divide this sentence into shingles", new string[] {"please", "please divide this", "please divide this sentence", "divide", "divide this sentence", "divide this sentence into", "this", "this sentence into", "this sentence into shingles", "sentence", "sentence into shingles", "into", "shingles"}, new int[] {0, 0, 0, 7, 7, 7, 14, 14, 14, 19, 19, 28, 33}, new int[] {6, 18, 27, 13, 27, 32, 18, 32, 41, 27, 41, 32, 41}, new int[] {1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 1, 1});

		analyzer = new ShingleAnalyzerWrapper(new MockAnalyzer(random(), MockTokenizer.WHITESPACE, false), 3, 4, ShingleFilter.DEFAULT_TOKEN_SEPARATOR, false, false, ShingleFilter.DEFAULT_FILLER_TOKEN);
		assertAnalyzesTo(analyzer, "please divide this sentence into shingles", new string[] {"please divide this", "please divide this sentence", "divide this sentence", "divide this sentence into", "this sentence into", "this sentence into shingles", "sentence into shingles"}, new int[] {0, 0, 7, 7, 14, 14, 19}, new int[] {18, 27, 27, 32, 32, 41, 41}, new int[] {1, 0, 1, 0, 1, 0, 1});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testNonDefaultMinAndSameMaxShingleSize() throws Exception
	  public virtual void testNonDefaultMinAndSameMaxShingleSize()
	  {
		ShingleAnalyzerWrapper analyzer = new ShingleAnalyzerWrapper(new MockAnalyzer(random(), MockTokenizer.WHITESPACE, false), 3, 3);
		assertAnalyzesTo(analyzer, "please divide this sentence into shingles", new string[] {"please", "please divide this", "divide", "divide this sentence", "this", "this sentence into", "sentence", "sentence into shingles", "into", "shingles"}, new int[] {0, 0, 7, 7, 14, 14, 19, 19, 28, 33}, new int[] {6, 18, 13, 27, 18, 32, 27, 41, 32, 41}, new int[] {1, 0, 1, 0, 1, 0, 1, 0, 1, 1});

		analyzer = new ShingleAnalyzerWrapper(new MockAnalyzer(random(), MockTokenizer.WHITESPACE, false), 3, 3, ShingleFilter.DEFAULT_TOKEN_SEPARATOR, false, false, ShingleFilter.DEFAULT_FILLER_TOKEN);
		assertAnalyzesTo(analyzer, "please divide this sentence into shingles", new string[] {"please divide this", "divide this sentence", "this sentence into", "sentence into shingles"}, new int[] {0, 7, 14, 19}, new int[] {18, 27, 32, 41}, new int[] {1, 1, 1, 1});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testNoTokenSeparator() throws Exception
	  public virtual void testNoTokenSeparator()
	  {
		ShingleAnalyzerWrapper analyzer = new ShingleAnalyzerWrapper(new MockAnalyzer(random(), MockTokenizer.WHITESPACE, false), ShingleFilter.DEFAULT_MIN_SHINGLE_SIZE, ShingleFilter.DEFAULT_MAX_SHINGLE_SIZE, "", true, false, ShingleFilter.DEFAULT_FILLER_TOKEN);
		assertAnalyzesTo(analyzer, "please divide into shingles", new string[] {"please", "pleasedivide", "divide", "divideinto", "into", "intoshingles", "shingles"}, new int[] {0, 0, 7, 7, 14, 14, 19}, new int[] {6, 13, 13, 18, 18, 27, 27}, new int[] {1, 0, 1, 0, 1, 0, 1});

		analyzer = new ShingleAnalyzerWrapper(new MockAnalyzer(random(), MockTokenizer.WHITESPACE, false), ShingleFilter.DEFAULT_MIN_SHINGLE_SIZE, ShingleFilter.DEFAULT_MAX_SHINGLE_SIZE, "", false, false, ShingleFilter.DEFAULT_FILLER_TOKEN);
		assertAnalyzesTo(analyzer, "please divide into shingles", new string[] {"pleasedivide", "divideinto", "intoshingles"}, new int[] {0, 7, 14}, new int[] {13, 18, 27}, new int[] {1, 1, 1});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testNullTokenSeparator() throws Exception
	  public virtual void testNullTokenSeparator()
	  {
		ShingleAnalyzerWrapper analyzer = new ShingleAnalyzerWrapper(new MockAnalyzer(random(), MockTokenizer.WHITESPACE, false), ShingleFilter.DEFAULT_MIN_SHINGLE_SIZE, ShingleFilter.DEFAULT_MAX_SHINGLE_SIZE, null, true, false, ShingleFilter.DEFAULT_FILLER_TOKEN);
		assertAnalyzesTo(analyzer, "please divide into shingles", new string[] {"please", "pleasedivide", "divide", "divideinto", "into", "intoshingles", "shingles"}, new int[] {0, 0, 7, 7, 14, 14, 19}, new int[] {6, 13, 13, 18, 18, 27, 27}, new int[] {1, 0, 1, 0, 1, 0, 1});

		analyzer = new ShingleAnalyzerWrapper(new MockAnalyzer(random(), MockTokenizer.WHITESPACE, false), ShingleFilter.DEFAULT_MIN_SHINGLE_SIZE, ShingleFilter.DEFAULT_MAX_SHINGLE_SIZE, "", false, false, ShingleFilter.DEFAULT_FILLER_TOKEN);
		assertAnalyzesTo(analyzer, "please divide into shingles", new string[] {"pleasedivide", "divideinto", "intoshingles"}, new int[] {0, 7, 14}, new int[] {13, 18, 27}, new int[] {1, 1, 1});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testAltTokenSeparator() throws Exception
	  public virtual void testAltTokenSeparator()
	  {
		ShingleAnalyzerWrapper analyzer = new ShingleAnalyzerWrapper(new MockAnalyzer(random(), MockTokenizer.WHITESPACE, false), ShingleFilter.DEFAULT_MIN_SHINGLE_SIZE, ShingleFilter.DEFAULT_MAX_SHINGLE_SIZE, "<SEP>", true, false, ShingleFilter.DEFAULT_FILLER_TOKEN);
		assertAnalyzesTo(analyzer, "please divide into shingles", new string[] {"please", "please<SEP>divide", "divide", "divide<SEP>into", "into", "into<SEP>shingles", "shingles"}, new int[] {0, 0, 7, 7, 14, 14, 19}, new int[] {6, 13, 13, 18, 18, 27, 27}, new int[] {1, 0, 1, 0, 1, 0, 1});

		analyzer = new ShingleAnalyzerWrapper(new MockAnalyzer(random(), MockTokenizer.WHITESPACE, false), ShingleFilter.DEFAULT_MIN_SHINGLE_SIZE, ShingleFilter.DEFAULT_MAX_SHINGLE_SIZE, "<SEP>", false, false, ShingleFilter.DEFAULT_FILLER_TOKEN);
		assertAnalyzesTo(analyzer, "please divide into shingles", new string[] {"please<SEP>divide", "divide<SEP>into", "into<SEP>shingles"}, new int[] {0, 7, 14}, new int[] {13, 18, 27}, new int[] {1, 1, 1});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testAltFillerToken() throws Exception
	  public virtual void testAltFillerToken()
	  {
		Analyzer @delegate = new AnalyzerAnonymousInnerClassHelper(this);

		ShingleAnalyzerWrapper analyzer = new ShingleAnalyzerWrapper(@delegate, ShingleFilter.DEFAULT_MIN_SHINGLE_SIZE, ShingleFilter.DEFAULT_MAX_SHINGLE_SIZE, ShingleFilter.DEFAULT_TOKEN_SEPARATOR, true, false, "--");
		assertAnalyzesTo(analyzer, "please divide into shingles", new string[] {"please", "please divide", "divide", "divide --", "-- shingles", "shingles"}, new int[] {0, 0, 7, 7, 19, 19}, new int[] {6, 13, 13, 19, 27, 27}, new int[] {1, 0, 1, 0, 1, 1});

		analyzer = new ShingleAnalyzerWrapper(@delegate, ShingleFilter.DEFAULT_MIN_SHINGLE_SIZE, ShingleFilter.DEFAULT_MAX_SHINGLE_SIZE, ShingleFilter.DEFAULT_TOKEN_SEPARATOR, false, false, null);
		assertAnalyzesTo(analyzer, "please divide into shingles", new string[] {"please divide", "divide ", " shingles"}, new int[] {0, 7, 19}, new int[] {13, 19, 27}, new int[] {1, 1, 1});

		analyzer = new ShingleAnalyzerWrapper(@delegate, ShingleFilter.DEFAULT_MIN_SHINGLE_SIZE, ShingleFilter.DEFAULT_MAX_SHINGLE_SIZE, ShingleFilter.DEFAULT_TOKEN_SEPARATOR, false, false, "");
		assertAnalyzesTo(analyzer, "please divide into shingles", new string[] {"please divide", "divide ", " shingles"}, new int[] {0, 7, 19}, new int[] {13, 19, 27}, new int[] {1, 1, 1});
	  }

	  private class AnalyzerAnonymousInnerClassHelper : Analyzer
	  {
		  private readonly ShingleAnalyzerWrapperTest outerInstance;

		  public AnalyzerAnonymousInnerClassHelper(ShingleAnalyzerWrapperTest outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			CharArraySet stopSet = StopFilter.makeStopSet(TEST_VERSION_CURRENT, "into");
			Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
			TokenFilter filter = new StopFilter(TEST_VERSION_CURRENT, tokenizer, stopSet);
			return new TokenStreamComponents(tokenizer, filter);
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testOutputUnigramsIfNoShinglesSingleToken() throws Exception
	  public virtual void testOutputUnigramsIfNoShinglesSingleToken()
	  {
		ShingleAnalyzerWrapper analyzer = new ShingleAnalyzerWrapper(new MockAnalyzer(random(), MockTokenizer.WHITESPACE, false), ShingleFilter.DEFAULT_MIN_SHINGLE_SIZE, ShingleFilter.DEFAULT_MAX_SHINGLE_SIZE, "", false, true, ShingleFilter.DEFAULT_FILLER_TOKEN);
		assertAnalyzesTo(analyzer, "please", new string[] {"please"}, new int[] {0}, new int[] {6}, new int[] {1});
	  }
	}

}