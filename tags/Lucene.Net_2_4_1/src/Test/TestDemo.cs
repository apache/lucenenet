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

using Analyzer = Lucene.Net.Analysis.Analyzer;
using StandardAnalyzer = Lucene.Net.Analysis.Standard.StandardAnalyzer;
using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;
using IndexWriter = Lucene.Net.Index.IndexWriter;
using ParseException = Lucene.Net.QueryParsers.ParseException;
using QueryParser = Lucene.Net.QueryParsers.QueryParser;
using IndexSearcher = Lucene.Net.Search.IndexSearcher;
using Query = Lucene.Net.Search.Query;
using ScoreDoc = Lucene.Net.Search.ScoreDoc;
using Directory = Lucene.Net.Store.Directory;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;
using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
using _TestUtil = Lucene.Net.Util._TestUtil;

namespace Lucene.Net
{
	/// <summary> A very simple demo used in the API documentation (src/java/overview.html).
	/// </summary>
	[TestFixture]
	public class TestDemo : LuceneTestCase
	{
		[Test]
		public virtual void  TestDemo_Renamed_Method()
		{
			Analyzer analyzer = new StandardAnalyzer();
			
			// Store the index in memory:
			Directory directory = new RAMDirectory();
			// To store an index on disk, use this instead (note that the 
			// parameter true will overwrite the index in that directory
			// if one exists):
			//Directory directory = FSDirectory.getDirectory("/tmp/testindex", true);
			IndexWriter iwriter = new IndexWriter(directory, analyzer, true, new IndexWriter.MaxFieldLength(25000));
			Document doc = new Document();
			System.String text = "This is the text to be indexed.";
			doc.Add(new Field("fieldname", text, Field.Store.YES, Field.Index.ANALYZED));
			iwriter.AddDocument(doc);
			iwriter.Close();

            _TestUtil.CheckIndex(directory);

			// Now search the index:
			IndexSearcher isearcher = new IndexSearcher(directory);
			// Parse a simple query that searches for "text":
			Lucene.Net.QueryParsers.QueryParser parser = new Lucene.Net.QueryParsers.QueryParser("fieldname", analyzer);
			Query query = parser.Parse("text");
			ScoreDoc[] hits = isearcher.Search(query, null, 1000).scoreDocs;
			Assert.AreEqual(1, hits.Length);
			// Iterate through the results:
			for (int i = 0; i < hits.Length; i++)
			{
				Document hitDoc = isearcher.Doc(hits[i].doc);
				Assert.AreEqual("This is the text to be indexed.", hitDoc.Get("fieldname"));
			}
			isearcher.Close();
			directory.Close();
		}
	}
}