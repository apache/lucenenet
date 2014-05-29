using System;

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

	using Field = Lucene.Net.Document.Field;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

	using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
	using MockTokenFilter = Lucene.Net.Analysis.MockTokenFilter;
	using MockTokenizer = Lucene.Net.Analysis.MockTokenizer;
	using Document = Lucene.Net.Document.Document;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using Term = Lucene.Net.Index.Term;
	using Lucene.Net.Search;
	using DefaultSimilarity = Lucene.Net.Search.Similarities.DefaultSimilarity;
	using Directory = Lucene.Net.Store.Directory;

	/// <summary>
	///*****************************************************************************
	/// Tests the span query bug in Lucene. It demonstrates that SpanTermQuerys don't
	/// work correctly in a BooleanQuery.
	/// 
	/// </summary>
	public class TestSpansAdvanced : LuceneTestCase
	{

	  // location to the index
	  protected internal Directory MDirectory;
	  protected internal IndexReader Reader;
	  protected internal IndexSearcher Searcher;

	  // field names in the index
	  private const string FIELD_ID = "ID";
	  protected internal const string FIELD_TEXT = "TEXT";

	  /// <summary>
	  /// Initializes the tests by adding 4 identical documents to the index.
	  /// </summary>
	  public override void SetUp()
	  {
		base.setUp();
		// create test index
		MDirectory = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), MDirectory, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random(), MockTokenizer.SIMPLE, true, MockTokenFilter.ENGLISH_STOPSET)).setMergePolicy(newLogMergePolicy()).setSimilarity(new DefaultSimilarity()));
		AddDocument(writer, "1", "I think it should work.");
		AddDocument(writer, "2", "I think it should work.");
		AddDocument(writer, "3", "I think it should work.");
		AddDocument(writer, "4", "I think it should work.");
		Reader = writer.Reader;
		writer.close();
		Searcher = newSearcher(Reader);
		Searcher.Similarity = new DefaultSimilarity();
	  }

	  public override void TearDown()
	  {
		Reader.close();
		MDirectory.close();
		MDirectory = null;
		base.tearDown();
	  }

	  /// <summary>
	  /// Adds the document to the index.
	  /// </summary>
	  /// <param name="writer"> the Lucene index writer </param>
	  /// <param name="id"> the unique id of the document </param>
	  /// <param name="text"> the text of the document </param>
	  protected internal virtual void AddDocument(RandomIndexWriter writer, string id, string text)
	  {

		Document document = new Document();
		document.add(newStringField(FIELD_ID, id, Field.Store.YES));
		document.add(newTextField(FIELD_TEXT, text, Field.Store.YES));
		writer.addDocument(document);
	  }

	  /// <summary>
	  /// Tests two span queries.
	  /// </summary>
	  public virtual void TestBooleanQueryWithSpanQueries()
	  {

		DoTestBooleanQueryWithSpanQueries(Searcher, 0.3884282f);
	  }

	  /// <summary>
	  /// Tests two span queries.
	  /// </summary>
	  protected internal virtual void DoTestBooleanQueryWithSpanQueries(IndexSearcher s, float expectedScore)
	  {

		Query spanQuery = new SpanTermQuery(new Term(FIELD_TEXT, "work"));
		BooleanQuery query = new BooleanQuery();
		query.add(spanQuery, BooleanClause.Occur_e.MUST);
		query.add(spanQuery, BooleanClause.Occur_e.MUST);
		string[] expectedIds = new string[] {"1", "2", "3", "4"};
		float[] expectedScores = new float[] {expectedScore, expectedScore, expectedScore, expectedScore};
		AssertHits(s, query, "two span queries", expectedIds, expectedScores);
	  }

	  /// <summary>
	  /// Checks to see if the hits are what we expected.
	  /// </summary>
	  /// <param name="query"> the query to execute </param>
	  /// <param name="description"> the description of the search </param>
	  /// <param name="expectedIds"> the expected document ids of the hits </param>
	  /// <param name="expectedScores"> the expected scores of the hits </param>
	  protected internal static void AssertHits(IndexSearcher s, Query query, string description, string[] expectedIds, float[] expectedScores)
	  {
		QueryUtils.check(random(), query, s);

		const float tolerance = 1e-5f;

		// Hits hits = searcher.search(query);
		// hits normalizes and throws things off if one score is greater than 1.0
		TopDocs topdocs = s.search(query, null, 10000);

		/// <summary>
		///***
		/// // display the hits System.out.println(hits.length() +
		/// " hits for search: \"" + description + '\"'); for (int i = 0; i <
		/// hits.length(); i++) { System.out.println("  " + FIELD_ID + ':' +
		/// hits.doc(i).get(FIELD_ID) + " (score:" + hits.score(i) + ')'); }
		/// ****
		/// </summary>

		// did we get the hits we expected
		Assert.AreEqual(expectedIds.Length, topdocs.totalHits);
		for (int i = 0; i < topdocs.totalHits; i++)
		{
		  // System.out.println(i + " exp: " + expectedIds[i]);
		  // System.out.println(i + " field: " + hits.doc(i).get(FIELD_ID));

		  int id = topdocs.scoreDocs[i].doc;
		  float score = topdocs.scoreDocs[i].score;
		  Document doc = s.doc(id);
		  Assert.AreEqual(expectedIds[i], doc.get(FIELD_ID));
		  bool scoreEq = Math.Abs(expectedScores[i] - score) < tolerance;
		  if (!scoreEq)
		  {
			Console.WriteLine(i + " warning, expected score: " + expectedScores[i] + ", actual " + score);
			Console.WriteLine(s.explain(query, id));
		  }
		  Assert.AreEqual(expectedScores[i], score, tolerance);
		  Assert.AreEqual(s.explain(query, id).Value, score, tolerance);
		}
	  }

	}
}