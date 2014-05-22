using System;
using System.Collections.Generic;
using System.Threading;

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
	using Codec = Lucene.Net.Codecs.Codec;
	using FilterCodec = Lucene.Net.Codecs.FilterCodec;
	using PostingsFormat = Lucene.Net.Codecs.PostingsFormat;
	using Lucene46Codec = Lucene.Net.Codecs.lucene46.Lucene46Codec;
	using Pulsing41PostingsFormat = Lucene.Net.Codecs.pulsing.Pulsing41PostingsFormat;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using FieldType = Lucene.Net.Document.FieldType;
	using StringField = Lucene.Net.Document.StringField;
	using TextField = Lucene.Net.Document.TextField;
	using OpenMode = Lucene.Net.Index.IndexWriterConfig.OpenMode;
	using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
	using PhraseQuery = Lucene.Net.Search.PhraseQuery;
	using AlreadyClosedException = Lucene.Net.Store.AlreadyClosedException;
	using BaseDirectoryWrapper = Lucene.Net.Store.BaseDirectoryWrapper;
	using Directory = Lucene.Net.Store.Directory;
	using LockObtainFailedException = Lucene.Net.Store.LockObtainFailedException;
	using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
	using RAMDirectory = Lucene.Net.Store.RAMDirectory;
	using IOUtils = Lucene.Net.Util.IOUtils;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;

	public class TestAddIndexes : LuceneTestCase
	{

	  public virtual void TestSimpleCase()
	  {
		// main directory
		Directory dir = newDirectory();
		// two auxiliary directories
		Directory aux = newDirectory();
		Directory aux2 = newDirectory();

		IndexWriter writer = null;

		writer = NewWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.CREATE));
		// add 100 documents
		AddDocs(writer, 100);
		Assert.AreEqual(100, writer.maxDoc());
		writer.close();
		TestUtil.checkIndex(dir);

		writer = NewWriter(aux, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.CREATE).setMergePolicy(newLogMergePolicy(false)));
		// add 40 documents in separate files
		AddDocs(writer, 40);
		Assert.AreEqual(40, writer.maxDoc());
		writer.close();

		writer = NewWriter(aux2, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.CREATE));
		// add 50 documents in compound files
		AddDocs2(writer, 50);
		Assert.AreEqual(50, writer.maxDoc());
		writer.close();

		// test doc count before segments are merged
		writer = NewWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.APPEND));
		Assert.AreEqual(100, writer.maxDoc());
		writer.addIndexes(aux, aux2);
		Assert.AreEqual(190, writer.maxDoc());
		writer.close();
		TestUtil.checkIndex(dir);

		// make sure the old index is correct
		VerifyNumDocs(aux, 40);

		// make sure the new index is correct
		VerifyNumDocs(dir, 190);

		// now add another set in.
		Directory aux3 = newDirectory();
		writer = NewWriter(aux3, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		// add 40 documents
		AddDocs(writer, 40);
		Assert.AreEqual(40, writer.maxDoc());
		writer.close();

		// test doc count before segments are merged
		writer = NewWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.APPEND));
		Assert.AreEqual(190, writer.maxDoc());
		writer.addIndexes(aux3);
		Assert.AreEqual(230, writer.maxDoc());
		writer.close();

		// make sure the new index is correct
		VerifyNumDocs(dir, 230);

		VerifyTermDocs(dir, new Term("content", "aaa"), 180);

		VerifyTermDocs(dir, new Term("content", "bbb"), 50);

		// now fully merge it.
		writer = NewWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.APPEND));
		writer.forceMerge(1);
		writer.close();

		// make sure the new index is correct
		VerifyNumDocs(dir, 230);

		VerifyTermDocs(dir, new Term("content", "aaa"), 180);

		VerifyTermDocs(dir, new Term("content", "bbb"), 50);

		// now add a single document
		Directory aux4 = newDirectory();
		writer = NewWriter(aux4, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		AddDocs2(writer, 1);
		writer.close();

		writer = NewWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.APPEND));
		Assert.AreEqual(230, writer.maxDoc());
		writer.addIndexes(aux4);
		Assert.AreEqual(231, writer.maxDoc());
		writer.close();

		VerifyNumDocs(dir, 231);

		VerifyTermDocs(dir, new Term("content", "bbb"), 51);
		dir.close();
		aux.close();
		aux2.close();
		aux3.close();
		aux4.close();
	  }

	  public virtual void TestWithPendingDeletes()
	  {
		// main directory
		Directory dir = newDirectory();
		// auxiliary directory
		Directory aux = newDirectory();

		SetUpDirs(dir, aux);
		IndexWriter writer = NewWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.APPEND));
		writer.addIndexes(aux);

		// Adds 10 docs, then replaces them with another 10
		// docs, so 10 pending deletes:
		for (int i = 0; i < 20; i++)
		{
		  Document doc = new Document();
		  doc.add(newStringField("id", "" + (i % 10), Field.Store.NO));
		  doc.add(newTextField("content", "bbb " + i, Field.Store.NO));
		  writer.updateDocument(new Term("id", "" + (i % 10)), doc);
		}
		// Deletes one of the 10 added docs, leaving 9:
		PhraseQuery q = new PhraseQuery();
		q.add(new Term("content", "bbb"));
		q.add(new Term("content", "14"));
		writer.deleteDocuments(q);

		writer.forceMerge(1);
		writer.commit();

		VerifyNumDocs(dir, 1039);
		VerifyTermDocs(dir, new Term("content", "aaa"), 1030);
		VerifyTermDocs(dir, new Term("content", "bbb"), 9);

		writer.close();
		dir.close();
		aux.close();
	  }

	  public virtual void TestWithPendingDeletes2()
	  {
		// main directory
		Directory dir = newDirectory();
		// auxiliary directory
		Directory aux = newDirectory();

		SetUpDirs(dir, aux);
		IndexWriter writer = NewWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.APPEND));

		// Adds 10 docs, then replaces them with another 10
		// docs, so 10 pending deletes:
		for (int i = 0; i < 20; i++)
		{
		  Document doc = new Document();
		  doc.add(newStringField("id", "" + (i % 10), Field.Store.NO));
		  doc.add(newTextField("content", "bbb " + i, Field.Store.NO));
		  writer.updateDocument(new Term("id", "" + (i % 10)), doc);
		}

		writer.addIndexes(aux);

		// Deletes one of the 10 added docs, leaving 9:
		PhraseQuery q = new PhraseQuery();
		q.add(new Term("content", "bbb"));
		q.add(new Term("content", "14"));
		writer.deleteDocuments(q);

		writer.forceMerge(1);
		writer.commit();

		VerifyNumDocs(dir, 1039);
		VerifyTermDocs(dir, new Term("content", "aaa"), 1030);
		VerifyTermDocs(dir, new Term("content", "bbb"), 9);

		writer.close();
		dir.close();
		aux.close();
	  }

	  public virtual void TestWithPendingDeletes3()
	  {
		// main directory
		Directory dir = newDirectory();
		// auxiliary directory
		Directory aux = newDirectory();

		SetUpDirs(dir, aux);
		IndexWriter writer = NewWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.APPEND));

		// Adds 10 docs, then replaces them with another 10
		// docs, so 10 pending deletes:
		for (int i = 0; i < 20; i++)
		{
		  Document doc = new Document();
		  doc.add(newStringField("id", "" + (i % 10), Field.Store.NO));
		  doc.add(newTextField("content", "bbb " + i, Field.Store.NO));
		  writer.updateDocument(new Term("id", "" + (i % 10)), doc);
		}

		// Deletes one of the 10 added docs, leaving 9:
		PhraseQuery q = new PhraseQuery();
		q.add(new Term("content", "bbb"));
		q.add(new Term("content", "14"));
		writer.deleteDocuments(q);

		writer.addIndexes(aux);

		writer.forceMerge(1);
		writer.commit();

		VerifyNumDocs(dir, 1039);
		VerifyTermDocs(dir, new Term("content", "aaa"), 1030);
		VerifyTermDocs(dir, new Term("content", "bbb"), 9);

		writer.close();
		dir.close();
		aux.close();
	  }

	  // case 0: add self or exceed maxMergeDocs, expect exception
	  public virtual void TestAddSelf()
	  {
		// main directory
		Directory dir = newDirectory();
		// auxiliary directory
		Directory aux = newDirectory();

		IndexWriter writer = null;

		writer = NewWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		// add 100 documents
		AddDocs(writer, 100);
		Assert.AreEqual(100, writer.maxDoc());
		writer.close();

		writer = NewWriter(aux, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.CREATE).setMaxBufferedDocs(1000).setMergePolicy(newLogMergePolicy(false)));
		// add 140 documents in separate files
		AddDocs(writer, 40);
		writer.close();
		writer = NewWriter(aux, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.CREATE).setMaxBufferedDocs(1000).setMergePolicy(newLogMergePolicy(false)));
		AddDocs(writer, 100);
		writer.close();

		writer = NewWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.APPEND));
		try
		{
		  // cannot add self
		  writer.addIndexes(aux, dir);
		  Assert.IsTrue(false);
		}
		catch (System.ArgumentException e)
		{
		  Assert.AreEqual(100, writer.maxDoc());
		}
		writer.close();

		// make sure the index is correct
		VerifyNumDocs(dir, 100);
		dir.close();
		aux.close();
	  }

	  // in all the remaining tests, make the doc count of the oldest segment
	  // in dir large so that it is never merged in addIndexes()
	  // case 1: no tail segments
	  public virtual void TestNoTailSegments()
	  {
		// main directory
		Directory dir = newDirectory();
		// auxiliary directory
		Directory aux = newDirectory();

		SetUpDirs(dir, aux);

		IndexWriter writer = NewWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.APPEND).setMaxBufferedDocs(10).setMergePolicy(newLogMergePolicy(4)));
		AddDocs(writer, 10);

		writer.addIndexes(aux);
		Assert.AreEqual(1040, writer.maxDoc());
		Assert.AreEqual(1000, writer.getDocCount(0));
		writer.close();

		// make sure the index is correct
		VerifyNumDocs(dir, 1040);
		dir.close();
		aux.close();
	  }

	  // case 2: tail segments, invariants hold, no copy
	  public virtual void TestNoCopySegments()
	  {
		// main directory
		Directory dir = newDirectory();
		// auxiliary directory
		Directory aux = newDirectory();

		SetUpDirs(dir, aux);

		IndexWriter writer = NewWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.APPEND).setMaxBufferedDocs(9).setMergePolicy(newLogMergePolicy(4)));
		AddDocs(writer, 2);

		writer.addIndexes(aux);
		Assert.AreEqual(1032, writer.maxDoc());
		Assert.AreEqual(1000, writer.getDocCount(0));
		writer.close();

		// make sure the index is correct
		VerifyNumDocs(dir, 1032);
		dir.close();
		aux.close();
	  }

	  // case 3: tail segments, invariants hold, copy, invariants hold
	  public virtual void TestNoMergeAfterCopy()
	  {
		// main directory
		Directory dir = newDirectory();
		// auxiliary directory
		Directory aux = newDirectory();

		SetUpDirs(dir, aux);

		IndexWriter writer = NewWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.APPEND).setMaxBufferedDocs(10).setMergePolicy(newLogMergePolicy(4)));

		writer.addIndexes(aux, new MockDirectoryWrapper(random(), new RAMDirectory(aux, newIOContext(random()))));
		Assert.AreEqual(1060, writer.maxDoc());
		Assert.AreEqual(1000, writer.getDocCount(0));
		writer.close();

		// make sure the index is correct
		VerifyNumDocs(dir, 1060);
		dir.close();
		aux.close();
	  }

	  // case 4: tail segments, invariants hold, copy, invariants not hold
	  public virtual void TestMergeAfterCopy()
	  {
		// main directory
		Directory dir = newDirectory();
		// auxiliary directory
		Directory aux = newDirectory();

		SetUpDirs(dir, aux, true);

		IndexWriterConfig dontMergeConfig = (new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()))).setMergePolicy(NoMergePolicy.COMPOUND_FILES);
		IndexWriter writer = new IndexWriter(aux, dontMergeConfig);
		for (int i = 0; i < 20; i++)
		{
		  writer.deleteDocuments(new Term("id", "" + i));
		}
		writer.close();
		IndexReader reader = DirectoryReader.open(aux);
		Assert.AreEqual(10, reader.numDocs());
		reader.close();

		writer = NewWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.APPEND).setMaxBufferedDocs(4).setMergePolicy(newLogMergePolicy(4)));

		if (VERBOSE)
		{
		  Console.WriteLine("\nTEST: now addIndexes");
		}
		writer.addIndexes(aux, new MockDirectoryWrapper(random(), new RAMDirectory(aux, newIOContext(random()))));
		Assert.AreEqual(1020, writer.maxDoc());
		Assert.AreEqual(1000, writer.getDocCount(0));
		writer.close();
		dir.close();
		aux.close();
	  }

	  // case 5: tail segments, invariants not hold
	  public virtual void TestMoreMerges()
	  {
		// main directory
		Directory dir = newDirectory();
		// auxiliary directory
		Directory aux = newDirectory();
		Directory aux2 = newDirectory();

		SetUpDirs(dir, aux, true);

		IndexWriter writer = NewWriter(aux2, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.CREATE).setMaxBufferedDocs(100).setMergePolicy(newLogMergePolicy(10)));
		writer.addIndexes(aux);
		Assert.AreEqual(30, writer.maxDoc());
		Assert.AreEqual(3, writer.SegmentCount);
		writer.close();

		IndexWriterConfig dontMergeConfig = (new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()))).setMergePolicy(NoMergePolicy.COMPOUND_FILES);
		writer = new IndexWriter(aux, dontMergeConfig);
		for (int i = 0; i < 27; i++)
		{
		  writer.deleteDocuments(new Term("id", "" + i));
		}
		writer.close();
		IndexReader reader = DirectoryReader.open(aux);
		Assert.AreEqual(3, reader.numDocs());
		reader.close();

		dontMergeConfig = (new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()))).setMergePolicy(NoMergePolicy.COMPOUND_FILES);
		writer = new IndexWriter(aux2, dontMergeConfig);
		for (int i = 0; i < 8; i++)
		{
		  writer.deleteDocuments(new Term("id", "" + i));
		}
		writer.close();
		reader = DirectoryReader.open(aux2);
		Assert.AreEqual(22, reader.numDocs());
		reader.close();

		writer = NewWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.APPEND).setMaxBufferedDocs(6).setMergePolicy(newLogMergePolicy(4)));

		writer.addIndexes(aux, aux2);
		Assert.AreEqual(1040, writer.maxDoc());
		Assert.AreEqual(1000, writer.getDocCount(0));
		writer.close();
		dir.close();
		aux.close();
		aux2.close();
	  }

	  private IndexWriter NewWriter(Directory dir, IndexWriterConfig conf)
	  {
		conf.MergePolicy = new LogDocMergePolicy();
		IndexWriter writer = new IndexWriter(dir, conf);
		return writer;
	  }

	  private void AddDocs(IndexWriter writer, int numDocs)
	  {
		for (int i = 0; i < numDocs; i++)
		{
		  Document doc = new Document();
		  doc.add(newTextField("content", "aaa", Field.Store.NO));
		  writer.addDocument(doc);
		}
	  }

	  private void AddDocs2(IndexWriter writer, int numDocs)
	  {
		for (int i = 0; i < numDocs; i++)
		{
		  Document doc = new Document();
		  doc.add(newTextField("content", "bbb", Field.Store.NO));
		  writer.addDocument(doc);
		}
	  }

	  private void VerifyNumDocs(Directory dir, int numDocs)
	  {
		IndexReader reader = DirectoryReader.open(dir);
		Assert.AreEqual(numDocs, reader.maxDoc());
		Assert.AreEqual(numDocs, reader.numDocs());
		reader.close();
	  }

	  private void VerifyTermDocs(Directory dir, Term term, int numDocs)
	  {
		IndexReader reader = DirectoryReader.open(dir);
		DocsEnum docsEnum = TestUtil.docs(random(), reader, term.field, term.bytes, null, null, DocsEnum.FLAG_NONE);
		int count = 0;
		while (docsEnum.nextDoc() != DocIdSetIterator.NO_MORE_DOCS)
		{
		  count++;
		}
		Assert.AreEqual(numDocs, count);
		reader.close();
	  }

	  private void SetUpDirs(Directory dir, Directory aux)
	  {
		SetUpDirs(dir, aux, false);
	  }

	  private void SetUpDirs(Directory dir, Directory aux, bool withID)
	  {
		IndexWriter writer = null;

		writer = NewWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.CREATE).setMaxBufferedDocs(1000));
		// add 1000 documents in 1 segment
		if (withID)
		{
		  AddDocsWithID(writer, 1000, 0);
		}
		else
		{
		  AddDocs(writer, 1000);
		}
		Assert.AreEqual(1000, writer.maxDoc());
		Assert.AreEqual(1, writer.SegmentCount);
		writer.close();

		writer = NewWriter(aux, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.CREATE).setMaxBufferedDocs(1000).setMergePolicy(newLogMergePolicy(false, 10)));
		// add 30 documents in 3 segments
		for (int i = 0; i < 3; i++)
		{
		  if (withID)
		  {
			AddDocsWithID(writer, 10, 10 * i);
		  }
		  else
		  {
			AddDocs(writer, 10);
		  }
		  writer.close();
		  writer = NewWriter(aux, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.APPEND).setMaxBufferedDocs(1000).setMergePolicy(newLogMergePolicy(false, 10)));
		}
		Assert.AreEqual(30, writer.maxDoc());
		Assert.AreEqual(3, writer.SegmentCount);
		writer.close();
	  }

	  // LUCENE-1270
	  public virtual void TestHangOnClose()
	  {

		Directory dir = newDirectory();
		LogByteSizeMergePolicy lmp = new LogByteSizeMergePolicy();
		lmp.NoCFSRatio = 0.0;
		lmp.MergeFactor = 100;
		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(5).setMergePolicy(lmp));

		Document doc = new Document();
		FieldType customType = new FieldType(TextField.TYPE_STORED);
		customType.StoreTermVectors = true;
		customType.StoreTermVectorPositions = true;
		customType.StoreTermVectorOffsets = true;
		doc.add(newField("content", "aaa bbb ccc ddd eee fff ggg hhh iii", customType));
		for (int i = 0;i < 60;i++)
		{
		  writer.addDocument(doc);
		}

		Document doc2 = new Document();
		FieldType customType2 = new FieldType();
		customType2.Stored = true;
		doc2.add(newField("content", "aaa bbb ccc ddd eee fff ggg hhh iii", customType2));
		doc2.add(newField("content", "aaa bbb ccc ddd eee fff ggg hhh iii", customType2));
		doc2.add(newField("content", "aaa bbb ccc ddd eee fff ggg hhh iii", customType2));
		doc2.add(newField("content", "aaa bbb ccc ddd eee fff ggg hhh iii", customType2));
		for (int i = 0;i < 10;i++)
		{
		  writer.addDocument(doc2);
		}
		writer.close();

		Directory dir2 = newDirectory();
		lmp = new LogByteSizeMergePolicy();
		lmp.MinMergeMB = 0.0001;
		lmp.NoCFSRatio = 0.0;
		lmp.MergeFactor = 4;
		writer = new IndexWriter(dir2, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMergeScheduler(new SerialMergeScheduler()).setMergePolicy(lmp));
		writer.addIndexes(dir);
		writer.close();
		dir.close();
		dir2.close();
	  }

	  // TODO: these are also in TestIndexWriter... add a simple doc-writing method
	  // like this to LuceneTestCase?
	  private void AddDoc(IndexWriter writer)
	  {
		  Document doc = new Document();
		  doc.add(newTextField("content", "aaa", Field.Store.NO));
		  writer.addDocument(doc);
	  }

	  private abstract class RunAddIndexesThreads
	  {
		  private readonly TestAddIndexes OuterInstance;


		internal Directory Dir, Dir2;
		internal const int NUM_INIT_DOCS = 17;
		internal IndexWriter Writer2;
		internal readonly IList<Exception> Failures = new List<Exception>();
		internal volatile bool DidClose;
		internal readonly IndexReader[] Readers;
		internal readonly int NUM_COPY;
		internal const int NUM_THREADS = 5;
		internal readonly Thread[] Threads = new Thread[NUM_THREADS];

		public RunAddIndexesThreads(TestAddIndexes outerInstance, int numCopy)
		{
			this.OuterInstance = outerInstance;
		  NUM_COPY = numCopy;
		  Dir = new MockDirectoryWrapper(random(), new RAMDirectory());
		  IndexWriter writer = new IndexWriter(Dir, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()))
			 .setMaxBufferedDocs(2));
		  for (int i = 0; i < NUM_INIT_DOCS; i++)
		  {
			outerInstance.AddDoc(writer);
		  }
		  writer.close();

		  Dir2 = newDirectory();
		  Writer2 = new IndexWriter(Dir2, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		  Writer2.commit();


		  Readers = new IndexReader[NUM_COPY];
		  for (int i = 0;i < NUM_COPY;i++)
		  {
			Readers[i] = DirectoryReader.open(Dir);
		  }
		}

		internal virtual void LaunchThreads(int numIter)
		{

		  for (int i = 0;i < NUM_THREADS;i++)
		  {
			Threads[i] = new ThreadAnonymousInnerClassHelper(this, numIter);
		  }

		  for (int i = 0;i < NUM_THREADS;i++)
		  {
			Threads[i].Start();
		  }
		}

		private class ThreadAnonymousInnerClassHelper : System.Threading.Thread
		{
			private readonly RunAddIndexesThreads OuterInstance;

			private int NumIter;

			public ThreadAnonymousInnerClassHelper(RunAddIndexesThreads outerInstance, int numIter)
			{
				this.OuterInstance = outerInstance;
				this.NumIter = numIter;
			}

			public override void Run()
			{
			  try
			  {

				Directory[] dirs = new Directory[OuterInstance.NUM_COPY];
				for (int k = 0;k < OuterInstance.NUM_COPY;k++)
				{
				  dirs[k] = new MockDirectoryWrapper(random(), new RAMDirectory(OuterInstance.Dir, newIOContext(random())));
				}

				int j = 0;

				while (true)
				{
				  // System.out.println(Thread.currentThread().getName() + ": iter j=" + j);
				  if (NumIter > 0 && j == NumIter)
				  {
					break;
				  }
				  outerInstance.DoBody(j++, dirs);
				}
			  }
			  catch (Exception t)
			  {
				outerInstance.Handle(t);
			  }
			}
		}

		internal virtual void JoinThreads()
		{
		  for (int i = 0;i < NUM_THREADS;i++)
		  {
			Threads[i].Join();
		  }
		}

		internal virtual void Close(bool doWait)
		{
		  DidClose = true;
		  Writer2.close(doWait);
		}

		internal virtual void CloseDir()
		{
		  for (int i = 0;i < NUM_COPY;i++)
		  {
			Readers[i].close();
		  }
		  Dir2.close();
		}

		internal abstract void DoBody(int j, Directory[] dirs);
		internal abstract void Handle(Exception t);
	  }

	  private class CommitAndAddIndexes : RunAddIndexesThreads
	  {
		  private readonly TestAddIndexes OuterInstance;

		public CommitAndAddIndexes(TestAddIndexes outerInstance, int numCopy) : base(outerInstance, numCopy)
		{
			this.OuterInstance = outerInstance;
		}

		internal override void Handle(Exception t)
		{
		  t.printStackTrace(System.out);
		  lock (Failures)
		  {
			Failures.Add(t);
		  }
		}

		internal override void DoBody(int j, Directory[] dirs)
		{
		  switch (j % 5)
		  {
		  case 0:
			if (VERBOSE)
			{
			  Console.WriteLine(Thread.CurrentThread.Name + ": TEST: addIndexes(Dir[]) then full merge");
			}
			Writer2.addIndexes(dirs);
			Writer2.forceMerge(1);
			break;
		  case 1:
			if (VERBOSE)
			{
			  Console.WriteLine(Thread.CurrentThread.Name + ": TEST: addIndexes(Dir[])");
			}
			Writer2.addIndexes(dirs);
			break;
		  case 2:
			if (VERBOSE)
			{
			  Console.WriteLine(Thread.CurrentThread.Name + ": TEST: addIndexes(IndexReader[])");
			}
			Writer2.addIndexes(Readers);
			break;
		  case 3:
			if (VERBOSE)
			{
			  Console.WriteLine(Thread.CurrentThread.Name + ": TEST: addIndexes(Dir[]) then maybeMerge");
			}
			Writer2.addIndexes(dirs);
			Writer2.maybeMerge();
			break;
		  case 4:
			if (VERBOSE)
			{
			  Console.WriteLine(Thread.CurrentThread.Name + ": TEST: commit");
			}
			Writer2.commit();
		break;
		  }
		}
	  }

	  // LUCENE-1335: test simultaneous addIndexes & commits
	  // from multiple threads
	  public virtual void TestAddIndexesWithThreads()
	  {

		int NUM_ITER = TEST_NIGHTLY ? 15 : 5;
		const int NUM_COPY = 3;
		CommitAndAddIndexes c = new CommitAndAddIndexes(this, NUM_COPY);
		c.LaunchThreads(NUM_ITER);

		for (int i = 0;i < 100;i++)
		{
		  AddDoc(c.Writer2);
		}

		c.JoinThreads();

		int expectedNumDocs = 100 + NUM_COPY * (4 * NUM_ITER / 5) * RunAddIndexesThreads.NUM_THREADS * RunAddIndexesThreads.NUM_INIT_DOCS;
		Assert.AreEqual("expected num docs don't match - failures: " + c.Failures, expectedNumDocs, c.Writer2.numDocs());

		c.Close(true);

		Assert.IsTrue("found unexpected failures: " + c.Failures, c.Failures.Count == 0);

		IndexReader reader = DirectoryReader.open(c.Dir2);
		Assert.AreEqual(expectedNumDocs, reader.numDocs());
		reader.close();

		c.CloseDir();
	  }

	  private class CommitAndAddIndexes2 : CommitAndAddIndexes
	  {
		  private readonly TestAddIndexes OuterInstance;

		public CommitAndAddIndexes2(TestAddIndexes outerInstance, int numCopy) : base(outerInstance, numCopy)
		{
			this.OuterInstance = outerInstance;
		}

		internal override void Handle(Exception t)
		{
		  if (!(t is AlreadyClosedException) && !(t is System.NullReferenceException))
		  {
			t.printStackTrace(System.out);
			lock (Failures)
			{
			  Failures.Add(t);
			}
		  }
		}
	  }

	  // LUCENE-1335: test simultaneous addIndexes & close
	  public virtual void TestAddIndexesWithClose()
	  {
		const int NUM_COPY = 3;
		CommitAndAddIndexes2 c = new CommitAndAddIndexes2(this, NUM_COPY);
		//c.writer2.setInfoStream(System.out);
		c.LaunchThreads(-1);

		// Close w/o first stopping/joining the threads
		c.Close(true);
		//c.writer2.close();

		c.JoinThreads();

		c.CloseDir();

		Assert.IsTrue(c.Failures.Count == 0);
	  }

	  private class CommitAndAddIndexes3 : RunAddIndexesThreads
	  {
		  private readonly TestAddIndexes OuterInstance;

		public CommitAndAddIndexes3(TestAddIndexes outerInstance, int numCopy) : base(outerInstance, numCopy)
		{
			this.OuterInstance = outerInstance;
		}

		internal override void DoBody(int j, Directory[] dirs)
		{
		  switch (j % 5)
		  {
		  case 0:
			if (VERBOSE)
			{
			  Console.WriteLine("TEST: " + Thread.CurrentThread.Name + ": addIndexes + full merge");
			}
			Writer2.addIndexes(dirs);
			Writer2.forceMerge(1);
			break;
		  case 1:
			if (VERBOSE)
			{
			  Console.WriteLine("TEST: " + Thread.CurrentThread.Name + ": addIndexes");
			}
			Writer2.addIndexes(dirs);
			break;
		  case 2:
			if (VERBOSE)
			{
			  Console.WriteLine("TEST: " + Thread.CurrentThread.Name + ": addIndexes(IR[])");
			}
			Writer2.addIndexes(Readers);
			break;
		  case 3:
			if (VERBOSE)
			{
			  Console.WriteLine("TEST: " + Thread.CurrentThread.Name + ": full merge");
			}
			Writer2.forceMerge(1);
			break;
		  case 4:
			if (VERBOSE)
			{
			  Console.WriteLine("TEST: " + Thread.CurrentThread.Name + ": commit");
			}
			Writer2.commit();
		break;
		  }
		}

		internal override void Handle(Exception t)
		{
		  bool report = true;

		  if (t is AlreadyClosedException || t is MergePolicy.MergeAbortedException || t is System.NullReferenceException)
		  {
			report = !DidClose;
		  }
		  else if (t is FileNotFoundException || t is NoSuchFileException)
		  {
			report = !DidClose;
		  }
		  else if (t is IOException)
		  {
			Exception t2 = t.InnerException;
			if (t2 is MergePolicy.MergeAbortedException)
			{
			  report = !DidClose;
			}
		  }
		  if (report)
		  {
			t.printStackTrace(System.out);
			lock (Failures)
			{
			  Failures.Add(t);
			}
		  }
		}
	  }

	  // LUCENE-1335: test simultaneous addIndexes & close
	  public virtual void TestAddIndexesWithCloseNoWait()
	  {

		const int NUM_COPY = 50;
		CommitAndAddIndexes3 c = new CommitAndAddIndexes3(this, NUM_COPY);
		c.LaunchThreads(-1);

		Thread.Sleep(TestUtil.Next(random(), 10, 500));

		// Close w/o first stopping/joining the threads
		if (VERBOSE)
		{
		  Console.WriteLine("TEST: now close(false)");
		}
		c.Close(false);

		c.JoinThreads();

		if (VERBOSE)
		{
		  Console.WriteLine("TEST: done join threads");
		}
		c.CloseDir();

		Assert.IsTrue(c.Failures.Count == 0);
	  }

	  // LUCENE-1335: test simultaneous addIndexes & close
	  public virtual void TestAddIndexesWithRollback()
	  {

		int NUM_COPY = TEST_NIGHTLY ? 50 : 5;
		CommitAndAddIndexes3 c = new CommitAndAddIndexes3(this, NUM_COPY);
		c.LaunchThreads(-1);

		Thread.Sleep(TestUtil.Next(random(), 10, 500));

		// Close w/o first stopping/joining the threads
		if (VERBOSE)
		{
		  Console.WriteLine("TEST: now force rollback");
		}
		c.DidClose = true;
		c.Writer2.rollback();

		c.JoinThreads();

		c.CloseDir();

		Assert.IsTrue(c.Failures.Count == 0);
	  }

	  // LUCENE-2996: tests that addIndexes(IndexReader) applies existing deletes correctly.
	  public virtual void TestExistingDeletes()
	  {
		Directory[] dirs = new Directory[2];
		for (int i = 0; i < dirs.Length; i++)
		{
		  dirs[i] = newDirectory();
		  IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		  IndexWriter writer = new IndexWriter(dirs[i], conf);
		  Document doc = new Document();
		  doc.add(new StringField("id", "myid", Field.Store.NO));
		  writer.addDocument(doc);
		  writer.close();
		}

		IndexWriterConfig conf = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		IndexWriter writer = new IndexWriter(dirs[0], conf);

		// Now delete the document
		writer.deleteDocuments(new Term("id", "myid"));
		IndexReader r = DirectoryReader.open(dirs[1]);
		try
		{
		  writer.addIndexes(r);
		}
		finally
		{
		  r.close();
		}
		writer.commit();
		Assert.AreEqual("Documents from the incoming index should not have been deleted", 1, writer.numDocs());
		writer.close();

		foreach (Directory dir in dirs)
		{
		  dir.close();
		}

	  }

	  // just like addDocs but with ID, starting from docStart
	  private void AddDocsWithID(IndexWriter writer, int numDocs, int docStart)
	  {
		for (int i = 0; i < numDocs; i++)
		{
		  Document doc = new Document();
		  doc.add(newTextField("content", "aaa", Field.Store.NO));
		  doc.add(newTextField("id", "" + (docStart + i), Field.Store.YES));
		  writer.addDocument(doc);
		}
	  }

	  public virtual void TestSimpleCaseCustomCodec()
	  {
		// main directory
		Directory dir = newDirectory();
		// two auxiliary directories
		Directory aux = newDirectory();
		Directory aux2 = newDirectory();
		Codec codec = new CustomPerFieldCodec();
		IndexWriter writer = null;

		writer = NewWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.CREATE).setCodec(codec));
		// add 100 documents
		AddDocsWithID(writer, 100, 0);
		Assert.AreEqual(100, writer.maxDoc());
		writer.commit();
		writer.close();
		TestUtil.checkIndex(dir);

		writer = NewWriter(aux, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.CREATE).setCodec(codec).setMaxBufferedDocs(10).setMergePolicy(newLogMergePolicy(false)));
		// add 40 documents in separate files
		AddDocs(writer, 40);
		Assert.AreEqual(40, writer.maxDoc());
		writer.commit();
		writer.close();

		writer = NewWriter(aux2, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.CREATE).setCodec(codec));
		// add 40 documents in compound files
		AddDocs2(writer, 50);
		Assert.AreEqual(50, writer.maxDoc());
		writer.commit();
		writer.close();

		// test doc count before segments are merged
		writer = NewWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.APPEND).setCodec(codec));
		Assert.AreEqual(100, writer.maxDoc());
		writer.addIndexes(aux, aux2);
		Assert.AreEqual(190, writer.maxDoc());
		writer.close();

		dir.close();
		aux.close();
		aux2.close();
	  }

	  private sealed class CustomPerFieldCodec : Lucene46Codec
	  {
		internal readonly PostingsFormat SimpleTextFormat = PostingsFormat.forName("SimpleText");
		internal readonly PostingsFormat DefaultFormat = PostingsFormat.forName("Lucene41");
		internal readonly PostingsFormat MockSepFormat = PostingsFormat.forName("MockSep");

		public override PostingsFormat GetPostingsFormatForField(string field)
		{
		  if (field.Equals("id"))
		  {
			return SimpleTextFormat;
		  }
		  else if (field.Equals("content"))
		  {
			return MockSepFormat;
		  }
		  else
		  {
			return DefaultFormat;
		  }
		}
	  }


	  // LUCENE-2790: tests that the non CFS files were deleted by addIndexes
	  public virtual void TestNonCFSLeftovers()
	  {
		Directory[] dirs = new Directory[2];
		for (int i = 0; i < dirs.Length; i++)
		{
		  dirs[i] = new RAMDirectory();
		  IndexWriter w = new IndexWriter(dirs[i], new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		  Document d = new Document();
		  FieldType customType = new FieldType(TextField.TYPE_STORED);
		  customType.StoreTermVectors = true;
		  d.add(new Field("c", "v", customType));
		  w.addDocument(d);
		  w.close();
		}

		IndexReader[] readers = new IndexReader[] {DirectoryReader.open(dirs[0]), DirectoryReader.open(dirs[1])};

		Directory dir = new MockDirectoryWrapper(random(), new RAMDirectory());
		IndexWriterConfig conf = (new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()))).setMergePolicy(newLogMergePolicy(true));
		MergePolicy lmp = conf.MergePolicy;
		// Force creation of CFS:
		lmp.NoCFSRatio = 1.0;
		lmp.MaxCFSSegmentSizeMB = double.PositiveInfinity;
		IndexWriter w3 = new IndexWriter(dir, conf);
		w3.addIndexes(readers);
		w3.close();
		// we should now see segments_X,
		// segments.gen,_Y.cfs,_Y.cfe, _Z.si
		Assert.AreEqual("Only one compound segment should exist, but got: " + Arrays.ToString(dir.listAll()), 5, dir.listAll().length);
		dir.close();
	  }

	  private sealed class UnRegisteredCodec : FilterCodec
	  {
		public UnRegisteredCodec() : base("NotRegistered", new Lucene46Codec())
		{
		}
	  }

	  /*
	   * simple test that ensures we getting expected exceptions 
	   */
	  public virtual void TestAddIndexMissingCodec()
	  {
		BaseDirectoryWrapper toAdd = newDirectory();
		// Disable checkIndex, else we get an exception because
		// of the unregistered codec:
		toAdd.CheckIndexOnClose = false;
		{
		  IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		  conf.Codec = new UnRegisteredCodec();
		  IndexWriter w = new IndexWriter(toAdd, conf);
		  Document doc = new Document();
		  FieldType customType = new FieldType();
		  customType.Indexed = true;
		  doc.add(newField("foo", "bar", customType));
		  w.addDocument(doc);
		  w.close();
		}

		{
		  Directory dir = newDirectory();
		  IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		  conf.Codec = TestUtil.alwaysPostingsFormat(new Pulsing41PostingsFormat(1 + random().Next(20)));
		  IndexWriter w = new IndexWriter(dir, conf);
		  try
		  {
			w.addIndexes(toAdd);
			Assert.Fail("no such codec");
		  }
		  catch (System.ArgumentException ex)
		  {
			// expected
		  }
		  w.close();
		  IndexReader open = DirectoryReader.open(dir);
		  Assert.AreEqual(0, open.numDocs());
		  open.close();
		  dir.close();
		}

		try
		{
		  DirectoryReader.open(toAdd);
		  Assert.Fail("no such codec");
		}
		catch (System.ArgumentException ex)
		{
		  // expected
		}
		toAdd.close();
	  }

	  // LUCENE-3575
	  public virtual void TestFieldNamesChanged()
	  {
		Directory d1 = newDirectory();
		RandomIndexWriter w = new RandomIndexWriter(random(), d1);
		Document doc = new Document();
		doc.add(newStringField("f1", "doc1 field1", Field.Store.YES));
		doc.add(newStringField("id", "1", Field.Store.YES));
		w.addDocument(doc);
		IndexReader r1 = w.Reader;
		w.close();

		Directory d2 = newDirectory();
		w = new RandomIndexWriter(random(), d2);
		doc = new Document();
		doc.add(newStringField("f2", "doc2 field2", Field.Store.YES));
		doc.add(newStringField("id", "2", Field.Store.YES));
		w.addDocument(doc);
		IndexReader r2 = w.Reader;
		w.close();

		Directory d3 = newDirectory();
		w = new RandomIndexWriter(random(), d3);
		w.addIndexes(r1, r2);
		r1.close();
		d1.close();
		r2.close();
		d2.close();

		IndexReader r3 = w.Reader;
		w.close();
		Assert.AreEqual(2, r3.numDocs());
		for (int docID = 0;docID < 2;docID++)
		{
		  Document d = r3.document(docID);
		  if (d.get("id").Equals("1"))
		  {
			Assert.AreEqual("doc1 field1", d.get("f1"));
		  }
		  else
		  {
			Assert.AreEqual("doc2 field2", d.get("f2"));
		  }
		}
		r3.close();
		d3.close();
	  }

	  public virtual void TestAddEmpty()
	  {
		Directory d1 = newDirectory();
		RandomIndexWriter w = new RandomIndexWriter(random(), d1);
		MultiReader empty = new MultiReader();
		w.addIndexes(empty);
		w.close();
		DirectoryReader dr = DirectoryReader.open(d1);
		foreach (AtomicReaderContext ctx in dr.leaves())
		{
		  Assert.IsTrue("empty segments should be dropped by addIndexes", ctx.reader().maxDoc() > 0);
		}
		dr.close();
		d1.close();
	  }

	  // Currently it's impossible to end up with a segment with all documents
	  // deleted, as such segments are dropped. Still, to validate that addIndexes
	  // works with such segments, or readers that end up in such state, we fake an
	  // all deleted segment.
	  public virtual void TestFakeAllDeleted()
	  {
		Directory src = newDirectory(), dest = newDirectory();
		RandomIndexWriter w = new RandomIndexWriter(random(), src);
		w.addDocument(new Document());
		IndexReader allDeletedReader = new AllDeletedFilterReader(w.Reader.leaves().get(0).reader());
		w.close();

		w = new RandomIndexWriter(random(), dest);
		w.addIndexes(allDeletedReader);
		w.close();
		DirectoryReader dr = DirectoryReader.open(src);
		foreach (AtomicReaderContext ctx in dr.leaves())
		{
		  Assert.IsTrue("empty segments should be dropped by addIndexes", ctx.reader().maxDoc() > 0);
		}
		dr.close();
		allDeletedReader.close();
		src.close();
		dest.close();
	  }

	  /// <summary>
	  /// Make sure an open IndexWriter on an incoming Directory
	  ///  causes a LockObtainFailedException 
	  /// </summary>
	  public virtual void TestLocksBlock()
	  {
		Directory src = newDirectory();
		RandomIndexWriter w1 = new RandomIndexWriter(random(), src);
		w1.addDocument(new Document());
		w1.commit();

		Directory dest = newDirectory();

		IndexWriterConfig iwc = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		iwc.WriteLockTimeout = 1;
		RandomIndexWriter w2 = new RandomIndexWriter(random(), dest, iwc);

		try
		{
		  w2.addIndexes(src);
		  Assert.Fail("did not hit expected exception");
		}
		catch (LockObtainFailedException lofe)
		{
		  // expected
		}

		IOUtils.close(w1, w2, src, dest);
	  }
	}

}