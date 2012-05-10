/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
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
using Lucene.Net.Support;
using NUnit.Framework;

using StandardAnalyzer = Lucene.Net.Analysis.Standard.StandardAnalyzer;
using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;
using MapFieldSelector = Lucene.Net.Documents.MapFieldSelector;
using Directory = Lucene.Net.Store.Directory;
using MockRAMDirectory = Lucene.Net.Store.MockRAMDirectory;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;
using BooleanQuery = Lucene.Net.Search.BooleanQuery;
using IndexSearcher = Lucene.Net.Search.IndexSearcher;
using Query = Lucene.Net.Search.Query;
using ScoreDoc = Lucene.Net.Search.ScoreDoc;
using Searcher = Lucene.Net.Search.Searcher;
using TermQuery = Lucene.Net.Search.TermQuery;
using Occur = Lucene.Net.Search.Occur;
using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

namespace Lucene.Net.Index
{
	
    [TestFixture]
	public class TestParallelReader:LuceneTestCase
	{
		
		private Searcher parallel;
		private Searcher single;
		
		[SetUp]
		public override void  SetUp()
		{
			base.SetUp();
			single = Single();
			parallel = Parallel();
		}
		
		[Test]
		public virtual void  TestQueries()
		{
			QueryTest(new TermQuery(new Term("f1", "v1")));
			QueryTest(new TermQuery(new Term("f1", "v2")));
			QueryTest(new TermQuery(new Term("f2", "v1")));
			QueryTest(new TermQuery(new Term("f2", "v2")));
			QueryTest(new TermQuery(new Term("f3", "v1")));
			QueryTest(new TermQuery(new Term("f3", "v2")));
			QueryTest(new TermQuery(new Term("f4", "v1")));
			QueryTest(new TermQuery(new Term("f4", "v2")));
			
			BooleanQuery bq1 = new BooleanQuery();
			bq1.Add(new TermQuery(new Term("f1", "v1")), Occur.MUST);
			bq1.Add(new TermQuery(new Term("f4", "v1")), Occur.MUST);
			QueryTest(bq1);
		}
		
		[Test]
		public virtual void  TestFieldNames()
		{
			Directory dir1 = GetDir1();
			Directory dir2 = GetDir2();
			ParallelReader pr = new ParallelReader();
            pr.Add(IndexReader.Open(dir1, false));
            pr.Add(IndexReader.Open(dir2, false));
            System.Collections.Generic.ICollection<string> fieldNames = pr.GetFieldNames(IndexReader.FieldOption.ALL);
			Assert.AreEqual(4, fieldNames.Count);
			Assert.IsTrue(CollectionsHelper.Contains(fieldNames, "f1"));
			Assert.IsTrue(CollectionsHelper.Contains(fieldNames, "f2"));
			Assert.IsTrue(CollectionsHelper.Contains(fieldNames, "f3"));
			Assert.IsTrue(CollectionsHelper.Contains(fieldNames, "f4"));
		}
		
		[Test]
		public virtual void  TestDocument()
		{
			Directory dir1 = GetDir1();
			Directory dir2 = GetDir2();
			ParallelReader pr = new ParallelReader();
            pr.Add(IndexReader.Open(dir1, false));
            pr.Add(IndexReader.Open(dir2, false));
			
			Document doc11 = pr.Document(0, new MapFieldSelector(new System.String[]{"f1"}));
			Document doc24 = pr.Document(1, new MapFieldSelector(new System.String[]{"f4"}));
			Document doc223 = pr.Document(1, new MapFieldSelector(new System.String[]{"f2", "f3"}));
			
			Assert.AreEqual(1, doc11.GetFields().Count);
			Assert.AreEqual(1, doc24.GetFields().Count);
			Assert.AreEqual(2, doc223.GetFields().Count);
			
			Assert.AreEqual("v1", doc11.Get("f1"));
			Assert.AreEqual("v2", doc24.Get("f4"));
			Assert.AreEqual("v2", doc223.Get("f2"));
			Assert.AreEqual("v2", doc223.Get("f3"));
		}
		
		[Test]
		public virtual void  TestIncompatibleIndexes()
		{
			// two documents:
			Directory dir1 = GetDir1();
			
			// one document only:
			Directory dir2 = new MockRAMDirectory();
			IndexWriter w2 = new IndexWriter(dir2, new StandardAnalyzer(Util.Version.LUCENE_CURRENT), true, IndexWriter.MaxFieldLength.LIMITED);
			Document d3 = new Document();
			d3.Add(new Field("f3", "v1", Field.Store.YES, Field.Index.ANALYZED));
			w2.AddDocument(d3);
			w2.Close();
			
			ParallelReader pr = new ParallelReader();
            pr.Add(IndexReader.Open(dir1, false));

			Assert.Throws<ArgumentException>(() => pr.Add(IndexReader.Open(dir2, false)),
			                                    "didn't get exptected exception: indexes don't have same number of documents");
		}
		
		[Test]
		public virtual void  TestIsCurrent()
		{
			Directory dir1 = GetDir1();
			Directory dir2 = GetDir2();
			ParallelReader pr = new ParallelReader();
			pr.Add(IndexReader.Open(dir1, false));
            pr.Add(IndexReader.Open(dir2, false));
			
			Assert.IsTrue(pr.IsCurrent());
            IndexReader modifier = IndexReader.Open(dir1, false);
			modifier.SetNorm(0, "f1", 100);
			modifier.Close();
			
			// one of the two IndexReaders which ParallelReader is using
			// is not current anymore
			Assert.IsFalse(pr.IsCurrent());

            modifier = IndexReader.Open(dir2, false);
			modifier.SetNorm(0, "f3", 100);
			modifier.Close();
			
			// now both are not current anymore
			Assert.IsFalse(pr.IsCurrent());
		}
		
		[Test]
		public virtual void  TestIsOptimized()
		{
			Directory dir1 = GetDir1();
			Directory dir2 = GetDir2();
			
			// add another document to ensure that the indexes are not optimized
			IndexWriter modifier = new IndexWriter(dir1, new StandardAnalyzer(Util.Version.LUCENE_CURRENT), IndexWriter.MaxFieldLength.LIMITED);
			Document d = new Document();
			d.Add(new Field("f1", "v1", Field.Store.YES, Field.Index.ANALYZED));
			modifier.AddDocument(d);
			modifier.Close();
			
			modifier = new IndexWriter(dir2, new StandardAnalyzer(Util.Version.LUCENE_CURRENT), IndexWriter.MaxFieldLength.LIMITED);
			d = new Document();
			d.Add(new Field("f2", "v2", Field.Store.YES, Field.Index.ANALYZED));
			modifier.AddDocument(d);
			modifier.Close();
			
			
			ParallelReader pr = new ParallelReader();
            pr.Add(IndexReader.Open(dir1, false));
            pr.Add(IndexReader.Open(dir2, false));
			Assert.IsFalse(pr.IsOptimized());
			pr.Close();
			
			modifier = new IndexWriter(dir1, new StandardAnalyzer(Util.Version.LUCENE_CURRENT), IndexWriter.MaxFieldLength.LIMITED);
			modifier.Optimize();
			modifier.Close();
			
			pr = new ParallelReader();
            pr.Add(IndexReader.Open(dir1, false));
            pr.Add(IndexReader.Open(dir2, false));
			// just one of the two indexes are optimized
			Assert.IsFalse(pr.IsOptimized());
			pr.Close();
			
			
			modifier = new IndexWriter(dir2, new StandardAnalyzer(Util.Version.LUCENE_CURRENT), IndexWriter.MaxFieldLength.LIMITED);
			modifier.Optimize();
			modifier.Close();
			
			pr = new ParallelReader();
            pr.Add(IndexReader.Open(dir1, false));
            pr.Add(IndexReader.Open(dir2, false));
			// now both indexes are optimized
			Assert.IsTrue(pr.IsOptimized());
			pr.Close();
		}
		
		[Test]
		public virtual void  TestAllTermDocs()
		{
			Directory dir1 = GetDir1();
			Directory dir2 = GetDir2();
			ParallelReader pr = new ParallelReader();
            pr.Add(IndexReader.Open(dir1, false));
            pr.Add(IndexReader.Open(dir2, false));
			int NUM_DOCS = 2;
            TermDocs td = pr.TermDocs(null);
			for (int i = 0; i < NUM_DOCS; i++)
			{
				Assert.IsTrue(td.Next());
				Assert.AreEqual(i, td.Doc);
				Assert.AreEqual(1, td.Freq);
			}
			td.Close();
			pr.Close();
			dir1.Close();
			dir2.Close();
		}
		
		
		private void  QueryTest(Query query)
		{
			ScoreDoc[] parallelHits = parallel.Search(query, null, 1000).ScoreDocs;
			ScoreDoc[] singleHits = single.Search(query, null, 1000).ScoreDocs;
			Assert.AreEqual(parallelHits.Length, singleHits.Length);
			for (int i = 0; i < parallelHits.Length; i++)
			{
				Assert.AreEqual(parallelHits[i].Score, singleHits[i].Score, 0.001f);
				Document docParallel = parallel.Doc(parallelHits[i].Doc);
				Document docSingle = single.Doc(singleHits[i].Doc);
				Assert.AreEqual(docParallel.Get("f1"), docSingle.Get("f1"));
				Assert.AreEqual(docParallel.Get("f2"), docSingle.Get("f2"));
				Assert.AreEqual(docParallel.Get("f3"), docSingle.Get("f3"));
				Assert.AreEqual(docParallel.Get("f4"), docSingle.Get("f4"));
			}
		}
		
		// Fields 1-4 indexed together:
		private Searcher Single()
		{
			Directory dir = new MockRAMDirectory();
			IndexWriter w = new IndexWriter(dir, new StandardAnalyzer(Util.Version.LUCENE_CURRENT), true, IndexWriter.MaxFieldLength.LIMITED);
			Document d1 = new Document();
			d1.Add(new Field("f1", "v1", Field.Store.YES, Field.Index.ANALYZED));
			d1.Add(new Field("f2", "v1", Field.Store.YES, Field.Index.ANALYZED));
			d1.Add(new Field("f3", "v1", Field.Store.YES, Field.Index.ANALYZED));
			d1.Add(new Field("f4", "v1", Field.Store.YES, Field.Index.ANALYZED));
			w.AddDocument(d1);
			Document d2 = new Document();
			d2.Add(new Field("f1", "v2", Field.Store.YES, Field.Index.ANALYZED));
			d2.Add(new Field("f2", "v2", Field.Store.YES, Field.Index.ANALYZED));
			d2.Add(new Field("f3", "v2", Field.Store.YES, Field.Index.ANALYZED));
			d2.Add(new Field("f4", "v2", Field.Store.YES, Field.Index.ANALYZED));
			w.AddDocument(d2);
			w.Close();

            return new IndexSearcher(dir, false);
		}
		
		// Fields 1 & 2 in one index, 3 & 4 in other, with ParallelReader:
		private Searcher Parallel()
		{
			Directory dir1 = GetDir1();
			Directory dir2 = GetDir2();
			ParallelReader pr = new ParallelReader();
            pr.Add(IndexReader.Open(dir1, false));
            pr.Add(IndexReader.Open(dir2, false));
			return new IndexSearcher(pr);
		}
		
		private Directory GetDir1()
		{
			Directory dir1 = new MockRAMDirectory();
			IndexWriter w1 = new IndexWriter(dir1, new StandardAnalyzer(Util.Version.LUCENE_CURRENT), true, IndexWriter.MaxFieldLength.LIMITED);
			Document d1 = new Document();
			d1.Add(new Field("f1", "v1", Field.Store.YES, Field.Index.ANALYZED));
			d1.Add(new Field("f2", "v1", Field.Store.YES, Field.Index.ANALYZED));
			w1.AddDocument(d1);
			Document d2 = new Document();
			d2.Add(new Field("f1", "v2", Field.Store.YES, Field.Index.ANALYZED));
			d2.Add(new Field("f2", "v2", Field.Store.YES, Field.Index.ANALYZED));
			w1.AddDocument(d2);
			w1.Close();
			return dir1;
		}
		
		private Directory GetDir2()
		{
			Directory dir2 = new RAMDirectory();
			IndexWriter w2 = new IndexWriter(dir2, new StandardAnalyzer(Util.Version.LUCENE_CURRENT), true, IndexWriter.MaxFieldLength.LIMITED);
			Document d3 = new Document();
			d3.Add(new Field("f3", "v1", Field.Store.YES, Field.Index.ANALYZED));
			d3.Add(new Field("f4", "v1", Field.Store.YES, Field.Index.ANALYZED));
			w2.AddDocument(d3);
			Document d4 = new Document();
			d4.Add(new Field("f3", "v2", Field.Store.YES, Field.Index.ANALYZED));
			d4.Add(new Field("f4", "v2", Field.Store.YES, Field.Index.ANALYZED));
			w2.AddDocument(d4);
			w2.Close();
			return dir2;
		}
	}
}