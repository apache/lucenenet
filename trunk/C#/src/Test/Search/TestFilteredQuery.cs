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
using IndexReader = Lucene.Net.Index.IndexReader;
using IndexWriter = Lucene.Net.Index.IndexWriter;
using Term = Lucene.Net.Index.Term;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;

namespace Lucene.Net.Search
{
	
	/// <summary> FilteredQuery JUnit tests.
	/// 
	/// <p>Created: Apr 21, 2004 1:21:46 PM
	/// 
	/// </summary>
	/// <author>   Tim Jones
	/// </author>
	/// <version>  $Id: TestFilteredQuery.java 150585 2004-10-10 15:44:45Z dnaber $
	/// </version>
	/// <since>   1.4
	/// </since>
	[TestFixture]
    public class TestFilteredQuery
	{
		//UPGRADE_NOTE: Field 'EnclosingInstance' was added to class 'AnonymousClassFilter' to access its enclosing instance. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1019'"
		[Serializable]
		private class AnonymousClassFilter : Filter
		{
			public AnonymousClassFilter(TestFilteredQuery enclosingInstance)
			{
				InitBlock(enclosingInstance);
			}
			private void  InitBlock(TestFilteredQuery enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private TestFilteredQuery enclosingInstance;
			public TestFilteredQuery Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			public override System.Collections.BitArray Bits(IndexReader reader)
			{
				System.Collections.BitArray bitset = new System.Collections.BitArray((5 % 64 == 0?5 / 64:5 / 64 + 1) * 64);
				bitset.Set(1, true);
				bitset.Set(3, true);
				return bitset;
			}
		}
		
		private IndexSearcher searcher;
		private RAMDirectory directory;
		private Query query;
		private Filter filter;
		
		[SetUp]
        public virtual void  SetUp()
		{
			directory = new RAMDirectory();
			IndexWriter writer = new IndexWriter(directory, new WhitespaceAnalyzer(), true);
			
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document();
			doc.Add(new Field("field", "one two three four five", Field.Store.YES, Field.Index.TOKENIZED));
			doc.Add(new Field("sorter", "b", Field.Store.YES, Field.Index.TOKENIZED));
			writer.AddDocument(doc);
			
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new Field("field", "one two three four", Field.Store.YES, Field.Index.TOKENIZED));
			doc.Add(new Field("sorter", "d", Field.Store.YES, Field.Index.TOKENIZED));
			writer.AddDocument(doc);
			
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new Field("field", "one two three y", Field.Store.YES, Field.Index.TOKENIZED));
			doc.Add(new Field("sorter", "a", Field.Store.YES, Field.Index.TOKENIZED));
			writer.AddDocument(doc);
			
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new Field("field", "one two x", Field.Store.YES, Field.Index.TOKENIZED));
			doc.Add(new Field("sorter", "c", Field.Store.YES, Field.Index.TOKENIZED));
			writer.AddDocument(doc);
			
			writer.Optimize();
			writer.Close();
			
			searcher = new IndexSearcher(directory);
			query = new TermQuery(new Term("field", "three"));
			filter = new AnonymousClassFilter(this);
		}
		
		[TearDown]
        public virtual void  TearDown()
		{
			searcher.Close();
			directory.Close();
		}
		
		[Test]
        public virtual void  TestFilteredQuery_Renamed_Method()
		{
			Query filteredquery = new FilteredQuery(query, filter);
			Hits hits = searcher.Search(filteredquery);
			Assert.AreEqual(1, hits.Length());
			Assert.AreEqual(1, hits.Id(0));
			
			hits = searcher.Search(filteredquery, new Sort("sorter"));
			Assert.AreEqual(1, hits.Length());
			Assert.AreEqual(1, hits.Id(0));
			
			filteredquery = new FilteredQuery(new TermQuery(new Term("field", "one")), filter);
			hits = searcher.Search(filteredquery);
			Assert.AreEqual(2, hits.Length());
			
			filteredquery = new FilteredQuery(new TermQuery(new Term("field", "x")), filter);
			hits = searcher.Search(filteredquery);
			Assert.AreEqual(1, hits.Length());
			Assert.AreEqual(3, hits.Id(0));
			
			filteredquery = new FilteredQuery(new TermQuery(new Term("field", "y")), filter);
			hits = searcher.Search(filteredquery);
			Assert.AreEqual(0, hits.Length());
		}
		
		/// <summary> This tests FilteredQuery's rewrite correctness</summary>
		[Test]
        public virtual void  TestRangeQuery()
		{
			RangeQuery rq = new RangeQuery(new Term("sorter", "b"), new Term("sorter", "d"), true);
			
			Query filteredquery = new FilteredQuery(rq, filter);
			Hits hits = searcher.Search(filteredquery);
			Assert.AreEqual(2, hits.Length());
		}
	}
}