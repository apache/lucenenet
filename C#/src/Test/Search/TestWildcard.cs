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
using SimpleAnalyzer = Lucene.Net.Analysis.SimpleAnalyzer;
using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;
using IndexWriter = Lucene.Net.Index.IndexWriter;
using Term = Lucene.Net.Index.Term;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;

namespace Lucene.Net.Search
{
	
	/// <summary> TestWildcard tests the '*' and '?' wildcard characters.
	/// 
	/// </summary>
	/// <version>  $Id: TestWildcard.java 329860 2005-10-31 17:06:29Z bmesser $
	/// </version>
	/// <author>  Otis Gospodnetic
	/// </author>
	[TestFixture]
    public class TestWildcard
	{
		[Test]
        public virtual void  TestEquals()
		{
			WildcardQuery wq1 = new WildcardQuery(new Term("field", "b*a"));
			WildcardQuery wq2 = new WildcardQuery(new Term("field", "b*a"));
			WildcardQuery wq3 = new WildcardQuery(new Term("field", "b*a"));
			
			// reflexive?
			Assert.AreEqual(wq1, wq2);
			Assert.AreEqual(wq2, wq1);
			
			// transitive?
			Assert.AreEqual(wq2, wq3);
			Assert.AreEqual(wq1, wq3);
			
			Assert.IsFalse(wq1.Equals(null));
			
			FuzzyQuery fq = new FuzzyQuery(new Term("field", "b*a"));
			Assert.IsFalse(wq1.Equals(fq));
			Assert.IsFalse(fq.Equals(wq1));
		}
		
		/// <summary> Tests Wildcard queries with an asterisk.</summary>
		[Test]
        public virtual void  TestAsterisk()
		{
			RAMDirectory indexStore = GetIndexStore("body", new System.String[]{"metal", "metals"});
			IndexSearcher searcher = new IndexSearcher(indexStore);
			Query query1 = new TermQuery(new Term("body", "metal"));
			Query query2 = new WildcardQuery(new Term("body", "metal*"));
			Query query3 = new WildcardQuery(new Term("body", "m*tal"));
			Query query4 = new WildcardQuery(new Term("body", "m*tal*"));
			Query query5 = new WildcardQuery(new Term("body", "m*tals"));
			
			BooleanQuery query6 = new BooleanQuery();
			query6.Add(query5, BooleanClause.Occur.SHOULD);
			
			BooleanQuery query7 = new BooleanQuery();
			query7.Add(query3, BooleanClause.Occur.SHOULD);
			query7.Add(query5, BooleanClause.Occur.SHOULD);
			
			// Queries do not automatically lower-case search terms:
			Query query8 = new WildcardQuery(new Term("body", "M*tal*"));
			
			AssertMatches(searcher, query1, 1);
			AssertMatches(searcher, query2, 2);
			AssertMatches(searcher, query3, 1);
			AssertMatches(searcher, query4, 2);
			AssertMatches(searcher, query5, 1);
			AssertMatches(searcher, query6, 1);
			AssertMatches(searcher, query7, 2);
			AssertMatches(searcher, query8, 0);
			AssertMatches(searcher, new WildcardQuery(new Term("body", "*tall")), 0);
			AssertMatches(searcher, new WildcardQuery(new Term("body", "*tal")), 1);
			AssertMatches(searcher, new WildcardQuery(new Term("body", "*tal*")), 2);
		}
		
		/// <summary> Tests Wildcard queries with a question mark.
		/// 
		/// </summary>
		/// <throws>  IOException if an error occurs </throws>
		[Test]
        public virtual void  TestQuestionmark()
		{
			RAMDirectory indexStore = GetIndexStore("body", new System.String[]{"metal", "metals", "mXtals", "mXtXls"});
			IndexSearcher searcher = new IndexSearcher(indexStore);
			Query query1 = new WildcardQuery(new Term("body", "m?tal"));
			Query query2 = new WildcardQuery(new Term("body", "metal?"));
			Query query3 = new WildcardQuery(new Term("body", "metals?"));
			Query query4 = new WildcardQuery(new Term("body", "m?t?ls"));
			Query query5 = new WildcardQuery(new Term("body", "M?t?ls"));
			Query query6 = new WildcardQuery(new Term("body", "meta??"));
			
			AssertMatches(searcher, query1, 1);
			AssertMatches(searcher, query2, 1);
			AssertMatches(searcher, query3, 0);
			AssertMatches(searcher, query4, 3);
			AssertMatches(searcher, query5, 0);
			AssertMatches(searcher, query6, 1); // Query: 'meta??' matches 'metals' not 'metal'
		}
		
		private RAMDirectory GetIndexStore(System.String field, System.String[] contents)
		{
			RAMDirectory indexStore = new RAMDirectory();
			IndexWriter writer = new IndexWriter(indexStore, new SimpleAnalyzer(), true);
			for (int i = 0; i < contents.Length; ++i)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document();
				doc.Add(new Field(field, contents[i], Field.Store.YES, Field.Index.TOKENIZED));
				writer.AddDocument(doc);
			}
			writer.Optimize();
			writer.Close();
			
			return indexStore;
		}
		
		private void  AssertMatches(IndexSearcher searcher, Query q, int expectedMatches)
		{
			Hits result = searcher.Search(q);
			Assert.AreEqual(expectedMatches, result.Length());
		}
	}
}