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
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Searchable = Lucene.Net.Search.Searchable;
using Lucene.Net.Store;

namespace Lucene.Net
{
	
	/// <summary>JUnit adaptation of an older test case SearchTest.</summary>
	/// <author>  dmitrys@earthlink.net
	/// </author>
	/// <version>  $Id: TestSearch.java 150494 2004-09-06 22:29:22Z dnaber $
	/// </version>
	[TestFixture]
    public class TestSearch
	{
		
		/// <summary>Main for running test case by itself. </summary>
		[STAThread]
		public static void  Main(System.String[] args)
		{
			// NUnit.Core.TestRunner.Run(new NUnit.Core.TestSuite(typeof(TestSearch))); // {{Aroush}} where is 'TestRunner' in NUnit?
		}
		
		/// <summary>This test performs a number of searches. It also compares output
		/// of searches using multi-file index segments with single-file
		/// index segments.
		/// 
		/// TODO: someone should check that the results of the searches are
		/// still correct by adding assert statements. Right now, the test
		/// passes if the results are the same between multi-file and
		/// single-file formats, even if the results are wrong.
		/// </summary>
		[Test]
        public virtual void  TestSearch_Renamed_Method()
		{
			System.IO.StringWriter sw = new System.IO.StringWriter();
			System.IO.StreamWriter pw = null; // new System.IO.StreamWriter(sw);    // {{Aroush}} how do you pass 'sw' to StreamWriter()?
			DoTestSearch(pw, false);
			pw.Close();
			sw.Close();
			System.String multiFileOutput = sw.GetStringBuilder().ToString();
			//System.out.println(multiFileOutput);
			
			sw = new System.IO.StringWriter();
			pw = null; // new System.IO.StreamWriter(sw);   // {{Aroush}} how do you pass 'sw' to StreamWriter()?
			DoTestSearch(pw, true);
			pw.Close();
			sw.Close();
			System.String singleFileOutput = sw.GetStringBuilder().ToString();
			
			Assert.AreEqual(multiFileOutput, singleFileOutput);
		}
		
		
		private void  DoTestSearch(System.IO.StreamWriter out_Renamed, bool useCompoundFile)
		{
			Directory directory = new RAMDirectory();
			Analyzer analyzer = new SimpleAnalyzer();
			IndexWriter writer = new IndexWriter(directory, analyzer, true);
			
			writer.SetUseCompoundFile(useCompoundFile);
			
			System.String[] docs = new System.String[]{"a b c d e", "a b c d e a b c d e", "a b c d e f g h i j", "a c e", "e c a", "a c e a c e", "a c e a b c"};
			for (int j = 0; j < docs.Length; j++)
			{
				Lucene.Net.Documents.Document d = new Lucene.Net.Documents.Document();
				d.Add(new Field("contents", docs[j], Field.Store.YES, Field.Index.TOKENIZED));
				writer.AddDocument(d);
			}
			writer.Close();
			
			Searcher searcher = new IndexSearcher(directory);
			
			System.String[] queries = new System.String[]{"a b", "\"a b\"", "\"a b c\"", "a c", "\"a c\"", "\"a c e\""};
			Hits hits = null;
			
			Lucene.Net.QueryParsers.QueryParser parser = new Lucene.Net.QueryParsers.QueryParser("contents", analyzer);
			parser.SetPhraseSlop(4);
			for (int j = 0; j < queries.Length; j++)
			{
				Query query = parser.Parse(queries[j]);
				out_Renamed.WriteLine("Query: " + query.ToString("contents"));
				
				//DateFilter filter =
				//  new DateFilter("modified", Time(1997,0,1), Time(1998,0,1));
				//DateFilter filter = DateFilter.Before("modified", Time(1997,00,01));
				//System.out.println(filter);
				
				hits = searcher.Search(query);
				
				out_Renamed.WriteLine(hits.Length() + " total results");
				for (int i = 0; i < hits.Length() && i < 10; i++)
				{
					Lucene.Net.Documents.Document d = hits.Doc(i);
					out_Renamed.WriteLine(i + " " + hits.Score(i) + " " + d.Get("contents"));
				}
			}
			searcher.Close();
		}
		
		internal static long Time(int year, int month, int day)
		{
            System.DateTime calendar = new System.DateTime(year, month, day, 0, 0, 0, 0, new System.Globalization.GregorianCalendar());
            return calendar.Ticks;
		}
	}
}