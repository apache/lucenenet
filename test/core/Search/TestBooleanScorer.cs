using System;
using System.Collections.Generic;

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
	using Field = Lucene.Net.Document.Field;
	using TextField = Lucene.Net.Document.TextField;
	using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using Term = Lucene.Net.Index.Term;
	using BooleanWeight = Lucene.Net.Search.BooleanQuery.BooleanWeight;
	using Directory = Lucene.Net.Store.Directory;
	using Bits = Lucene.Net.Util.Bits;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
using System.Diagnostics;

	public class TestBooleanScorer : LuceneTestCase
	{
	  private const string FIELD = "category";

	  public virtual void TestMethod()
	  {
		Directory directory = newDirectory();

		string[] values = new string[] {"1", "2", "3", "4"};

		RandomIndexWriter writer = new RandomIndexWriter(random(), directory);
		for (int i = 0; i < values.Length; i++)
		{
		  Document doc = new Document();
		  doc.add(newStringField(FIELD, values[i], Field.Store.YES));
		  writer.addDocument(doc);
		}
		IndexReader ir = writer.Reader;
		writer.close();

		BooleanQuery booleanQuery1 = new BooleanQuery();
		booleanQuery1.add(new TermQuery(new Term(FIELD, "1")), BooleanClause.Occur_e.SHOULD);
		booleanQuery1.add(new TermQuery(new Term(FIELD, "2")), BooleanClause.Occur_e.SHOULD);

		BooleanQuery query = new BooleanQuery();
		query.add(booleanQuery1, BooleanClause.Occur_e.MUST);
		query.add(new TermQuery(new Term(FIELD, "9")), BooleanClause.Occur_e.MUST_NOT);

		IndexSearcher indexSearcher = newSearcher(ir);
		ScoreDoc[] hits = indexSearcher.search(query, null, 1000).scoreDocs;
		Assert.AreEqual("Number of matched documents", 2, hits.Length);
		ir.close();
		directory.close();
	  }

	  public virtual void TestEmptyBucketWithMoreDocs()
	  {
		// this test checks the logic of nextDoc() when all sub scorers have docs
		// beyond the first bucket (for example). Currently, the code relies on the
		// 'more' variable to work properly, and this test ensures that if the logic
		// changes, we have a test to back it up.

		Directory directory = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), directory);
		writer.commit();
		IndexReader ir = writer.Reader;
		writer.close();
		IndexSearcher searcher = newSearcher(ir);
		BooleanWeight weight = (BooleanWeight) (new BooleanQuery()).createWeight(searcher);
		BulkScorer[] scorers = new BulkScorer[] {new BulkScorer() {private int doc = -1; public bool score(Collector c, int maxDoc) {Debug.Assert(doc == -1); doc = 3000; FakeScorer fs = new FakeScorer(); fs.doc = doc; fs.score = 1.0f; c.setScorer(fs); c.collect(3000); return false;}}};

		BooleanScorer bs = new BooleanScorer(weight, false, 1, Arrays.asList(scorers), Collections.emptyList<BulkScorer>(), scorers.Length);

		IList<int?> hits = new List<int>();
		bs.score(new CollectorAnonymousInnerClassHelper(this, hits));

		Assert.AreEqual("should have only 1 hit", 1, hits.Count);
		Assert.AreEqual("hit should have been docID=3000", 3000, (int)hits[0]);
		ir.close();
		directory.close();
	  }

	  private class CollectorAnonymousInnerClassHelper : Collector
	  {
		  private readonly TestBooleanScorer OuterInstance;

//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: private IList<int?> hits;
		  private IList<int?> Hits;

		  public CollectorAnonymousInnerClassHelper<T1>(TestBooleanScorer outerInstance, IList<T1> hits)
		  {
			  this.OuterInstance = outerInstance;
			  this.Hits = hits;
		  }

		  internal int docBase;
		  public override Scorer Scorer
		  {
			  set
			  {
			  }
		  }

		  public override void Collect(int doc)
		  {
			Hits.Add(docBase + doc);
		  }

		  public override AtomicReaderContext NextReader
		  {
			  set
			  {
				docBase = value.docBase;
			  }
		  }

		  public override bool AcceptsDocsOutOfOrder()
		  {
			return true;
		  }
	  }

	  public virtual void TestMoreThan32ProhibitedClauses()
	  {
		Directory d = newDirectory();
		RandomIndexWriter w = new RandomIndexWriter(random(), d);
		Document doc = new Document();
		doc.add(new TextField("field", "0 1 2 3 4 5 6 7 8 9 10 11 12 13 14 15 16 17 18 19 20 21 22 23 24 25 26 27 28 29 30 31 32 33", Field.Store.NO));
		w.addDocument(doc);
		doc = new Document();
		doc.add(new TextField("field", "33", Field.Store.NO));
		w.addDocument(doc);
		IndexReader r = w.Reader;
		w.close();
		// we don't wrap with AssertingIndexSearcher in order to have the original scorer in setScorer.
		IndexSearcher s = newSearcher(r, true, false);

		BooleanQuery q = new BooleanQuery();
		for (int term = 0;term < 33;term++)
		{
		  q.add(new BooleanClause(new TermQuery(new Term("field", "" + term)), BooleanClause.Occur.MUST_NOT));
		}
		q.add(new BooleanClause(new TermQuery(new Term("field", "33")), BooleanClause.Occur.SHOULD));

		int[] count = new int[1];
		s.search(q, new CollectorAnonymousInnerClassHelper2(this, doc, count));

		Assert.AreEqual(1, count[0]);

		r.close();
		d.close();
	  }

	  private class CollectorAnonymousInnerClassHelper2 : Collector
	  {
		  private readonly TestBooleanScorer OuterInstance;

		  private Document Doc;
		  private int[] Count;

		  public CollectorAnonymousInnerClassHelper2(TestBooleanScorer outerInstance, Document doc, int[] count)
		  {
			  this.OuterInstance = outerInstance;
			  this.Doc = doc;
			  this.Count = count;
		  }


		  public override Scorer Scorer
		  {
			  set
			  {
				// Make sure we got BooleanScorer:
				Type clazz = value.GetType();
				Assert.AreEqual("Scorer is implemented by wrong class", typeof(FakeScorer).Name, clazz.Name);
			  }
		  }

		  public override void Collect(int doc)
		  {
			Count[0]++;
		  }

		  public override AtomicReaderContext NextReader
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

	  /// <summary>
	  /// Throws UOE if Weight.scorer is called </summary>
	  private class CrazyMustUseBulkScorerQuery : Query
	  {

		public override string ToString(string field)
		{
		  return "MustUseBulkScorerQuery";
		}

		public override Weight CreateWeight(IndexSearcher searcher)
		{
		  return new WeightAnonymousInnerClassHelper(this);
		}

		private class WeightAnonymousInnerClassHelper : Weight
		{
			private readonly CrazyMustUseBulkScorerQuery OuterInstance;

			public WeightAnonymousInnerClassHelper(CrazyMustUseBulkScorerQuery outerInstance)
			{
				this.OuterInstance = outerInstance;
			}

			public override Explanation Explain(AtomicReaderContext context, int doc)
			{
			  throw new System.NotSupportedException();
			}

			public override Query Query
			{
				get
				{
				  return OuterInstance;
				}
			}

			public override float ValueForNormalization
			{
				get
				{
				  return 1.0f;
				}
			}

			public override void Normalize(float norm, float topLevelBoost)
			{
			}

			public override Scorer Scorer(AtomicReaderContext context, Bits acceptDocs)
			{
			  throw new System.NotSupportedException();
			}

			public override BulkScorer BulkScorer(AtomicReaderContext context, bool scoreDocsInOrder, Bits acceptDocs)
			{
			  return new BulkScorerAnonymousInnerClassHelper(this);
			}

			private class BulkScorerAnonymousInnerClassHelper : BulkScorer
			{
				private readonly WeightAnonymousInnerClassHelper OuterInstance;

				public BulkScorerAnonymousInnerClassHelper(WeightAnonymousInnerClassHelper outerInstance)
				{
					this.outerInstance = outerInstance;
				}


				public override bool Score(Collector collector, int max)
				{
				  collector.Scorer = new FakeScorer();
				  collector.collect(0);
				  return false;
				}
			}
		}
	  }

	  /// <summary>
	  /// Make sure BooleanScorer can embed another
	  ///  BooleanScorer. 
	  /// </summary>
	  public virtual void TestEmbeddedBooleanScorer()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter w = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		doc.add(newTextField("field", "doctors are people who prescribe medicines of which they know little, to cure diseases of which they know less, in human beings of whom they know nothing", Field.Store.NO));
		w.addDocument(doc);
		IndexReader r = w.Reader;
		w.close();

		IndexSearcher s = newSearcher(r);
		BooleanQuery q1 = new BooleanQuery();
		q1.add(new TermQuery(new Term("field", "little")), BooleanClause.Occur.SHOULD);
		q1.add(new TermQuery(new Term("field", "diseases")), BooleanClause.Occur.SHOULD);

		BooleanQuery q2 = new BooleanQuery();
		q2.add(q1, BooleanClause.Occur.SHOULD);
		q2.add(new CrazyMustUseBulkScorerQuery(), BooleanClause.Occur.SHOULD);

		Assert.AreEqual(1, s.search(q2, 10).totalHits);
		r.close();
		dir.close();
	  }
	}

}