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

	using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
	using Document = Lucene.Net.Document.Document;
	using TextField = Lucene.Net.Document.TextField;
	using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using Term = Lucene.Net.Index.Term;
	using Directory = Lucene.Net.Store.Directory;
	using FixedBitSet = Lucene.Net.Util.FixedBitSet;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;

	public class TestBooleanOr : LuceneTestCase
	{

	  private static string FIELD_T = "T";
	  private static string FIELD_C = "C";

	  private TermQuery T1 = new TermQuery(new Term(FIELD_T, "files"));
	  private TermQuery T2 = new TermQuery(new Term(FIELD_T, "deleting"));
	  private TermQuery C1 = new TermQuery(new Term(FIELD_C, "production"));
	  private TermQuery C2 = new TermQuery(new Term(FIELD_C, "optimize"));

	  private IndexSearcher Searcher = null;
	  private Directory Dir;
	  private IndexReader Reader;


	  private int Search(Query q)
	  {
		QueryUtils.check(random(), q,Searcher);
		return Searcher.search(q, null, 1000).totalHits;
	  }

	  public virtual void TestElements()
	  {
		Assert.AreEqual(1, Search(T1));
		Assert.AreEqual(1, Search(T2));
		Assert.AreEqual(1, Search(C1));
		Assert.AreEqual(1, Search(C2));
	  }

	  /// <summary>
	  /// <code>T:files T:deleting C:production C:optimize </code>
	  /// it works.
	  /// </summary>
	  public virtual void TestFlat()
	  {
		BooleanQuery q = new BooleanQuery();
		q.add(new BooleanClause(T1, BooleanClause.Occur.SHOULD));
		q.add(new BooleanClause(T2, BooleanClause.Occur.SHOULD));
		q.add(new BooleanClause(C1, BooleanClause.Occur.SHOULD));
		q.add(new BooleanClause(C2, BooleanClause.Occur.SHOULD));
		Assert.AreEqual(1, Search(q));
	  }

	  /// <summary>
	  /// <code>(T:files T:deleting) (+C:production +C:optimize)</code>
	  /// it works.
	  /// </summary>
	  public virtual void TestParenthesisMust()
	  {
		BooleanQuery q3 = new BooleanQuery();
		q3.add(new BooleanClause(T1, BooleanClause.Occur.SHOULD));
		q3.add(new BooleanClause(T2, BooleanClause.Occur.SHOULD));
		BooleanQuery q4 = new BooleanQuery();
		q4.add(new BooleanClause(C1, BooleanClause.Occur.MUST));
		q4.add(new BooleanClause(C2, BooleanClause.Occur.MUST));
		BooleanQuery q2 = new BooleanQuery();
		q2.add(q3, BooleanClause.Occur.SHOULD);
		q2.add(q4, BooleanClause.Occur.SHOULD);
		Assert.AreEqual(1, Search(q2));
	  }

	  /// <summary>
	  /// <code>(T:files T:deleting) +(C:production C:optimize)</code>
	  /// not working. results NO HIT.
	  /// </summary>
	  public virtual void TestParenthesisMust2()
	  {
		BooleanQuery q3 = new BooleanQuery();
		q3.add(new BooleanClause(T1, BooleanClause.Occur.SHOULD));
		q3.add(new BooleanClause(T2, BooleanClause.Occur.SHOULD));
		BooleanQuery q4 = new BooleanQuery();
		q4.add(new BooleanClause(C1, BooleanClause.Occur.SHOULD));
		q4.add(new BooleanClause(C2, BooleanClause.Occur.SHOULD));
		BooleanQuery q2 = new BooleanQuery();
		q2.add(q3, BooleanClause.Occur.SHOULD);
		q2.add(q4, BooleanClause.Occur.MUST);
		Assert.AreEqual(1, Search(q2));
	  }

	  /// <summary>
	  /// <code>(T:files T:deleting) (C:production C:optimize)</code>
	  /// not working. results NO HIT.
	  /// </summary>
	  public virtual void TestParenthesisShould()
	  {
		BooleanQuery q3 = new BooleanQuery();
		q3.add(new BooleanClause(T1, BooleanClause.Occur.SHOULD));
		q3.add(new BooleanClause(T2, BooleanClause.Occur.SHOULD));
		BooleanQuery q4 = new BooleanQuery();
		q4.add(new BooleanClause(C1, BooleanClause.Occur.SHOULD));
		q4.add(new BooleanClause(C2, BooleanClause.Occur.SHOULD));
		BooleanQuery q2 = new BooleanQuery();
		q2.add(q3, BooleanClause.Occur.SHOULD);
		q2.add(q4, BooleanClause.Occur.SHOULD);
		Assert.AreEqual(1, Search(q2));
	  }

	  public override void SetUp()
	  {
		base.setUp();

		//
		Dir = newDirectory();


		//
		RandomIndexWriter writer = new RandomIndexWriter(random(), Dir);

		//
		Document d = new Document();
		d.add(newField(FIELD_T, "Optimize not deleting all files", TextField.TYPE_STORED));
		d.add(newField(FIELD_C, "Deleted When I run an optimize in our production environment.", TextField.TYPE_STORED));

		//
		writer.addDocument(d);

		Reader = writer.Reader;
		//
		Searcher = newSearcher(Reader);
		writer.close();
	  }

	  public override void TearDown()
	  {
		Reader.close();
		Dir.close();
		base.tearDown();
	  }

	  public virtual void TestBooleanScorerMax()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter riw = new RandomIndexWriter(random(), dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));

		int docCount = atLeast(10000);

		for (int i = 0;i < docCount;i++)
		{
		  Document doc = new Document();
		  doc.add(newField("field", "a", TextField.TYPE_NOT_STORED));
		  riw.addDocument(doc);
		}

		riw.forceMerge(1);
		IndexReader r = riw.Reader;
		riw.close();

		IndexSearcher s = newSearcher(r);
		BooleanQuery bq = new BooleanQuery();
		bq.add(new TermQuery(new Term("field", "a")), BooleanClause.Occur.SHOULD);
		bq.add(new TermQuery(new Term("field", "a")), BooleanClause.Occur.SHOULD);

		Weight w = s.createNormalizedWeight(bq);

		Assert.AreEqual(1, s.IndexReader.leaves().size());
		BulkScorer scorer = w.bulkScorer(s.IndexReader.leaves().get(0), false, null);

		FixedBitSet hits = new FixedBitSet(docCount);
		AtomicInteger end = new AtomicInteger();
		Collector c = new CollectorAnonymousInnerClassHelper(this, scorer, hits, end);

		while ((int)end < docCount)
		{
		  int inc = TestUtil.Next(random(), 1, 1000);
		  end.getAndAdd(inc);
		  scorer.score(c, (int)end);
		}

		Assert.AreEqual(docCount, hits.cardinality());
		r.close();
		dir.close();
	  }

	  private class CollectorAnonymousInnerClassHelper : Collector
	  {
		  private readonly TestBooleanOr OuterInstance;

		  private BulkScorer Scorer;
		  private FixedBitSet Hits;
		  private AtomicInteger End;

		  public CollectorAnonymousInnerClassHelper(TestBooleanOr outerInstance, BulkScorer scorer, FixedBitSet hits, AtomicInteger end)
		  {
			  this.OuterInstance = outerInstance;
			  this.Scorer = scorer;
			  this.Hits = hits;
			  this.End = end;
		  }

		  public override AtomicReaderContext NextReader
		  {
			  set
			  {
			  }
		  }

		  public override void Collect(int doc)
		  {
			Assert.IsTrue("collected doc=" + doc + " beyond max=" + End, doc < (int)End);
			Hits.set(doc);
		  }

		  public override Scorer Scorer
		  {
			  set
			  {
			  }
		  }

		  public override bool AcceptsDocsOutOfOrder()
		  {
			return true;
		  }
	  }
	}

}