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

using SimpleAnalyzer = Lucene.Net.Analysis.SimpleAnalyzer;
using DateTools = Lucene.Net.Documents.DateTools;
using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;
using IndexWriter = Lucene.Net.Index.IndexWriter;
using Term = Lucene.Net.Index.Term;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;

namespace Lucene.Net.Search
{
	
	/// <summary> DateFilter JUnit tests.
	/// 
	/// </summary>
	/// <author>  Otis Gospodnetic
	/// </author>
    /// <version>  $Revision: 472959 $
    /// </version>
	[TestFixture]
    public class TestDateFilter
	{
		
		/// <summary> </summary>
		[Test]
        public virtual void  TestBefore()
		{
			// create an index
			RAMDirectory indexStore = new RAMDirectory();
			IndexWriter writer = new IndexWriter(indexStore, new SimpleAnalyzer(), true);
			
			long now = System.DateTime.Now.Millisecond;
			
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document();
			// add time that is in the past
            doc.Add(new Field("datefield", Lucene.Net.Documents.DateTools.TimeToString(now - 1000 * 100000, Lucene.Net.Documents.DateTools.Resolution.MILLISECOND), Field.Store.YES, Field.Index.UN_TOKENIZED));
            doc.Add(new Field("body", "Today is a very sunny day in New York City", Field.Store.YES, Field.Index.TOKENIZED));
			writer.AddDocument(doc);
			writer.Optimize();
			writer.Close();
			
			IndexSearcher searcher = new IndexSearcher(indexStore);
			
			// filter that should preserve matches
			//DateFilter df1 = DateFilter.Before("datefield", now);
            RangeFilter df1 = new RangeFilter("datefield", Lucene.Net.Documents.DateTools.TimeToString(now - 2000 * 100000, Lucene.Net.Documents.DateTools.Resolution.MILLISECOND), Lucene.Net.Documents.DateTools.TimeToString(now, Lucene.Net.Documents.DateTools.Resolution.MILLISECOND), false, true);
            // filter that should discard matches
			//DateFilter df2 = DateFilter.Before("datefield", now - 999999);
            RangeFilter df2 = new RangeFilter("datefield", Lucene.Net.Documents.DateTools.TimeToString(0, Lucene.Net.Documents.DateTools.Resolution.MILLISECOND), Lucene.Net.Documents.DateTools.TimeToString(now - 2000 * 100000, Lucene.Net.Documents.DateTools.Resolution.MILLISECOND), true, false);
			
			// search something that doesn't exist with DateFilter
			Query query1 = new TermQuery(new Term("body", "NoMatchForThis"));
			
			// search for something that does exists
			Query query2 = new TermQuery(new Term("body", "sunny"));
			
			Hits result;
			
			// ensure that queries return expected results without DateFilter first
			result = searcher.Search(query1);
			Assert.AreEqual(0, result.Length());
			
			result = searcher.Search(query2);
			Assert.AreEqual(1, result.Length());
			
			
			// run queries with DateFilter
			result = searcher.Search(query1, df1);
			Assert.AreEqual(0, result.Length());
			
			result = searcher.Search(query1, df2);
			Assert.AreEqual(0, result.Length());
			
			result = searcher.Search(query2, df1);
			Assert.AreEqual(1, result.Length());
			
			result = searcher.Search(query2, df2);
			Assert.AreEqual(0, result.Length());
		}
		
		/// <summary> </summary>
		[Test]
        public virtual void  TestAfter()
		{
			// create an index
			RAMDirectory indexStore = new RAMDirectory();
			IndexWriter writer = new IndexWriter(indexStore, new SimpleAnalyzer(), true);
			
			long now = System.DateTime.Now.Millisecond;
			
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document();
			// add time that is in the future
            doc.Add(new Field("datefield", Lucene.Net.Documents.DateTools.TimeToString(now + 888888, Lucene.Net.Documents.DateTools.Resolution.MILLISECOND), Field.Store.YES, Field.Index.UN_TOKENIZED));
            doc.Add(new Field("body", "Today is a very sunny day in New York City", Field.Store.YES, Field.Index.TOKENIZED));
			writer.AddDocument(doc);
			writer.Optimize();
			writer.Close();
			
			IndexSearcher searcher = new IndexSearcher(indexStore);
			
			// filter that should preserve matches
			//DateFilter df1 = DateFilter.After("datefield", now);
            RangeFilter df1 = new RangeFilter("datefield", Lucene.Net.Documents.DateTools.TimeToString(now, Lucene.Net.Documents.DateTools.Resolution.MILLISECOND), Lucene.Net.Documents.DateTools.TimeToString(now + 999999, Lucene.Net.Documents.DateTools.Resolution.MILLISECOND), true, false);
            // filter that should discard matches
			//DateFilter df2 = DateFilter.After("datefield", now + 999999);
            RangeFilter df2 = new RangeFilter("datefield", Lucene.Net.Documents.DateTools.TimeToString(now + 999999, Lucene.Net.Documents.DateTools.Resolution.MILLISECOND), Lucene.Net.Documents.DateTools.TimeToString(now + 999999999, Lucene.Net.Documents.DateTools.Resolution.MILLISECOND), false, true);
			
			// search something that doesn't exist with DateFilter
			Query query1 = new TermQuery(new Term("body", "NoMatchForThis"));
			
			// search for something that does exists
			Query query2 = new TermQuery(new Term("body", "sunny"));
			
			Hits result;
			
			// ensure that queries return expected results without DateFilter first
			result = searcher.Search(query1);
			Assert.AreEqual(0, result.Length());
			
			result = searcher.Search(query2);
			Assert.AreEqual(1, result.Length());
			
			
			// run queries with DateFilter
			result = searcher.Search(query1, df1);
			Assert.AreEqual(0, result.Length());
			
			result = searcher.Search(query1, df2);
			Assert.AreEqual(0, result.Length());
			
			result = searcher.Search(query2, df1);
			Assert.AreEqual(1, result.Length());
			
			result = searcher.Search(query2, df2);
			Assert.AreEqual(0, result.Length());
		}
	}
}