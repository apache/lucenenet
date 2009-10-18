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

using RAMDirectory = Lucene.Net.Store.RAMDirectory;
using IndexWriter = Lucene.Net.Index.IndexWriter;
using Term = Lucene.Net.Index.Term;
using WhitespaceAnalyzer = Lucene.Net.Analysis.WhitespaceAnalyzer;
using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;

namespace Lucene.Net.Search
{
	
	/// <summary> Tests {@link PrefixFilter} class.
	/// 
	/// </summary>
	/// <author>  Yura Smolsky
	/// </author>
	/// <author>  yonik
	/// </author>
    [TestFixture]
    public class TestPrefixFilter
	{
        [Test]
		public virtual void  _TestPrefixFilter()
		{
			RAMDirectory directory = new RAMDirectory();
			
			System.String[] categories = new System.String[]{"/Computers/Linux", "/Computers/Mac/One", "/Computers/Mac/Two", "/Computers/Windows"};
			IndexWriter writer = new IndexWriter(directory, new WhitespaceAnalyzer(), true);
			for (int i = 0; i < categories.Length; i++)
			{
				Document doc = new Document();
				doc.Add(new Field("category", categories[i], Field.Store.YES, Field.Index.UN_TOKENIZED));
				writer.AddDocument(doc);
			}
			writer.Close();
			
			// PrefixFilter combined with ConstantScoreQuery
			PrefixFilter filter = new PrefixFilter(new Term("category", "/Computers"));
			Query query = new ConstantScoreQuery(filter);
			IndexSearcher searcher = new IndexSearcher(directory);
			Hits hits = searcher.Search(query);
			Assert.AreEqual(4, hits.Length());
			
			// test middle of values
			filter = new PrefixFilter(new Term("category", "/Computers/Mac"));
			query = new ConstantScoreQuery(filter);
			hits = searcher.Search(query);
			Assert.AreEqual(2, hits.Length());
			
			// test start of values
			filter = new PrefixFilter(new Term("category", "/Computers/Linux"));
			query = new ConstantScoreQuery(filter);
			hits = searcher.Search(query);
			Assert.AreEqual(1, hits.Length());
			
			// test end of values
			filter = new PrefixFilter(new Term("category", "/Computers/Windows"));
			query = new ConstantScoreQuery(filter);
			hits = searcher.Search(query);
			Assert.AreEqual(1, hits.Length());
			
			// test non-existant
			filter = new PrefixFilter(new Term("category", "/Computers/ObsoleteOS"));
			query = new ConstantScoreQuery(filter);
			hits = searcher.Search(query);
			Assert.AreEqual(0, hits.Length());
			
			// test non-existant, before values
			filter = new PrefixFilter(new Term("category", "/Computers/AAA"));
			query = new ConstantScoreQuery(filter);
			hits = searcher.Search(query);
			Assert.AreEqual(0, hits.Length());
			
			// test non-existant, after values
			filter = new PrefixFilter(new Term("category", "/Computers/ZZZ"));
			query = new ConstantScoreQuery(filter);
			hits = searcher.Search(query);
			Assert.AreEqual(0, hits.Length());
			
			// test zero length prefix
			filter = new PrefixFilter(new Term("category", ""));
			query = new ConstantScoreQuery(filter);
			hits = searcher.Search(query);
			Assert.AreEqual(4, hits.Length());
			
			// test non existent field
			filter = new PrefixFilter(new Term("nonexistantfield", "/Computers"));
			query = new ConstantScoreQuery(filter);
			hits = searcher.Search(query);
			Assert.AreEqual(0, hits.Length());
		}
	}
}