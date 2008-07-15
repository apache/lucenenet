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

using DateTools = Lucene.Net.Documents.DateTools;
using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;
using IndexWriter = Lucene.Net.Index.IndexWriter;
using QueryParser = Lucene.Net.QueryParsers.QueryParser;
using Directory = Lucene.Net.Store.Directory;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;
using WhitespaceAnalyzer = Lucene.Net.Analysis.WhitespaceAnalyzer;

namespace Lucene.Net.Search
{
	
	/// <summary> Test date sorting, i.e. auto-sorting of fields with type "long".
	/// See http://issues.apache.org/jira/browse/LUCENE-1045 
	/// </summary>
	[TestFixture]
	public class TestDateSort
	{
		
		private const System.String TEXT_FIELD = "text";
		private const System.String DATE_TIME_FIELD = "dateTime";
		
		private static Directory directory;
		
		[SetUp]
		public virtual void  SetUp()
		{
			// Create an index writer.
			directory = new RAMDirectory();
			IndexWriter writer = new IndexWriter(directory, new WhitespaceAnalyzer(), true);

			// oldest doc:
			// Add the first document.  text = "Document 1"  dateTime = Oct 10 03:25:22 EDT 2007
			writer.AddDocument(CreateDocument("Document 1", 633275835220000000L));
			// Add the second document.  text = "Document 2"  dateTime = Oct 10 03:25:26 EDT 2007 
			writer.AddDocument(CreateDocument("Document 2", 633275835260000000L));
			// Add the third document.  text = "Document 3"  dateTime = Oct 11 07:12:13 EDT 2007 
			writer.AddDocument(CreateDocument("Document 3", 633276835330000000L));
			// Add the fourth document.  text = "Document 4"  dateTime = Oct 11 08:02:09 EDT 2007
			writer.AddDocument(CreateDocument("Document 4", 633276865290000000L));
			// latest doc:
			// Add the fifth document.  text = "Document 5"  dateTime = Oct 12 13:25:43 EDT 2007
			writer.AddDocument(CreateDocument("Document 5", 633277923430000000L));

			//// oldest doc:
			//// Add the first document.  text = "Document 1"  dateTime = Oct 10 03:25:22 EDT 2007
			//writer.AddDocument(CreateDocument("Document 1", 1192001122000L));
			//// Add the second document.  text = "Document 2"  dateTime = Oct 10 03:25:26 EDT 2007 
			//writer.AddDocument(CreateDocument("Document 2", 1192001126000L));
			//// Add the third document.  text = "Document 3"  dateTime = Oct 11 07:12:13 EDT 2007 
			//writer.AddDocument(CreateDocument("Document 3", 1192101133000L));
			//// Add the fourth document.  text = "Document 4"  dateTime = Oct 11 08:02:09 EDT 2007
			//writer.AddDocument(CreateDocument("Document 4", 1192104129000L));
			//// latest doc:
			//// Add the fifth document.  text = "Document 5"  dateTime = Oct 12 13:25:43 EDT 2007
			//writer.AddDocument(CreateDocument("Document 5", 1192209943000L));
			
			writer.Optimize();
			writer.Close();
		}
		
		[Test]
		public virtual void  TestReverseDateSort()
		{
			IndexSearcher searcher = new IndexSearcher(directory);
			
			// Create a Sort object.  reverse is set to true.
			// problem occurs only with SortField.AUTO:
			Sort sort = new Sort(new SortField(DATE_TIME_FIELD, SortField.AUTO, true));

			Lucene.Net.QueryParsers.QueryParser queryParser = new Lucene.Net.QueryParsers.QueryParser(TEXT_FIELD, new WhitespaceAnalyzer());
			Query query = queryParser.Parse("Document");
			
			// Execute the search and process the search results.
			System.String[] actualOrder = new System.String[5];
			Hits hits = searcher.Search(query, sort);
			for (int i = 0; i < hits.Length(); i++)
			{
				Document document = hits.Doc(i);
				System.String text = document.Get(TEXT_FIELD);
				actualOrder[i] = text;
			}
			searcher.Close();
			
			// Set up the expected order (i.e. Document 5, 4, 3, 2, 1).
			System.String[] expectedOrder = new System.String[5];
			expectedOrder[0] = "Document 5";
			expectedOrder[1] = "Document 4";
			expectedOrder[2] = "Document 3";
			expectedOrder[3] = "Document 2";
			expectedOrder[4] = "Document 1";
			
			Assert.AreEqual(new System.Collections.ArrayList(expectedOrder), new System.Collections.ArrayList(actualOrder));
		}
		
		private static Document CreateDocument(System.String text, long time)
		{
			Document document = new Document();
			
			// Add the text field.
			Field textField = new Field(TEXT_FIELD, text, Field.Store.YES, Field.Index.TOKENIZED);
			document.Add(textField);
			
			// Add the date/time field.
			System.String dateTimeString = DateTools.TimeToString(time, DateTools.Resolution.SECOND);
			Field dateTimeField = new Field(DATE_TIME_FIELD, dateTimeString, Field.Store.YES, Field.Index.UN_TOKENIZED);
			document.Add(dateTimeField);
			
			return document;
		}
	}
}