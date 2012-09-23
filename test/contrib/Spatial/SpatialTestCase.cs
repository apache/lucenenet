/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Spatial.Util;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;
using Directory = Lucene.Net.Store.Directory;

namespace Lucene.Net.Contrib.Spatial.Test
{
	public class SpatialTestCase : LuceneTestCase
	{
		private DirectoryReader indexReader;
		private IndexWriter indexWriter;
		private Directory directory;
		protected IndexSearcher indexSearcher;

		[SetUp]
		public override void SetUp()
		{
			base.SetUp();

			directory = NewDirectory();

			indexWriter = new IndexWriter(directory, new WhitespaceAnalyzer(), true, IndexWriter.MaxFieldLength.UNLIMITED);
		}

		[TearDown]
		public override void TearDown()
		{
			if (indexWriter != null)
			{
				indexWriter.Dispose();
				indexWriter = null;
			}
			if (indexReader != null)
			{
				indexReader.Dispose();
				indexReader = null;
			}
			if (directory != null)
			{
				directory.Dispose();
				directory = null;
			}
            CompatibilityExtensions.PurgeSpatialCaches(null);
			base.TearDown();
		}
		// ================================================= Helper Methods ================================================

		public static Directory NewDirectory()
		{
			return new RAMDirectory();
		}

		/// <summary>
		/// create a new searcher over the reader.
		/// </summary>
		/// <param name="r"></param>
		/// <returns></returns>
		public static IndexSearcher newSearcher(IndexReader r)
		{
			return new IndexSearcher(r);
		}

		protected void addDocument(Document doc)
		{
			indexWriter.AddDocument(doc);
		}

		protected void addDocumentsAndCommit(List<Document> documents)
		{
			foreach (var document in documents)
			{
				indexWriter.AddDocument(document);
			}
			commit();
		}

		protected void deleteAll()
		{
			indexWriter.DeleteAll();
		}

		protected void commit()
		{
			indexWriter.Commit();
			if (indexReader == null)
			{
				indexReader = (DirectoryReader)IndexReader.Open(directory, true);
			}
			else
			{
				indexReader = (DirectoryReader)indexReader.Reopen();
			}
			indexSearcher = newSearcher(indexReader);
		}

		protected void verifyDocumentsIndexed(int numDocs)
		{
			Assert.AreEqual(numDocs, indexReader.NumDocs());
		}

		protected SearchResults executeQuery(Query query, int numDocs)
		{
			try
			{
				TopDocs topDocs = indexSearcher.Search(query, numDocs);

				var results = new List<SearchResult>();
				foreach (ScoreDoc scoreDoc in topDocs.ScoreDocs)
				{
					results.Add(new SearchResult(scoreDoc.Score, indexSearcher.Doc(scoreDoc.Doc)));
				}
				return new SearchResults(topDocs.TotalHits, results);
			}
			catch (IOException ioe)
			{
				throw new Exception("IOException thrown while executing query", ioe);
			}
		}

		// ================================================= Inner Classes =================================================

		protected class SearchResults
		{

			public int numFound;
			public List<SearchResult> results;

			public SearchResults(int numFound, List<SearchResult> results)
			{
				this.numFound = numFound;
				this.results = results;
			}

			public StringBuilder toDebugString()
			{
				StringBuilder str = new StringBuilder();
				str.Append("found: ").Append(numFound).Append('[');
				foreach (SearchResult r in results)
				{
					String id = r.document.Get("id");
					str.Append(id).Append(", ");
				}
				str.Append(']');
				return str;
			}

			public override String ToString()
			{
				return "[found:" + numFound + " " + results + "]";
			}
		}

		protected class SearchResult
		{

			public float score;
			public Document document;

			public SearchResult(float score, Document document)
			{
				this.score = score;
				this.document = document;
			}

			public override String ToString()
			{
				return "[" + score + "=" + document + "]";
			}
		}
	}
}
