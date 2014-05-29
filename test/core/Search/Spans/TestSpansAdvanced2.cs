namespace Lucene.Net.Search.Spans
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

	using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
	using MockTokenFilter = Lucene.Net.Analysis.MockTokenFilter;
	using MockTokenizer = Lucene.Net.Analysis.MockTokenizer;
	using DirectoryReader = Lucene.Net.Index.DirectoryReader;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using Term = Lucene.Net.Index.Term;
	using OpenMode = Lucene.Net.Index.IndexWriterConfig.OpenMode;
	using Lucene.Net.Search;
	using DefaultSimilarity = Lucene.Net.Search.Similarities.DefaultSimilarity;

	/// <summary>
	///*****************************************************************************
	/// Some expanded tests to make sure my patch doesn't break other SpanTermQuery
	/// functionality.
	/// 
	/// </summary>
	public class TestSpansAdvanced2 : TestSpansAdvanced
	{
	  internal IndexSearcher Searcher2;
	  internal IndexReader Reader2;

	  /// <summary>
	  /// Initializes the tests by adding documents to the index.
	  /// </summary>
	  public override void SetUp()
	  {
		base.SetUp();

		// create test index
		RandomIndexWriter writer = new RandomIndexWriter(random(), MDirectory, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random(), MockTokenizer.SIMPLE, true, MockTokenFilter.ENGLISH_STOPSET)).setOpenMode(OpenMode.APPEND).setMergePolicy(newLogMergePolicy()).setSimilarity(new DefaultSimilarity()));
		AddDocument(writer, "A", "Should we, could we, would we?");
		AddDocument(writer, "B", "It should.  Should it?");
		AddDocument(writer, "C", "It shouldn't.");
		AddDocument(writer, "D", "Should we, should we, should we.");
		Reader2 = writer.Reader;
		writer.close();

		// re-open the searcher since we added more docs
		Searcher2 = newSearcher(Reader2);
		Searcher2.Similarity = new DefaultSimilarity();
	  }

	  public override void TearDown()
	  {
		Reader2.close();
		base.TearDown();
	  }

	  /// <summary>
	  /// Verifies that the index has the correct number of documents.
	  /// </summary>
	  public virtual void TestVerifyIndex()
	  {
		IndexReader reader = DirectoryReader.open(MDirectory);
		Assert.AreEqual(8, reader.numDocs());
		reader.close();
	  }

	  /// <summary>
	  /// Tests a single span query that matches multiple documents.
	  /// </summary>
	  public virtual void TestSingleSpanQuery()
	  {

		Query spanQuery = new SpanTermQuery(new Term(FIELD_TEXT, "should"));
		string[] expectedIds = new string[] {"B", "D", "1", "2", "3", "4", "A"};
		float[] expectedScores = new float[] {0.625f, 0.45927936f, 0.35355338f, 0.35355338f, 0.35355338f, 0.35355338f, 0.26516503f};
		AssertHits(Searcher2, spanQuery, "single span query", expectedIds, expectedScores);
	  }

	  /// <summary>
	  /// Tests a single span query that matches multiple documents.
	  /// </summary>
	  public virtual void TestMultipleDifferentSpanQueries()
	  {

		Query spanQuery1 = new SpanTermQuery(new Term(FIELD_TEXT, "should"));
		Query spanQuery2 = new SpanTermQuery(new Term(FIELD_TEXT, "we"));
		BooleanQuery query = new BooleanQuery();
		query.add(spanQuery1, BooleanClause.Occur_e.MUST);
		query.add(spanQuery2, BooleanClause.Occur_e.MUST);
		string[] expectedIds = new string[] {"D", "A"};
		// these values were pre LUCENE-413
		// final float[] expectedScores = new float[] { 0.93163157f, 0.20698164f };
		float[] expectedScores = new float[] {1.0191123f, 0.93163157f};
		AssertHits(Searcher2, query, "multiple different span queries", expectedIds, expectedScores);
	  }

	  /// <summary>
	  /// Tests two span queries.
	  /// </summary>
	  public override void TestBooleanQueryWithSpanQueries()
	  {

		DoTestBooleanQueryWithSpanQueries(Searcher2, 0.73500174f);
	  }
	}

}