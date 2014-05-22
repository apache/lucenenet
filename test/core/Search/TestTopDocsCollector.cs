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

	using Document = Lucene.Net.Document.Document;
	using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using Directory = Lucene.Net.Store.Directory;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

	public class TestTopDocsCollector : LuceneTestCase
	{

	  private sealed class MyTopsDocCollector : TopDocsCollector<ScoreDoc>
	  {

		internal int Idx = 0;
		internal int @base = 0;

		public MyTopsDocCollector(int size) : base(new HitQueue(size, false))
		{
		}

		protected internal override TopDocs NewTopDocs(ScoreDoc[] results, int start)
		{
		  if (results == null)
		  {
			return EMPTY_TOPDOCS;
		  }

		  float maxScore = float.NaN;
		  if (start == 0)
		  {
			maxScore = results[0].score;
		  }
		  else
		  {
			for (int i = pq.size(); i > 1; i--)
			{
				pq.pop();
			}
			maxScore = pq.pop().score;
		  }

		  return new TopDocs(totalHits, results, maxScore);
		}

		public override void Collect(int doc)
		{
		  ++totalHits;
		  pq.insertWithOverflow(new ScoreDoc(doc + @base, Scores[Idx++]));
		}

		public override AtomicReaderContext NextReader
		{
			set
			{
			  @base = value.docBase;
			}
		}

		public override Scorer Scorer
		{
			set
			{
			  // Don't do anything. Assign scores in random
			}
		}

		public override bool AcceptsDocsOutOfOrder()
		{
		  return true;
		}

	  }

	  // Scores array to be used by MyTopDocsCollector. If it is changed, MAX_SCORE
	  // must also change.
	  private static readonly float[] Scores = new float[] {0.7767749f, 1.7839992f, 8.9925785f, 7.9608946f, 0.07948637f, 2.6356435f, 7.4950366f, 7.1490803f, 8.108544f, 4.961808f, 2.2423935f, 7.285586f, 4.6699767f, 2.9655676f, 6.953706f, 5.383931f, 6.9916306f, 8.365894f, 7.888485f, 8.723962f, 3.1796896f, 0.39971232f, 1.3077754f, 6.8489285f, 9.17561f, 5.060466f, 7.9793315f, 8.601509f, 4.1858315f, 0.28146625f};

	  private const float MAX_SCORE = 9.17561f;

	  private Directory Dir;
	  private IndexReader Reader;

	  private TopDocsCollector<ScoreDoc> DoSearch(int numResults)
	  {
		Query q = new MatchAllDocsQuery();
		IndexSearcher searcher = newSearcher(Reader);
		TopDocsCollector<ScoreDoc> tdc = new MyTopsDocCollector(numResults);
		searcher.search(q, tdc);
		return tdc;
	  }

	  public override void SetUp()
	  {
		base.setUp();

		// populate an index with 30 documents, this should be enough for the test.
		// The documents have no content - the test uses MatchAllDocsQuery().
		Dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), Dir);
		for (int i = 0; i < 30; i++)
		{
		  writer.addDocument(new Document());
		}
		Reader = writer.Reader;
		writer.close();
	  }

	  public override void TearDown()
	  {
		Reader.close();
		Dir.close();
		Dir = null;
		base.tearDown();
	  }

	  public virtual void TestInvalidArguments()
	  {
		int numResults = 5;
		TopDocsCollector<ScoreDoc> tdc = DoSearch(numResults);

		// start < 0
		Assert.AreEqual(0, tdc.topDocs(-1).scoreDocs.length);

		// start > pq.size()
		Assert.AreEqual(0, tdc.topDocs(numResults + 1).scoreDocs.length);

		// start == pq.size()
		Assert.AreEqual(0, tdc.topDocs(numResults).scoreDocs.length);

		// howMany < 0
		Assert.AreEqual(0, tdc.topDocs(0, -1).scoreDocs.length);

		// howMany == 0
		Assert.AreEqual(0, tdc.topDocs(0, 0).scoreDocs.length);

	  }

	  public virtual void TestZeroResults()
	  {
		TopDocsCollector<ScoreDoc> tdc = new MyTopsDocCollector(5);
		Assert.AreEqual(0, tdc.topDocs(0, 1).scoreDocs.length);
	  }

	  public virtual void TestFirstResultsPage()
	  {
		TopDocsCollector<ScoreDoc> tdc = DoSearch(15);
		Assert.AreEqual(10, tdc.topDocs(0, 10).scoreDocs.length);
	  }

	  public virtual void TestSecondResultsPages()
	  {
		TopDocsCollector<ScoreDoc> tdc = DoSearch(15);
		// ask for more results than are available
		Assert.AreEqual(5, tdc.topDocs(10, 10).scoreDocs.length);

		// ask for 5 results (exactly what there should be
		tdc = DoSearch(15);
		Assert.AreEqual(5, tdc.topDocs(10, 5).scoreDocs.length);

		// ask for less results than there are
		tdc = DoSearch(15);
		Assert.AreEqual(4, tdc.topDocs(10, 4).scoreDocs.length);
	  }

	  public virtual void TestGetAllResults()
	  {
		TopDocsCollector<ScoreDoc> tdc = DoSearch(15);
		Assert.AreEqual(15, tdc.topDocs().scoreDocs.length);
	  }

	  public virtual void TestGetResultsFromStart()
	  {
		TopDocsCollector<ScoreDoc> tdc = DoSearch(15);
		// should bring all results
		Assert.AreEqual(15, tdc.topDocs(0).scoreDocs.length);

		tdc = DoSearch(15);
		// get the last 5 only.
		Assert.AreEqual(5, tdc.topDocs(10).scoreDocs.length);
	  }

	  public virtual void TestMaxScore()
	  {
		// ask for all results
		TopDocsCollector<ScoreDoc> tdc = DoSearch(15);
		TopDocs td = tdc.topDocs();
		Assert.AreEqual(MAX_SCORE, td.MaxScore, 0f);

		// ask for 5 last results
		tdc = DoSearch(15);
		td = tdc.topDocs(10);
		Assert.AreEqual(MAX_SCORE, td.MaxScore, 0f);
	  }

	  // this does not test the PQ's correctness, but whether topDocs()
	  // implementations return the results in decreasing score order.
	  public virtual void TestResultsOrder()
	  {
		TopDocsCollector<ScoreDoc> tdc = DoSearch(15);
		ScoreDoc[] sd = tdc.topDocs().scoreDocs;

		Assert.AreEqual(MAX_SCORE, sd[0].score, 0f);
		for (int i = 1; i < sd.Length; i++)
		{
		  Assert.IsTrue(sd[i - 1].score >= sd[i].score);
		}
	  }

	}

}