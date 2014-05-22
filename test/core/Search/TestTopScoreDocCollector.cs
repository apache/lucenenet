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
	using IndexReader = Lucene.Net.Index.IndexReader;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using Occur = Lucene.Net.Search.BooleanClause.Occur;
	using Directory = Lucene.Net.Store.Directory;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

	public class TestTopScoreDocCollector : LuceneTestCase
	{

	  public virtual void TestOutOfOrderCollection()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		for (int i = 0; i < 10; i++)
		{
		  writer.addDocument(new Document());
		}

		bool[] inOrder = new bool[] {false, true};
		string[] actualTSDCClass = new string[] {"OutOfOrderTopScoreDocCollector", "InOrderTopScoreDocCollector"};

		BooleanQuery bq = new BooleanQuery();
		// Add a Query with SHOULD, since bw.scorer() returns BooleanScorer2
		// which delegates to BS if there are no mandatory clauses.
		bq.add(new MatchAllDocsQuery(), Occur.SHOULD);
		// Set minNrShouldMatch to 1 so that BQ will not optimize rewrite to return
		// the clause instead of BQ.
		bq.MinimumNumberShouldMatch = 1;
		IndexReader reader = writer.Reader;
		IndexSearcher searcher = newSearcher(reader);
		for (int i = 0; i < inOrder.Length; i++)
		{
		  TopDocsCollector<ScoreDoc> tdc = TopScoreDocCollector.create(3, inOrder[i]);
		  Assert.AreEqual("Lucene.Net.Search.TopScoreDocCollector$" + actualTSDCClass[i], tdc.GetType().Name);

		  searcher.search(new MatchAllDocsQuery(), tdc);

		  ScoreDoc[] sd = tdc.topDocs().scoreDocs;
		  Assert.AreEqual(3, sd.Length);
		  for (int j = 0; j < sd.Length; j++)
		  {
			Assert.AreEqual("expected doc Id " + j + " found " + sd[j].doc, j, sd[j].doc);
		  }
		}
		writer.close();
		reader.close();
		dir.close();
	  }

	}

}