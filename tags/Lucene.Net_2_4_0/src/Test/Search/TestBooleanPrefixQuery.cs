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

//using TestRunner = junit.textui.TestRunner;
using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;
using IndexReader = Lucene.Net.Index.IndexReader;
using IndexWriter = Lucene.Net.Index.IndexWriter;
using Term = Lucene.Net.Index.Term;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;
using WhitespaceAnalyzer = Lucene.Net.Analysis.WhitespaceAnalyzer;
using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

namespace Lucene.Net.Search
{
	
	/// <summary> </summary>
	/// <version>  $Id: TestBooleanPrefixQuery.java 583534 2007-10-10 16:46:35Z mikemccand $
	/// 
	/// </version>
	
	[TestFixture]
	public class TestBooleanPrefixQuery : LuceneTestCase
	{
		
		[STAThread]
		public static void  Main(System.String[] args)
		{
			// NUnit.Core.TestRunner.Run(Suite());  // {{Aroush}} where is 'TestRunner' in NUnit?
		}
		
		/*  // {{Aroush}} Do we need this method?
		public static TestCase Suite()
		{
			return new NUnit.Core.TestSuite(typeof(TestBooleanPrefixQuery));
		}
		*/
		
		[Test]
		public virtual void  TestMethod()
		{
			RAMDirectory directory = new RAMDirectory();
			
			System.String[] categories = new System.String[]{"food", "foodanddrink", "foodanddrinkandgoodtimes", "food and drink"};
			
			Query rw1 = null;
			Query rw2 = null;
			try
			{
				IndexWriter writer = new IndexWriter(directory, new WhitespaceAnalyzer(), true, IndexWriter.MaxFieldLength.LIMITED);
				for (int i = 0; i < categories.Length; i++)
				{
					Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document();
					doc.Add(new Field("category", categories[i], Field.Store.YES, Field.Index.NOT_ANALYZED));
					writer.AddDocument(doc);
				}
				writer.Close();
				
				IndexReader reader = IndexReader.Open(directory);
				PrefixQuery query = new PrefixQuery(new Term("category", "foo"));
				
				rw1 = query.Rewrite(reader);
				
				BooleanQuery bq = new BooleanQuery();
				bq.Add(query, BooleanClause.Occur.MUST);
				
				rw2 = bq.Rewrite(reader);
			}
			catch (System.IO.IOException e)
			{
				Assert.Fail(e.Message);
			}
			
			BooleanQuery bq1 = null;
			if (rw1 is BooleanQuery)
			{
				bq1 = (BooleanQuery) rw1;
			}
			
			BooleanQuery bq2 = null;
			if (rw2 is BooleanQuery)
			{
				bq2 = (BooleanQuery) rw2;
			}
			else
			{
				Assert.Fail("Rewrite");
			}
			
			Assert.AreEqual(bq1.GetClauses().Length, bq2.GetClauses().Length, "Number of Clauses Mismatch");
		}
	}
}