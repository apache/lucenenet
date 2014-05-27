using System;
using System.Collections.Generic;
using System.Text;
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


	using Lucene.Net.Analysis;
	using CharTermAttribute = Lucene.Net.Analysis.Tokenattributes.CharTermAttribute;
	using PositionIncrementAttribute = Lucene.Net.Analysis.Tokenattributes.PositionIncrementAttribute;
	using Codec = Lucene.Net.Codecs.Codec;
	using SimpleTextCodec = Lucene.Net.Codecs.simpletext.SimpleTextCodec;
	using BinaryDocValuesField = Lucene.Net.Document.BinaryDocValuesField;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using FieldType = Lucene.Net.Document.FieldType;
	using NumericDocValuesField = Lucene.Net.Document.NumericDocValuesField;
	using SortedDocValuesField = Lucene.Net.Document.SortedDocValuesField;
	using SortedSetDocValuesField = Lucene.Net.Document.SortedSetDocValuesField;
	using StoredField = Lucene.Net.Document.StoredField;
	using StringField = Lucene.Net.Document.StringField;
	using TextField = Lucene.Net.Document.TextField;
	using IndexOptions = Lucene.Net.Index.FieldInfo.IndexOptions_e;
	using OpenMode = Lucene.Net.Index.IndexWriterConfig.OpenMode;
	using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
	using FieldCache = Lucene.Net.Search.FieldCache;
	using IndexSearcher = Lucene.Net.Search.IndexSearcher;
	using MatchAllDocsQuery = Lucene.Net.Search.MatchAllDocsQuery;
	using PhraseQuery = Lucene.Net.Search.PhraseQuery;
	using ScoreDoc = Lucene.Net.Search.ScoreDoc;
	using TermQuery = Lucene.Net.Search.TermQuery;
	using AlreadyClosedException = Lucene.Net.Store.AlreadyClosedException;
	using BaseDirectoryWrapper = Lucene.Net.Store.BaseDirectoryWrapper;
	using Directory = Lucene.Net.Store.Directory;
	using IOContext = Lucene.Net.Store.IOContext;
	using IndexOutput = Lucene.Net.Store.IndexOutput;
	using Lock = Lucene.Net.Store.Lock;
	using LockFactory = Lucene.Net.Store.LockFactory;
	using LockObtainFailedException = Lucene.Net.Store.LockObtainFailedException;
	using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
	using NoLockFactory = Lucene.Net.Store.NoLockFactory;
	using RAMDirectory = Lucene.Net.Store.RAMDirectory;
	using SimpleFSLockFactory = Lucene.Net.Store.SimpleFSLockFactory;
	using SingleInstanceLockFactory = Lucene.Net.Store.SingleInstanceLockFactory;
	using Bits = Lucene.Net.Util.Bits;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using Constants = Lucene.Net.Util.Constants;
	using IOUtils = Lucene.Net.Util.IOUtils;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using SetOnce = Lucene.Net.Util.SetOnce;
	using TestUtil = Lucene.Net.Util.TestUtil;
	using ThreadInterruptedException = Lucene.Net.Util.ThreadInterruptedException;
	using Automaton = Lucene.Net.Util.Automaton.Automaton;
	using BasicAutomata = Lucene.Net.Util.Automaton.BasicAutomata;
	using CharacterRunAutomaton = Lucene.Net.Util.Automaton.CharacterRunAutomaton;
	using PackedInts = Lucene.Net.Util.Packed.PackedInts;
	using Test = org.junit.Test;

	public class TestIndexWriter : LuceneTestCase
	{

		private static readonly FieldType StoredTextType = new FieldType(TextField.TYPE_NOT_STORED);
		public virtual void TestDocCount()
		{
			Directory dir = newDirectory();

			IndexWriter writer = null;
			IndexReader reader = null;
			int i;

			long savedWriteLockTimeout = IndexWriterConfig.DefaultWriteLockTimeout;
			try
			{
			  IndexWriterConfig.DefaultWriteLockTimeout = 2000;
			  Assert.AreEqual(2000, IndexWriterConfig.DefaultWriteLockTimeout);
			  writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
			}
			finally
			{
			  IndexWriterConfig.DefaultWriteLockTimeout = savedWriteLockTimeout;
			}

			// add 100 documents
			for (i = 0; i < 100; i++)
			{
				AddDocWithIndex(writer,i);
			}
			Assert.AreEqual(100, writer.maxDoc());
			writer.close();

			// delete 40 documents
			writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMergePolicy(NoMergePolicy.NO_COMPOUND_FILES));
			for (i = 0; i < 40; i++)
			{
				writer.deleteDocuments(new Term("id", "" + i));
			}
			writer.close();

			reader = DirectoryReader.open(dir);
			Assert.AreEqual(60, reader.numDocs());
			reader.close();

			// merge the index down and check that the new doc count is correct
			writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
			Assert.AreEqual(60, writer.numDocs());
			writer.forceMerge(1);
			Assert.AreEqual(60, writer.maxDoc());
			Assert.AreEqual(60, writer.numDocs());
			writer.close();

			// check that the index reader gives the same numbers.
			reader = DirectoryReader.open(dir);
			Assert.AreEqual(60, reader.maxDoc());
			Assert.AreEqual(60, reader.numDocs());
			reader.close();

			// make sure opening a new index for create over
			// this existing one works correctly:
			writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.CREATE));
			Assert.AreEqual(0, writer.maxDoc());
			Assert.AreEqual(0, writer.numDocs());
			writer.close();
			dir.close();
		}

		internal static void AddDoc(IndexWriter writer)
		{
			Document doc = new Document();
			doc.add(newTextField("content", "aaa", Field.Store.NO));
			writer.addDocument(doc);
		}

		internal static void AddDocWithIndex(IndexWriter writer, int index)
		{
			Document doc = new Document();
			doc.add(newField("content", "aaa " + index, StoredTextType));
			doc.add(newField("id", "" + index, StoredTextType));
			writer.addDocument(doc);
		}



		public static void AssertNoUnreferencedFiles(Directory dir, string message)
		{
		  string[] startFiles = dir.listAll();
		  (new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())))).rollback();
		  string[] endFiles = dir.listAll();

		  Arrays.sort(startFiles);
		  Arrays.sort(endFiles);

		  if (!Arrays.Equals(startFiles, endFiles))
		  {
			Assert.Fail(message + ": before delete:\n    " + ArrayToString(startFiles) + "\n  after delete:\n    " + ArrayToString(endFiles));
		  }
		}

		internal static string ArrayToString(string[] l)
		{
		  string s = "";
		  for (int i = 0;i < l.Length;i++)
		  {
			if (i > 0)
			{
			  s += "\n    ";
			}
			s += l[i];
		  }
		  return s;
		}

		// Make sure we can open an index for create even when a
		// reader holds it open (this fails pre lock-less
		// commits on windows):
		public virtual void TestCreateWithReader()
		{
		  Directory dir = newDirectory();

		  // add one document & close writer
		  IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		  AddDoc(writer);
		  writer.close();

		  // now open reader:
		  IndexReader reader = DirectoryReader.open(dir);
		  Assert.AreEqual("should be one document", reader.numDocs(), 1);

		  // now open index for create:
		  writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.CREATE));
		  Assert.AreEqual("should be zero documents", writer.maxDoc(), 0);
		  AddDoc(writer);
		  writer.close();

		  Assert.AreEqual("should be one document", reader.numDocs(), 1);
		  IndexReader reader2 = DirectoryReader.open(dir);
		  Assert.AreEqual("should be one document", reader2.numDocs(), 1);
		  reader.close();
		  reader2.close();

		  dir.close();
		}

		public virtual void TestChangesAfterClose()
		{
			Directory dir = newDirectory();

			IndexWriter writer = null;

			writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
			AddDoc(writer);

			// close
			writer.close();
			try
			{
			  AddDoc(writer);
			  Assert.Fail("did not hit AlreadyClosedException");
			}
			catch (AlreadyClosedException e)
			{
			  // expected
			}
			dir.close();
		}



		public virtual void TestIndexNoDocuments()
		{
		  Directory dir = newDirectory();
		  IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		  writer.commit();
		  writer.close();

		  IndexReader reader = DirectoryReader.open(dir);
		  Assert.AreEqual(0, reader.maxDoc());
		  Assert.AreEqual(0, reader.numDocs());
		  reader.close();

		  writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.APPEND));
		  writer.commit();
		  writer.close();

		  reader = DirectoryReader.open(dir);
		  Assert.AreEqual(0, reader.maxDoc());
		  Assert.AreEqual(0, reader.numDocs());
		  reader.close();
		  dir.close();
		}

		public virtual void TestManyFields()
		{
		  Directory dir = newDirectory();
		  IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(10));
		  for (int j = 0;j < 100;j++)
		  {
			Document doc = new Document();
			doc.add(newField("a" + j, "aaa" + j, StoredTextType));
			doc.add(newField("b" + j, "aaa" + j, StoredTextType));
			doc.add(newField("c" + j, "aaa" + j, StoredTextType));
			doc.add(newField("d" + j, "aaa", StoredTextType));
			doc.add(newField("e" + j, "aaa", StoredTextType));
			doc.add(newField("f" + j, "aaa", StoredTextType));
			writer.addDocument(doc);
		  }
		  writer.close();

		  IndexReader reader = DirectoryReader.open(dir);
		  Assert.AreEqual(100, reader.maxDoc());
		  Assert.AreEqual(100, reader.numDocs());
		  for (int j = 0;j < 100;j++)
		  {
			Assert.AreEqual(1, reader.docFreq(new Term("a" + j, "aaa" + j)));
			Assert.AreEqual(1, reader.docFreq(new Term("b" + j, "aaa" + j)));
			Assert.AreEqual(1, reader.docFreq(new Term("c" + j, "aaa" + j)));
			Assert.AreEqual(1, reader.docFreq(new Term("d" + j, "aaa")));
			Assert.AreEqual(1, reader.docFreq(new Term("e" + j, "aaa")));
			Assert.AreEqual(1, reader.docFreq(new Term("f" + j, "aaa")));
		  }
		  reader.close();
		  dir.close();
		}

		public virtual void TestSmallRAMBuffer()
		{
		  Directory dir = newDirectory();
		  IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setRAMBufferSizeMB(0.000001).setMergePolicy(newLogMergePolicy(10)));
		  int lastNumFile = dir.listAll().length;
		  for (int j = 0;j < 9;j++)
		  {
			Document doc = new Document();
			doc.add(newField("field", "aaa" + j, StoredTextType));
			writer.addDocument(doc);
			int numFile = dir.listAll().length;
			// Verify that with a tiny RAM buffer we see new
			// segment after every doc
			Assert.IsTrue(numFile > lastNumFile);
			lastNumFile = numFile;
		  }
		  writer.close();
		  dir.close();
		}

		// Make sure it's OK to change RAM buffer size and
		// maxBufferedDocs in a write session
		public virtual void TestChangingRAMBuffer()
		{
		  Directory dir = newDirectory();
		  IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		  writer.Config.MaxBufferedDocs = 10;
		  writer.Config.RAMBufferSizeMB = IndexWriterConfig.DISABLE_AUTO_FLUSH;

		  int lastFlushCount = -1;
		  for (int j = 1;j < 52;j++)
		  {
			Document doc = new Document();
			doc.add(new Field("field", "aaa" + j, StoredTextType));
			writer.addDocument(doc);
			TestUtil.syncConcurrentMerges(writer);
			int flushCount = writer.FlushCount;
			if (j == 1)
			{
			  lastFlushCount = flushCount;
			}
			else if (j < 10)
			  // No new files should be created
			{
			  Assert.AreEqual(flushCount, lastFlushCount);
			}
			else if (10 == j)
			{
			  Assert.IsTrue(flushCount > lastFlushCount);
			  lastFlushCount = flushCount;
			  writer.Config.RAMBufferSizeMB = 0.000001;
			  writer.Config.MaxBufferedDocs = IndexWriterConfig.DISABLE_AUTO_FLUSH;
			}
			else if (j < 20)
			{
			  Assert.IsTrue(flushCount > lastFlushCount);
			  lastFlushCount = flushCount;
			}
			else if (20 == j)
			{
			  writer.Config.RAMBufferSizeMB = 16;
			  writer.Config.MaxBufferedDocs = IndexWriterConfig.DISABLE_AUTO_FLUSH;
			  lastFlushCount = flushCount;
			}
			else if (j < 30)
			{
			  Assert.AreEqual(flushCount, lastFlushCount);
			}
			else if (30 == j)
			{
			  writer.Config.RAMBufferSizeMB = 0.000001;
			  writer.Config.MaxBufferedDocs = IndexWriterConfig.DISABLE_AUTO_FLUSH;
			}
			else if (j < 40)
			{
			  Assert.IsTrue(flushCount > lastFlushCount);
			  lastFlushCount = flushCount;
			}
			else if (40 == j)
			{
			  writer.Config.MaxBufferedDocs = 10;
			  writer.Config.RAMBufferSizeMB = IndexWriterConfig.DISABLE_AUTO_FLUSH;
			  lastFlushCount = flushCount;
			}
			else if (j < 50)
			{
			  Assert.AreEqual(flushCount, lastFlushCount);
			  writer.Config.MaxBufferedDocs = 10;
			  writer.Config.RAMBufferSizeMB = IndexWriterConfig.DISABLE_AUTO_FLUSH;
			}
			else if (50 == j)
			{
			  Assert.IsTrue(flushCount > lastFlushCount);
			}
		  }
		  writer.close();
		  dir.close();
		}

		public virtual void TestChangingRAMBuffer2()
		{
		  Directory dir = newDirectory();
		  IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		  writer.Config.MaxBufferedDocs = 10;
		  writer.Config.MaxBufferedDeleteTerms = 10;
		  writer.Config.RAMBufferSizeMB = IndexWriterConfig.DISABLE_AUTO_FLUSH;

		  for (int j = 1;j < 52;j++)
		  {
			Document doc = new Document();
			doc.add(new Field("field", "aaa" + j, StoredTextType));
			writer.addDocument(doc);
		  }

		  int lastFlushCount = -1;
		  for (int j = 1;j < 52;j++)
		  {
			writer.deleteDocuments(new Term("field", "aaa" + j));
			TestUtil.syncConcurrentMerges(writer);
			int flushCount = writer.FlushCount;

			if (j == 1)
			{
			  lastFlushCount = flushCount;
			}
			else if (j < 10)
			{
			  // No new files should be created
			  Assert.AreEqual(flushCount, lastFlushCount);
			}
			else if (10 == j)
			{
			  Assert.IsTrue("" + j, flushCount > lastFlushCount);
			  lastFlushCount = flushCount;
			  writer.Config.RAMBufferSizeMB = 0.000001;
			  writer.Config.MaxBufferedDeleteTerms = 1;
			}
			else if (j < 20)
			{
			  Assert.IsTrue(flushCount > lastFlushCount);
			  lastFlushCount = flushCount;
			}
			else if (20 == j)
			{
			  writer.Config.RAMBufferSizeMB = 16;
			  writer.Config.MaxBufferedDeleteTerms = IndexWriterConfig.DISABLE_AUTO_FLUSH;
			  lastFlushCount = flushCount;
			}
			else if (j < 30)
			{
			  Assert.AreEqual(flushCount, lastFlushCount);
			}
			else if (30 == j)
			{
			  writer.Config.RAMBufferSizeMB = 0.000001;
			  writer.Config.MaxBufferedDeleteTerms = IndexWriterConfig.DISABLE_AUTO_FLUSH;
			  writer.Config.MaxBufferedDeleteTerms = 1;
			}
			else if (j < 40)
			{
			  Assert.IsTrue(flushCount > lastFlushCount);
			  lastFlushCount = flushCount;
			}
			else if (40 == j)
			{
			  writer.Config.MaxBufferedDeleteTerms = 10;
			  writer.Config.RAMBufferSizeMB = IndexWriterConfig.DISABLE_AUTO_FLUSH;
			  lastFlushCount = flushCount;
			}
			else if (j < 50)
			{
			  Assert.AreEqual(flushCount, lastFlushCount);
			  writer.Config.MaxBufferedDeleteTerms = 10;
			  writer.Config.RAMBufferSizeMB = IndexWriterConfig.DISABLE_AUTO_FLUSH;
			}
			else if (50 == j)
			{
			  Assert.IsTrue(flushCount > lastFlushCount);
			}
		  }
		  writer.close();
		  dir.close();
		}

		public virtual void TestDiverseDocs()
		{
		  Directory dir = newDirectory();
		  IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setRAMBufferSizeMB(0.5));
		  int n = atLeast(1);
		  for (int i = 0;i < n;i++)
		  {
			// First, docs where every term is unique (heavy on
			// Posting instances)
			for (int j = 0;j < 100;j++)
			{
			  Document doc = new Document();
			  for (int k = 0;k < 100;k++)
			  {
				doc.add(newField("field", Convert.ToString(random().Next()), StoredTextType));
			  }
			  writer.addDocument(doc);
			}

			// Next, many single term docs where only one term
			// occurs (heavy on byte blocks)
			for (int j = 0;j < 100;j++)
			{
			  Document doc = new Document();
			  doc.add(newField("field", "aaa aaa aaa aaa aaa aaa aaa aaa aaa aaa", StoredTextType));
			  writer.addDocument(doc);
			}

			// Next, many single term docs where only one term
			// occurs but the terms are very long (heavy on
			// char[] arrays)
			for (int j = 0;j < 100;j++)
			{
			  StringBuilder b = new StringBuilder();
			  string x = Convert.ToString(j) + ".";
			  for (int k = 0;k < 1000;k++)
			  {
				b.Append(x);
			  }
			  string longTerm = b.ToString();

			  Document doc = new Document();
			  doc.add(newField("field", longTerm, StoredTextType));
			  writer.addDocument(doc);
			}
		  }
		  writer.close();

		  IndexReader reader = DirectoryReader.open(dir);
		  IndexSearcher searcher = newSearcher(reader);
		  int totalHits = searcher.search(new TermQuery(new Term("field", "aaa")), null, 1).totalHits;
		  Assert.AreEqual(n * 100, totalHits);
		  reader.close();

		  dir.close();
		}

		public virtual void TestEnablingNorms()
		{
		  Directory dir = newDirectory();
		  IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(10));
		  // Enable norms for only 1 doc, pre flush
		  FieldType customType = new FieldType(TextField.TYPE_STORED);
		  customType.OmitNorms = true;
		  for (int j = 0;j < 10;j++)
		  {
			Document doc = new Document();
			Field f = null;
			if (j != 8)
			{
			  f = newField("field", "aaa", customType);
			}
			else
			{
			  f = newField("field", "aaa", StoredTextType);
			}
			doc.add(f);
			writer.addDocument(doc);
		  }
		  writer.close();

		  Term searchTerm = new Term("field", "aaa");

		  IndexReader reader = DirectoryReader.open(dir);
		  IndexSearcher searcher = newSearcher(reader);
		  ScoreDoc[] hits = searcher.search(new TermQuery(searchTerm), null, 1000).scoreDocs;
		  Assert.AreEqual(10, hits.Length);
		  reader.close();

		  writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.CREATE).setMaxBufferedDocs(10));
		  // Enable norms for only 1 doc, post flush
		  for (int j = 0;j < 27;j++)
		  {
			Document doc = new Document();
			Field f = null;
			if (j != 26)
			{
			  f = newField("field", "aaa", customType);
			}
			else
			{
			  f = newField("field", "aaa", StoredTextType);
			}
			doc.add(f);
			writer.addDocument(doc);
		  }
		  writer.close();
		  reader = DirectoryReader.open(dir);
		  searcher = newSearcher(reader);
		  hits = searcher.search(new TermQuery(searchTerm), null, 1000).scoreDocs;
		  Assert.AreEqual(27, hits.Length);
		  reader.close();

		  reader = DirectoryReader.open(dir);
		  reader.close();

		  dir.close();
		}

		public virtual void TestHighFreqTerm()
		{
		  Directory dir = newDirectory();
		  IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setRAMBufferSizeMB(0.01));
		  // Massive doc that has 128 K a's
		  StringBuilder b = new StringBuilder(1024 * 1024);
		  for (int i = 0;i < 4096;i++)
		  {
			b.Append(" a a a a a a a a");
			b.Append(" a a a a a a a a");
			b.Append(" a a a a a a a a");
			b.Append(" a a a a a a a a");
		  }
		  Document doc = new Document();
		  FieldType customType = new FieldType(TextField.TYPE_STORED);
		  customType.StoreTermVectors = true;
		  customType.StoreTermVectorPositions = true;
		  customType.StoreTermVectorOffsets = true;
		  doc.add(newField("field", b.ToString(), customType));
		  writer.addDocument(doc);
		  writer.close();

		  IndexReader reader = DirectoryReader.open(dir);
		  Assert.AreEqual(1, reader.maxDoc());
		  Assert.AreEqual(1, reader.numDocs());
		  Term t = new Term("field", "a");
		  Assert.AreEqual(1, reader.docFreq(t));
		  DocsEnum td = TestUtil.docs(random(), reader, "field", new BytesRef("a"), MultiFields.getLiveDocs(reader), null, DocsEnum.FLAG_FREQS);
		  td.nextDoc();
		  Assert.AreEqual(128 * 1024, td.freq());
		  reader.close();
		  dir.close();
		}

		// Make sure that a Directory implementation that does
		// not use LockFactory at all (ie overrides makeLock and
		// implements its own private locking) works OK.  this
		// was raised on java-dev as loss of backwards
		// compatibility.
		public virtual void TestNullLockFactory()
		{

		  public class MyRAMDirectory : MockDirectoryWrapper
		  {
			private LockFactory myLockFactory;
			MyRAMDirectory(Directory @delegate)
			{
			  base(random(), @delegate);
			  lockFactory = null;
			  myLockFactory = new SingleInstanceLockFactory();
			}
			public Lock makeLock(string name)
			{
			  return myLockFactory.makeLock(name);
			}
		  }

		  Directory dir = new MyRAMDirectory(new RAMDirectory());
		  IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		  for (int i = 0; i < 100; i++)
		  {
			AddDoc(writer);
		  }
		  writer.close();
		  Term searchTerm = new Term("content", "aaa");
		  IndexReader reader = DirectoryReader.open(dir);
		  IndexSearcher searcher = newSearcher(reader);
		  ScoreDoc[] hits = searcher.search(new TermQuery(searchTerm), null, 1000).scoreDocs;
		  Assert.AreEqual("did not get right number of hits", 100, hits.Length);
		  reader.close();

		  writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.CREATE));
		  writer.close();
		  dir.close();
		}

		public virtual void TestFlushWithNoMerging()
		{
		  Directory dir = newDirectory();
		  IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(2).setMergePolicy(newLogMergePolicy(10)));
		  Document doc = new Document();
		  FieldType customType = new FieldType(TextField.TYPE_STORED);
		  customType.StoreTermVectors = true;
		  customType.StoreTermVectorPositions = true;
		  customType.StoreTermVectorOffsets = true;
		  doc.add(newField("field", "aaa", customType));
		  for (int i = 0;i < 19;i++)
		  {
			writer.addDocument(doc);
		  }
		  writer.flush(false, true);
		  writer.close();
		  SegmentInfos sis = new SegmentInfos();
		  sis.read(dir);
		  // Since we flushed w/o allowing merging we should now
		  // have 10 segments
		  Assert.AreEqual(10, sis.size());
		  dir.close();
		}

		// Make sure we can flush segment w/ norms, then add
		// empty doc (no norms) and flush
		public virtual void TestEmptyDocAfterFlushingRealDoc()
		{
		  Directory dir = newDirectory();
		  IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		  Document doc = new Document();
		  FieldType customType = new FieldType(TextField.TYPE_STORED);
		  customType.StoreTermVectors = true;
		  customType.StoreTermVectorPositions = true;
		  customType.StoreTermVectorOffsets = true;
		  doc.add(newField("field", "aaa", customType));
		  writer.addDocument(doc);
		  writer.commit();
		  if (VERBOSE)
		  {
			Console.WriteLine("\nTEST: now add empty doc");
		  }
		  writer.addDocument(new Document());
		  writer.close();
		  IndexReader reader = DirectoryReader.open(dir);
		  Assert.AreEqual(2, reader.numDocs());
		  reader.close();
		  dir.close();
		}



	  /// <summary>
	  /// Test that no NullPointerException will be raised,
	  /// when adding one document with a single, empty field
	  /// and term vectors enabled.
	  /// </summary>
	  public virtual void TestBadSegment()
	  {
		Directory dir = newDirectory();
		IndexWriter iw = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));

		Document document = new Document();
		FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
		customType.StoreTermVectors = true;
		document.add(newField("tvtest", "", customType));
		iw.addDocument(document);
		iw.close();
		dir.close();
	  }

	  // LUCENE-1036
	  public virtual void TestMaxThreadPriority()
	  {
		int pri = Thread.CurrentThread.Priority;
		try
		{
		  Directory dir = newDirectory();
		  IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(2).setMergePolicy(newLogMergePolicy());
		  ((LogMergePolicy) conf.MergePolicy).MergeFactor = 2;
		  IndexWriter iw = new IndexWriter(dir, conf);
		  Document document = new Document();
		  FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
		  customType.StoreTermVectors = true;
		  document.add(newField("tvtest", "a b c", customType));
		  Thread.CurrentThread.Priority = Thread.MAX_PRIORITY;
		  for (int i = 0;i < 4;i++)
		  {
			iw.addDocument(document);
		  }
		  iw.close();
		  dir.close();
		}
		finally
		{
		  Thread.CurrentThread.Priority = pri;
		}
	  }

	  public virtual void TestVariableSchema()
	  {
		Directory dir = newDirectory();
		for (int i = 0;i < 20;i++)
		{
		  if (VERBOSE)
		  {
			Console.WriteLine("TEST: iter=" + i);
		  }
		  IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(2).setMergePolicy(newLogMergePolicy()));
		  //LogMergePolicy lmp = (LogMergePolicy) writer.getConfig().getMergePolicy();
		  //lmp.setMergeFactor(2);
		  //lmp.setNoCFSRatio(0.0);
		  Document doc = new Document();
		  string contents = "aa bb cc dd ee ff gg hh ii jj kk";

		  FieldType customType = new FieldType(TextField.TYPE_STORED);
		  FieldType type = null;
		  if (i == 7)
		  {
			// Add empty docs here
			doc.add(newTextField("content3", "", Field.Store.NO));
		  }
		  else
		  {
			if (i % 2 == 0)
			{
			  doc.add(newField("content4", contents, customType));
			  type = customType;
			}
			else
			{
			  type = TextField.TYPE_NOT_STORED;
			}
			doc.add(newTextField("content1", contents, Field.Store.NO));
			doc.add(newField("content3", "", customType));
			doc.add(newField("content5", "", type));
		  }

		  for (int j = 0;j < 4;j++)
		  {
			writer.addDocument(doc);
		  }

		  writer.close();

		  if (0 == i % 4)
		  {
			writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
			//LogMergePolicy lmp2 = (LogMergePolicy) writer.getConfig().getMergePolicy();
			//lmp2.setNoCFSRatio(0.0);
			writer.forceMerge(1);
			writer.close();
		  }
		}
		dir.close();
	  }

	  // LUCENE-1084: test unlimited field length
	  public virtual void TestUnlimitedMaxFieldLength()
	  {
		Directory dir = newDirectory();

		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));

		Document doc = new Document();
		StringBuilder b = new StringBuilder();
		for (int i = 0;i < 10000;i++)
		{
		  b.Append(" a");
		}
		b.Append(" x");
		doc.add(newTextField("field", b.ToString(), Field.Store.NO));
		writer.addDocument(doc);
		writer.close();

		IndexReader reader = DirectoryReader.open(dir);
		Term t = new Term("field", "x");
		Assert.AreEqual(1, reader.docFreq(t));
		reader.close();
		dir.close();
	  }



	  // LUCENE-1179
	  public virtual void TestEmptyFieldName()
	  {
		Directory dir = newDirectory();
		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		Document doc = new Document();
		doc.add(newTextField("", "a b c", Field.Store.NO));
		writer.addDocument(doc);
		writer.close();
		dir.close();
	  }

	  public virtual void TestEmptyFieldNameTerms()
	  {
		Directory dir = newDirectory();
		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		Document doc = new Document();
		doc.add(newTextField("", "a b c", Field.Store.NO));
		writer.addDocument(doc);
		writer.close();
		DirectoryReader reader = DirectoryReader.open(dir);
		AtomicReader subreader = getOnlySegmentReader(reader);
		TermsEnum te = subreader.fields().terms("").iterator(null);
		Assert.AreEqual(new BytesRef("a"), te.next());
		Assert.AreEqual(new BytesRef("b"), te.next());
		Assert.AreEqual(new BytesRef("c"), te.next());
		assertNull(te.next());
		reader.close();
		dir.close();
	  }

	  public virtual void TestEmptyFieldNameWithEmptyTerm()
	  {
		Directory dir = newDirectory();
		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		Document doc = new Document();
		doc.add(newStringField("", "", Field.Store.NO));
		doc.add(newStringField("", "a", Field.Store.NO));
		doc.add(newStringField("", "b", Field.Store.NO));
		doc.add(newStringField("", "c", Field.Store.NO));
		writer.addDocument(doc);
		writer.close();
		DirectoryReader reader = DirectoryReader.open(dir);
		AtomicReader subreader = getOnlySegmentReader(reader);
		TermsEnum te = subreader.fields().terms("").iterator(null);
		Assert.AreEqual(new BytesRef(""), te.next());
		Assert.AreEqual(new BytesRef("a"), te.next());
		Assert.AreEqual(new BytesRef("b"), te.next());
		Assert.AreEqual(new BytesRef("c"), te.next());
		assertNull(te.next());
		reader.close();
		dir.close();
	  }



	  private sealed class MockIndexWriter : IndexWriter
	  {

		public MockIndexWriter(Directory dir, IndexWriterConfig conf) : base(dir, conf)
		{
		}

		internal bool AfterWasCalled;
		internal bool BeforeWasCalled;

		public override void DoAfterFlush()
		{
		  AfterWasCalled = true;
		}

		protected internal override void DoBeforeFlush()
		{
		  BeforeWasCalled = true;
		}
	  }


	  // LUCENE-1222
	  public virtual void TestDoBeforeAfterFlush()
	  {
		Directory dir = newDirectory();
		MockIndexWriter w = new MockIndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		Document doc = new Document();
		FieldType customType = new FieldType(TextField.TYPE_STORED);
		doc.add(newField("field", "a field", customType));
		w.addDocument(doc);
		w.commit();
		Assert.IsTrue(w.BeforeWasCalled);
		Assert.IsTrue(w.AfterWasCalled);
		w.BeforeWasCalled = false;
		w.AfterWasCalled = false;
		w.deleteDocuments(new Term("field", "field"));
		w.commit();
		Assert.IsTrue(w.BeforeWasCalled);
		Assert.IsTrue(w.AfterWasCalled);
		w.close();

		IndexReader ir = DirectoryReader.open(dir);
		Assert.AreEqual(0, ir.numDocs());
		ir.close();

		dir.close();
	  }

	  // LUCENE-1255
	  public virtual void TestNegativePositions()
	  {
		TokenStream tokens = new TokenStreamAnonymousInnerClassHelper(this);

		Directory dir = newDirectory();
		IndexWriter w = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		Document doc = new Document();
		doc.add(new TextField("field", tokens));
		try
		{
		  w.addDocument(doc);
		  Assert.Fail("did not hit expected exception");
		}
		catch (System.ArgumentException iea)
		{
		  // expected
		}
		w.close();
		dir.close();
	  }

	  private class TokenStreamAnonymousInnerClassHelper : TokenStream
	  {
		  private readonly TestIndexWriter OuterInstance;

		  public TokenStreamAnonymousInnerClassHelper(TestIndexWriter outerInstance)
		  {
			  this.OuterInstance = outerInstance;
			  termAtt = addAttribute(typeof(CharTermAttribute));
			  posIncrAtt = addAttribute(typeof(PositionIncrementAttribute));
			  terms = Arrays.asList("a","b","c").GetEnumerator();
			  first = true;
		  }

		  internal readonly CharTermAttribute termAtt;
		  internal readonly PositionIncrementAttribute posIncrAtt;

		  internal readonly IEnumerator<string> terms;
		  internal bool first;

		  public override bool IncrementToken()
		  {
			if (!terms.hasNext())
			{
				return false;
			}
			ClearAttributes();
			termAtt.append(terms.next());
			posIncrAtt.PositionIncrement = first ? 0 : 1;
			first = false;
			return true;
		  }
	  }

	  // LUCENE-2529
	  public virtual void TestPositionIncrementGapEmptyField()
	  {
		Directory dir = newDirectory();
		MockAnalyzer analyzer = new MockAnalyzer(random());
		analyzer.PositionIncrementGap = 100;
		IndexWriter w = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, analyzer));
		Document doc = new Document();
		FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
		customType.StoreTermVectors = true;
		customType.StoreTermVectorPositions = true;
		Field f = newField("field", "", customType);
		Field f2 = newField("field", "crunch man", customType);
		doc.add(f);
		doc.add(f2);
		w.addDocument(doc);
		w.close();

		IndexReader r = DirectoryReader.open(dir);
		Terms tpv = r.getTermVectors(0).terms("field");
		TermsEnum termsEnum = tpv.iterator(null);
		Assert.IsNotNull(termsEnum.next());
		DocsAndPositionsEnum dpEnum = termsEnum.docsAndPositions(null, null);
		Assert.IsNotNull(dpEnum);
		Assert.IsTrue(dpEnum.nextDoc() != DocIdSetIterator.NO_MORE_DOCS);
		Assert.AreEqual(1, dpEnum.freq());
		Assert.AreEqual(100, dpEnum.nextPosition());

		Assert.IsNotNull(termsEnum.next());
		dpEnum = termsEnum.docsAndPositions(null, dpEnum);
		Assert.IsNotNull(dpEnum);
		Assert.IsTrue(dpEnum.nextDoc() != DocIdSetIterator.NO_MORE_DOCS);
		Assert.AreEqual(1, dpEnum.freq());
		Assert.AreEqual(101, dpEnum.nextPosition());
		assertNull(termsEnum.next());

		r.close();
		dir.close();
	  }

	  public virtual void TestDeadlock()
	  {
		Directory dir = newDirectory();
		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(2));
		Document doc = new Document();

		FieldType customType = new FieldType(TextField.TYPE_STORED);
		customType.StoreTermVectors = true;
		customType.StoreTermVectorPositions = true;
		customType.StoreTermVectorOffsets = true;

		doc.add(newField("content", "aaa bbb ccc ddd eee fff ggg hhh iii", customType));
		writer.addDocument(doc);
		writer.addDocument(doc);
		writer.addDocument(doc);
		writer.commit();
		// index has 2 segments

		Directory dir2 = newDirectory();
		IndexWriter writer2 = new IndexWriter(dir2, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		writer2.addDocument(doc);
		writer2.close();

		IndexReader r1 = DirectoryReader.open(dir2);
		writer.addIndexes(r1, r1);
		writer.close();

		IndexReader r3 = DirectoryReader.open(dir);
		Assert.AreEqual(5, r3.numDocs());
		r3.close();

		r1.close();

		dir2.close();
		dir.close();
	  }

	  private class IndexerThreadInterrupt : System.Threading.Thread
	  {
		  private readonly TestIndexWriter OuterInstance;

		internal volatile bool Failed;
		internal volatile bool Finish;

		internal volatile bool AllowInterrupt = false;
		internal readonly Random Random;
		internal readonly Directory Adder;

		internal IndexerThreadInterrupt(TestIndexWriter outerInstance)
		{
			this.OuterInstance = outerInstance;
		  this.Random = new Random(random().nextLong());
		  // make a little directory for addIndexes
		  // LUCENE-2239: won't work with NIOFS/MMAP
		  Adder = new MockDirectoryWrapper(Random, new RAMDirectory());
		  IndexWriterConfig conf = newIndexWriterConfig(Random, TEST_VERSION_CURRENT, new MockAnalyzer(Random));
		  IndexWriter w = new IndexWriter(Adder, conf);
		  Document doc = new Document();
		  doc.add(newStringField(Random, "id", "500", Field.Store.NO));
		  doc.add(newField(Random, "field", "some prepackaged text contents", StoredTextType));
		  if (defaultCodecSupportsDocValues())
		  {
			doc.add(new BinaryDocValuesField("binarydv", new BytesRef("500")));
			doc.add(new NumericDocValuesField("numericdv", 500));
			doc.add(new SortedDocValuesField("sorteddv", new BytesRef("500")));
		  }
		  if (defaultCodecSupportsSortedSet())
		  {
			doc.add(new SortedSetDocValuesField("sortedsetdv", new BytesRef("one")));
			doc.add(new SortedSetDocValuesField("sortedsetdv", new BytesRef("two")));
		  }
		  w.addDocument(doc);
		  doc = new Document();
		  doc.add(newStringField(Random, "id", "501", Field.Store.NO));
		  doc.add(newField(Random, "field", "some more contents", StoredTextType));
		  if (defaultCodecSupportsDocValues())
		  {
			doc.add(new BinaryDocValuesField("binarydv", new BytesRef("501")));
			doc.add(new NumericDocValuesField("numericdv", 501));
			doc.add(new SortedDocValuesField("sorteddv", new BytesRef("501")));
		  }
		  if (defaultCodecSupportsSortedSet())
		  {
			doc.add(new SortedSetDocValuesField("sortedsetdv", new BytesRef("two")));
			doc.add(new SortedSetDocValuesField("sortedsetdv", new BytesRef("three")));
		  }
		  w.addDocument(doc);
		  w.deleteDocuments(new Term("id", "500"));
		  w.close();
		}

		public override void Run()
		{
		  // LUCENE-2239: won't work with NIOFS/MMAP
		  MockDirectoryWrapper dir = new MockDirectoryWrapper(Random, new RAMDirectory());

		  // When interrupt arrives in w.close(), when it's
		  // writing liveDocs, this can lead to double-write of
		  // _X_N.del:
		  //dir.setPreventDoubleWrite(false);
		  IndexWriter w = null;
		  while (!Finish)
		  {
			try
			{

			  while (!Finish)
			  {
				if (w != null)
				{
				  // If interrupt arrives inside here, it's
				  // fine: we will cycle back and the first
				  // thing we do is try to close again,
				  // i.e. we'll never try to open a new writer
				  // until this one successfully closes:
				  w.close();
				  w = null;
				}
				IndexWriterConfig conf = newIndexWriterConfig(Random, TEST_VERSION_CURRENT, new MockAnalyzer(Random)).setMaxBufferedDocs(2);
				w = new IndexWriter(dir, conf);

				Document doc = new Document();
				Field idField = newStringField(Random, "id", "", Field.Store.NO);
				Field binaryDVField = null;
				Field numericDVField = null;
				Field sortedDVField = null;
				Field sortedSetDVField = new SortedSetDocValuesField("sortedsetdv", new BytesRef());
				doc.add(idField);
				doc.add(newField(Random, "field", "some text contents", StoredTextType));
				if (defaultCodecSupportsDocValues())
				{
				  binaryDVField = new BinaryDocValuesField("binarydv", new BytesRef());
				  numericDVField = new NumericDocValuesField("numericdv", 0);
				  sortedDVField = new SortedDocValuesField("sorteddv", new BytesRef());
				  doc.add(binaryDVField);
				  doc.add(numericDVField);
				  doc.add(sortedDVField);
				}
				if (defaultCodecSupportsSortedSet())
				{
				  doc.add(sortedSetDVField);
				}
				for (int i = 0;i < 100;i++)
				{
				  idField.StringValue = Convert.ToString(i);
				  if (defaultCodecSupportsDocValues())
				  {
					binaryDVField.BytesValue = new BytesRef(idField.stringValue());
					numericDVField.LongValue = i;
					sortedDVField.BytesValue = new BytesRef(idField.stringValue());
				  }
				  sortedSetDVField.BytesValue = new BytesRef(idField.stringValue());
				  int action = Random.Next(100);
				  if (action == 17)
				  {
					w.addIndexes(Adder);
				  }
				  else if (action % 30 == 0)
				  {
					w.deleteAll();
				  }
				  else if (action % 2 == 0)
				  {
					w.updateDocument(new Term("id", idField.stringValue()), doc);
				  }
				  else
				  {
					w.addDocument(doc);
				  }
				  if (Random.Next(3) == 0)
				  {
					IndexReader r = null;
					try
					{
					  r = DirectoryReader.open(w, Random.nextBoolean());
					  if (Random.nextBoolean() && r.maxDoc() > 0)
					  {
						int docid = Random.Next(r.maxDoc());
						w.tryDeleteDocument(r, docid);
					  }
					}
					finally
					{
					  IOUtils.closeWhileHandlingException(r);
					}
				  }
				  if (i % 10 == 0)
				  {
					w.commit();
				  }
				  if (Random.Next(50) == 0)
				  {
					w.forceMerge(1);
				  }
				}
				w.close();
				w = null;
				DirectoryReader.open(dir).close();

				// Strangely, if we interrupt a thread before
				// all classes are loaded, the class loader
				// seems to do scary things with the interrupt
				// status.  In java 1.5, it'll throw an
				// incorrect ClassNotFoundException.  In java
				// 1.6, it'll silently clear the interrupt.
				// So, on first iteration through here we
				// don't open ourselves up for interrupts
				// until we've done the above loop.
				AllowInterrupt = true;
			  }
			}
			catch (ThreadInterruptedException re)
			{
			  // NOTE: important to leave this verbosity/noise
			  // on!!  this test doesn't repro easily so when
			  // Jenkins hits a fail we need to study where the
			  // interrupts struck!
			  Console.WriteLine("TEST: got interrupt");
			  re.printStackTrace(System.out);
			  Exception e = re.InnerException;
			  Assert.IsTrue(e is InterruptedException);
			  if (Finish)
			  {
				break;
			  }
			}
			catch (Exception t)
			{
			  Console.WriteLine("FAILED; unexpected exception");
			  t.printStackTrace(System.out);
			  Failed = true;
			  break;
			}
		  }

		  if (!Failed)
		  {
			if (VERBOSE)
			{
			  Console.WriteLine("TEST: now rollback");
			}
			// clear interrupt state:
			Thread.interrupted();
			if (w != null)
			{
			  try
			  {
				w.rollback();
			  }
			  catch (IOException ioe)
			  {
				throw new Exception(ioe);
			  }
			}

			try
			{
			  TestUtil.checkIndex(dir);
			}
			catch (Exception e)
			{
			  Failed = true;
			  Console.WriteLine("CheckIndex FAILED: unexpected exception");
			  e.printStackTrace(System.out);
			}
			try
			{
			  IndexReader r = DirectoryReader.open(dir);
			  //System.out.println("doc count=" + r.numDocs());
			  r.close();
			}
			catch (Exception e)
			{
			  Failed = true;
			  Console.WriteLine("DirectoryReader.open FAILED: unexpected exception");
			  e.printStackTrace(System.out);
			}
		  }
		  try
		  {
			IOUtils.close(dir);
		  }
		  catch (IOException e)
		  {
			throw new Exception(e);
		  }
		  try
		  {
			IOUtils.close(Adder);
		  }
		  catch (IOException e)
		  {
			throw new Exception(e);
		  }
		}
	  }

	  public virtual void TestThreadInterruptDeadlock()
	  {
		IndexerThreadInterrupt t = new IndexerThreadInterrupt(this);
		t.Daemon = true;
		t.Start();

		// Force class loader to load ThreadInterruptedException
		// up front... else we can see a false failure if 2nd
		// interrupt arrives while class loader is trying to
		// init this class (in servicing a first interrupt):
		Assert.IsTrue((new ThreadInterruptedException(new InterruptedException())).InnerException is InterruptedException);

		// issue 300 interrupts to child thread
		int numInterrupts = atLeast(300);
		int i = 0;
		while (i < numInterrupts)
		{
		  // TODO: would be nice to also sometimes interrupt the
		  // CMS merge threads too ...
		  Thread.Sleep(10);
		  if (t.AllowInterrupt)
		  {
			i++;
			t.Interrupt();
		  }
		  if (!t.IsAlive)
		  {
			break;
		  }
		}
		t.Finish = true;
		t.Join();
		Assert.IsFalse(t.Failed);
	  }

	  /// <summary>
	  /// testThreadInterruptDeadlock but with 2 indexer threads </summary>
	  public virtual void TestTwoThreadsInterruptDeadlock()
	  {
		IndexerThreadInterrupt t1 = new IndexerThreadInterrupt(this);
		t1.Daemon = true;
		t1.Start();

		IndexerThreadInterrupt t2 = new IndexerThreadInterrupt(this);
		t2.Daemon = true;
		t2.Start();

		// Force class loader to load ThreadInterruptedException
		// up front... else we can see a false failure if 2nd
		// interrupt arrives while class loader is trying to
		// init this class (in servicing a first interrupt):
		Assert.IsTrue((new ThreadInterruptedException(new InterruptedException())).InnerException is InterruptedException);

		// issue 300 interrupts to child thread
		int numInterrupts = atLeast(300);
		int i = 0;
		while (i < numInterrupts)
		{
		  // TODO: would be nice to also sometimes interrupt the
		  // CMS merge threads too ...
		  Thread.Sleep(10);
		  IndexerThreadInterrupt t = random().nextBoolean() ? t1 : t2;
		  if (t.AllowInterrupt)
		  {
			i++;
			t.Interrupt();
		  }
		  if (!t1.IsAlive && !t2.IsAlive)
		  {
			break;
		  }
		}
		t1.Finish = true;
		t2.Finish = true;
		t1.Join();
		t2.Join();
		Assert.IsFalse(t1.Failed);
		Assert.IsFalse(t2.Failed);
	  }


	  public virtual void TestIndexStoreCombos()
	  {
		Directory dir = newDirectory();
		IndexWriter w = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		sbyte[] b = new sbyte[50];
		for (int i = 0;i < 50;i++)
		{
		  b[i] = (sbyte)(i + 77);
		}

		Document doc = new Document();

		FieldType customType = new FieldType(StoredField.TYPE);
		customType.Tokenized = true;

		Field f = new Field("binary", b, 10, 17, customType);
		customType.Indexed = true;
		f.TokenStream = new MockTokenizer(new StringReader("doc1field1"), MockTokenizer.WHITESPACE, false);

		FieldType customType2 = new FieldType(TextField.TYPE_STORED);

		Field f2 = newField("string", "value", customType2);
		f2.TokenStream = new MockTokenizer(new StringReader("doc1field2"), MockTokenizer.WHITESPACE, false);
		doc.add(f);
		doc.add(f2);
		w.addDocument(doc);

		// add 2 docs to test in-memory merging
		f.TokenStream = new MockTokenizer(new StringReader("doc2field1"), MockTokenizer.WHITESPACE, false);
		f2.TokenStream = new MockTokenizer(new StringReader("doc2field2"), MockTokenizer.WHITESPACE, false);
		w.addDocument(doc);

		// force segment flush so we can force a segment merge with doc3 later.
		w.commit();

		f.TokenStream = new MockTokenizer(new StringReader("doc3field1"), MockTokenizer.WHITESPACE, false);
		f2.TokenStream = new MockTokenizer(new StringReader("doc3field2"), MockTokenizer.WHITESPACE, false);

		w.addDocument(doc);
		w.commit();
		w.forceMerge(1); // force segment merge.
		w.close();

		IndexReader ir = DirectoryReader.open(dir);
		Document doc2 = ir.document(0);
		IndexableField f3 = doc2.getField("binary");
		b = f3.binaryValue().bytes;
		Assert.IsTrue(b != null);
		Assert.AreEqual(17, b.Length, 17);
		Assert.AreEqual(87, b[0]);

		Assert.IsTrue(ir.document(0).getField("binary").binaryValue() != null);
		Assert.IsTrue(ir.document(1).getField("binary").binaryValue() != null);
		Assert.IsTrue(ir.document(2).getField("binary").binaryValue() != null);

		Assert.AreEqual("value", ir.document(0).get("string"));
		Assert.AreEqual("value", ir.document(1).get("string"));
		Assert.AreEqual("value", ir.document(2).get("string"));


		// test that the terms were indexed.
		Assert.IsTrue(TestUtil.docs(random(), ir, "binary", new BytesRef("doc1field1"), null, null, DocsEnum.FLAG_NONE).nextDoc() != DocIdSetIterator.NO_MORE_DOCS);
		Assert.IsTrue(TestUtil.docs(random(), ir, "binary", new BytesRef("doc2field1"), null, null, DocsEnum.FLAG_NONE).nextDoc() != DocIdSetIterator.NO_MORE_DOCS);
		Assert.IsTrue(TestUtil.docs(random(), ir, "binary", new BytesRef("doc3field1"), null, null, DocsEnum.FLAG_NONE).nextDoc() != DocIdSetIterator.NO_MORE_DOCS);
		Assert.IsTrue(TestUtil.docs(random(), ir, "string", new BytesRef("doc1field2"), null, null, DocsEnum.FLAG_NONE).nextDoc() != DocIdSetIterator.NO_MORE_DOCS);
		Assert.IsTrue(TestUtil.docs(random(), ir, "string", new BytesRef("doc2field2"), null, null, DocsEnum.FLAG_NONE).nextDoc() != DocIdSetIterator.NO_MORE_DOCS);
		Assert.IsTrue(TestUtil.docs(random(), ir, "string", new BytesRef("doc3field2"), null, null, DocsEnum.FLAG_NONE).nextDoc() != DocIdSetIterator.NO_MORE_DOCS);

		ir.close();
		dir.close();

	  }

	  public virtual void TestNoDocsIndex()
	  {
		Directory dir = newDirectory();
		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		writer.addDocument(new Document());
		writer.close();

		dir.close();
	  }

	  public virtual void TestIndexDivisor()
	  {
		Directory dir = newDirectory();
		IndexWriterConfig config = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		config.TermIndexInterval = 2;
		IndexWriter w = new IndexWriter(dir, config);
		StringBuilder s = new StringBuilder();
		// must be > 256
		for (int i = 0;i < 300;i++)
		{
		  s.Append(' ').Append(i);
		}
		Document d = new Document();
		Field f = newTextField("field", s.ToString(), Field.Store.NO);
		d.add(f);
		w.addDocument(d);

		AtomicReader r = getOnlySegmentReader(w.Reader);
		TermsEnum t = r.fields().terms("field").iterator(null);
		int count = 0;
		while (t.next() != null)
		{
		  DocsEnum docs = TestUtil.docs(random(), t, null, null, DocsEnum.FLAG_NONE);
		  Assert.AreEqual(0, docs.nextDoc());
		  Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, docs.nextDoc());
		  count++;
		}
		Assert.AreEqual(300, count);
		r.close();
		w.close();
		dir.close();
	  }

	  public virtual void TestDeleteUnusedFiles()
	  {
		for (int iter = 0;iter < 2;iter++)
		{
		  Directory dir = newMockDirectory(); // relies on windows semantics

		  MergePolicy mergePolicy = newLogMergePolicy(true);

		  // this test expects all of its segments to be in CFS
		  mergePolicy.NoCFSRatio = 1.0;
		  mergePolicy.MaxCFSSegmentSizeMB = double.PositiveInfinity;

		  IndexWriter w = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMergePolicy(mergePolicy).setUseCompoundFile(true));
		  Document doc = new Document();
		  doc.add(newTextField("field", "go", Field.Store.NO));
		  w.addDocument(doc);
		  DirectoryReader r;
		  if (iter == 0)
		  {
			// use NRT
			r = w.Reader;
		  }
		  else
		  {
			// don't use NRT
			w.commit();
			r = DirectoryReader.open(dir);
		  }

		  IList<string> files = new List<string>(Arrays.asList(dir.listAll()));

		  // RAMDir won't have a write.lock, but fs dirs will:
		  files.Remove("write.lock");

		  Assert.IsTrue(files.Contains("_0.cfs"));
		  Assert.IsTrue(files.Contains("_0.cfe"));
		  Assert.IsTrue(files.Contains("_0.si"));
		  if (iter == 1)
		  {
			// we run a full commit so there should be a segments file etc.
			Assert.IsTrue(files.Contains("segments_1"));
			Assert.IsTrue(files.Contains("segments.gen"));
			Assert.AreEqual(files.ToString(), files.Count, 5);
		  }
		  else
		  {
			// this is an NRT reopen - no segments files yet

			Assert.AreEqual(files.ToString(), files.Count, 3);
		  }
		  w.addDocument(doc);
		  w.forceMerge(1);
		  if (iter == 1)
		  {
			w.commit();
		  }
		  IndexReader r2 = DirectoryReader.openIfChanged(r);
		  Assert.IsNotNull(r2);
		  Assert.IsTrue(r != r2);
		  files = Arrays.asList(dir.listAll());

		  // NOTE: here we rely on "Windows" behavior, ie, even
		  // though IW wanted to delete _0.cfs since it was
		  // merged away, because we have a reader open
		  // against this file, it should still be here:
		  Assert.IsTrue(files.Contains("_0.cfs"));
		  // forceMerge created this
		  //Assert.IsTrue(files.contains("_2.cfs"));
		  w.deleteUnusedFiles();

		  files = Arrays.asList(dir.listAll());
		  // r still holds this file open
		  Assert.IsTrue(files.Contains("_0.cfs"));
		  //Assert.IsTrue(files.contains("_2.cfs"));

		  r.close();
		  if (iter == 0)
		  {
			// on closing NRT reader, it calls writer.deleteUnusedFiles
			files = Arrays.asList(dir.listAll());
			Assert.IsFalse(files.Contains("_0.cfs"));
		  }
		  else
		  {
			// now writer can remove it
			w.deleteUnusedFiles();
			files = Arrays.asList(dir.listAll());
			Assert.IsFalse(files.Contains("_0.cfs"));
		  }
		  //Assert.IsTrue(files.contains("_2.cfs"));

		  w.close();
		  r2.close();

		  dir.close();
		}
	  }

	  public virtual void TestDeleteUnsedFiles2()
	  {
		// Validates that iw.deleteUnusedFiles() also deletes unused index commits
		// in case a deletion policy which holds onto commits is used.
		Directory dir = newDirectory();
		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setIndexDeletionPolicy(new SnapshotDeletionPolicy(new KeepOnlyLastCommitDeletionPolicy())));
		SnapshotDeletionPolicy sdp = (SnapshotDeletionPolicy) writer.Config.IndexDeletionPolicy;

		// First commit
		Document doc = new Document();

		FieldType customType = new FieldType(TextField.TYPE_STORED);
		customType.StoreTermVectors = true;
		customType.StoreTermVectorPositions = true;
		customType.StoreTermVectorOffsets = true;

		doc.add(newField("c", "val", customType));
		writer.addDocument(doc);
		writer.commit();
		Assert.AreEqual(1, DirectoryReader.listCommits(dir).size());

		// Keep that commit
		IndexCommit id = sdp.snapshot();

		// Second commit - now KeepOnlyLastCommit cannot delete the prev commit.
		doc = new Document();
		doc.add(newField("c", "val", customType));
		writer.addDocument(doc);
		writer.commit();
		Assert.AreEqual(2, DirectoryReader.listCommits(dir).size());

		// Should delete the unreferenced commit
		sdp.release(id);
		writer.deleteUnusedFiles();
		Assert.AreEqual(1, DirectoryReader.listCommits(dir).size());

		writer.close();
		dir.close();
	  }

	  public virtual void TestEmptyFSDirWithNoLock()
	  {
		// Tests that if FSDir is opened w/ a NoLockFactory (or SingleInstanceLF),
		// then IndexWriter ctor succeeds. Previously (LUCENE-2386) it failed
		// when listAll() was called in IndexFileDeleter.
		Directory dir = newFSDirectory(createTempDir("emptyFSDirNoLock"), NoLockFactory.NoLockFactory);
		(new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())))).close();
		dir.close();
	  }

	  public virtual void TestEmptyDirRollback()
	  {
		// TODO: generalize this test
		assumeFalse("test makes assumptions about file counts", Codec.Default is SimpleTextCodec);
		// Tests that if IW is created over an empty Directory, some documents are
		// indexed, flushed (but not committed) and then IW rolls back, then no
		// files are left in the Directory.
		Directory dir = newDirectory();
		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(2).setMergePolicy(newLogMergePolicy()).setUseCompoundFile(false));
		string[] files = dir.listAll();

		// Creating over empty dir should not create any files,
		// or, at most the write.lock file
		int extraFileCount;
		if (files.Length == 1)
		{
		  Assert.IsTrue(files[0].EndsWith("write.lock"));
		  extraFileCount = 1;
		}
		else
		{
		  Assert.AreEqual(0, files.Length);
		  extraFileCount = 0;
		}

		Document doc = new Document();
		FieldType customType = new FieldType(TextField.TYPE_STORED);
		customType.StoreTermVectors = true;
		customType.StoreTermVectorPositions = true;
		customType.StoreTermVectorOffsets = true;
		// create as many files as possible
		doc.add(newField("c", "val", customType));
		writer.addDocument(doc);
		// Adding just one document does not call flush yet.
		int computedExtraFileCount = 0;
		foreach (string file in dir.listAll())
		{
		  if (file.LastIndexOf('.') < 0 || !Arrays.asList("fdx", "fdt", "tvx", "tvd", "tvf").contains(file.Substring(file.LastIndexOf('.') + 1)))
			  // don't count stored fields and term vectors in
		  {
			++computedExtraFileCount;
		  }
		}
		Assert.AreEqual("only the stored and term vector files should exist in the directory", extraFileCount, computedExtraFileCount);

		doc = new Document();
		doc.add(newField("c", "val", customType));
		writer.addDocument(doc);

		// The second document should cause a flush.
		Assert.IsTrue("flush should have occurred and files should have been created", dir.listAll().length > 5 + extraFileCount);

		// After rollback, IW should remove all files
		writer.rollback();
		string[] allFiles = dir.listAll();
		Assert.IsTrue("no files should exist in the directory after rollback", allFiles.Length == 0 || Arrays.Equals(allFiles, new string[] {IndexWriter.WRITE_LOCK_NAME}));

		// Since we rolled-back above, that close should be a no-op
		writer.close();
		allFiles = dir.listAll();
		Assert.IsTrue("expected a no-op close after IW.rollback()", allFiles.Length == 0 || Arrays.Equals(allFiles, new string[] {IndexWriter.WRITE_LOCK_NAME}));
		dir.close();
	  }

	  public virtual void TestNoSegmentFile()
	  {
		BaseDirectoryWrapper dir = newDirectory();
		dir.LockFactory = NoLockFactory.NoLockFactory;
		IndexWriter w = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(2));

		Document doc = new Document();
		FieldType customType = new FieldType(TextField.TYPE_STORED);
		customType.StoreTermVectors = true;
		customType.StoreTermVectorPositions = true;
		customType.StoreTermVectorOffsets = true;
		doc.add(newField("c", "val", customType));
		w.addDocument(doc);
		w.addDocument(doc);
		IndexWriter w2 = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(2).setOpenMode(OpenMode.CREATE));

		w2.close();
		// If we don't do that, the test fails on Windows
		w.rollback();

		// this test leaves only segments.gen, which causes
		// DirectoryReader.indexExists to return true:
		dir.CheckIndexOnClose = false;
		dir.close();
	  }

	  public virtual void TestNoUnwantedTVFiles()
	  {

		Directory dir = newDirectory();
		IndexWriter indexWriter = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setRAMBufferSizeMB(0.01).setMergePolicy(newLogMergePolicy()));
		indexWriter.Config.MergePolicy.NoCFSRatio = 0.0;

		string BIG = "alskjhlaksjghlaksjfhalksvjepgjioefgjnsdfjgefgjhelkgjhqewlrkhgwlekgrhwelkgjhwelkgrhwlkejg";
		BIG = BIG + BIG + BIG + BIG;

		FieldType customType = new FieldType(TextField.TYPE_STORED);
		customType.OmitNorms = true;
		FieldType customType2 = new FieldType(TextField.TYPE_STORED);
		customType2.Tokenized = false;
		FieldType customType3 = new FieldType(TextField.TYPE_STORED);
		customType3.Tokenized = false;
		customType3.OmitNorms = true;

		for (int i = 0; i < 2; i++)
		{
		  Document doc = new Document();
		  doc.add(new Field("id", Convert.ToString(i) + BIG, customType3));
		  doc.add(new Field("str", Convert.ToString(i) + BIG, customType2));
		  doc.add(new Field("str2", Convert.ToString(i) + BIG, StoredTextType));
		  doc.add(new Field("str3", Convert.ToString(i) + BIG, customType));
		  indexWriter.addDocument(doc);
		}

		indexWriter.close();

		TestUtil.checkIndex(dir);

		AssertNoUnreferencedFiles(dir, "no tv files");
		DirectoryReader r0 = DirectoryReader.open(dir);
		foreach (AtomicReaderContext ctx in r0.leaves())
		{
		  SegmentReader sr = (SegmentReader) ctx.reader();
		  Assert.IsFalse(sr.FieldInfos.hasVectors());
		}

		r0.close();
		dir.close();
	  }

	  internal sealed class StringSplitAnalyzer : Analyzer
	  {
		public override TokenStreamComponents CreateComponents(string fieldName, Reader reader)
		{
		  return new TokenStreamComponents(new StringSplitTokenizer(reader));
		}
	  }

	  private class StringSplitTokenizer : Tokenizer
	  {
		internal string[] Tokens;
		internal int Upto;
		internal readonly CharTermAttribute TermAtt = addAttribute(typeof(CharTermAttribute));

		public StringSplitTokenizer(Reader r) : base(r)
		{
		  try
		  {
			Reader = r;
		  }
		  catch (IOException e)
		  {
			throw new Exception(e);
		  }
		}

		public override bool IncrementToken()
		{
		  ClearAttributes();
		  if (Upto < Tokens.Length)
		  {
			TermAtt.SetEmpty();
			TermAtt.append(Tokens[Upto]);
			Upto++;
			return true;
		  }
		  else
		  {
			return false;
		  }
		}

		public override void Reset()
		{
		  base.reset();
		  this.Upto = 0;
		  StringBuilder b = new StringBuilder();
		  char[] buffer = new char[1024];
		  int n;
		  while ((n = input.read(buffer)) != -1)
		  {
			b.Append(buffer, 0, n);
		  }
		  this.Tokens = b.ToString().Split(" ", true);
		}
	  }

	  /// <summary>
	  /// Make sure we skip wicked long terms.
	  /// </summary>
	  public virtual void TestWickedLongTerm()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter w = new RandomIndexWriter(random(), dir, new StringSplitAnalyzer());

		char[] chars = new char[DocumentsWriterPerThread.MAX_TERM_LENGTH_UTF8];
		Arrays.fill(chars, 'x');
		Document doc = new Document();
		string bigTerm = new string(chars);
		BytesRef bigTermBytesRef = new BytesRef(bigTerm);

		// this contents produces a too-long term:
		string contents = "abc xyz x" + bigTerm + " another term";
		doc.add(new TextField("content", contents, Field.Store.NO));
		try
		{
		  w.addDocument(doc);
		  Assert.Fail("should have hit exception");
		}
		catch (System.ArgumentException iae)
		{
		  // expected
		}

		// Make sure we can add another normal document
		doc = new Document();
		doc.add(new TextField("content", "abc bbb ccc", Field.Store.NO));
		w.addDocument(doc);

		// So we remove the deleted doc:
		w.forceMerge(1);

		IndexReader reader = w.Reader;
		w.close();

		// Make sure all terms < max size were indexed
		Assert.AreEqual(1, reader.docFreq(new Term("content", "abc")));
		Assert.AreEqual(1, reader.docFreq(new Term("content", "bbb")));
		Assert.AreEqual(0, reader.docFreq(new Term("content", "term")));

		// Make sure the doc that has the massive term is NOT in
		// the index:
		Assert.AreEqual("document with wicked long term is in the index!", 1, reader.numDocs());

		reader.close();
		dir.close();
		dir = newDirectory();

		// Make sure we can add a document with exactly the
		// maximum length term, and search on that term:
		doc = new Document();
		FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
		customType.Tokenized = false;
		Field contentField = new Field("content", "", customType);
		doc.add(contentField);

		w = new RandomIndexWriter(random(), dir);

		contentField.StringValue = "other";
		w.addDocument(doc);

		contentField.StringValue = "term";
		w.addDocument(doc);

		contentField.StringValue = bigTerm;
		w.addDocument(doc);

		contentField.StringValue = "zzz";
		w.addDocument(doc);

		reader = w.Reader;
		w.close();
		Assert.AreEqual(1, reader.docFreq(new Term("content", bigTerm)));

		SortedDocValues dti = FieldCache.DEFAULT.getTermsIndex(SlowCompositeReaderWrapper.wrap(reader), "content", random().nextFloat() * PackedInts.FAST);
		Assert.AreEqual(4, dti.ValueCount);
		BytesRef br = new BytesRef();
		dti.lookupOrd(2, br);
		Assert.AreEqual(bigTermBytesRef, br);
		reader.close();
		dir.close();
	  }

	  // LUCENE-3183
	  public virtual void TestEmptyFieldNameTIIOne()
	  {
		Directory dir = newDirectory();
		IndexWriterConfig iwc = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		iwc.TermIndexInterval = 1;
		iwc.ReaderTermsIndexDivisor = 1;
		IndexWriter writer = new IndexWriter(dir, iwc);
		Document doc = new Document();
		doc.add(newTextField("", "a b c", Field.Store.NO));
		writer.addDocument(doc);
		writer.close();
		dir.close();
	  }

	  public virtual void TestDeleteAllNRTLeftoverFiles()
	  {

		Directory d = new MockDirectoryWrapper(random(), new RAMDirectory());
		IndexWriter w = new IndexWriter(d, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		Document doc = new Document();
		for (int i = 0; i < 20; i++)
		{
		  for (int j = 0; j < 100; ++j)
		  {
			w.addDocument(doc);
		  }
		  w.commit();
		  DirectoryReader.open(w, true).close();

		  w.deleteAll();
		  w.commit();
		  // Make sure we accumulate no files except for empty
		  // segments_N and segments.gen:
		  Assert.IsTrue(d.listAll().length <= 2);
		}

		w.close();
		d.close();
	  }

	  public virtual void TestNRTReaderVersion()
	  {
		Directory d = new MockDirectoryWrapper(random(), new RAMDirectory());
		IndexWriter w = new IndexWriter(d, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		Document doc = new Document();
		doc.add(newStringField("id", "0", Field.Store.YES));
		w.addDocument(doc);
		DirectoryReader r = w.Reader;
		long version = r.Version;
		r.close();

		w.addDocument(doc);
		r = w.Reader;
		long version2 = r.Version;
		r.close();
		assert(version2 > version);

		w.deleteDocuments(new Term("id", "0"));
		r = w.Reader;
		w.close();
		long version3 = r.Version;
		r.close();
		assert(version3 > version2);
		d.close();
	  }

	  public virtual void TestWhetherDeleteAllDeletesWriteLock()
	  {
		Directory d = newFSDirectory(createTempDir("TestIndexWriter.testWhetherDeleteAllDeletesWriteLock"));
		// Must use SimpleFSLockFactory... NativeFSLockFactory
		// somehow "knows" a lock is held against write.lock
		// even if you remove that file:
		d.LockFactory = new SimpleFSLockFactory();
		RandomIndexWriter w1 = new RandomIndexWriter(random(), d);
		w1.deleteAll();
		try
		{
		  new RandomIndexWriter(random(), d, newIndexWriterConfig(TEST_VERSION_CURRENT, null).setWriteLockTimeout(100));
		  Assert.Fail("should not be able to create another writer");
		}
		catch (LockObtainFailedException lofe)
		{
		  // expected
		}
		w1.close();
		d.close();
	  }

	  public virtual void TestChangeIndexOptions()
	  {
		Directory dir = newDirectory();
		IndexWriter w = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));

		FieldType docsAndFreqs = new FieldType(TextField.TYPE_NOT_STORED);
		docsAndFreqs.IndexOptions = IndexOptions.DOCS_AND_FREQS;

		FieldType docsOnly = new FieldType(TextField.TYPE_NOT_STORED);
		docsOnly.IndexOptions = IndexOptions.DOCS_ONLY;

		Document doc = new Document();
		doc.add(new Field("field", "a b c", docsAndFreqs));
		w.addDocument(doc);
		w.addDocument(doc);

		doc = new Document();
		doc.add(new Field("field", "a b c", docsOnly));
		w.addDocument(doc);
		w.close();
		dir.close();
	  }

	  public virtual void TestOnlyUpdateDocuments()
	  {
		Directory dir = newDirectory();
		IndexWriter w = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));

		IList<Document> docs = new List<Document>();
		docs.Add(new Document());
		w.updateDocuments(new Term("foo", "bar"), docs);
		w.close();
		dir.close();
	  }

	  // LUCENE-3872
	  public virtual void TestPrepareCommitThenClose()
	  {
		Directory dir = newDirectory();
		IndexWriter w = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));

		w.prepareCommit();
		try
		{
		  w.close();
		  Assert.Fail("should have hit exception");
		}
		catch (IllegalStateException ise)
		{
		  // expected
		}
		w.commit();
		w.close();
		IndexReader r = DirectoryReader.open(dir);
		Assert.AreEqual(0, r.maxDoc());
		r.close();
		dir.close();
	  }

	  // LUCENE-3872
	  public virtual void TestPrepareCommitThenRollback()
	  {
		Directory dir = newDirectory();
		IndexWriter w = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));

		w.prepareCommit();
		w.rollback();
		Assert.IsFalse(DirectoryReader.indexExists(dir));
		dir.close();
	  }

	  // LUCENE-3872
	  public virtual void TestPrepareCommitThenRollback2()
	  {
		Directory dir = newDirectory();
		IndexWriter w = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));

		w.commit();
		w.addDocument(new Document());
		w.prepareCommit();
		w.rollback();
		Assert.IsTrue(DirectoryReader.indexExists(dir));
		IndexReader r = DirectoryReader.open(dir);
		Assert.AreEqual(0, r.maxDoc());
		r.close();
		dir.close();
	  }

	  public virtual void TestDontInvokeAnalyzerForUnAnalyzedFields()
	  {
		Analyzer analyzer = new AnalyzerAnonymousInnerClassHelper(this);
		Directory dir = newDirectory();
		IndexWriter w = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, analyzer));
		Document doc = new Document();
		FieldType customType = new FieldType(StringField.TYPE_NOT_STORED);
		customType.StoreTermVectors = true;
		customType.StoreTermVectorPositions = true;
		customType.StoreTermVectorOffsets = true;
		Field f = newField("field", "abcd", customType);
		doc.add(f);
		doc.add(f);
		Field f2 = newField("field", "", customType);
		doc.add(f2);
		doc.add(f);
		w.addDocument(doc);
		w.close();
		dir.close();
	  }

	  private class AnalyzerAnonymousInnerClassHelper : Analyzer
	  {
		  private readonly TestIndexWriter OuterInstance;

		  public AnalyzerAnonymousInnerClassHelper(TestIndexWriter outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents CreateComponents(string fieldName, Reader reader)
		  {
			throw new IllegalStateException("don't invoke me!");
		  }

		  public override int GetPositionIncrementGap(string fieldName)
		  {
			throw new IllegalStateException("don't invoke me!");
		  }

		  public override int GetOffsetGap(string fieldName)
		  {
			throw new IllegalStateException("don't invoke me!");
		  }
	  }

	  //LUCENE-1468 -- make sure opening an IndexWriter with
	  // create=true does not remove non-index files

	  public virtual void TestOtherFiles()
	  {
		Directory dir = newDirectory();
		IndexWriter iw = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		iw.addDocument(new Document());
		iw.close();
		try
		{
		  // Create my own random file:
		  IndexOutput @out = dir.createOutput("myrandomfile", newIOContext(random()));
		  @out.writeByte((sbyte) 42);
		  @out.close();

		  (new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())))).close();

		  Assert.IsTrue(slowFileExists(dir, "myrandomfile"));
		}
		finally
		{
		  dir.close();
		}
	  }

	  // LUCENE-3849
	  public virtual void TestStopwordsPosIncHole()
	  {
		Directory dir = newDirectory();
		Analyzer a = new AnalyzerAnonymousInnerClassHelper2(this);
		RandomIndexWriter iw = new RandomIndexWriter(random(), dir, a);
		Document doc = new Document();
		doc.add(new TextField("body", "just a", Field.Store.NO));
		doc.add(new TextField("body", "test of gaps", Field.Store.NO));
		iw.addDocument(doc);
		IndexReader ir = iw.Reader;
		iw.close();
		IndexSearcher @is = newSearcher(ir);
		PhraseQuery pq = new PhraseQuery();
		pq.add(new Term("body", "just"), 0);
		pq.add(new Term("body", "test"), 2);
		// body:"just ? test"
		Assert.AreEqual(1, @is.search(pq, 5).totalHits);
		ir.close();
		dir.close();
	  }

	  private class AnalyzerAnonymousInnerClassHelper2 : Analyzer
	  {
		  private readonly TestIndexWriter OuterInstance;

		  public AnalyzerAnonymousInnerClassHelper2(TestIndexWriter outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents CreateComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new MockTokenizer(reader);
			TokenStream stream = new MockTokenFilter(tokenizer, MockTokenFilter.ENGLISH_STOPSET);
			return new TokenStreamComponents(tokenizer, stream);
		  }
	  }

	  // LUCENE-3849
	  public virtual void TestStopwordsPosIncHole2()
	  {
		// use two stopfilters for testing here
		Directory dir = newDirectory();
		Automaton secondSet = BasicAutomata.makeString("foobar");
		Analyzer a = new AnalyzerAnonymousInnerClassHelper3(this, secondSet);
		RandomIndexWriter iw = new RandomIndexWriter(random(), dir, a);
		Document doc = new Document();
		doc.add(new TextField("body", "just a foobar", Field.Store.NO));
		doc.add(new TextField("body", "test of gaps", Field.Store.NO));
		iw.addDocument(doc);
		IndexReader ir = iw.Reader;
		iw.close();
		IndexSearcher @is = newSearcher(ir);
		PhraseQuery pq = new PhraseQuery();
		pq.add(new Term("body", "just"), 0);
		pq.add(new Term("body", "test"), 3);
		// body:"just ? ? test"
		Assert.AreEqual(1, @is.search(pq, 5).totalHits);
		ir.close();
		dir.close();
	  }

	  private class AnalyzerAnonymousInnerClassHelper3 : Analyzer
	  {
		  private readonly TestIndexWriter OuterInstance;

		  private Automaton SecondSet;

		  public AnalyzerAnonymousInnerClassHelper3(TestIndexWriter outerInstance, Automaton secondSet)
		  {
			  this.OuterInstance = outerInstance;
			  this.SecondSet = secondSet;
		  }

		  protected internal override TokenStreamComponents CreateComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new MockTokenizer(reader);
			TokenStream stream = new MockTokenFilter(tokenizer, MockTokenFilter.ENGLISH_STOPSET);
			stream = new MockTokenFilter(stream, new CharacterRunAutomaton(SecondSet));
			return new TokenStreamComponents(tokenizer, stream);
		  }
	  }

	  // here we do better, there is no current segments file, so we don't delete anything.
	  // however, if you actually go and make a commit, the next time you run indexwriter
	  // this file will be gone.
	  public virtual void TestOtherFiles2()
	  {
		Directory dir = newDirectory();
		try
		{
		  // Create my own random file:
		  IndexOutput @out = dir.createOutput("_a.frq", newIOContext(random()));
		  @out.writeByte((sbyte) 42);
		  @out.close();

		  (new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())))).close();

		  Assert.IsTrue(slowFileExists(dir, "_a.frq"));

		  IndexWriter iw = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		  iw.addDocument(new Document());
		  iw.close();

		  Assert.IsFalse(slowFileExists(dir, "_a.frq"));
		}
		finally
		{
		  dir.close();
		}
	  }

	  // LUCENE-4398
	  public virtual void TestRotatingFieldNames()
	  {
		Directory dir = newFSDirectory(createTempDir("TestIndexWriter.testChangingFields"));
		IndexWriterConfig iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		iwc.RAMBufferSizeMB = 0.2;
		iwc.MaxBufferedDocs = -1;
		IndexWriter w = new IndexWriter(dir, iwc);
		int upto = 0;

		FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
		ft.OmitNorms = true;

		int firstDocCount = -1;
		for (int iter = 0;iter < 10;iter++)
		{
		  int startFlushCount = w.FlushCount;
		  int docCount = 0;
		  while (w.FlushCount == startFlushCount)
		  {
			Document doc = new Document();
			for (int i = 0;i < 10;i++)
			{
			  doc.add(new Field("field" + (upto++), "content", ft));
			}
			w.addDocument(doc);
			docCount++;
		  }

		  if (VERBOSE)
		  {
			Console.WriteLine("TEST: iter=" + iter + " flushed after docCount=" + docCount);
		  }

		  if (iter == 0)
		  {
			firstDocCount = docCount;
		  }

		  Assert.IsTrue("flushed after too few docs: first segment flushed at docCount=" + firstDocCount + ", but current segment flushed after docCount=" + docCount + "; iter=" + iter, ((float) docCount) / firstDocCount > 0.9);

		  if (upto > 5000)
		  {
			// Start re-using field names after a while
			// ... important because otherwise we can OOME due
			// to too many FieldInfo instances.
			upto = 0;
		  }
		}
		w.close();
		dir.close();
	  }

	  // LUCENE-4575
	  public virtual void TestCommitWithUserDataOnly()
	  {
		Directory dir = newDirectory();
		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, null));
		writer.commit(); // first commit to complete IW create transaction.

		// this should store the commit data, even though no other changes were made
		writer.CommitData = new Dictionary<string, string>() {{put("key", "value");}};
		writer.commit();

		DirectoryReader r = DirectoryReader.open(dir);
		Assert.AreEqual("value", r.IndexCommit.UserData.get("key"));
		r.close();

		// now check setCommitData and prepareCommit/commit sequence
		writer.CommitData = new Dictionary<string, string>() {{put("key", "value1");}};
		writer.prepareCommit();
		writer.CommitData = new Dictionary<string, string>() {{put("key", "value2");}};
		writer.commit(); // should commit the first commitData only, per protocol

		r = DirectoryReader.open(dir);
		Assert.AreEqual("value1", r.IndexCommit.UserData.get("key"));
		r.close();

		// now should commit the second commitData - there was a bug where 
		// IndexWriter.finishCommit overrode the second commitData
		writer.commit();
		r = DirectoryReader.open(dir);
		Assert.AreEqual("IndexWriter.finishCommit may have overridden the second commitData", "value2", r.IndexCommit.UserData.get("key"));
		r.close();

		writer.close();
		dir.close();
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testGetCommitData() throws Exception
	  public virtual void TestGetCommitData()
	  {
		Directory dir = newDirectory();
		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, null));
		writer.CommitData = new Dictionary<string, string>() {{put("key", "value");}};
		Assert.AreEqual("value", writer.CommitData.get("key"));
		writer.close();

		// validate that it's also visible when opening a new IndexWriter
		writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, null).setOpenMode(OpenMode.APPEND));
		Assert.AreEqual("value", writer.CommitData.get("key"));
		writer.close();

		dir.close();
	  }

	  public virtual void TestIterableThrowsException()
	  {
		Directory dir = newDirectory();
		IndexWriter w = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		int iters = atLeast(100);
		int docCount = 0;
		int docId = 0;
		Set<string> liveIds = new HashSet<string>();
		for (int i = 0; i < iters; i++)
		{
		  IList<IEnumerable<IndexableField>> docs = new List<IEnumerable<IndexableField>>();
		  FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
		  FieldType idFt = new FieldType(TextField.TYPE_STORED);

		  int numDocs = atLeast(4);
		  for (int j = 0; j < numDocs; j++)
		  {
			Document doc = new Document();
			doc.add(newField("id", "" + (docId++), idFt));
			doc.add(newField("foo", TestUtil.randomSimpleString(random()), ft));
			docs.Add(doc);
		  }
		  bool success = false;
		  try
		  {
			w.addDocuments(new RandomFailingFieldIterable(docs, random()));
			success = true;
		  }
		  catch (Exception e)
		  {
			Assert.AreEqual("boom", e.Message);
		  }
		  finally
		  {
			if (success)
			{
			  docCount += docs.Count;
			  foreach (IEnumerable<IndexableField> indexDocument in docs)
			  {
				liveIds.add(((Document)indexDocument).get("id"));
			  }
			}
		  }
		}
		DirectoryReader reader = w.Reader;
		Assert.AreEqual(docCount, reader.numDocs());
		IList<AtomicReaderContext> leaves = reader.leaves();
		foreach (AtomicReaderContext atomicReaderContext in leaves)
		{
		  AtomicReader ar = atomicReaderContext.reader();
		  Bits liveDocs = ar.LiveDocs;
		  int maxDoc = ar.maxDoc();
		  for (int i = 0; i < maxDoc; i++)
		  {
			if (liveDocs == null || liveDocs.get(i))
			{
			  Assert.IsTrue(liveIds.remove(ar.document(i).get("id")));
			}
		  }
		}
		Assert.IsTrue(liveIds.Empty);
		IOUtils.close(reader, w, dir);
	  }

	  private class RandomFailingFieldIterable : IEnumerable<IEnumerable<IndexableField>>
	  {
		internal readonly IList<IEnumerable<IndexableField>> DocList;
		internal readonly Random Random;

		public RandomFailingFieldIterable(IList<IEnumerable<IndexableField>> docList, Random random)
		{
		  this.DocList = docList;
		  this.Random = random;
		}

		public virtual IEnumerator<IEnumerable<IndexableField>> GetEnumerator()
		{
		  IEnumerator<IEnumerable<IndexableField>> docIter = DocList.GetEnumerator();
		  return new IteratorAnonymousInnerClassHelper(this, docIter);
		}

		private class IteratorAnonymousInnerClassHelper : IEnumerator<IEnumerable<IndexableField>>
		{
			private readonly RandomFailingFieldIterable OuterInstance;

			private IEnumerator<IEnumerable<IndexableField>> DocIter;

			public IteratorAnonymousInnerClassHelper(RandomFailingFieldIterable outerInstance, IEnumerator<IEnumerable<IndexableField>> docIter)
			{
				this.OuterInstance = outerInstance;
				this.DocIter = docIter;
			}


			public virtual bool HasNext()
			{
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
			  return DocIter.hasNext();
			}

			public virtual IEnumerable<IndexableField> Next()
			{
			  if (OuterInstance.Random.Next(5) == 0)
			  {
				throw new Exception("boom");
			  }
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
			  return DocIter.next();
			}

			public virtual void Remove()
			{
				throw new System.NotSupportedException();
			}


		}

	  }

	  // LUCENE-2727/LUCENE-2812/LUCENE-4738:
	  public virtual void TestCorruptFirstCommit()
	  {
		for (int i = 0;i < 6;i++)
		{
		  BaseDirectoryWrapper dir = newDirectory();
		  dir.createOutput("segments_0", IOContext.DEFAULT).close();
		  IndexWriterConfig iwc = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		  int mode = i / 2;
		  if (mode == 0)
		  {
			iwc.OpenMode = OpenMode.CREATE;
		  }
		  else if (mode == 1)
		  {
			iwc.OpenMode = OpenMode.APPEND;
		  }
		  else if (mode == 2)
		  {
			iwc.OpenMode = OpenMode.CREATE_OR_APPEND;
		  }

		  if (VERBOSE)
		  {
			Console.WriteLine("\nTEST: i=" + i);
		  }

		  try
		  {
			if ((i & 1) == 0)
			{
			  (new IndexWriter(dir, iwc)).close();
			}
			else
			{
			  (new IndexWriter(dir, iwc)).rollback();
			}
			if (mode != 0)
			{
			  Assert.Fail("expected exception");
			}
		  }
		  catch (IOException ioe)
		  {
			// OpenMode.APPEND should throw an exception since no
			// index exists:
			if (mode == 0)
			{
			  // Unexpected
			  throw ioe;
			}
		  }

		  if (VERBOSE)
		  {
			Console.WriteLine("  at close: " + Arrays.ToString(dir.listAll()));
		  }

		  if (mode != 0)
		  {
			dir.CheckIndexOnClose = false;
		  }
		  dir.close();
		}
	  }

	  public virtual void TestHasUncommittedChanges()
	  {
		Directory dir = newDirectory();
		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		Assert.IsTrue(writer.hasUncommittedChanges()); // this will be true because a commit will create an empty index
		Document doc = new Document();
		doc.add(newTextField("myfield", "a b c", Field.Store.NO));
		writer.addDocument(doc);
		Assert.IsTrue(writer.hasUncommittedChanges());

		// Must commit, waitForMerges, commit again, to be
		// certain that hasUncommittedChanges returns false:
		writer.commit();
		writer.waitForMerges();
		writer.commit();
		Assert.IsFalse(writer.hasUncommittedChanges());
		writer.addDocument(doc);
		Assert.IsTrue(writer.hasUncommittedChanges());
		writer.commit();
		doc = new Document();
		doc.add(newStringField("id", "xyz", Field.Store.YES));
		writer.addDocument(doc);
		Assert.IsTrue(writer.hasUncommittedChanges());

		// Must commit, waitForMerges, commit again, to be
		// certain that hasUncommittedChanges returns false:
		writer.commit();
		writer.waitForMerges();
		writer.commit();
		Assert.IsFalse(writer.hasUncommittedChanges());
		writer.deleteDocuments(new Term("id", "xyz"));
		Assert.IsTrue(writer.hasUncommittedChanges());

		// Must commit, waitForMerges, commit again, to be
		// certain that hasUncommittedChanges returns false:
		writer.commit();
		writer.waitForMerges();
		writer.commit();
		Assert.IsFalse(writer.hasUncommittedChanges());
		writer.close();

		writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		Assert.IsFalse(writer.hasUncommittedChanges());
		writer.addDocument(doc);
		Assert.IsTrue(writer.hasUncommittedChanges());

		writer.close();
		dir.close();
	  }

	  public virtual void TestMergeAllDeleted()
	  {
		Directory dir = newDirectory();
		IndexWriterConfig iwc = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		SetOnce<IndexWriter> iwRef = new SetOnce<IndexWriter>();
		iwc.InfoStream = new RandomIndexWriter.TestPointInfoStream(iwc.InfoStream, new TestPointAnonymousInnerClassHelper(this, iwRef));
		IndexWriter evilWriter = new IndexWriter(dir, iwc);
		iwRef.set(evilWriter);
		for (int i = 0; i < 1000; i++)
		{
		  AddDoc(evilWriter);
		  if (random().Next(17) == 0)
		  {
			evilWriter.commit();
		  }
		}
		evilWriter.deleteDocuments(new MatchAllDocsQuery());
		evilWriter.forceMerge(1);
		evilWriter.close();
		dir.close();
	  }

	  private class TestPointAnonymousInnerClassHelper : RandomIndexWriter.TestPoint
	  {
		  private readonly TestIndexWriter OuterInstance;

		  private SetOnce<IndexWriter> IwRef;

		  public TestPointAnonymousInnerClassHelper(TestIndexWriter outerInstance, SetOnce<IndexWriter> iwRef)
		  {
			  this.OuterInstance = outerInstance;
			  this.IwRef = iwRef;
		  }

		  public override void Apply(string message)
		  {
			if ("startCommitMerge".Equals(message))
			{
			  IwRef.get().KeepFullyDeletedSegments = false;
			}
			else if ("startMergeInit".Equals(message))
			{
			  IwRef.get().KeepFullyDeletedSegments = true;
			}
		  }
	  }

	  // LUCENE-5239
	  public virtual void TestDeleteSameTermAcrossFields()
	  {
		Directory dir = newDirectory();
		IndexWriterConfig iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		IndexWriter w = new IndexWriter(dir, iwc);
		Document doc = new Document();
		doc.add(new TextField("a", "foo", Field.Store.NO));
		w.addDocument(doc);

		// Should not delete the document; with LUCENE-5239 the
		// "foo" from the 2nd delete term would incorrectly
		// match field a's "foo":
		w.deleteDocuments(new Term("a", "xxx"));
		w.deleteDocuments(new Term("b", "foo"));
		IndexReader r = w.Reader;
		w.close();

		// Make sure document was not (incorrectly) deleted:
		Assert.AreEqual(1, r.numDocs());
		r.close();
		dir.close();
	  }

	  // LUCENE-5574
	  public virtual void TestClosingNRTReaderDoesNotCorruptYourIndex()
	  {

		// Windows disallows deleting & overwriting files still
		// open for reading:
		assumeFalse("this test can't run on Windows", Constants.WINDOWS);

		MockDirectoryWrapper dir = newMockDirectory();

		// Allow deletion of still open files:
		dir.NoDeleteOpenFile = false;

		// Allow writing to same file more than once:
		dir.PreventDoubleWrite = false;

		IndexWriterConfig iwc = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		LogMergePolicy lmp = new LogDocMergePolicy();
		lmp.MergeFactor = 2;
		iwc.MergePolicy = lmp;

		RandomIndexWriter w = new RandomIndexWriter(random(), dir, iwc);
		Document doc = new Document();
		doc.add(new TextField("a", "foo", Field.Store.NO));
		w.addDocument(doc);
		w.commit();
		w.addDocument(doc);

		// Get a new reader, but this also sets off a merge:
		IndexReader r = w.Reader;
		w.close();

		// Blow away index and make a new writer:
		foreach (string fileName in dir.listAll())
		{
		  dir.deleteFile(fileName);
		}

		w = new RandomIndexWriter(random(), dir);
		w.addDocument(doc);
		w.close();
		r.close();
		dir.close();
	  }
	}

}