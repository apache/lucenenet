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

using NUnit.Framework;

using WhitespaceAnalyzer = Lucene.Net.Analysis.WhitespaceAnalyzer;
using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;
using IndexWriter = Lucene.Net.Index.IndexWriter;
using Term = Lucene.Net.Index.Term;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;

namespace Lucene.Net.Search
{
	
	/// <author>  goller
	/// </author>
	[TestFixture]
    public class TestRangeQuery
	{
		
		private int docCount = 0;
		private RAMDirectory dir;
		
		[SetUp]
        public virtual void  SetUp()
		{
			dir = new RAMDirectory();
		}
		
		[Test]
        public virtual void  TestExclusive()
		{
			Query query = new RangeQuery(new Term("content", "A"), new Term("content", "C"), false);
			InitializeIndex(new System.String[]{"A", "B", "C", "D"});
			IndexSearcher searcher = new IndexSearcher(dir);
			Hits hits = searcher.Search(query);
			Assert.AreEqual(1, hits.Length(), "A,B,C,D, only B in range");
			searcher.Close();
			
			InitializeIndex(new System.String[]{"A", "B", "D"});
			searcher = new IndexSearcher(dir);
			hits = searcher.Search(query);
			Assert.AreEqual(1, hits.Length(), "A,B,D, only B in range");
			searcher.Close();
			
			AddDoc("C");
			searcher = new IndexSearcher(dir);
			hits = searcher.Search(query);
			Assert.AreEqual(1, hits.Length(), "C added, still only B in range");
			searcher.Close();
		}
		
		[Test]
        public virtual void  TestInclusive()
		{
			Query query = new RangeQuery(new Term("content", "A"), new Term("content", "C"), true);
			
			InitializeIndex(new System.String[]{"A", "B", "C", "D"});
			IndexSearcher searcher = new IndexSearcher(dir);
			Hits hits = searcher.Search(query);
			Assert.AreEqual(3, hits.Length(), "A,B,C,D - A,B,C in range");
			searcher.Close();
			
			InitializeIndex(new System.String[]{"A", "B", "D"});
			searcher = new IndexSearcher(dir);
			hits = searcher.Search(query);
			Assert.AreEqual(2, hits.Length(), "A,B,D - A and B in range");
			searcher.Close();
			
			AddDoc("C");
			searcher = new IndexSearcher(dir);
			hits = searcher.Search(query);
			Assert.AreEqual(3, hits.Length(), "C added - A, B, C in range");
			searcher.Close();
		}
		
		[Test]
        public virtual void  TestEqualsHashcode()
		{
			Query query = new RangeQuery(new Term("content", "A"), new Term("content", "C"), true);
			query.SetBoost(1.0f);
			Query other = new RangeQuery(new Term("content", "A"), new Term("content", "C"), true);
			other.SetBoost(1.0f);
			
			Assert.AreEqual(query, query, "query equals itself is true");
			Assert.AreEqual(query, other, "equivalent queries are equal");
			Assert.AreEqual(query.GetHashCode(), other.GetHashCode(), "hashcode must return same value when equals is true");
			
			other.SetBoost(2.0f);
			Assert.IsFalse(query.Equals(other), "Different boost queries are not equal");
			
			other = new RangeQuery(new Term("notcontent", "A"), new Term("notcontent", "C"), true);
			Assert.IsFalse(query.Equals(other), "Different fields are not equal");
			
			other = new RangeQuery(new Term("content", "X"), new Term("content", "C"), true);
			Assert.IsFalse(query.Equals(other), "Different lower terms are not equal");
			
			other = new RangeQuery(new Term("content", "A"), new Term("content", "Z"), true);
			Assert.IsFalse(query.Equals(other), "Different upper terms are not equal");
			
			query = new RangeQuery(null, new Term("content", "C"), true);
			other = new RangeQuery(null, new Term("content", "C"), true);
			Assert.AreEqual(query, other, "equivalent queries with null lowerterms are equal()");
			Assert.AreEqual(query.GetHashCode(), other.GetHashCode(), "hashcode must return same value when equals is true");
			
			query = new RangeQuery(new Term("content", "C"), null, true);
			other = new RangeQuery(new Term("content", "C"), null, true);
			Assert.AreEqual(query, other, "equivalent queries with null upperterms are equal()");
			Assert.AreEqual(query.GetHashCode(), other.GetHashCode(), "hashcode returns same value");
			
			query = new RangeQuery(null, new Term("content", "C"), true);
			other = new RangeQuery(new Term("content", "C"), null, true);
			Assert.IsFalse(query.Equals(other), "queries with different upper and lower terms are not equal");
			
			query = new RangeQuery(new Term("content", "A"), new Term("content", "C"), false);
			other = new RangeQuery(new Term("content", "A"), new Term("content", "C"), true);
			Assert.IsFalse(query.Equals(other), "queries with different inclusive are not equal");
		}
		
		private void  InitializeIndex(System.String[] values)
		{
			IndexWriter writer = new IndexWriter(dir, new WhitespaceAnalyzer(), true);
			for (int i = 0; i < values.Length; i++)
			{
				InsertDoc(writer, values[i]);
			}
			writer.Close();
		}
		
		private void  AddDoc(System.String content)
		{
			IndexWriter writer = new IndexWriter(dir, new WhitespaceAnalyzer(), false);
			InsertDoc(writer, content);
			writer.Close();
		}
		
		private void  InsertDoc(IndexWriter writer, System.String content)
		{
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document();
			
			doc.Add(new Field("id", "id" + docCount, Field.Store.YES, Field.Index.UN_TOKENIZED));
			doc.Add(new Field("content", content, Field.Store.NO, Field.Index.TOKENIZED));
			
			writer.AddDocument(doc);
			docCount++;
		}
	}
}