/*
 * Copyright 2005 The Apache Software Foundation
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
using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;
using IndexWriter = Lucene.Net.Index.IndexWriter;
using QueryParser = Lucene.Net.QueryParsers.QueryParser;
using Hits = Lucene.Net.Search.Hits;
using IndexSearcher = Lucene.Net.Search.IndexSearcher;
using Query = Lucene.Net.Search.Query;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;

namespace Lucene.Net.Analysis
{
	[TestFixture]
	public class TestKeywordAnalyzer
	{
		
		private RAMDirectory directory;
		private IndexSearcher searcher;
		
        [SetUp]
		public virtual void  SetUp()
		{
			directory = new RAMDirectory();
			IndexWriter writer = new IndexWriter(directory, new SimpleAnalyzer(), true);
			
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document();
			doc.Add(new Field("partnum", "Q36", Field.Store.YES, Field.Index.UN_TOKENIZED));
			doc.Add(new Field("description", "Illidium Space Modulator", Field.Store.YES, Field.Index.TOKENIZED));
			writer.AddDocument(doc);
			
			writer.Close();
			
			searcher = new IndexSearcher(directory);
		}
		
        [Test]
		public virtual void  TestPerFieldAnalyzer()
		{
			PerFieldAnalyzerWrapper analyzer = new PerFieldAnalyzerWrapper(new SimpleAnalyzer());
			analyzer.AddAnalyzer("partnum", new KeywordAnalyzer());
			
			Lucene.Net.QueryParsers.QueryParser queryParser = new Lucene.Net.QueryParsers.QueryParser("description", analyzer);
			Query query = queryParser.Parse("partnum:Q36 AND SPACE");
			
			Hits hits = searcher.Search(query);
			Assert.AreEqual("+partnum:Q36 +space", query.ToString("description"), "Q36 kept as-is");
			Assert.AreEqual(1, hits.Length(), "doc found!");
		}
	}
}