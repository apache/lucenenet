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
using StandardAnalyzer = Lucene.Net.Analysis.Standard.StandardAnalyzer;
using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;
using Lucene.Net.Search;
using Searchable = Lucene.Net.Search.Searchable;
using Occur = Lucene.Net.Search.BooleanClause.Occur;
using Directory = Lucene.Net.Store.Directory;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;

namespace Lucene.Net.Index
{
	[TestFixture]
	public class TestParallelReader
	{
		
		private Searcher parallel;
		private Searcher single;
		
		[SetUp]
        public virtual void  SetUp()
		{
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
			pr.Add(IndexReader.Open(dir1));
			pr.Add(IndexReader.Open(dir2));
			System.Collections.ICollection fieldNames = pr.GetFieldNames(IndexReader.FieldOption.ALL);
			Assert.AreEqual(4, fieldNames.Count);
			Assert.IsTrue(CollectionContains(fieldNames, "f1"));
			Assert.IsTrue(CollectionContains(fieldNames, "f2"));
			Assert.IsTrue(CollectionContains(fieldNames, "f3"));
			Assert.IsTrue(CollectionContains(fieldNames, "f4"));
		}

        public static bool CollectionContains(System.Collections.ICollection col, System.String val)
        {
            for (System.Collections.IEnumerator iterator = col.GetEnumerator(); iterator.MoveNext(); )
            {
                System.Collections.DictionaryEntry fi = (System.Collections.DictionaryEntry) iterator.Current;
                System.String s = fi.Key.ToString();
                if (s == val)
                    return true;
            }
            return false;
        }
		
		[Test]
        public virtual void  TestIncompatibleIndexes()
		{
			// two documents:
			Directory dir1 = GetDir1();
			
			// one document only:
			Directory dir2 = new RAMDirectory();
			IndexWriter w2 = new IndexWriter(dir2, new StandardAnalyzer(), true);
			Lucene.Net.Documents.Document d3 = new Lucene.Net.Documents.Document();
			d3.Add(new Field("f3", "v1", Field.Store.YES, Field.Index.TOKENIZED));
			w2.AddDocument(d3);
			w2.Close();
			
			ParallelReader pr = new ParallelReader();
			pr.Add(IndexReader.Open(dir1));
			try
			{
				pr.Add(IndexReader.Open(dir2));
				Assert.Fail("didn't get exptected exception: indexes don't have same number of documents");
			}
			catch (System.ArgumentException e)
			{
				// expected exception
			}
		}
		
		private void  QueryTest(Query query)
		{
			Hits parallelHits = parallel.Search(query);
			Hits singleHits = single.Search(query);
			Assert.AreEqual(parallelHits.Length(), singleHits.Length());
			for (int i = 0; i < parallelHits.Length(); i++)
			{
				Assert.AreEqual(parallelHits.Score(i), singleHits.Score(i), 0.001f);
				Lucene.Net.Documents.Document docParallel = parallelHits.Doc(i);
				Lucene.Net.Documents.Document docSingle = singleHits.Doc(i);
				Assert.AreEqual(docParallel.Get("f1"), docSingle.Get("f1"));
				Assert.AreEqual(docParallel.Get("f2"), docSingle.Get("f2"));
				Assert.AreEqual(docParallel.Get("f3"), docSingle.Get("f3"));
				Assert.AreEqual(docParallel.Get("f4"), docSingle.Get("f4"));
			}
		}
		
		// Fiels 1-4 indexed together:
		private Searcher Single()
		{
			Directory dir = new RAMDirectory();
			IndexWriter w = new IndexWriter(dir, new StandardAnalyzer(), true);
			Lucene.Net.Documents.Document d1 = new Lucene.Net.Documents.Document();
			d1.Add(new Field("f1", "v1", Field.Store.YES, Field.Index.TOKENIZED));
			d1.Add(new Field("f2", "v1", Field.Store.YES, Field.Index.TOKENIZED));
			d1.Add(new Field("f3", "v1", Field.Store.YES, Field.Index.TOKENIZED));
			d1.Add(new Field("f4", "v1", Field.Store.YES, Field.Index.TOKENIZED));
			w.AddDocument(d1);
			Lucene.Net.Documents.Document d2 = new Lucene.Net.Documents.Document();
			d2.Add(new Field("f1", "v2", Field.Store.YES, Field.Index.TOKENIZED));
			d2.Add(new Field("f2", "v2", Field.Store.YES, Field.Index.TOKENIZED));
			d2.Add(new Field("f3", "v2", Field.Store.YES, Field.Index.TOKENIZED));
			d2.Add(new Field("f4", "v2", Field.Store.YES, Field.Index.TOKENIZED));
			w.AddDocument(d2);
			w.Close();
			
			return new IndexSearcher(dir);
		}
		
		// Fields 1 & 2 in one index, 3 & 4 in other, with ParallelReader:
		private Searcher Parallel()
		{
			Directory dir1 = GetDir1();
			Directory dir2 = GetDir2();
			ParallelReader pr = new ParallelReader();
			pr.Add(IndexReader.Open(dir1));
			pr.Add(IndexReader.Open(dir2));
			return new IndexSearcher(pr);
		}
		
		private Directory GetDir1()
		{
			Directory dir1 = new RAMDirectory();
			IndexWriter w1 = new IndexWriter(dir1, new StandardAnalyzer(), true);
			Lucene.Net.Documents.Document d1 = new Lucene.Net.Documents.Document();
			d1.Add(new Field("f1", "v1", Field.Store.YES, Field.Index.TOKENIZED));
			d1.Add(new Field("f2", "v1", Field.Store.YES, Field.Index.TOKENIZED));
			w1.AddDocument(d1);
			Lucene.Net.Documents.Document d2 = new Lucene.Net.Documents.Document();
			d2.Add(new Field("f1", "v2", Field.Store.YES, Field.Index.TOKENIZED));
			d2.Add(new Field("f2", "v2", Field.Store.YES, Field.Index.TOKENIZED));
			w1.AddDocument(d2);
			w1.Close();
			return dir1;
		}
		
		private Directory GetDir2()
		{
			Directory dir2 = new RAMDirectory();
			IndexWriter w2 = new IndexWriter(dir2, new StandardAnalyzer(), true);
			Lucene.Net.Documents.Document d3 = new Lucene.Net.Documents.Document();
			d3.Add(new Field("f3", "v1", Field.Store.YES, Field.Index.TOKENIZED));
			d3.Add(new Field("f4", "v1", Field.Store.YES, Field.Index.TOKENIZED));
			w2.AddDocument(d3);
			Lucene.Net.Documents.Document d4 = new Lucene.Net.Documents.Document();
			d4.Add(new Field("f3", "v2", Field.Store.YES, Field.Index.TOKENIZED));
			d4.Add(new Field("f4", "v2", Field.Store.YES, Field.Index.TOKENIZED));
			w2.AddDocument(d4);
			w2.Close();
			return dir2;
		}
	}
}