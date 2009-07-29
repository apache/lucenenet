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

using Analyzer = Lucene.Net.Analysis.Analyzer;
using SimpleAnalyzer = Lucene.Net.Analysis.SimpleAnalyzer;
using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;
using IndexWriter = Lucene.Net.Index.IndexWriter;
using QueryParser = Lucene.Net.QueryParsers.QueryParser;
using IndexSearcher = Lucene.Net.Search.IndexSearcher;
using Query = Lucene.Net.Search.Query;
using ScoreDoc = Lucene.Net.Search.ScoreDoc;
using Searcher = Lucene.Net.Search.Searcher;
using Directory = Lucene.Net.Store.Directory;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;

namespace Lucene.Net
{
	class SearchTestForDuplicates
	{
		internal const System.String PRIORITY_FIELD = "priority";
		internal const System.String ID_FIELD = "id";
		internal const System.String HIGH_PRIORITY = "high";
		internal const System.String MED_PRIORITY = "medium";
		internal const System.String LOW_PRIORITY = "low";
		
		[STAThread]
		public static void  Main(System.String[] args)
		{
			try
			{
				Directory directory = new RAMDirectory();
				Analyzer analyzer = new SimpleAnalyzer();
				IndexWriter writer = new IndexWriter(directory, analyzer, true, IndexWriter.MaxFieldLength.LIMITED);
				
				int MAX_DOCS = 225;
				
				for (int j = 0; j < MAX_DOCS; j++)
				{
					Lucene.Net.Documents.Document d = new Lucene.Net.Documents.Document();
					d.Add(new Field(PRIORITY_FIELD, HIGH_PRIORITY, Field.Store.YES, Field.Index.ANALYZED));
					d.Add(new Field(ID_FIELD, System.Convert.ToString(j), Field.Store.YES, Field.Index.ANALYZED));
					writer.AddDocument(d);
				}
				writer.Close();
				
				// try a search without OR
				Searcher searcher = new IndexSearcher(directory);
				ScoreDoc[] hits = null;
				
				Lucene.Net.QueryParsers.QueryParser parser = new Lucene.Net.QueryParsers.QueryParser(PRIORITY_FIELD, analyzer);
				
				Query query = parser.Parse(HIGH_PRIORITY);
				System.Console.Out.WriteLine("Query: " + query.ToString(PRIORITY_FIELD));
				
				hits = searcher.Search(query, null, 1000).scoreDocs;
				PrintHits(hits, searcher);
				
				searcher.Close();
				
				// try a new search with OR
				searcher = new IndexSearcher(directory);
				hits = null;
				
				parser = new Lucene.Net.QueryParsers.QueryParser(PRIORITY_FIELD, analyzer);
				
				query = parser.Parse(HIGH_PRIORITY + " OR " + MED_PRIORITY);
				System.Console.Out.WriteLine("Query: " + query.ToString(PRIORITY_FIELD));

                hits = searcher.Search(query, null, 1000).scoreDocs;
                PrintHits(hits, searcher);
				
				searcher.Close();
			}
			catch (System.Exception e)
			{
				System.Console.Out.WriteLine(" caught a " + e.GetType() + "\n with message: " + e.Message);
			}
		}
		
		private static void  PrintHits(ScoreDoc[] hits, Searcher searcher)
		{
			System.Console.Out.WriteLine(hits.Length + " total results\n");
			for (int i = 0; i < hits.Length; i++)
			{
				if (i < 10 || (i > 94 && i < 105))
				{
					Lucene.Net.Documents.Document d = searcher.Doc(hits[i].doc);
					System.Console.Out.WriteLine(i + " " + d.Get(ID_FIELD));
				}
			}
		}
	}
}