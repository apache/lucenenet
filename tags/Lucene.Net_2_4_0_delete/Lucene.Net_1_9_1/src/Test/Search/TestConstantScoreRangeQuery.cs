/*
 * Copyright 2004 The Apache Software Foundation
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using NUnit.Framework;
using WhitespaceAnalyzer = Lucene.Net.Analysis.WhitespaceAnalyzer;
using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;
using IndexReader = Lucene.Net.Index.IndexReader;
using IndexWriter = Lucene.Net.Index.IndexWriter;
using Term = Lucene.Net.Index.Term;
using Directory = Lucene.Net.Store.Directory;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;

namespace Lucene.Net.Search
{

	[TestFixture]
	public class TestConstantScoreRangeQuery : BaseTestRangeFilter
	{
		private class AnonymousClassHitCollector : HitCollector
		{
			public AnonymousClassHitCollector(TestConstantScoreRangeQuery enclosingInstance)
			{
				InitBlock(enclosingInstance);
			}
			private void  InitBlock(TestConstantScoreRangeQuery enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private TestConstantScoreRangeQuery enclosingInstance;
			public TestConstantScoreRangeQuery Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			public override void  Collect(int doc, float score)
			{
				Enclosing_Instance.AssertEquals("score for doc " + doc + " was not correct", 1.0f, score);
			}
		}
		
		/// <summary>threshold for comparing floats </summary>
		public const float SCORE_COMP_THRESH = 1e-6f;
		
		public TestConstantScoreRangeQuery(System.String name) : base(name)
		{
		}
		public TestConstantScoreRangeQuery() : base()
		{
		}
		
		internal Directory small;
		
		internal virtual void  AssertEquals(System.String m, float e, float a)
		{
			Assert.AreEqual(e, a, m, SCORE_COMP_THRESH);
		}
		
        [SetUp]
        public virtual void  SetUp()
		{
			
			System.String[] data = new System.String[]{"A 1 2 3 4 5 6", "Z       4 5 6", null, "B   2   4 5 6", "Y     3   5 6", null, "C     3     6", "X       4 5 6"};
			
			small = new RAMDirectory();
			IndexWriter writer = new IndexWriter(small, new WhitespaceAnalyzer(), true);
			
			for (int i = 0; i < data.Length; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document();
				doc.Add(Field.Keyword("id", System.Convert.ToString(i)));
				doc.Add(Field.Keyword("all", "all"));
				if (null != data[i])
				{
					doc.Add(Field.Text("data", data[i]));
				}
				writer.AddDocument(doc);
			}
			
			writer.Optimize();
			writer.Close();
		}
		
		
		
		/// <summary>macro for readability </summary>
		public static Query Csrq(System.String f, System.String l, System.String h, bool il, bool ih)
		{
			return new ConstantScoreRangeQuery(f, l, h, il, ih);
		}
		
        [Test]
        public virtual void  TestBasics()
		{
			QueryUtils.Check(Csrq("data", "1", "6", T, T));
			QueryUtils.Check(Csrq("data", "A", "Z", T, T));
			QueryUtils.CheckUnequal(Csrq("data", "1", "6", T, T), Csrq("data", "A", "Z", T, T));
		}
		
        [Test]
        public virtual void  TestEqualScores()
		{
			// NOTE: uses index build in *this* SetUp
			
			IndexReader reader = IndexReader.Open(small);
			IndexSearcher search = new IndexSearcher(reader);
			
			Hits result;
			
			// some hits match more terms then others, score should be the same
			
			result = search.Search(Csrq("data", "1", "6", T, T));
			int numHits = result.Length();
			Assert.AreEqual(6, numHits, "wrong number of results");
			float score = result.Score(0);
			for (int i = 1; i < numHits; i++)
			{
				AssertEquals("score for " + i + " was not the same", score, result.Score(i));
			}
		}
		
        [Test]
        public virtual void  TestBoost()
		{
			// NOTE: uses index build in *this* SetUp
			
			IndexReader reader = IndexReader.Open(small);
			IndexSearcher search = new IndexSearcher(reader);
			
			// test for correct application of query normalization
			// must use a non score normalizing method for this.
			Query q = Csrq("data", "1", "6", T, T);
			q.SetBoost(100);
			search.Search(q, null, new AnonymousClassHitCollector(this));
			
			
			//
			// Ensure that boosting works to score one clause of a query higher
			// than another.
			//
			Query q1 = Csrq("data", "A", "A", T, T); // matches document #0
			q1.SetBoost(.1f);
			Query q2 = Csrq("data", "Z", "Z", T, T); // matches document #1
			BooleanQuery bq = new BooleanQuery(true);
			bq.Add(q1, BooleanClause.Occur.SHOULD);
			bq.Add(q2, BooleanClause.Occur.SHOULD);
			
			Hits hits = search.Search(bq);
			Assert.AreEqual(1, hits.Id(0));
			Assert.AreEqual(0, hits.Id(1));
			Assert.IsTrue(hits.Score(0) > hits.Score(1));
			
			q1 = Csrq("data", "A", "A", T, T); // matches document #0
			q1.SetBoost(10f);
			q2 = Csrq("data", "Z", "Z", T, T); // matches document #1
			bq = new BooleanQuery(true);
			bq.Add(q1, BooleanClause.Occur.SHOULD);
			bq.Add(q2, BooleanClause.Occur.SHOULD);
			
			hits = search.Search(bq);
			Assert.AreEqual(0, hits.Id(0));
			Assert.AreEqual(1, hits.Id(1));
			Assert.IsTrue(hits.Score(0) > hits.Score(1));
		}
		
		[Test]
        public virtual void  TestBooleanOrderUnAffected()
		{
			// NOTE: uses index build in *this* SetUp
			
			IndexReader reader = IndexReader.Open(small);
			IndexSearcher search = new IndexSearcher(reader);
			
			// first do a regular RangeQuery which uses term expansion so
			// docs with more terms in range get higher scores
			
			Query rq = new RangeQuery(new Term("data", "1"), new Term("data", "4"), T);
			
			Hits expected = search.Search(rq);
			int numHits = expected.Length();
			
			// now do a boolean where which also contains a
			// ConstantScoreRangeQuery and make sure hte order is the same
			
			BooleanQuery q = new BooleanQuery();
			q.Add(rq, T, F);
			q.Add(Csrq("data", "1", "6", T, T), T, F);
			
			Hits actual = search.Search(q);
			
			Assert.AreEqual(numHits, actual.Length(), "wrong numebr of hits");
			for (int i = 0; i < numHits; i++)
			{
				Assert.AreEqual(expected.Id(i), actual.Id(i), "mismatch in docid for hit#" + i);
			}
		}
		
		[Test]
        public virtual void  TestRangeQueryId()
		{
			// NOTE: uses index build in *super* SetUp
			
			IndexReader reader = IndexReader.Open(index);
			IndexSearcher search = new IndexSearcher(reader);
			
			int medId = ((maxId - minId) / 2);
			
			System.String minIP = Pad(minId);
			System.String maxIP = Pad(maxId);
			System.String medIP = Pad(medId);
			
			int numDocs = reader.NumDocs();
			
			Assert.AreEqual(numDocs, 1 + maxId - minId, "num of docs");
			
			Hits result;
			
			// test id, bounded on both ends
			
			result = search.Search(Csrq("id", minIP, maxIP, T, T));
			Assert.AreEqual(numDocs, result.Length(), "find all");
			
			result = search.Search(Csrq("id", minIP, maxIP, T, F));
			Assert.AreEqual(numDocs - 1, result.Length(), "all but last");
			
			result = search.Search(Csrq("id", minIP, maxIP, F, T));
			Assert.AreEqual(numDocs - 1, result.Length(), "all but first");
			
			result = search.Search(Csrq("id", minIP, maxIP, F, F));
			Assert.AreEqual(numDocs - 2, result.Length(), "all but ends");
			
			result = search.Search(Csrq("id", medIP, maxIP, T, T));
			Assert.AreEqual(1 + maxId - medId, result.Length(), "med and up");
			
			result = search.Search(Csrq("id", minIP, medIP, T, T));
			Assert.AreEqual(1 + medId - minId, result.Length(), "up to med");
			
			// unbounded id
			
			result = search.Search(Csrq("id", minIP, null, T, F));
			Assert.AreEqual(numDocs, result.Length(), "min and up");
			
			result = search.Search(Csrq("id", null, maxIP, F, T));
			Assert.AreEqual(numDocs, result.Length(), "max and down");
			
			result = search.Search(Csrq("id", minIP, null, F, F));
			Assert.AreEqual(numDocs - 1, result.Length(), "not min, but up");
			
			result = search.Search(Csrq("id", null, maxIP, F, F));
			Assert.AreEqual(numDocs - 1, result.Length(), "not max, but down");
			
			result = search.Search(Csrq("id", medIP, maxIP, T, F));
			Assert.AreEqual(maxId - medId, result.Length(), "med and up, not max");
			
			result = search.Search(Csrq("id", minIP, medIP, F, T));
			Assert.AreEqual(medId - minId, result.Length(), "not min, up to med");
			
			// very small sets
			
			result = search.Search(Csrq("id", minIP, minIP, F, F));
			Assert.AreEqual(0, result.Length(), "min,min,F,F");
			result = search.Search(Csrq("id", medIP, medIP, F, F));
			Assert.AreEqual(0, result.Length(), "med,med,F,F");
			result = search.Search(Csrq("id", maxIP, maxIP, F, F));
			Assert.AreEqual(0, result.Length(), "max,max,F,F");
			
			result = search.Search(Csrq("id", minIP, minIP, T, T));
			Assert.AreEqual(1, result.Length(), "min,min,T,T");
			result = search.Search(Csrq("id", null, minIP, F, T));
			Assert.AreEqual(1, result.Length(), "nul,min,F,T");
			
			result = search.Search(Csrq("id", maxIP, maxIP, T, T));
			Assert.AreEqual(1, result.Length(), "max,max,T,T");
			result = search.Search(Csrq("id", maxIP, null, T, F));
			Assert.AreEqual(1, result.Length(), "max,nul,T,T");
			
			result = search.Search(Csrq("id", medIP, medIP, T, T));
			Assert.AreEqual(1, result.Length(), "med,med,T,T");
		}
		
        [Test]
        public virtual void  TestRangeQueryRand()
		{
			// NOTE: uses index build in *super* SetUp
			
			IndexReader reader = IndexReader.Open(index);
			IndexSearcher search = new IndexSearcher(reader);
			
			System.String minRP = Pad(minR);
			System.String maxRP = Pad(maxR);
			
			int numDocs = reader.NumDocs();
			
			Assert.AreEqual(numDocs, 1 + maxId - minId, "num of docs");
			
			Hits result;
			Query q = new TermQuery(new Term("body", "body"));
			
			// test extremes, bounded on both ends
			
			result = search.Search(Csrq("rand", minRP, maxRP, T, T));
			Assert.AreEqual(numDocs, result.Length(), "find all");
			
			result = search.Search(Csrq("rand", minRP, maxRP, T, F));
			Assert.AreEqual(numDocs - 1, result.Length(), "all but biggest");
			
			result = search.Search(Csrq("rand", minRP, maxRP, F, T));
			Assert.AreEqual(numDocs - 1, result.Length(), "all but smallest");
			
			result = search.Search(Csrq("rand", minRP, maxRP, F, F));
			Assert.AreEqual(numDocs - 2, result.Length(), "all but extremes");
			
			// unbounded
			
			result = search.Search(Csrq("rand", minRP, null, T, F));
			Assert.AreEqual(numDocs, result.Length(), "smallest and up");
			
			result = search.Search(Csrq("rand", null, maxRP, F, T));
			Assert.AreEqual(numDocs, result.Length(), "biggest and down");
			
			result = search.Search(Csrq("rand", minRP, null, F, F));
			Assert.AreEqual(numDocs - 1, result.Length(), "not smallest, but up");
			
			result = search.Search(Csrq("rand", null, maxRP, F, F));
			Assert.AreEqual(numDocs - 1, result.Length(), "not biggest, but down");
			
			// very small sets
			
			result = search.Search(Csrq("rand", minRP, minRP, F, F));
			Assert.AreEqual(0, result.Length(), "min,min,F,F");
			result = search.Search(Csrq("rand", maxRP, maxRP, F, F));
			Assert.AreEqual(0, result.Length(), "max,max,F,F");
			
			result = search.Search(Csrq("rand", minRP, minRP, T, T));
			Assert.AreEqual(1, result.Length(), "min,min,T,T");
			result = search.Search(Csrq("rand", null, minRP, F, T));
			Assert.AreEqual(1, result.Length(), "nul,min,F,T");
			
			result = search.Search(Csrq("rand", maxRP, maxRP, T, T));
			Assert.AreEqual(1, result.Length(), "max,max,T,T");
			result = search.Search(Csrq("rand", maxRP, null, T, F));
			Assert.AreEqual(1, result.Length(), "max,nul,T,T");
		}
	}
}