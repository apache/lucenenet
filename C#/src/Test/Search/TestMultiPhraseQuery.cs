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
using SimpleAnalyzer = Lucene.Net.Analysis.SimpleAnalyzer;
using StandardAnalyzer = Lucene.Net.Analysis.Standard.StandardAnalyzer;
using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;
using IndexReader = Lucene.Net.Index.IndexReader;
using IndexWriter = Lucene.Net.Index.IndexWriter;
using Term = Lucene.Net.Index.Term;
using TermEnum = Lucene.Net.Index.TermEnum;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;
using NUnit.Framework;

namespace Lucene.Net.Search
{
	
	/// <summary> This class tests the MultiPhraseQuery class.
	/// 
	/// </summary>
	/// <author>  Otis Gospodnetic, Daniel Naber
	/// </author>
	/// <version>  $Id: TestMultiPhraseQuery.java 219387 2005-07-17 10:47:14Z dnaber $
	/// </version>
	[TestFixture]
    public class TestMultiPhraseQuery
	{
		
		[Test]
        public virtual void  TestPhrasePrefix()
		{
			RAMDirectory indexStore = new RAMDirectory();
			IndexWriter writer = new IndexWriter(indexStore, new SimpleAnalyzer(), true);
			Add("blueberry pie", writer);
			Add("blueberry strudel", writer);
			Add("blueberry pizza", writer);
			Add("blueberry chewing gum", writer);
			Add("bluebird pizza", writer);
			Add("bluebird foobar pizza", writer);
			Add("piccadilly circus", writer);
			writer.Optimize();
			writer.Close();
			
			IndexSearcher searcher = new IndexSearcher(indexStore);
			
			// search for "blueberry pi*":
			MultiPhraseQuery query1 = new MultiPhraseQuery();
			// search for "strawberry pi*":
			MultiPhraseQuery query2 = new MultiPhraseQuery();
			query1.Add(new Term("body", "blueberry"));
			query2.Add(new Term("body", "strawberry"));
			
			System.Collections.ArrayList termsWithPrefix = new System.Collections.ArrayList();
			IndexReader ir = IndexReader.Open(indexStore);
			
			// this TermEnum gives "piccadilly", "pie" and "pizza".
			System.String prefix = "pi";
			TermEnum te = ir.Terms(new Term("body", prefix));
			do 
			{
				if (te.Term().Text().StartsWith(prefix))
				{
					termsWithPrefix.Add(te.Term());
				}
			}
			while (te.Next());

			query1.Add((Term[]) termsWithPrefix.ToArray(typeof(Term)));
			Assert.AreEqual("body:\"blueberry (piccadilly pie pizza)\"", query1.ToString());
			query2.Add((Term[]) termsWithPrefix.ToArray(typeof(Term)));
			Assert.AreEqual("body:\"strawberry (piccadilly pie pizza)\"", query2.ToString());
			
			Hits result;
			result = searcher.Search(query1);
			Assert.AreEqual(2, result.Length());
			result = searcher.Search(query2);
			Assert.AreEqual(0, result.Length());
			
			// search for "blue* pizza":
			MultiPhraseQuery query3 = new MultiPhraseQuery();
			termsWithPrefix.Clear();
			prefix = "blue";
			te = ir.Terms(new Term("body", prefix));
			do 
			{
				if (te.Term().Text().StartsWith(prefix))
				{
					termsWithPrefix.Add(te.Term());
				}
			}
			while (te.Next());
			query3.Add((Term[]) termsWithPrefix.ToArray(typeof(Term)));
			query3.Add(new Term("body", "pizza"));
			
			result = searcher.Search(query3);
			Assert.AreEqual(2, result.Length()); // blueberry pizza, bluebird pizza
			Assert.AreEqual("body:\"(blueberry bluebird) pizza\"", query3.ToString());
			
			// test slop:
			query3.SetSlop(1);
			result = searcher.Search(query3);
			Assert.AreEqual(3, result.Length()); // blueberry pizza, bluebird pizza, bluebird foobar pizza
			
			MultiPhraseQuery query4 = new MultiPhraseQuery();
			try
			{
				query4.Add(new Term("field1", "foo"));
				query4.Add(new Term("field2", "foobar"));
				Assert.Fail();
			}
			catch (System.ArgumentException e)
			{
				// okay, all terms must belong to the same field
			}
			
			searcher.Close();
			indexStore.Close();
		}
		
		private void  Add(System.String s, IndexWriter writer)
		{
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document();
			doc.Add(new Field("body", s, Field.Store.YES, Field.Index.TOKENIZED));
			writer.AddDocument(doc);
		}
		
		[Test]
        public virtual void  TestBooleanQueryContainingSingleTermPrefixQuery()
		{
			// this tests against bug 33161 (now fixed)
			// In order to cause the bug, the outer query must have more than one term 
			// and all terms required.
			// The contained PhraseMultiQuery must contain exactly one term array.
			
			RAMDirectory indexStore = new RAMDirectory();
			IndexWriter writer = new IndexWriter(indexStore, new SimpleAnalyzer(), true);
			Add("blueberry pie", writer);
			Add("blueberry chewing gum", writer);
			Add("blue raspberry pie", writer);
			writer.Optimize();
			writer.Close();
			
			IndexSearcher searcher = new IndexSearcher(indexStore);
			// This query will be equivalent to +body:pie +body:"blue*"
			BooleanQuery q = new BooleanQuery();
			q.Add(new TermQuery(new Term("body", "pie")), BooleanClause.Occur.MUST);
			
			MultiPhraseQuery trouble = new MultiPhraseQuery();
			trouble.Add(new Term[]{new Term("body", "blueberry"), new Term("body", "blue")});
			q.Add(trouble, BooleanClause.Occur.MUST);
			
			// exception will be thrown here without fix
			Hits hits = searcher.Search(q);
			
			Assert.AreEqual(2, hits.Length(), "Wrong number of hits");
			searcher.Close();
		}
		
		[Test]
        public virtual void  TestPhrasePrefixWithBooleanQuery()
		{
			RAMDirectory indexStore = new RAMDirectory();
			IndexWriter writer = new IndexWriter(indexStore, new StandardAnalyzer(new System.String[]{}), true);
			Add("This is a test", "object", writer);
			Add("a note", "note", writer);
			writer.Close();
			
			IndexSearcher searcher = new IndexSearcher(indexStore);
			
			// This query will be equivalent to +type:note +body:"a t*"
			BooleanQuery q = new BooleanQuery();
			q.Add(new TermQuery(new Term("type", "note")), BooleanClause.Occur.MUST);
			
			MultiPhraseQuery trouble = new MultiPhraseQuery();
			trouble.Add(new Term("body", "a"));
			trouble.Add(new Term[]{new Term("body", "test"), new Term("body", "this")});
			q.Add(trouble, BooleanClause.Occur.MUST);
			
			// exception will be thrown here without fix for #35626:
			Hits hits = searcher.Search(q);
			Assert.AreEqual(0, hits.Length(), "Wrong number of hits");
			searcher.Close();
		}
		
		private void  Add(System.String s, System.String type, IndexWriter writer)
		{
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document();
			doc.Add(new Field("body", s, Field.Store.YES, Field.Index.TOKENIZED));
			doc.Add(new Field("type", type, Field.Store.YES, Field.Index.UN_TOKENIZED));
			writer.AddDocument(doc);
		}
	}
}