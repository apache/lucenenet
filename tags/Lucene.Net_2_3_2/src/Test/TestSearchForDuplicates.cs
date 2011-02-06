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

using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Store;
using Lucene.Net.Analysis;
using Lucene.Net.Search;
using Searchable = Lucene.Net.Search.Searchable;
using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

namespace Lucene.Net
{
	
	
	/// <summary>JUnit adaptation of an older test case DocTest.
	/// 
	/// </summary>
	/// <version>  $Id: TestSearchForDuplicates.java 583534 2007-10-10 16:46:35Z mikemccand $
	/// </version>
	[TestFixture]
	public class TestSearchForDuplicates : LuceneTestCase
	{
		
		/// <summary>Main for running test case by itself. </summary>
		[STAThread]
		public static void  Main(System.String[] args)
		{
			// NUnit.Core.TestRunner.Run(new NUnit.Core.TestSuite(typeof(TestSearchForDuplicates)));    // {{Aroush-1.9}} where is 'TestRunner' in NUnit
		}
		
		
		
		internal const System.String PRIORITY_FIELD = "priority";
		internal const System.String ID_FIELD = "id";
		internal const System.String HIGH_PRIORITY = "high";
		internal const System.String MED_PRIORITY = "medium";
		internal const System.String LOW_PRIORITY = "low";
		
		
		/// <summary>This test compares search results when using and not using compound
		/// files.
		/// 
		/// TODO: There is rudimentary search result validation as well, but it is
		/// simply based on asserting the output observed in the old test case,
		/// without really knowing if the output is correct. Someone needs to
		/// validate this output and make any changes to the checkHits method.
		/// </summary>
		[Test]
		public virtual void  TestRun()
		{
			System.IO.MemoryStream sw = new System.IO.MemoryStream();
			System.IO.StreamWriter pw = new System.IO.StreamWriter(sw);
			DoTest(pw, false);
			pw.Close();
			sw.Close();
			System.String multiFileOutput = System.Text.ASCIIEncoding.ASCII.GetString(sw.ToArray());
			//System.out.println(multiFileOutput);
			
			sw = new System.IO.MemoryStream();
			pw = new System.IO.StreamWriter(sw);
			DoTest(pw, true);
			pw.Close();
			sw.Close();
			System.String singleFileOutput = System.Text.ASCIIEncoding.ASCII.GetString(sw.ToArray());
			
			Assert.AreEqual(multiFileOutput, singleFileOutput);
		}
		
		
		private void  DoTest(System.IO.StreamWriter out_Renamed, bool useCompoundFiles)
		{
			Directory directory = new RAMDirectory();
			Analyzer analyzer = new SimpleAnalyzer();
			IndexWriter writer = new IndexWriter(directory, analyzer, true);
			
			writer.SetUseCompoundFile(useCompoundFiles);
			
			int MAX_DOCS = 225;
			
			for (int j = 0; j < MAX_DOCS; j++)
			{
				Lucene.Net.Documents.Document d = new Lucene.Net.Documents.Document();
				d.Add(new Field(PRIORITY_FIELD, HIGH_PRIORITY, Field.Store.YES, Field.Index.TOKENIZED));
				d.Add(new Field(ID_FIELD, System.Convert.ToString(j), Field.Store.YES, Field.Index.TOKENIZED));
				writer.AddDocument(d);
			}
			writer.Close();
			
			// try a search without OR
			Searcher searcher = new IndexSearcher(directory);
			Hits hits = null;
			
			Lucene.Net.QueryParsers.QueryParser parser = new Lucene.Net.QueryParsers.QueryParser(PRIORITY_FIELD, analyzer);
			
			Query query = parser.Parse(HIGH_PRIORITY);
			out_Renamed.WriteLine("Query: " + query.ToString(PRIORITY_FIELD));
			
			hits = searcher.Search(query);
			PrintHits(out_Renamed, hits);
			CheckHits(hits, MAX_DOCS);
			
			searcher.Close();
			
			// try a new search with OR
			searcher = new IndexSearcher(directory);
			hits = null;
			
			parser = new Lucene.Net.QueryParsers.QueryParser(PRIORITY_FIELD, analyzer);
			
			query = parser.Parse(HIGH_PRIORITY + " OR " + MED_PRIORITY);
			out_Renamed.WriteLine("Query: " + query.ToString(PRIORITY_FIELD));
			
			hits = searcher.Search(query);
			PrintHits(out_Renamed, hits);
			CheckHits(hits, MAX_DOCS);
			
			searcher.Close();
		}
		
		
		private void  PrintHits(System.IO.StreamWriter out_Renamed, Hits hits)
		{
			out_Renamed.WriteLine(hits.Length() + " total results\n");
			for (int i = 0; i < hits.Length(); i++)
			{
				if (i < 10 || (i > 94 && i < 105))
				{
					Lucene.Net.Documents.Document d = hits.Doc(i);
					out_Renamed.WriteLine(i + " " + d.Get(ID_FIELD));
				}
			}
		}
		
		private void  CheckHits(Hits hits, int expectedCount)
		{
			Assert.AreEqual(expectedCount, hits.Length(), "total results");
			for (int i = 0; i < hits.Length(); i++)
			{
				if (i < 10 || (i > 94 && i < 105))
				{
					Lucene.Net.Documents.Document d = hits.Doc(i);
					Assert.AreEqual(System.Convert.ToString(i), d.Get(ID_FIELD), "check " + i);
				}
			}
		}
	}
}