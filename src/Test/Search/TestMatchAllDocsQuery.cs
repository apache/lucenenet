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

using StandardAnalyzer = Lucene.Net.Analysis.Standard.StandardAnalyzer;
using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;
using IndexWriter = Lucene.Net.Index.IndexWriter;
using Term = Lucene.Net.Index.Term;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;

namespace Lucene.Net.Search
{
	
	/// <summary> Tests MatchAllDocsQuery.
	/// 
	/// </summary>
	/// <author>  Daniel Naber
	/// </author>
	[TestFixture]
    public class TestMatchAllDocsQuery
	{
		[Test]
		public virtual void  TestQuery()
		{
			RAMDirectory dir = new RAMDirectory();
			IndexWriter iw = new IndexWriter(dir, new StandardAnalyzer(), true);
			AddDoc("one", iw);
			AddDoc("two", iw);
			AddDoc("three four", iw);
			iw.Close();
			
			IndexSearcher is_Renamed = new IndexSearcher(dir);
			Hits hits = is_Renamed.Search(new MatchAllDocsQuery());
			Assert.AreEqual(3, hits.Length());
			
			// some artificial queries to trigger the use of skipTo():
			
			BooleanQuery bq = new BooleanQuery();
			bq.Add(new MatchAllDocsQuery(), BooleanClause.Occur.MUST);
			bq.Add(new MatchAllDocsQuery(), BooleanClause.Occur.MUST);
			hits = is_Renamed.Search(bq);
			Assert.AreEqual(3, hits.Length());
			
			bq = new BooleanQuery();
			bq.Add(new MatchAllDocsQuery(), BooleanClause.Occur.MUST);
			bq.Add(new TermQuery(new Term("key", "three")), BooleanClause.Occur.MUST);
			hits = is_Renamed.Search(bq);
			Assert.AreEqual(1, hits.Length());
			
			// delete a document:
			is_Renamed.GetIndexReader().DeleteDocument(0);
			hits = is_Renamed.Search(new MatchAllDocsQuery());
			Assert.AreEqual(2, hits.Length());
			
			is_Renamed.Close();
		}
		
		[Test]
        public virtual void  TestEquals()
		{
			Query q1 = new MatchAllDocsQuery();
			Query q2 = new MatchAllDocsQuery();
			Assert.IsTrue(q1.Equals(q2));
			q1.SetBoost(1.5f);
			Assert.IsFalse(q1.Equals(q2));
		}
		
		private void  AddDoc(System.String text, IndexWriter iw)
		{
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document();
			doc.Add(new Field("key", text, Field.Store.YES, Field.Index.TOKENIZED));
			iw.AddDocument(doc);
		}
	}
}