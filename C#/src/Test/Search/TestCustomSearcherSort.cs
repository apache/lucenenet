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
using StandardAnalyzer = Lucene.Net.Analysis.Standard.StandardAnalyzer;
using DateTools = Lucene.Net.Documents.DateTools;
using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;
using IndexReader = Lucene.Net.Index.IndexReader;
using IndexWriter = Lucene.Net.Index.IndexWriter;
using Term = Lucene.Net.Index.Term;
using Directory = Lucene.Net.Store.Directory;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;

namespace Lucene.Net.Search
{
	
	/// <summary> Unit test for sorting code.
	/// 
	/// </summary>
	/// <author>   Martin Seitz (T-Systems)
	/// </author>
	
	[Serializable]
	[TestFixture]
    public class TestCustomSearcherSort
	{
		
		private Directory index = null;
		private Query query = null;
		// reduced from 20000 to 2000 to speed up test...
		private const int INDEX_SIZE = 2000;
		
		
		[STAThread]
		public static void  Main(System.String[] argv)
		{
			// TestRunner.run(Suite()); // {{Aroush}} how is this done in NUnit?
		}
		
		public static NUnit.Framework.TestCase Suite()
		{
			return null; // return new NUnit.Core.TestSuite(typeof(TestCustomSearcherSort)); {{Aroush}} how is this done in NUnit?
		}
		
		
		// create an index for testing
		private Directory GetIndex()
		{
			RAMDirectory indexStore = new RAMDirectory();
			IndexWriter writer = new IndexWriter(indexStore, new StandardAnalyzer(), true);
			RandomGen random = new RandomGen(this);
			for (int i = 0; i < INDEX_SIZE; ++i)
			{
				// don't decrease; if to low the problem doesn't show up
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document();
				if ((i % 5) != 0)
				{
					// some documents must not have an entry in the first sort field
					doc.Add(new Field("publicationDate_", random.GetLuceneDate(), Field.Store.YES, Field.Index.UN_TOKENIZED));
				}
				if ((i % 7) == 0)
				{
					// some documents to match the query (see below) 
					doc.Add(new Field("content", "test", Field.Store.YES, Field.Index.TOKENIZED));
				}
				// every document has a defined 'mandant' field
				doc.Add(new Field("mandant", System.Convert.ToString(i % 3), Field.Store.YES, Field.Index.UN_TOKENIZED));
				writer.AddDocument(doc);
			}
			writer.Optimize();
			writer.Close();
			return indexStore;
		}
		
		/// <summary> Create index and query for test cases. </summary>
		[TestFixtureSetUp]
        public virtual void  SetUp()
		{
			index = GetIndex();
			query = new TermQuery(new Term("content", "test"));
		}
		
		/// <summary> Run the test using two CustomSearcher instances. </summary>
        [Test]
        public virtual void  TestFieldSortCustomSearcher()
		{
			// log("Run testFieldSortCustomSearcher");
			// define the sort criteria
			Sort custSort = new Sort(new SortField[]{new SortField("publicationDate_"), SortField.FIELD_SCORE});
			Searcher searcher = new CustomSearcher(this, index, 2);
			// search and check hits
			MatchHits(searcher, custSort);
		}
		/// <summary> Run the test using one CustomSearcher wrapped by a MultiSearcher. </summary>
		[Test]
        public virtual void  TestFieldSortSingleSearcher()
		{
			// log("Run testFieldSortSingleSearcher");
			// define the sort criteria
			Sort custSort = new Sort(new SortField[]{new SortField("publicationDate_"), SortField.FIELD_SCORE});
			Searcher searcher = new MultiSearcher(new Lucene.Net.Search.Searchable[]{new CustomSearcher(this, index, 2)});
			// search and check hits
			MatchHits(searcher, custSort);
		}
		/// <summary> Run the test using two CustomSearcher instances. </summary>
		[Test]
        public virtual void  TestFieldSortMultiCustomSearcher()
		{
			// log("Run testFieldSortMultiCustomSearcher");
			// define the sort criteria
			Sort custSort = new Sort(new SortField[]{new SortField("publicationDate_"), SortField.FIELD_SCORE});
			Searcher searcher = new MultiSearcher(new Lucene.Net.Search.Searchable[]{new CustomSearcher(this, index, 0), new CustomSearcher(this, index, 2)});
			// search and check hits
			MatchHits(searcher, custSort);
		}
		
		
		// make sure the documents returned by the search match the expected list
		private void  MatchHits(Searcher searcher, Sort sort)
		{
			// make a query without sorting first
			Hits hitsByRank = searcher.Search(query);
			CheckHits(hitsByRank, "Sort by rank: "); // check for duplicates
			System.Collections.IDictionary resultMap = new System.Collections.SortedList();
			// store hits in TreeMap - TreeMap does not allow duplicates; existing entries are silently overwritten
			for (int hitid = 0; hitid < hitsByRank.Length(); ++hitid)
			{
				resultMap[(System.Int32) hitsByRank.Id(hitid)] = (System.Int32) hitid; // Value: Hits-Objekt Index
			}
			
			// now make a query using the sort criteria
			Hits resultSort = searcher.Search(query, sort);
			CheckHits(resultSort, "Sort by custom criteria: "); // check for duplicates
			
			System.String lf = SupportClass.AppSettings.Get("line.separator", "\n");
			// besides the sorting both sets of hits must be identical
			for (int hitid = 0; hitid < resultSort.Length(); ++hitid)
			{
				System.Int32 idHitDate = (System.Int32) resultSort.Id(hitid); // document ID from sorted search
				if (!resultMap.Contains(idHitDate))
				{
					Log("ID " + idHitDate + " not found. Possibliy a duplicate.");
				}
				Assert.IsTrue(resultMap.Contains(idHitDate)); // same ID must be in the Map from the rank-sorted search
				// every hit must appear once in both result sets --> remove it from the Map.
				// At the end the Map must be empty!
				resultMap.Remove(idHitDate);
			}
			if (resultMap.Count == 0)
			{
				// log("All hits matched");
			}
			else
			{
				Log("Couldn't match " + resultMap.Count + " hits.");
			}
			Assert.AreEqual(resultMap.Count, 0);
		}
		
		/// <summary> Check the hits for duplicates.</summary>
		/// <param name="hits">
		/// </param>
		private void  CheckHits(Hits hits, System.String prefix)
		{
			if (hits != null)
			{
				System.Collections.IDictionary idMap = new System.Collections.SortedList();
				for (int docnum = 0; docnum < hits.Length(); ++docnum)
				{
					System.Int32 luceneId;
					try
					{
						luceneId = (System.Int32) hits.Id(docnum);
						if (idMap.Contains(luceneId))
						{
							System.Text.StringBuilder message = new System.Text.StringBuilder(prefix);
							message.Append("Duplicate key for hit index = ");
							message.Append(docnum);
							message.Append(", previous index = ");
							message.Append(((System.Int32) idMap[luceneId]).ToString());
							message.Append(", Lucene ID = ");
							message.Append(luceneId);
							Log(message.ToString());
						}
						else
						{
							idMap[luceneId] = (System.Int32) docnum;
						}
					}
					catch (System.IO.IOException ioe)
					{
						System.Text.StringBuilder message = new System.Text.StringBuilder(prefix);
						message.Append("Error occurred for hit index = ");
						message.Append(docnum);
						message.Append(" (");
						message.Append(ioe.Message);
						message.Append(")");
						Log(message.ToString());
					}
				}
			}
		}
		
		// Simply write to console - choosen to be independant of log4j etc 
		private void  Log(System.String message)
		{
			System.Console.Out.WriteLine(message);
		}
		
		public class CustomSearcher : IndexSearcher
		{
			private void  InitBlock(TestCustomSearcherSort enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private TestCustomSearcherSort enclosingInstance;
			public TestCustomSearcherSort Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			private int switcher;
			/// <param name="directory">
			/// </param>
			/// <throws>  IOException </throws>
			public CustomSearcher(TestCustomSearcherSort enclosingInstance, Directory directory, int switcher):base(directory)
			{
				InitBlock(enclosingInstance);
				this.switcher = switcher;
			}
			/// <param name="r">
			/// </param>
			public CustomSearcher(TestCustomSearcherSort enclosingInstance, IndexReader r, int switcher):base(r)
			{
				InitBlock(enclosingInstance);
				this.switcher = switcher;
			}
			/// <param name="path">
			/// </param>
			/// <throws>  IOException </throws>
			public CustomSearcher(TestCustomSearcherSort enclosingInstance, System.String path, int switcher):base(path)
			{
				InitBlock(enclosingInstance);
				this.switcher = switcher;
			}
			/* (non-Javadoc)
			* @see Lucene.Net.search.Searchable#search(Lucene.Net.search.Query, Lucene.Net.search.Filter, int, Lucene.Net.search.Sort)
			*/
			public override TopFieldDocs Search(Query query, Filter filter, int nDocs, Sort sort)
			{
				BooleanQuery bq = new BooleanQuery();
				bq.Add(query, BooleanClause.Occur.MUST);
				bq.Add(new TermQuery(new Term("mandant", System.Convert.ToString(switcher))), BooleanClause.Occur.MUST);
				return base.Search(bq, filter, nDocs, sort);
			}
			/* (non-Javadoc)
			* @see Lucene.Net.search.Searchable#search(Lucene.Net.search.Query, Lucene.Net.search.Filter, int)
			*/
			public override TopDocs Search(Query query, Filter filter, int nDocs)
			{
				BooleanQuery bq = new BooleanQuery();
				bq.Add(query, BooleanClause.Occur.MUST);
				bq.Add(new TermQuery(new Term("mandant", System.Convert.ToString(switcher))), BooleanClause.Occur.MUST);
				return base.Search(bq, filter, nDocs);
			}
		}

        private class RandomGen
		{
			public RandomGen(TestCustomSearcherSort enclosingInstance)
			{
				InitBlock(enclosingInstance);
			}
			private void  InitBlock(TestCustomSearcherSort enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
				System.DateTime temp_calendar;
				temp_calendar = new System.DateTime(1980, 1, 1, 0, 0, 0, 0, new System.Globalization.GregorianCalendar());
				base_Renamed = temp_calendar;
			}
			private TestCustomSearcherSort enclosingInstance;
			public TestCustomSearcherSort Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}

            private System.Random random = new System.Random((System.Int32) 0); // to generate some arbitrary contents
			// private System.Globalization.Calendar base_Renamed;
            private System.DateTime base_Renamed;
			
			// Just to generate some different Lucene Date strings
			public System.String GetLuceneDate()
			{
                long v1 = base_Renamed.Millisecond;
                long v2 = random.Next();
                long v3 = System.Int32.MinValue;
                return DateTools.TimeToString(v1 + v2 - v3, DateTools.Resolution.DAY);
            }
		}
	}
}