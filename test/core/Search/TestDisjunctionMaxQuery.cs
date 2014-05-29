using System;

namespace Lucene.Net.Search
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
	using Analyzer = Lucene.Net.Analysis.Analyzer;
	using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
	using Document = Lucene.Net.Document.Document;
	using FieldType = Lucene.Net.Document.FieldType;
	using TextField = Lucene.Net.Document.TextField;
	using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
	using DirectoryReader = Lucene.Net.Index.DirectoryReader;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using IndexWriter = Lucene.Net.Index.IndexWriter;
	using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
	using SlowCompositeReaderWrapper = Lucene.Net.Index.SlowCompositeReaderWrapper;
	using FieldInvertState = Lucene.Net.Index.FieldInvertState;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using Term = Lucene.Net.Index.Term;
	using DefaultSimilarity = Lucene.Net.Search.Similarities.DefaultSimilarity;
	using Similarity = Lucene.Net.Search.Similarities.Similarity;
	using SpanQuery = Lucene.Net.Search.Spans.SpanQuery;
	using SpanTermQuery = Lucene.Net.Search.Spans.SpanTermQuery;
	using Directory = Lucene.Net.Store.Directory;


	/// <summary>
	/// Test of the DisjunctionMaxQuery.
	/// 
	/// </summary>
	public class TestDisjunctionMaxQuery : LuceneTestCase
	{

	  /// <summary>
	  /// threshold for comparing floats </summary>
	  public static readonly float SCORE_COMP_THRESH = 0.0000f;

	  /// <summary>
	  /// Similarity to eliminate tf, idf and lengthNorm effects to isolate test
	  /// case.
	  /// 
	  /// <p>
	  /// same as TestRankingSimilarity in TestRanking.zip from
	  /// http://issues.apache.org/jira/browse/LUCENE-323
	  /// </p>
	  /// </summary>
	  private class TestSimilarity : DefaultSimilarity
	  {

		public TestSimilarity()
		{
		}

		public override float Tf(float freq)
		{
		  if (freq > 0.0f)
		  {
			  return 1.0f;
		  }
		  else
		  {
			  return 0.0f;
		  }
		}

		public override float LengthNorm(FieldInvertState state)
		{
		  // Disable length norm
		  return state.Boost;
		}

		public override float Idf(long docFreq, long numDocs)
		{
		  return 1.0f;
		}
	  }

	  public Similarity Sim = new TestSimilarity();
	  public Directory Index;
	  public IndexReader r;
	  public IndexSearcher s;

	  private static readonly FieldType NonAnalyzedType = new FieldType(TextField.TYPE_STORED);
	  static TestDisjunctionMaxQuery()
	  {
		NonAnalyzedType.Tokenized = false;
	  }

	  public override void SetUp()
	  {
		base.setUp();

		Index = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), Index, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setSimilarity(Sim).setMergePolicy(newLogMergePolicy()));

		// hed is the most important field, dek is secondary

		// d1 is an "ok" match for: albino elephant
		{
		  Document d1 = new Document();
		  d1.add(newField("id", "d1", NonAnalyzedType)); // Field.Keyword("id",
																				   // "d1"));
		  d1.add(newTextField("hed", "elephant", Field.Store.YES)); // Field.Text("hed", "elephant"));
		  d1.add(newTextField("dek", "elephant", Field.Store.YES)); // Field.Text("dek", "elephant"));
		  writer.addDocument(d1);
		}

		// d2 is a "good" match for: albino elephant
		{
		  Document d2 = new Document();
		  d2.add(newField("id", "d2", NonAnalyzedType)); // Field.Keyword("id",
																				   // "d2"));
		  d2.add(newTextField("hed", "elephant", Field.Store.YES)); // Field.Text("hed", "elephant"));
		  d2.add(newTextField("dek", "albino", Field.Store.YES)); // Field.Text("dek",
																					// "albino"));
		  d2.add(newTextField("dek", "elephant", Field.Store.YES)); // Field.Text("dek", "elephant"));
		  writer.addDocument(d2);
		}

		// d3 is a "better" match for: albino elephant
		{
		  Document d3 = new Document();
		  d3.add(newField("id", "d3", NonAnalyzedType)); // Field.Keyword("id",
																				   // "d3"));
		  d3.add(newTextField("hed", "albino", Field.Store.YES)); // Field.Text("hed",
																					// "albino"));
		  d3.add(newTextField("hed", "elephant", Field.Store.YES)); // Field.Text("hed", "elephant"));
		  writer.addDocument(d3);
		}

		// d4 is the "best" match for: albino elephant
		{
		  Document d4 = new Document();
		  d4.add(newField("id", "d4", NonAnalyzedType)); // Field.Keyword("id",
																				   // "d4"));
		  d4.add(newTextField("hed", "albino", Field.Store.YES)); // Field.Text("hed",
																					// "albino"));
		  d4.add(newField("hed", "elephant", NonAnalyzedType)); // Field.Text("hed", "elephant"));
		  d4.add(newTextField("dek", "albino", Field.Store.YES)); // Field.Text("dek",
																					// "albino"));
		  writer.addDocument(d4);
		}

		r = SlowCompositeReaderWrapper.wrap(writer.Reader);
		writer.close();
		s = newSearcher(r);
		s.Similarity = Sim;
	  }

	  public override void TearDown()
	  {
		r.close();
		Index.close();
		base.tearDown();
	  }

	  public virtual void TestSkipToFirsttimeMiss()
	  {
		DisjunctionMaxQuery dq = new DisjunctionMaxQuery(0.0f);
		dq.add(Tq("id", "d1"));
		dq.add(Tq("dek", "DOES_NOT_EXIST"));

		QueryUtils.check(random(), dq, s);
		Assert.IsTrue(s.TopReaderContext is AtomicReaderContext);
		Weight dw = s.createNormalizedWeight(dq);
		AtomicReaderContext context = (AtomicReaderContext)s.TopReaderContext;
		Scorer ds = dw.scorer(context, context.reader().LiveDocs);
		bool skipOk = ds.advance(3) != DocIdSetIterator.NO_MORE_DOCS;
		if (skipOk)
		{
		  Assert.Fail("firsttime skipTo found a match? ... " + r.document(ds.docID()).get("id"));
		}
	  }

	  public virtual void TestSkipToFirsttimeHit()
	  {
		DisjunctionMaxQuery dq = new DisjunctionMaxQuery(0.0f);
		dq.add(Tq("dek", "albino"));
		dq.add(Tq("dek", "DOES_NOT_EXIST"));
		Assert.IsTrue(s.TopReaderContext is AtomicReaderContext);
		QueryUtils.check(random(), dq, s);
		Weight dw = s.createNormalizedWeight(dq);
		AtomicReaderContext context = (AtomicReaderContext)s.TopReaderContext;
		Scorer ds = dw.scorer(context, context.reader().LiveDocs);
		Assert.IsTrue("firsttime skipTo found no match", ds.advance(3) != DocIdSetIterator.NO_MORE_DOCS);
		Assert.AreEqual("found wrong docid", "d4", r.document(ds.docID()).get("id"));
	  }

	  public virtual void TestSimpleEqualScores1()
	  {

		DisjunctionMaxQuery q = new DisjunctionMaxQuery(0.0f);
		q.add(Tq("hed", "albino"));
		q.add(Tq("hed", "elephant"));
		QueryUtils.check(random(), q, s);

		ScoreDoc[] h = s.search(q, null, 1000).scoreDocs;

		try
		{
		  Assert.AreEqual("all docs should match " + q.ToString(), 4, h.Length);

		  float score = h[0].score;
		  for (int i = 1; i < h.Length; i++)
		  {
			Assert.AreEqual("score #" + i + " is not the same", score, h[i].score, SCORE_COMP_THRESH);
		  }
		}
		catch (Exception e)
		{
		  PrintHits("testSimpleEqualScores1", h, s);
		  throw e;
		}

	  }

	  public virtual void TestSimpleEqualScores2()
	  {

		DisjunctionMaxQuery q = new DisjunctionMaxQuery(0.0f);
		q.add(Tq("dek", "albino"));
		q.add(Tq("dek", "elephant"));
		QueryUtils.check(random(), q, s);

		ScoreDoc[] h = s.search(q, null, 1000).scoreDocs;

		try
		{
		  Assert.AreEqual("3 docs should match " + q.ToString(), 3, h.Length);
		  float score = h[0].score;
		  for (int i = 1; i < h.Length; i++)
		  {
			Assert.AreEqual("score #" + i + " is not the same", score, h[i].score, SCORE_COMP_THRESH);
		  }
		}
		catch (Exception e)
		{
		  PrintHits("testSimpleEqualScores2", h, s);
		  throw e;
		}

	  }

	  public virtual void TestSimpleEqualScores3()
	  {

		DisjunctionMaxQuery q = new DisjunctionMaxQuery(0.0f);
		q.add(Tq("hed", "albino"));
		q.add(Tq("hed", "elephant"));
		q.add(Tq("dek", "albino"));
		q.add(Tq("dek", "elephant"));
		QueryUtils.check(random(), q, s);

		ScoreDoc[] h = s.search(q, null, 1000).scoreDocs;

		try
		{
		  Assert.AreEqual("all docs should match " + q.ToString(), 4, h.Length);
		  float score = h[0].score;
		  for (int i = 1; i < h.Length; i++)
		  {
			Assert.AreEqual("score #" + i + " is not the same", score, h[i].score, SCORE_COMP_THRESH);
		  }
		}
		catch (Exception e)
		{
		  PrintHits("testSimpleEqualScores3", h, s);
		  throw e;
		}

	  }

	  public virtual void TestSimpleTiebreaker()
	  {

		DisjunctionMaxQuery q = new DisjunctionMaxQuery(0.01f);
		q.add(Tq("dek", "albino"));
		q.add(Tq("dek", "elephant"));
		QueryUtils.check(random(), q, s);

		ScoreDoc[] h = s.search(q, null, 1000).scoreDocs;

		try
		{
		  Assert.AreEqual("3 docs should match " + q.ToString(), 3, h.Length);
		  Assert.AreEqual("wrong first", "d2", s.doc(h[0].doc).get("id"));
		  float score0 = h[0].score;
		  float score1 = h[1].score;
		  float score2 = h[2].score;
		  Assert.IsTrue("d2 does not have better score then others: " + score0 + " >? " + score1, score0 > score1);
		  Assert.AreEqual("d4 and d1 don't have equal scores", score1, score2, SCORE_COMP_THRESH);
		}
		catch (Exception e)
		{
		  PrintHits("testSimpleTiebreaker", h, s);
		  throw e;
		}
	  }

	  public virtual void TestBooleanRequiredEqualScores()
	  {

		BooleanQuery q = new BooleanQuery();
		{
		  DisjunctionMaxQuery q1 = new DisjunctionMaxQuery(0.0f);
		  q1.add(Tq("hed", "albino"));
		  q1.add(Tq("dek", "albino"));
		  q.add(q1, BooleanClause.Occur_e.MUST); // true,false);
		  QueryUtils.check(random(), q1, s);

		}
		{
		  DisjunctionMaxQuery q2 = new DisjunctionMaxQuery(0.0f);
		  q2.add(Tq("hed", "elephant"));
		  q2.add(Tq("dek", "elephant"));
		  q.add(q2, BooleanClause.Occur_e.MUST); // true,false);
		  QueryUtils.check(random(), q2, s);
		}

		QueryUtils.check(random(), q, s);

		ScoreDoc[] h = s.search(q, null, 1000).scoreDocs;

		try
		{
		  Assert.AreEqual("3 docs should match " + q.ToString(), 3, h.Length);
		  float score = h[0].score;
		  for (int i = 1; i < h.Length; i++)
		  {
			Assert.AreEqual("score #" + i + " is not the same", score, h[i].score, SCORE_COMP_THRESH);
		  }
		}
		catch (Exception e)
		{
		  PrintHits("testBooleanRequiredEqualScores1", h, s);
		  throw e;
		}
	  }

	  public virtual void TestBooleanOptionalNoTiebreaker()
	  {

		BooleanQuery q = new BooleanQuery();
		{
		  DisjunctionMaxQuery q1 = new DisjunctionMaxQuery(0.0f);
		  q1.add(Tq("hed", "albino"));
		  q1.add(Tq("dek", "albino"));
		  q.add(q1, BooleanClause.Occur_e.SHOULD); // false,false);
		}
		{
		  DisjunctionMaxQuery q2 = new DisjunctionMaxQuery(0.0f);
		  q2.add(Tq("hed", "elephant"));
		  q2.add(Tq("dek", "elephant"));
		  q.add(q2, BooleanClause.Occur_e.SHOULD); // false,false);
		}
		QueryUtils.check(random(), q, s);

		ScoreDoc[] h = s.search(q, null, 1000).scoreDocs;

		try
		{
		  Assert.AreEqual("4 docs should match " + q.ToString(), 4, h.Length);
		  float score = h[0].score;
		  for (int i = 1; i < h.Length - 1; i++) // note: -1
		  {
			Assert.AreEqual("score #" + i + " is not the same", score, h[i].score, SCORE_COMP_THRESH);
		  }
		  Assert.AreEqual("wrong last", "d1", s.doc(h[h.Length - 1].doc).get("id"));
		  float score1 = h[h.Length - 1].score;
		  Assert.IsTrue("d1 does not have worse score then others: " + score + " >? " + score1, score > score1);
		}
		catch (Exception e)
		{
		  PrintHits("testBooleanOptionalNoTiebreaker", h, s);
		  throw e;
		}
	  }

	  public virtual void TestBooleanOptionalWithTiebreaker()
	  {

		BooleanQuery q = new BooleanQuery();
		{
		  DisjunctionMaxQuery q1 = new DisjunctionMaxQuery(0.01f);
		  q1.add(Tq("hed", "albino"));
		  q1.add(Tq("dek", "albino"));
		  q.add(q1, BooleanClause.Occur_e.SHOULD); // false,false);
		}
		{
		  DisjunctionMaxQuery q2 = new DisjunctionMaxQuery(0.01f);
		  q2.add(Tq("hed", "elephant"));
		  q2.add(Tq("dek", "elephant"));
		  q.add(q2, BooleanClause.Occur_e.SHOULD); // false,false);
		}
		QueryUtils.check(random(), q, s);

		ScoreDoc[] h = s.search(q, null, 1000).scoreDocs;

		try
		{

		  Assert.AreEqual("4 docs should match " + q.ToString(), 4, h.Length);

		  float score0 = h[0].score;
		  float score1 = h[1].score;
		  float score2 = h[2].score;
		  float score3 = h[3].score;

		  string doc0 = s.doc(h[0].doc).get("id");
		  string doc1 = s.doc(h[1].doc).get("id");
		  string doc2 = s.doc(h[2].doc).get("id");
		  string doc3 = s.doc(h[3].doc).get("id");

		  Assert.IsTrue("doc0 should be d2 or d4: " + doc0, doc0.Equals("d2") || doc0.Equals("d4"));
		  Assert.IsTrue("doc1 should be d2 or d4: " + doc0, doc1.Equals("d2") || doc1.Equals("d4"));
		  Assert.AreEqual("score0 and score1 should match", score0, score1, SCORE_COMP_THRESH);
		  Assert.AreEqual("wrong third", "d3", doc2);
		  Assert.IsTrue("d3 does not have worse score then d2 and d4: " + score1 + " >? " + score2, score1 > score2);

		  Assert.AreEqual("wrong fourth", "d1", doc3);
		  Assert.IsTrue("d1 does not have worse score then d3: " + score2 + " >? " + score3, score2 > score3);

		}
		catch (Exception e)
		{
		  PrintHits("testBooleanOptionalWithTiebreaker", h, s);
		  throw e;
		}

	  }

	  public virtual void TestBooleanOptionalWithTiebreakerAndBoost()
	  {

		BooleanQuery q = new BooleanQuery();
		{
		  DisjunctionMaxQuery q1 = new DisjunctionMaxQuery(0.01f);
		  q1.add(Tq("hed", "albino", 1.5f));
		  q1.add(Tq("dek", "albino"));
		  q.add(q1, BooleanClause.Occur_e.SHOULD); // false,false);
		}
		{
		  DisjunctionMaxQuery q2 = new DisjunctionMaxQuery(0.01f);
		  q2.add(Tq("hed", "elephant", 1.5f));
		  q2.add(Tq("dek", "elephant"));
		  q.add(q2, BooleanClause.Occur_e.SHOULD); // false,false);
		}
		QueryUtils.check(random(), q, s);

		ScoreDoc[] h = s.search(q, null, 1000).scoreDocs;

		try
		{

		  Assert.AreEqual("4 docs should match " + q.ToString(), 4, h.Length);

		  float score0 = h[0].score;
		  float score1 = h[1].score;
		  float score2 = h[2].score;
		  float score3 = h[3].score;

		  string doc0 = s.doc(h[0].doc).get("id");
		  string doc1 = s.doc(h[1].doc).get("id");
		  string doc2 = s.doc(h[2].doc).get("id");
		  string doc3 = s.doc(h[3].doc).get("id");

		  Assert.AreEqual("doc0 should be d4: ", "d4", doc0);
		  Assert.AreEqual("doc1 should be d3: ", "d3", doc1);
		  Assert.AreEqual("doc2 should be d2: ", "d2", doc2);
		  Assert.AreEqual("doc3 should be d1: ", "d1", doc3);

		  Assert.IsTrue("d4 does not have a better score then d3: " + score0 + " >? " + score1, score0 > score1);
		  Assert.IsTrue("d3 does not have a better score then d2: " + score1 + " >? " + score2, score1 > score2);
		  Assert.IsTrue("d3 does not have a better score then d1: " + score2 + " >? " + score3, score2 > score3);

		}
		catch (Exception e)
		{
		  PrintHits("testBooleanOptionalWithTiebreakerAndBoost", h, s);
		  throw e;
		}
	  }

	  // LUCENE-4477 / LUCENE-4401:
	  public virtual void TestBooleanSpanQuery()
	  {
		int hits = 0;
		Directory directory = newDirectory();
		Analyzer indexerAnalyzer = new MockAnalyzer(random());

		IndexWriterConfig config = new IndexWriterConfig(TEST_VERSION_CURRENT, indexerAnalyzer);
		IndexWriter writer = new IndexWriter(directory, config);
		string FIELD = "content";
		Document d = new Document();
		d.add(new TextField(FIELD, "clockwork orange", Field.Store.YES));
		writer.addDocument(d);
		writer.close();

		IndexReader indexReader = DirectoryReader.open(directory);
		IndexSearcher searcher = newSearcher(indexReader);

		DisjunctionMaxQuery query = new DisjunctionMaxQuery(1.0f);
		SpanQuery sq1 = new SpanTermQuery(new Term(FIELD, "clockwork"));
		SpanQuery sq2 = new SpanTermQuery(new Term(FIELD, "clckwork"));
		query.add(sq1);
		query.add(sq2);
		TopScoreDocCollector collector = TopScoreDocCollector.create(1000, true);
		searcher.search(query, collector);
		hits = collector.topDocs().scoreDocs.length;
		foreach (ScoreDoc scoreDoc in collector.topDocs().scoreDocs)
		{
		  Console.WriteLine(scoreDoc.doc);
		}
		indexReader.close();
		Assert.AreEqual(hits, 1);
		directory.close();
	  }

	  /// <summary>
	  /// macro </summary>
	  protected internal virtual Query Tq(string f, string t)
	  {
		return new TermQuery(new Term(f, t));
	  }

	  /// <summary>
	  /// macro </summary>
	  protected internal virtual Query Tq(string f, string t, float b)
	  {
		Query q = Tq(f, t);
		q.Boost = b;
		return q;
	  }

	  protected internal virtual void PrintHits(string test, ScoreDoc[] h, IndexSearcher searcher)
	  {

		Console.Error.WriteLine("------- " + test + " -------");

		DecimalFormat f = new DecimalFormat("0.000000000", DecimalFormatSymbols.getInstance(Locale.ROOT));

		for (int i = 0; i < h.Length; i++)
		{
		  Document d = searcher.doc(h[i].doc);
		  float score = h[i].score;
		  Console.Error.WriteLine("#" + i + ": " + f.format(score) + " - " + d.get("id"));
		}
	  }
	}

}