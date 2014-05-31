using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;

namespace Lucene.Net.Index
{
	/// <summary>
	/// Copyright 2006 The Apache Software Foundation
	/// 
	/// Licensed under the Apache License, Version 2.0 (the "License");
	/// you may not use this file except in compliance with the License.
	/// You may obtain a copy of the License at
	/// 
	///     http://www.apache.org/licenses/LICENSE-2.0
	/// 
	/// Unless required by applicable law or agreed to in writing, software
	/// distributed under the License is distributed on an "AS IS" BASIS,
	/// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	/// See the License for the specific language governing permissions and
	/// limitations under the License.
	/// </summary>

	using AlreadyClosedException = Lucene.Net.Store.AlreadyClosedException;
	using Directory = Lucene.Net.Store.Directory;
	using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using FieldType = Lucene.Net.Document.FieldType;
	using TextField = Lucene.Net.Document.TextField;
	using OpenMode = Lucene.Net.Index.IndexWriterConfig.OpenMode_e;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;



	public class TestIndexWriterMerging : LuceneTestCase
	{

	  /// <summary>
	  /// Tests that index merging (specifically addIndexes(Directory...)) doesn't
	  /// change the index order of documents.
	  /// </summary>
	  public virtual void TestLucene()
	  {
		int num = 100;

		Directory indexA = newDirectory();
		Directory indexB = newDirectory();

		FillIndex(random(), indexA, 0, num);
		bool fail = VerifyIndex(indexA, 0);
		if (fail)
		{
		  Assert.Fail("Index a is invalid");
		}

		FillIndex(random(), indexB, num, num);
		fail = VerifyIndex(indexB, num);
		if (fail)
		{
		  Assert.Fail("Index b is invalid");
		}

		Directory merged = newDirectory();

		IndexWriter writer = new IndexWriter(merged, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMergePolicy(newLogMergePolicy(2)));
		writer.addIndexes(indexA, indexB);
		writer.forceMerge(1);
		writer.close();

		fail = VerifyIndex(merged, 0);

		Assert.IsFalse("The merged index is invalid", fail);
		indexA.close();
		indexB.close();
		merged.close();
	  }

	  private bool VerifyIndex(Directory directory, int startAt)
	  {
		bool fail = false;
		IndexReader reader = DirectoryReader.open(directory);

		int max = reader.maxDoc();
		for (int i = 0; i < max; i++)
		{
		  Document temp = reader.document(i);
		  //System.out.println("doc "+i+"="+temp.getField("count").stringValue());
		  //compare the index doc number to the value that it should be
		  if (!temp.getField("count").stringValue().Equals((i + startAt) + ""))
		  {
			fail = true;
			Console.WriteLine("Document " + (i + startAt) + " is returning document " + temp.getField("count").stringValue());
		  }
		}
		reader.close();
		return fail;
	  }

	  private void FillIndex(Random random, Directory dir, int start, int numDocs)
	  {

		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random)).setOpenMode(OpenMode.CREATE).setMaxBufferedDocs(2).setMergePolicy(newLogMergePolicy(2)));

		for (int i = start; i < (start + numDocs); i++)
		{
		  Document temp = new Document();
		  temp.add(newStringField("count", ("" + i), Field.Store.YES));

		  writer.addDocument(temp);
		}
		writer.close();
	  }

	  // LUCENE-325: test forceMergeDeletes, when 2 singular merges
	  // are required
	  public virtual void TestForceMergeDeletes()
	  {
		Directory dir = newDirectory();
		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(2).setRAMBufferSizeMB(IndexWriterConfig.DISABLE_AUTO_FLUSH));
		Document document = new Document();

		FieldType customType = new FieldType();
		customType.Stored = true;

		FieldType customType1 = new FieldType(TextField.TYPE_NOT_STORED);
		customType1.Tokenized = false;
		customType1.StoreTermVectors = true;
		customType1.StoreTermVectorPositions = true;
		customType1.StoreTermVectorOffsets = true;

		Field idField = newStringField("id", "", Field.Store.NO);
		document.add(idField);
		Field storedField = newField("stored", "stored", customType);
		document.add(storedField);
		Field termVectorField = newField("termVector", "termVector", customType1);
		document.add(termVectorField);
		for (int i = 0;i < 10;i++)
		{
		  idField.StringValue = "" + i;
		  writer.addDocument(document);
		}
		writer.close();

		IndexReader ir = DirectoryReader.open(dir);
		Assert.AreEqual(10, ir.maxDoc());
		Assert.AreEqual(10, ir.numDocs());
		ir.close();

		IndexWriterConfig dontMergeConfig = (new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()))).setMergePolicy(NoMergePolicy.COMPOUND_FILES);
		writer = new IndexWriter(dir, dontMergeConfig);
		writer.deleteDocuments(new Term("id", "0"));
		writer.deleteDocuments(new Term("id", "7"));
		writer.close();

		ir = DirectoryReader.open(dir);
		Assert.AreEqual(8, ir.numDocs());
		ir.close();

		writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMergePolicy(newLogMergePolicy()));
		Assert.AreEqual(8, writer.numDocs());
		Assert.AreEqual(10, writer.maxDoc());
		writer.forceMergeDeletes();
		Assert.AreEqual(8, writer.numDocs());
		writer.close();
		ir = DirectoryReader.open(dir);
		Assert.AreEqual(8, ir.maxDoc());
		Assert.AreEqual(8, ir.numDocs());
		ir.close();
		dir.close();
	  }

	  // LUCENE-325: test forceMergeDeletes, when many adjacent merges are required
	  public virtual void TestForceMergeDeletes2()
	  {
		Directory dir = newDirectory();
		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(2).setRAMBufferSizeMB(IndexWriterConfig.DISABLE_AUTO_FLUSH).setMergePolicy(newLogMergePolicy(50)));

		Document document = new Document();

		FieldType customType = new FieldType();
		customType.Stored = true;

		FieldType customType1 = new FieldType(TextField.TYPE_NOT_STORED);
		customType1.Tokenized = false;
		customType1.StoreTermVectors = true;
		customType1.StoreTermVectorPositions = true;
		customType1.StoreTermVectorOffsets = true;

		Field storedField = newField("stored", "stored", customType);
		document.add(storedField);
		Field termVectorField = newField("termVector", "termVector", customType1);
		document.add(termVectorField);
		Field idField = newStringField("id", "", Field.Store.NO);
		document.add(idField);
		for (int i = 0;i < 98;i++)
		{
		  idField.StringValue = "" + i;
		  writer.addDocument(document);
		}
		writer.close();

		IndexReader ir = DirectoryReader.open(dir);
		Assert.AreEqual(98, ir.maxDoc());
		Assert.AreEqual(98, ir.numDocs());
		ir.close();

		IndexWriterConfig dontMergeConfig = (new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()))).setMergePolicy(NoMergePolicy.COMPOUND_FILES);
		writer = new IndexWriter(dir, dontMergeConfig);
		for (int i = 0;i < 98;i += 2)
		{
		  writer.deleteDocuments(new Term("id", "" + i));
		}
		writer.close();

		ir = DirectoryReader.open(dir);
		Assert.AreEqual(49, ir.numDocs());
		ir.close();

		writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMergePolicy(newLogMergePolicy(3)));
		Assert.AreEqual(49, writer.numDocs());
		writer.forceMergeDeletes();
		writer.close();
		ir = DirectoryReader.open(dir);
		Assert.AreEqual(49, ir.maxDoc());
		Assert.AreEqual(49, ir.numDocs());
		ir.close();
		dir.close();
	  }

	  // LUCENE-325: test forceMergeDeletes without waiting, when
	  // many adjacent merges are required
	  public virtual void TestForceMergeDeletes3()
	  {
		Directory dir = newDirectory();
		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(2).setRAMBufferSizeMB(IndexWriterConfig.DISABLE_AUTO_FLUSH).setMergePolicy(newLogMergePolicy(50)));

		FieldType customType = new FieldType();
		customType.Stored = true;

		FieldType customType1 = new FieldType(TextField.TYPE_NOT_STORED);
		customType1.Tokenized = false;
		customType1.StoreTermVectors = true;
		customType1.StoreTermVectorPositions = true;
		customType1.StoreTermVectorOffsets = true;

		Document document = new Document();
		Field storedField = newField("stored", "stored", customType);
		document.add(storedField);
		Field termVectorField = newField("termVector", "termVector", customType1);
		document.add(termVectorField);
		Field idField = newStringField("id", "", Field.Store.NO);
		document.add(idField);
		for (int i = 0;i < 98;i++)
		{
		  idField.StringValue = "" + i;
		  writer.addDocument(document);
		}
		writer.close();

		IndexReader ir = DirectoryReader.open(dir);
		Assert.AreEqual(98, ir.maxDoc());
		Assert.AreEqual(98, ir.numDocs());
		ir.close();

		IndexWriterConfig dontMergeConfig = (new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()))).setMergePolicy(NoMergePolicy.COMPOUND_FILES);
		writer = new IndexWriter(dir, dontMergeConfig);
		for (int i = 0;i < 98;i += 2)
		{
		  writer.deleteDocuments(new Term("id", "" + i));
		}
		writer.close();
		ir = DirectoryReader.open(dir);
		Assert.AreEqual(49, ir.numDocs());
		ir.close();

		writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMergePolicy(newLogMergePolicy(3)));
		writer.forceMergeDeletes(false);
		writer.close();
		ir = DirectoryReader.open(dir);
		Assert.AreEqual(49, ir.maxDoc());
		Assert.AreEqual(49, ir.numDocs());
		ir.close();
		dir.close();
	  }

	  // Just intercepts all merges & verifies that we are never
	  // merging a segment with >= 20 (maxMergeDocs) docs
	  private class MyMergeScheduler : MergeScheduler
	  {
		  private readonly TestIndexWriterMerging OuterInstance;

		  public MyMergeScheduler(TestIndexWriterMerging outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }

		public override void Merge(IndexWriter writer, MergeTrigger trigger, bool newMergesFound)
		{
			lock (this)
			{
        
			  while (true)
			  {
				MergePolicy.OneMerge merge = writer.NextMerge;
				if (merge == null)
				{
				  break;
				}
				for (int i = 0;i < merge.segments.size();i++)
				{
				  Debug.Assert(merge.segments.get(i).info.DocCount < 20);
				}
				writer.merge(merge);
			  }
			}
		}

		public override void Close()
		{
		}
	  }

	  // LUCENE-1013
	  public virtual void TestSetMaxMergeDocs()
	  {
		Directory dir = newDirectory();
		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMergeScheduler(new MyMergeScheduler(this)).setMaxBufferedDocs(2).setMergePolicy(newLogMergePolicy());
		LogMergePolicy lmp = (LogMergePolicy) conf.MergePolicy;
		lmp.MaxMergeDocs = 20;
		lmp.MergeFactor = 2;
		IndexWriter iw = new IndexWriter(dir, conf);
		Document document = new Document();

		FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
		customType.StoreTermVectors = true;

		document.add(newField("tvtest", "a b c", customType));
		for (int i = 0;i < 177;i++)
		{
		  iw.addDocument(document);
		}
		iw.close();
		dir.close();
	  }

	  public virtual void TestNoWaitClose()
	  {
		Directory directory = newDirectory();

		Document doc = new Document();
		FieldType customType = new FieldType(TextField.TYPE_STORED);
		customType.Tokenized = false;

		Field idField = newField("id", "", customType);
		doc.add(idField);

		for (int pass = 0;pass < 2;pass++)
		{
		  if (VERBOSE)
		  {
			Console.WriteLine("TEST: pass=" + pass);
		  }

		  IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.CREATE).setMaxBufferedDocs(2).setMergePolicy(newLogMergePolicy());
		  if (pass == 2)
		  {
			conf.MergeScheduler = new SerialMergeScheduler();
		  }

		  IndexWriter writer = new IndexWriter(directory, conf);
		  ((LogMergePolicy) writer.Config.MergePolicy).MergeFactor = 100;

		  for (int iter = 0;iter < 10;iter++)
		  {
			if (VERBOSE)
			{
			  Console.WriteLine("TEST: iter=" + iter);
			}
			for (int j = 0;j < 199;j++)
			{
			  idField.StringValue = Convert.ToString(iter * 201 + j);
			  writer.addDocument(doc);
			}

			int delID = iter * 199;
			for (int j = 0;j < 20;j++)
			{
			  writer.deleteDocuments(new Term("id", Convert.ToString(delID)));
			  delID += 5;
			}

			// Force a bunch of merge threads to kick off so we
			// stress out aborting them on close:
			((LogMergePolicy) writer.Config.MergePolicy).MergeFactor = 2;

			IndexWriter finalWriter = writer;
			List<Exception> failure = new List<Exception>();
			Thread t1 = new ThreadAnonymousInnerClassHelper(this, doc, finalWriter, failure);

			if (failure.Count > 0)
			{
			  throw failure[0];
			}

			t1.Start();

			writer.close(false);
			t1.Join();

			// Make sure reader can read
			IndexReader reader = DirectoryReader.open(directory);
			reader.close();

			// Reopen
			writer = new IndexWriter(directory, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.APPEND).setMergePolicy(newLogMergePolicy()));
		  }
		  writer.close();
		}

		directory.close();
	  }

	  private class ThreadAnonymousInnerClassHelper : System.Threading.Thread
	  {
		  private readonly TestIndexWriterMerging OuterInstance;

		  private Document Doc;
		  private IndexWriter FinalWriter;
		  private List<Exception> Failure;

		  public ThreadAnonymousInnerClassHelper(TestIndexWriterMerging outerInstance, Document doc, IndexWriter finalWriter, List<Exception> failure)
		  {
			  this.OuterInstance = outerInstance;
			  this.Doc = doc;
			  this.FinalWriter = finalWriter;
			  this.Failure = failure;
		  }

		  public override void Run()
		  {
			bool done = false;
			while (!done)
			{
			  for (int i = 0;i < 100;i++)
			  {
				try
				{
				  FinalWriter.addDocument(Doc);
				}
				catch (AlreadyClosedException e)
				{
				  done = true;
				  break;
				}
				catch (System.NullReferenceException e)
				{
				  done = true;
				  break;
				}
				catch (Exception e)
				{
				  e.printStackTrace(System.out);
				  Failure.Add(e);
				  done = true;
				  break;
				}
			  }
			  Thread.@yield();
			}

		  }
	  }
	}

}