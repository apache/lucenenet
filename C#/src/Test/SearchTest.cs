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

using Lucene.Net.Store;
using Lucene.Net.Documents;
using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Searchable = Lucene.Net.Search.Searchable;
using Lucene.Net.QueryParsers;

namespace Lucene.Net
{
	
	class SearchTest
	{
		[STAThread]
		public static void  Main(System.String[] args)
		{
			try
			{
				Directory directory = new RAMDirectory();
				Analyzer analyzer = new SimpleAnalyzer();
				IndexWriter writer = new IndexWriter(directory, analyzer, true);
				
				System.String[] docs = new System.String[]{"a b c d e", "a b c d e a b c d e", "a b c d e f g h i j", "a c e", "e c a", "a c e a c e", "a c e a b c"};
				for (int j = 0; j < docs.Length; j++)
				{
					Lucene.Net.Documents.Document d = new Lucene.Net.Documents.Document();
					d.Add(new Field("contents", docs[j], Field.Store.YES, Field.Index.TOKENIZED));
					writer.AddDocument(d);
				}
				writer.Close();
				
				Searcher searcher = new IndexSearcher(directory);
				
				System.String[] queries = new System.String[]{"\"a c e\""};
				Hits hits = null;
				
				Lucene.Net.QueryParsers.QueryParser parser = new Lucene.Net.QueryParsers.QueryParser("contents", analyzer);
				parser.SetPhraseSlop(4);
				for (int j = 0; j < queries.Length; j++)
				{
					Query query = parser.Parse(queries[j]);
					System.Console.Out.WriteLine("Query: " + query.ToString("contents"));
					
					//DateFilter filter =
					//  new DateFilter("modified", Time(1997,0,1), Time(1998,0,1));
					//DateFilter filter = DateFilter.Before("modified", Time(1997,00,01));
					//System.out.println(filter);
					
					hits = searcher.Search(query);
					
					System.Console.Out.WriteLine(hits.Length() + " total results");
					for (int i = 0; i < hits.Length() && i < 10; i++)
					{
						Lucene.Net.Documents.Document d = hits.Doc(i);
						System.Console.Out.WriteLine(i + " " + hits.Score(i) + " " + d.Get("contents"));
					}
				}
				searcher.Close();
			}
			catch (System.Exception e)
			{
				System.Console.Out.WriteLine(" caught a " + e.GetType() + "\n with message: " + e.Message);
			}
		}
		
		internal static long Time(int year, int month, int day)
		{
			System.DateTime calendar = new System.DateTime(year, month, day, 0, 0, 0, 0, new System.Globalization.GregorianCalendar());
			return calendar.Ticks;
		}
	}
}