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

using IndexWriter = Lucene.Net.Index.IndexWriter;
using QueryParser = Lucene.Net.QueryParsers.QueryParser;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;
using SimpleAnalyzer = Lucene.Net.Analysis.SimpleAnalyzer;
using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;

namespace Lucene.Net.Search
{
	
	/// <summary>Similarity unit test.
	/// 
	/// </summary>
	/// <author>  Doug Cutting
	/// </author>
	/// <version>  $Revision: 150497 $
	/// </version>
	[TestFixture]
    public class TestNot
	{
		
		[Test]
        public virtual void  TestNot_Renamed_Method()
		{
			RAMDirectory store = new RAMDirectory();
			IndexWriter writer = new IndexWriter(store, new SimpleAnalyzer(), true);
			
			Lucene.Net.Documents.Document d1 = new Lucene.Net.Documents.Document();
			d1.Add(new Field("field", "a b", Field.Store.YES, Field.Index.TOKENIZED));
			
			writer.AddDocument(d1);
			writer.Optimize();
			writer.Close();
			
			Searcher searcher = new IndexSearcher(store);
            Lucene.Net.QueryParsers.QueryParser parser = new Lucene.Net.QueryParsers.QueryParser("field", new SimpleAnalyzer());
            Lucene.Net.Search.Query query = parser.Parse("a NOT b");
            //System.out.println(query);
			Hits hits = searcher.Search(query);
			Assert.AreEqual(0, hits.Length());
		}
	}
}