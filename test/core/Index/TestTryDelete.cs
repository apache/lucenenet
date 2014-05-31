using System;

namespace Lucene.Net.Index
{

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

	using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
	using Document = Lucene.Net.Document.Document;
	using Store = Lucene.Net.Document.Field.Store;
	using StringField = Lucene.Net.Document.StringField;
	using OpenMode = Lucene.Net.Index.IndexWriterConfig.OpenMode_e;
	using IndexSearcher = Lucene.Net.Search.IndexSearcher;
	using ReferenceManager = Lucene.Net.Search.ReferenceManager;
	using SearcherFactory = Lucene.Net.Search.SearcherFactory;
	using SearcherManager = Lucene.Net.Search.SearcherManager;
	using TermQuery = Lucene.Net.Search.TermQuery;
	using TopDocs = Lucene.Net.Search.TopDocs;
	using Directory = Lucene.Net.Store.Directory;
	using RAMDirectory = Lucene.Net.Store.RAMDirectory;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;


	public class TestTryDelete : LuceneTestCase
	{
	  private static IndexWriter GetWriter(Directory directory)
	  {
		MergePolicy policy = new LogByteSizeMergePolicy();
		IndexWriterConfig conf = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		conf.MergePolicy = policy;
		conf.OpenMode_e = OpenMode.CREATE_OR_APPEND;

		IndexWriter writer = new IndexWriter(directory, conf);

		return writer;
	  }

	  private static Directory CreateIndex()
	  {
		Directory directory = new RAMDirectory();

		IndexWriter writer = GetWriter(directory);

		for (int i = 0; i < 10; i++)
		{
		  Document doc = new Document();
		  doc.add(new StringField("foo", Convert.ToString(i), Store.YES));
		  writer.addDocument(doc);
		}

		writer.commit();
		writer.close();

		return directory;
	  }

	  public virtual void TestTryDeleteDocument()
	  {
		Directory directory = CreateIndex();

		IndexWriter writer = GetWriter(directory);

		ReferenceManager<IndexSearcher> mgr = new SearcherManager(writer, true, new SearcherFactory());

		TrackingIndexWriter mgrWriter = new TrackingIndexWriter(writer);

		IndexSearcher searcher = mgr.acquire();

		TopDocs topDocs = searcher.search(new TermQuery(new Term("foo", "0")), 100);
		Assert.AreEqual(1, topDocs.totalHits);

		long result;
		if (random().nextBoolean())
		{
		  IndexReader r = DirectoryReader.open(writer, true);
		  result = mgrWriter.tryDeleteDocument(r, 0);
		  r.close();
		}
		else
		{
		  result = mgrWriter.tryDeleteDocument(searcher.IndexReader, 0);
		}

		// The tryDeleteDocument should have succeeded:
		Assert.IsTrue(result != -1);

		Assert.IsTrue(writer.hasDeletions());

		if (random().nextBoolean())
		{
		  writer.commit();
		}

		Assert.IsTrue(writer.hasDeletions());

		mgr.maybeRefresh();

		searcher = mgr.acquire();

		topDocs = searcher.search(new TermQuery(new Term("foo", "0")), 100);

		Assert.AreEqual(0, topDocs.totalHits);
	  }

	  public virtual void TestTryDeleteDocumentCloseAndReopen()
	  {
		Directory directory = CreateIndex();

		IndexWriter writer = GetWriter(directory);

		ReferenceManager<IndexSearcher> mgr = new SearcherManager(writer, true, new SearcherFactory());

		IndexSearcher searcher = mgr.acquire();

		TopDocs topDocs = searcher.search(new TermQuery(new Term("foo", "0")), 100);
		Assert.AreEqual(1, topDocs.totalHits);

		TrackingIndexWriter mgrWriter = new TrackingIndexWriter(writer);
		long result = mgrWriter.tryDeleteDocument(DirectoryReader.open(writer, true), 0);

		Assert.AreEqual(1, result);

		writer.commit();

		Assert.IsTrue(writer.hasDeletions());

		mgr.maybeRefresh();

		searcher = mgr.acquire();

		topDocs = searcher.search(new TermQuery(new Term("foo", "0")), 100);

		Assert.AreEqual(0, topDocs.totalHits);

		writer.close();

		searcher = new IndexSearcher(DirectoryReader.open(directory));

		topDocs = searcher.search(new TermQuery(new Term("foo", "0")), 100);

		Assert.AreEqual(0, topDocs.totalHits);

	  }

	  public virtual void TestDeleteDocuments()
	  {
		Directory directory = CreateIndex();

		IndexWriter writer = GetWriter(directory);

		ReferenceManager<IndexSearcher> mgr = new SearcherManager(writer, true, new SearcherFactory());

		IndexSearcher searcher = mgr.acquire();

		TopDocs topDocs = searcher.search(new TermQuery(new Term("foo", "0")), 100);
		Assert.AreEqual(1, topDocs.totalHits);

		TrackingIndexWriter mgrWriter = new TrackingIndexWriter(writer);
		long result = mgrWriter.deleteDocuments(new TermQuery(new Term("foo", "0")));

		Assert.AreEqual(1, result);

		// writer.commit();

		Assert.IsTrue(writer.hasDeletions());

		mgr.maybeRefresh();

		searcher = mgr.acquire();

		topDocs = searcher.search(new TermQuery(new Term("foo", "0")), 100);

		Assert.AreEqual(0, topDocs.totalHits);
	  }
	}

}