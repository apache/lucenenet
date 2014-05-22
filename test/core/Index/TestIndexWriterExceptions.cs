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


	using Analyzer = Lucene.Net.Analysis.Analyzer;
	using CannedTokenStream = Lucene.Net.Analysis.CannedTokenStream;
	using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
	using MockTokenizer = Lucene.Net.Analysis.MockTokenizer;
	using Token = Lucene.Net.Analysis.Token;
	using TokenFilter = Lucene.Net.Analysis.TokenFilter;
	using TokenStream = Lucene.Net.Analysis.TokenStream;
	using BinaryDocValuesField = Lucene.Net.Document.BinaryDocValuesField;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using FieldType = Lucene.Net.Document.FieldType;
	using NumericDocValuesField = Lucene.Net.Document.NumericDocValuesField;
	using SortedDocValuesField = Lucene.Net.Document.SortedDocValuesField;
	using SortedSetDocValuesField = Lucene.Net.Document.SortedSetDocValuesField;
	using StringField = Lucene.Net.Document.StringField;
	using TextField = Lucene.Net.Document.TextField;
	using OpenMode = Lucene.Net.Index.IndexWriterConfig.OpenMode;
	using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
	using IndexSearcher = Lucene.Net.Search.IndexSearcher;
	using PhraseQuery = Lucene.Net.Search.PhraseQuery;
	using BaseDirectoryWrapper = Lucene.Net.Store.BaseDirectoryWrapper;
	using Directory = Lucene.Net.Store.Directory;
	using IOContext = Lucene.Net.Store.IOContext;
	using IndexInput = Lucene.Net.Store.IndexInput;
	using IndexOutput = Lucene.Net.Store.IndexOutput;
	using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
	using FakeIOException = Lucene.Net.Store.MockDirectoryWrapper.FakeIOException;
	using RAMDirectory = Lucene.Net.Store.RAMDirectory;
	using Bits = Lucene.Net.Util.Bits;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using InfoStream = Lucene.Net.Util.InfoStream;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;

	public class TestIndexWriterExceptions : LuceneTestCase
	{

	  private class DocCopyIterator : IEnumerable<Document>
	  {
		internal readonly Document Doc;
		internal readonly int Count;

		/* private field types */
		/* private field types */

		internal static readonly FieldType Custom1 = new FieldType(TextField.TYPE_NOT_STORED);
		internal static readonly FieldType Custom2 = new FieldType();
		internal static readonly FieldType Custom3 = new FieldType();
		internal static readonly FieldType Custom4 = new FieldType(StringField.TYPE_NOT_STORED);
		internal static readonly FieldType Custom5 = new FieldType(TextField.TYPE_STORED);

		static DocCopyIterator()
		{

		  Custom1.StoreTermVectors = true;
		  Custom1.StoreTermVectorPositions = true;
		  Custom1.StoreTermVectorOffsets = true;

		  Custom2.Stored = true;
		  Custom2.Indexed = true;

		  Custom3.Stored = true;

		  Custom4.StoreTermVectors = true;
		  Custom4.StoreTermVectorPositions = true;
		  Custom4.StoreTermVectorOffsets = true;

		  Custom5.StoreTermVectors = true;
		  Custom5.StoreTermVectorPositions = true;
		  Custom5.StoreTermVectorOffsets = true;
		}

		public DocCopyIterator(Document doc, int count)
		{
		  this.Count = count;
		  this.Doc = doc;
		}

		public virtual IEnumerator<Document> GetEnumerator()
		{
		  return new IteratorAnonymousInnerClassHelper(this);
		}

		private class IteratorAnonymousInnerClassHelper : IEnumerator<Document>
		{
			private readonly DocCopyIterator OuterInstance;

			public IteratorAnonymousInnerClassHelper(DocCopyIterator outerInstance)
			{
				this.OuterInstance = outerInstance;
			}

			internal int upto;

			public virtual bool HasNext()
			{
			  return upto < OuterInstance.Count;
			}

			public virtual Document Next()
			{
			  upto++;
			  return OuterInstance.Doc;
			}

			public virtual void Remove()
			{
			  throw new System.NotSupportedException();
			}
		}
	  }

	  private class IndexerThread : System.Threading.Thread
	  {
		  private readonly TestIndexWriterExceptions OuterInstance;


		internal IndexWriter Writer;

		internal readonly Random r = new Random(random().nextLong());
		internal volatile Exception Failure;

		public IndexerThread(TestIndexWriterExceptions outerInstance, int i, IndexWriter writer)
		{
			this.OuterInstance = outerInstance;
		  Name = "Indexer " + i;
		  this.Writer = writer;
		}

		public override void Run()
		{

		  Document doc = new Document();

		  doc.add(newTextField(r, "content1", "aaa bbb ccc ddd", Field.Store.YES));
		  doc.add(newField(r, "content6", "aaa bbb ccc ddd", DocCopyIterator.Custom1));
		  doc.add(newField(r, "content2", "aaa bbb ccc ddd", DocCopyIterator.Custom2));
		  doc.add(newField(r, "content3", "aaa bbb ccc ddd", DocCopyIterator.Custom3));

		  doc.add(newTextField(r, "content4", "aaa bbb ccc ddd", Field.Store.NO));
		  doc.add(newStringField(r, "content5", "aaa bbb ccc ddd", Field.Store.NO));
		  if (defaultCodecSupportsDocValues())
		  {
			doc.add(new NumericDocValuesField("numericdv", 5));
			doc.add(new BinaryDocValuesField("binarydv", new BytesRef("hello")));
			doc.add(new SortedDocValuesField("sorteddv", new BytesRef("world")));
		  }
		  if (defaultCodecSupportsSortedSet())
		  {
			doc.add(new SortedSetDocValuesField("sortedsetdv", new BytesRef("hellllo")));
			doc.add(new SortedSetDocValuesField("sortedsetdv", new BytesRef("again")));
		  }

		  doc.add(newField(r, "content7", "aaa bbb ccc ddd", DocCopyIterator.Custom4));

		  Field idField = newField(r, "id", "", DocCopyIterator.Custom2);
		  doc.add(idField);

		  long stopTime = System.currentTimeMillis() + 500;

		  do
		  {
			if (VERBOSE)
			{
			  Console.WriteLine(Thread.CurrentThread.Name + ": TEST: IndexerThread: cycle");
			}
			outerInstance.DoFail.set(this);
			string id = "" + r.Next(50);
			idField.StringValue = id;
			Term idTerm = new Term("id", id);
			try
			{
			  if (r.nextBoolean())
			  {
				Writer.updateDocuments(idTerm, new DocCopyIterator(doc, TestUtil.Next(r, 1, 20)));
			  }
			  else
			  {
				Writer.updateDocument(idTerm, doc);
			  }
			}
			catch (Exception re)
			{
			  if (VERBOSE)
			  {
				Console.WriteLine(Thread.CurrentThread.Name + ": EXC: ");
				re.printStackTrace(System.out);
			  }
			  try
			  {
				TestUtil.checkIndex(Writer.Directory);
			  }
			  catch (IOException ioe)
			  {
				Console.WriteLine(Thread.CurrentThread.Name + ": unexpected exception1");
				ioe.printStackTrace(System.out);
				Failure = ioe;
				break;
			  }
			}
			catch (Exception t)
			{
			  Console.WriteLine(Thread.CurrentThread.Name + ": unexpected exception2");
			  t.printStackTrace(System.out);
			  Failure = t;
			  break;
			}

			outerInstance.DoFail.set(null);

			// After a possible exception (above) I should be able
			// to add a new document without hitting an
			// exception:
			try
			{
			  Writer.updateDocument(idTerm, doc);
			}
			catch (Exception t)
			{
			  Console.WriteLine(Thread.CurrentThread.Name + ": unexpected exception3");
			  t.printStackTrace(System.out);
			  Failure = t;
			  break;
			}
		  } while (System.currentTimeMillis() < stopTime);
		}
	  }

	  internal ThreadLocal<Thread> DoFail = new ThreadLocal<Thread>();

	  private class TestPoint1 : RandomIndexWriter.TestPoint
	  {
		  private readonly TestIndexWriterExceptions OuterInstance;

		  public TestPoint1(TestIndexWriterExceptions outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }

		internal Random r = new Random(random().nextLong());
		public override void Apply(string name)
		{
		  if (outerInstance.DoFail.get() != null && !name.Equals("startDoFlush") && r.Next(40) == 17)
		  {
			if (VERBOSE)
			{
			  Console.WriteLine(Thread.CurrentThread.Name + ": NOW FAIL: " + name);
			  (new Exception()).printStackTrace(System.out);
			}
			throw new Exception(Thread.CurrentThread.Name + ": intentionally failing at " + name);
		  }
		}
	  }

	  public virtual void TestRandomExceptions()
	  {
		if (VERBOSE)
		{
		  Console.WriteLine("\nTEST: start testRandomExceptions");
		}
		Directory dir = newDirectory();

		MockAnalyzer analyzer = new MockAnalyzer(random());
		analyzer.EnableChecks = false; // disable workflow checking as we forcefully close() in exceptional cases.

		IndexWriter writer = RandomIndexWriter.mockIndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, analyzer).setRAMBufferSizeMB(0.1).setMergeScheduler(new ConcurrentMergeScheduler()), new TestPoint1(this));
		((ConcurrentMergeScheduler) writer.Config.MergeScheduler).setSuppressExceptions();
		//writer.setMaxBufferedDocs(10);
		if (VERBOSE)
		{
		  Console.WriteLine("TEST: initial commit");
		}
		writer.commit();

		IndexerThread thread = new IndexerThread(this, 0, writer);
		thread.Run();
		if (thread.Failure != null)
		{
		  thread.Failure.printStackTrace(System.out);
		  Assert.Fail("thread " + thread.Name + ": hit unexpected failure");
		}

		if (VERBOSE)
		{
		  Console.WriteLine("TEST: commit after thread start");
		}
		writer.commit();

		try
		{
		  writer.close();
		}
		catch (Exception t)
		{
		  Console.WriteLine("exception during close:");
		  t.printStackTrace(System.out);
		  writer.rollback();
		}

		// Confirm that when doc hits exception partway through tokenization, it's deleted:
		IndexReader r2 = DirectoryReader.open(dir);
		int count = r2.docFreq(new Term("content4", "aaa"));
		int count2 = r2.docFreq(new Term("content4", "ddd"));
		Assert.AreEqual(count, count2);
		r2.close();

		dir.close();
	  }

	  public virtual void TestRandomExceptionsThreads()
	  {
		Directory dir = newDirectory();
		MockAnalyzer analyzer = new MockAnalyzer(random());
		analyzer.EnableChecks = false; // disable workflow checking as we forcefully close() in exceptional cases.
		IndexWriter writer = RandomIndexWriter.mockIndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, analyzer).setRAMBufferSizeMB(0.2).setMergeScheduler(new ConcurrentMergeScheduler()), new TestPoint1(this));
		((ConcurrentMergeScheduler) writer.Config.MergeScheduler).setSuppressExceptions();
		//writer.setMaxBufferedDocs(10);
		writer.commit();

		const int NUM_THREADS = 4;

		IndexerThread[] threads = new IndexerThread[NUM_THREADS];
		for (int i = 0;i < NUM_THREADS;i++)
		{
		  threads[i] = new IndexerThread(this, i, writer);
		  threads[i].Start();
		}

		for (int i = 0;i < NUM_THREADS;i++)
		{
		  threads[i].Join();
		}

		for (int i = 0;i < NUM_THREADS;i++)
		{
		  if (threads[i].Failure != null)
		  {
			Assert.Fail("thread " + threads[i].Name + ": hit unexpected failure");
		  }
		}

		writer.commit();

		try
		{
		  writer.close();
		}
		catch (Exception t)
		{
		  Console.WriteLine("exception during close:");
		  t.printStackTrace(System.out);
		  writer.rollback();
		}

		// Confirm that when doc hits exception partway through tokenization, it's deleted:
		IndexReader r2 = DirectoryReader.open(dir);
		int count = r2.docFreq(new Term("content4", "aaa"));
		int count2 = r2.docFreq(new Term("content4", "ddd"));
		Assert.AreEqual(count, count2);
		r2.close();

		dir.close();
	  }

	  // LUCENE-1198
	  private sealed class TestPoint2 : RandomIndexWriter.TestPoint
	  {
		internal bool DoFail;

		public override void Apply(string name)
		{
		  if (DoFail && name.Equals("DocumentsWriterPerThread addDocument start"))
		  {
			throw new Exception("intentionally failing");
		  }
		}
	  }

	  private static string CRASH_FAIL_MESSAGE = "I'm experiencing problems";

	  private class CrashingFilter : TokenFilter
	  {
		  private readonly TestIndexWriterExceptions OuterInstance;

		internal string FieldName;
		internal int Count;

		public CrashingFilter(TestIndexWriterExceptions outerInstance, string fieldName, TokenStream input) : base(input)
		{
			this.OuterInstance = outerInstance;
		  this.FieldName = fieldName;
		}

		public override bool IncrementToken()
		{
		  if (this.FieldName.Equals("crash") && Count++ >= 4)
		  {
			throw new IOException(CRASH_FAIL_MESSAGE);
		  }
		  return input.IncrementToken();
		}

		public override void Reset()
		{
		  base.reset();
		  Count = 0;
		}
	  }

	  public virtual void TestExceptionDocumentsWriterInit()
	  {
		Directory dir = newDirectory();
		TestPoint2 testPoint = new TestPoint2();
		IndexWriter w = RandomIndexWriter.mockIndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())), testPoint);
		Document doc = new Document();
		doc.add(newTextField("field", "a field", Field.Store.YES));
		w.addDocument(doc);
		testPoint.DoFail = true;
		try
		{
		  w.addDocument(doc);
		  Assert.Fail("did not hit exception");
		}
		catch (Exception re)
		{
		  // expected
		}
		w.close();
		dir.close();
	  }

	  // LUCENE-1208
	  public virtual void TestExceptionJustBeforeFlush()
	  {
		Directory dir = newDirectory();
		IndexWriter w = RandomIndexWriter.mockIndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(2), new TestPoint1(this));
		Document doc = new Document();
		doc.add(newTextField("field", "a field", Field.Store.YES));
		w.addDocument(doc);

		Analyzer analyzer = new AnalyzerAnonymousInnerClassHelper(this, Analyzer.PER_FIELD_REUSE_STRATEGY);

		Document crashDoc = new Document();
		crashDoc.add(newTextField("crash", "do it on token 4", Field.Store.YES));
		try
		{
		  w.addDocument(crashDoc, analyzer);
		  Assert.Fail("did not hit expected exception");
		}
		catch (IOException ioe)
		{
		  // expected
		}
		w.addDocument(doc);
		w.close();
		dir.close();
	  }

	  private class AnalyzerAnonymousInnerClassHelper : Analyzer
	  {
		  private readonly TestIndexWriterExceptions OuterInstance;

		  public AnalyzerAnonymousInnerClassHelper(TestIndexWriterExceptions outerInstance, UnknownType PER_FIELD_REUSE_STRATEGY) : base(PER_FIELD_REUSE_STRATEGY)
		  {
			  this.OuterInstance = outerInstance;
		  }

		  public override TokenStreamComponents CreateComponents(string fieldName, Reader reader)
		  {
			MockTokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
			tokenizer.EnableChecks = false; // disable workflow checking as we forcefully close() in exceptional cases.
			return new TokenStreamComponents(tokenizer, new CrashingFilter(OuterInstance, fieldName, tokenizer));
		  }
	  }

	  private sealed class TestPoint3 : RandomIndexWriter.TestPoint
	  {
		internal bool DoFail;
		internal bool Failed;
		public override void Apply(string name)
		{
		  if (DoFail && name.Equals("startMergeInit"))
		  {
			Failed = true;
			throw new Exception("intentionally failing");
		  }
		}
	  }


	  // LUCENE-1210
	  public virtual void TestExceptionOnMergeInit()
	  {
		Directory dir = newDirectory();
		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(2).setMergePolicy(newLogMergePolicy());
		ConcurrentMergeScheduler cms = new ConcurrentMergeScheduler();
		cms.setSuppressExceptions();
		conf.MergeScheduler = cms;
		((LogMergePolicy) conf.MergePolicy).MergeFactor = 2;
		TestPoint3 testPoint = new TestPoint3();
		IndexWriter w = RandomIndexWriter.mockIndexWriter(dir, conf, testPoint);
		testPoint.DoFail = true;
		Document doc = new Document();
		doc.add(newTextField("field", "a field", Field.Store.YES));
		for (int i = 0;i < 10;i++)
		{
		  try
		  {
			w.addDocument(doc);
		  }
		  catch (Exception re)
		  {
			break;
		  }
		}

		((ConcurrentMergeScheduler) w.Config.MergeScheduler).sync();
		Assert.IsTrue(testPoint.Failed);
		w.close();
		dir.close();
	  }

	  // LUCENE-1072
	  public virtual void TestExceptionFromTokenStream()
	  {
		Directory dir = newDirectory();
		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new AnalyzerAnonymousInnerClassHelper(this));
		conf.MaxBufferedDocs = Math.Max(3, conf.MaxBufferedDocs);

		IndexWriter writer = new IndexWriter(dir, conf);

		Document doc = new Document();
		string contents = "aa bb cc dd ee ff gg hh ii jj kk";
		doc.add(newTextField("content", contents, Field.Store.NO));
		try
		{
		  writer.addDocument(doc);
		  Assert.Fail("did not hit expected exception");
		}
		catch (Exception e)
		{
		}

		// Make sure we can add another normal document
		doc = new Document();
		doc.add(newTextField("content", "aa bb cc dd", Field.Store.NO));
		writer.addDocument(doc);

		// Make sure we can add another normal document
		doc = new Document();
		doc.add(newTextField("content", "aa bb cc dd", Field.Store.NO));
		writer.addDocument(doc);

		writer.close();
		IndexReader reader = DirectoryReader.open(dir);
		Term t = new Term("content", "aa");
		Assert.AreEqual(3, reader.docFreq(t));

		// Make sure the doc that hit the exception was marked
		// as deleted:
		DocsEnum tdocs = TestUtil.docs(random(), reader, t.field(), new BytesRef(t.text()), MultiFields.getLiveDocs(reader), null, 0);

		int count = 0;
		while (tdocs.nextDoc() != DocIdSetIterator.NO_MORE_DOCS)
		{
		  count++;
		}
		Assert.AreEqual(2, count);

		Assert.AreEqual(reader.docFreq(new Term("content", "gg")), 0);
		reader.close();
		dir.close();
	  }

	  private class AnalyzerAnonymousInnerClassHelper : Analyzer
	  {
		  private readonly TestIndexWriterExceptions OuterInstance;

		  public AnalyzerAnonymousInnerClassHelper(TestIndexWriterExceptions outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }


		  public override TokenStreamComponents CreateComponents(string fieldName, Reader reader)
		  {
			MockTokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.SIMPLE, true);
			tokenizer.EnableChecks = false; // disable workflow checking as we forcefully close() in exceptional cases.
			return new TokenStreamComponents(tokenizer, new TokenFilterAnonymousInnerClassHelper(this, tokenizer));
		  }

		  private class TokenFilterAnonymousInnerClassHelper : TokenFilter
		  {
			  private readonly AnalyzerAnonymousInnerClassHelper OuterInstance;

			  public TokenFilterAnonymousInnerClassHelper(AnalyzerAnonymousInnerClassHelper outerInstance, MockTokenizer tokenizer) : base(tokenizer)
			  {
				  this.outerInstance = outerInstance;
				  count = 0;
			  }

			  private int count;

			  public override bool IncrementToken()
			  {
				if (count++ == 5)
				{
				  throw new IOException();
				}
				return input.IncrementToken();
			  }

			  public override void Reset()
			  {
				base.reset();
				this.count = 0;
			  }
		  }

	  }

	  private class FailOnlyOnFlush : MockDirectoryWrapper.Failure
	  {
		internal bool DoFail = false;
		internal int Count;

		public override void SetDoFail()
		{
		  this.DoFail = true;
		}
		public override void ClearDoFail()
		{
		  this.DoFail = false;
		}

		public override void Eval(MockDirectoryWrapper dir)
		{
		  if (DoFail)
		  {
			StackTraceElement[] trace = (new Exception()).StackTrace;
			bool sawAppend = false;
			bool sawFlush = false;
			for (int i = 0; i < trace.Length; i++)
			{
			  if (sawAppend && sawFlush)
			  {
				break;
			  }
			  if (typeof(FreqProxTermsWriterPerField).Name.Equals(trace[i].ClassName) && "flush".Equals(trace[i].MethodName))
			  {
				sawAppend = true;
			  }
			  if ("flush".Equals(trace[i].MethodName))
			  {
				sawFlush = true;
			  }
			}

			if (sawAppend && sawFlush && Count++ >= 30)
			{
			  DoFail = false;
			  throw new IOException("now failing during flush");
			}
		  }
		}
	  }

	  // LUCENE-1072: make sure an errant exception on flushing
	  // one segment only takes out those docs in that one flush
	  public virtual void TestDocumentsWriterAbort()
	  {
		MockDirectoryWrapper dir = newMockDirectory();
		FailOnlyOnFlush failure = new FailOnlyOnFlush();
		failure.SetDoFail();
		dir.failOn(failure);

		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(2));
		Document doc = new Document();
		string contents = "aa bb cc dd ee ff gg hh ii jj kk";
		doc.add(newTextField("content", contents, Field.Store.NO));
		bool hitError = false;
		for (int i = 0;i < 200;i++)
		{
		  try
		  {
			writer.addDocument(doc);
		  }
		  catch (IOException ioe)
		  {
			// only one flush should fail:
			Assert.IsFalse(hitError);
			hitError = true;
		  }
		}
		Assert.IsTrue(hitError);
		writer.close();
		IndexReader reader = DirectoryReader.open(dir);
		Assert.AreEqual(198, reader.docFreq(new Term("content", "aa")));
		reader.close();
		dir.close();
	  }

	  public virtual void TestDocumentsWriterExceptions()
	  {
		Analyzer analyzer = new AnalyzerAnonymousInnerClassHelper(this, Analyzer.PER_FIELD_REUSE_STRATEGY);

		for (int i = 0;i < 2;i++)
		{
		  if (VERBOSE)
		  {
			Console.WriteLine("TEST: cycle i=" + i);
		  }
		  Directory dir = newDirectory();
		  IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, analyzer).setMergePolicy(newLogMergePolicy()));

		  // don't allow a sudden merge to clean up the deleted
		  // doc below:
		  LogMergePolicy lmp = (LogMergePolicy) writer.Config.MergePolicy;
		  lmp.MergeFactor = Math.Max(lmp.MergeFactor, 5);

		  Document doc = new Document();
		  doc.add(newField("contents", "here are some contents", DocCopyIterator.Custom5));
		  writer.addDocument(doc);
		  writer.addDocument(doc);
		  doc.add(newField("crash", "this should crash after 4 terms", DocCopyIterator.Custom5));
		  doc.add(newField("other", "this will not get indexed", DocCopyIterator.Custom5));
		  try
		  {
			writer.addDocument(doc);
			Assert.Fail("did not hit expected exception");
		  }
		  catch (IOException ioe)
		  {
			if (VERBOSE)
			{
			  Console.WriteLine("TEST: hit expected exception");
			  ioe.printStackTrace(System.out);
			}
		  }

		  if (0 == i)
		  {
			doc = new Document();
			doc.add(newField("contents", "here are some contents", DocCopyIterator.Custom5));
			writer.addDocument(doc);
			writer.addDocument(doc);
		  }
		  writer.close();

		  if (VERBOSE)
		  {
			Console.WriteLine("TEST: open reader");
		  }
		  IndexReader reader = DirectoryReader.open(dir);
		  if (i == 0)
		  {
			int expected = 5;
			Assert.AreEqual(expected, reader.docFreq(new Term("contents", "here")));
			Assert.AreEqual(expected, reader.maxDoc());
			int numDel = 0;
			Bits liveDocs = MultiFields.getLiveDocs(reader);
			Assert.IsNotNull(liveDocs);
			for (int j = 0;j < reader.maxDoc();j++)
			{
			  if (!liveDocs.get(j))
			  {
				numDel++;
			  }
			  else
			  {
				reader.document(j);
				reader.getTermVectors(j);
			  }
			}
			Assert.AreEqual(1, numDel);
		  }
		  reader.close();

		  writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, analyzer).setMaxBufferedDocs(10));
		  doc = new Document();
		  doc.add(newField("contents", "here are some contents", DocCopyIterator.Custom5));
		  for (int j = 0;j < 17;j++)
		  {
			writer.addDocument(doc);
		  }
		  writer.forceMerge(1);
		  writer.close();

		  reader = DirectoryReader.open(dir);
		  int expected = 19 + (1 - i) * 2;
		  Assert.AreEqual(expected, reader.docFreq(new Term("contents", "here")));
		  Assert.AreEqual(expected, reader.maxDoc());
		  int numDel = 0;
		  assertNull(MultiFields.getLiveDocs(reader));
		  for (int j = 0;j < reader.maxDoc();j++)
		  {
			reader.document(j);
			reader.getTermVectors(j);
		  }
		  reader.close();
		  Assert.AreEqual(0, numDel);

		  dir.close();
		}
	  }

	  private class AnalyzerAnonymousInnerClassHelper : Analyzer
	  {
		  private readonly TestIndexWriterExceptions OuterInstance;

		  public AnalyzerAnonymousInnerClassHelper(TestIndexWriterExceptions outerInstance, UnknownType PER_FIELD_REUSE_STRATEGY) : base(PER_FIELD_REUSE_STRATEGY)
		  {
			  this.OuterInstance = outerInstance;
		  }

		  public override TokenStreamComponents CreateComponents(string fieldName, Reader reader)
		  {
			MockTokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
			tokenizer.EnableChecks = false; // disable workflow checking as we forcefully close() in exceptional cases.
			return new TokenStreamComponents(tokenizer, new CrashingFilter(OuterInstance, fieldName, tokenizer));
		  }
	  }

	  public virtual void TestDocumentsWriterExceptionThreads()
	  {
		Analyzer analyzer = new AnalyzerAnonymousInnerClassHelper2(this, Analyzer.PER_FIELD_REUSE_STRATEGY);

		const int NUM_THREAD = 3;
		const int NUM_ITER = 100;

		for (int i = 0;i < 2;i++)
		{
		  Directory dir = newDirectory();

		  {
			IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, analyzer).setMaxBufferedDocs(-1).setMergePolicy(random().nextBoolean() ? NoMergePolicy.COMPOUND_FILES : NoMergePolicy.NO_COMPOUND_FILES));
			// don't use a merge policy here they depend on the DWPThreadPool and its max thread states etc.
			int finalI = i;

			Thread[] threads = new Thread[NUM_THREAD];
			for (int t = 0;t < NUM_THREAD;t++)
			{
			  threads[t] = new ThreadAnonymousInnerClassHelper(this, NUM_ITER, writer, finalI, t);
			  threads[t].Start();
			}

			for (int t = 0;t < NUM_THREAD;t++)
			{
			  threads[t].Join();
			}

			writer.close();
		  }

		  IndexReader reader = DirectoryReader.open(dir);
		  int expected = (3 + (1 - i) * 2) * NUM_THREAD * NUM_ITER;
		  Assert.AreEqual("i=" + i, expected, reader.docFreq(new Term("contents", "here")));
		  Assert.AreEqual(expected, reader.maxDoc());
		  int numDel = 0;
		  Bits liveDocs = MultiFields.getLiveDocs(reader);
		  Assert.IsNotNull(liveDocs);
		  for (int j = 0;j < reader.maxDoc();j++)
		  {
			if (!liveDocs.get(j))
			{
			  numDel++;
			}
			else
			{
			  reader.document(j);
			  reader.getTermVectors(j);
			}
		  }
		  reader.close();

		  Assert.AreEqual(NUM_THREAD * NUM_ITER, numDel);

		  IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, analyzer).setMaxBufferedDocs(10));
		  Document doc = new Document();
		  doc.add(newField("contents", "here are some contents", DocCopyIterator.Custom5));
		  for (int j = 0;j < 17;j++)
		  {
			writer.addDocument(doc);
		  }
		  writer.forceMerge(1);
		  writer.close();

		  reader = DirectoryReader.open(dir);
		  expected += 17 - NUM_THREAD * NUM_ITER;
		  Assert.AreEqual(expected, reader.docFreq(new Term("contents", "here")));
		  Assert.AreEqual(expected, reader.maxDoc());
		  assertNull(MultiFields.getLiveDocs(reader));
		  for (int j = 0;j < reader.maxDoc();j++)
		  {
			reader.document(j);
			reader.getTermVectors(j);
		  }
		  reader.close();

		  dir.close();
		}
	  }

	  private class AnalyzerAnonymousInnerClassHelper2 : Analyzer
	  {
		  private readonly TestIndexWriterExceptions OuterInstance;

		  public AnalyzerAnonymousInnerClassHelper2(TestIndexWriterExceptions outerInstance, UnknownType PER_FIELD_REUSE_STRATEGY) : base(PER_FIELD_REUSE_STRATEGY)
		  {
			  this.OuterInstance = outerInstance;
		  }

		  public override TokenStreamComponents CreateComponents(string fieldName, Reader reader)
		  {
			MockTokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
			tokenizer.EnableChecks = false; // disable workflow checking as we forcefully close() in exceptional cases.
			return new TokenStreamComponents(tokenizer, new CrashingFilter(OuterInstance, fieldName, tokenizer));
		  }
	  }

	  private class ThreadAnonymousInnerClassHelper : System.Threading.Thread
	  {
		  private readonly TestIndexWriterExceptions OuterInstance;

		  private int NUM_ITER;
		  private IndexWriter Writer;
		  private int FinalI;
		  private int t;

		  public ThreadAnonymousInnerClassHelper(TestIndexWriterExceptions outerInstance, int NUM_ITER, IndexWriter writer, int finalI, int t)
		  {
			  this.OuterInstance = outerInstance;
			  this.NUM_ITER = NUM_ITER;
			  this.Writer = writer;
			  this.FinalI = finalI;
			  this.t = t;
		  }

		  public override void Run()
		  {
			try
			{
			  for (int iter = 0;iter < NUM_ITER;iter++)
			  {
				Document doc = new Document();
				doc.add(newField("contents", "here are some contents", DocCopyIterator.Custom5));
				Writer.addDocument(doc);
				Writer.addDocument(doc);
				doc.add(newField("crash", "this should crash after 4 terms", DocCopyIterator.Custom5));
				doc.add(newField("other", "this will not get indexed", DocCopyIterator.Custom5));
				try
				{
				  Writer.addDocument(doc);
				  Assert.Fail("did not hit expected exception");
				}
				catch (IOException ioe)
				{
				}

				if (0 == FinalI)
				{
				  doc = new Document();
				  doc.add(newField("contents", "here are some contents", DocCopyIterator.Custom5));
				  Writer.addDocument(doc);
				  Writer.addDocument(doc);
				}
			  }
			}
			catch (Exception t)
			{
			  lock (this)
			  {
				Console.WriteLine(Thread.CurrentThread.Name + ": ERROR: hit unexpected exception");
				t.printStackTrace(System.out);
			  }
			  Assert.Fail();
			}
		  }
	  }

	  // Throws IOException during MockDirectoryWrapper.sync
	  private class FailOnlyInSync : MockDirectoryWrapper.Failure
	  {
		internal bool DidFail;
		public override void Eval(MockDirectoryWrapper dir)
		{
		  if (OuterInstance.DoFail)
		  {
			StackTraceElement[] trace = (new Exception()).StackTrace;
			for (int i = 0; i < trace.Length; i++)
			{
			  if (OuterInstance.DoFail && typeof(MockDirectoryWrapper).Name.Equals(trace[i].ClassName) && "sync".Equals(trace[i].MethodName))
			  {
				DidFail = true;
				if (VERBOSE)
				{
				  Console.WriteLine("TEST: now throw exc:");
				  (new Exception()).printStackTrace(System.out);
				}
				throw new IOException("now failing on purpose during sync");
			  }
			}
		  }
		}
	  }

	  // TODO: these are also in TestIndexWriter... add a simple doc-writing method
	  // like this to LuceneTestCase?
	  private void AddDoc(IndexWriter writer)
	  {
		  Document doc = new Document();
		  doc.add(newTextField("content", "aaa", Field.Store.NO));
		  writer.addDocument(doc);
	  }

	  // LUCENE-1044: test exception during sync
	  public virtual void TestExceptionDuringSync()
	  {
		MockDirectoryWrapper dir = newMockDirectory();
		FailOnlyInSync failure = new FailOnlyInSync();
		dir.failOn(failure);

		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(2).setMergeScheduler(new ConcurrentMergeScheduler()).setMergePolicy(newLogMergePolicy(5)));
		failure.setDoFail();

		for (int i = 0; i < 23; i++)
		{
		  AddDoc(writer);
		  if ((i - 1) % 2 == 0)
		  {
			try
			{
			  writer.commit();
			}
			catch (IOException ioe)
			{
			  // expected
			}
		  }
		}
		((ConcurrentMergeScheduler) writer.Config.MergeScheduler).sync();
		Assert.IsTrue(failure.DidFail);
		failure.clearDoFail();
		writer.close();

		IndexReader reader = DirectoryReader.open(dir);
		Assert.AreEqual(23, reader.numDocs());
		reader.close();
		dir.close();
	  }

	  private class FailOnlyInCommit : MockDirectoryWrapper.Failure
	  {

		internal bool FailOnCommit, FailOnDeleteFile;
		internal readonly bool DontFailDuringGlobalFieldMap;
		internal const string PREPARE_STAGE = "prepareCommit";
		internal const string FINISH_STAGE = "finishCommit";
		internal readonly string Stage;

		public FailOnlyInCommit(bool dontFailDuringGlobalFieldMap, string stage)
		{
		  this.DontFailDuringGlobalFieldMap = dontFailDuringGlobalFieldMap;
		  this.Stage = stage;
		}

		public override void Eval(MockDirectoryWrapper dir)
		{
		  StackTraceElement[] trace = (new Exception()).StackTrace;
		  bool isCommit = false;
		  bool isDelete = false;
		  bool isInGlobalFieldMap = false;
		  for (int i = 0; i < trace.Length; i++)
		  {
			if (isCommit && isDelete && isInGlobalFieldMap)
			{
			  break;
			}
			if (typeof(SegmentInfos).Name.Equals(trace[i].ClassName) && Stage.Equals(trace[i].MethodName))
			{
			  isCommit = true;
			}
			if (typeof(MockDirectoryWrapper).Name.Equals(trace[i].ClassName) && "deleteFile".Equals(trace[i].MethodName))
			{
			  isDelete = true;
			}
			if (typeof(SegmentInfos).Name.Equals(trace[i].ClassName) && "writeGlobalFieldMap".Equals(trace[i].MethodName))
			{
			  isInGlobalFieldMap = true;
			}

		  }
		  if (isInGlobalFieldMap && DontFailDuringGlobalFieldMap)
		  {
			isCommit = false;
		  }
		  if (isCommit)
		  {
			if (!isDelete)
			{
			  FailOnCommit = true;
			  throw new Exception("now fail first");
			}
			else
			{
			  FailOnDeleteFile = true;
			  throw new IOException("now fail during delete");
			}
		  }
		}
	  }

	  public virtual void TestExceptionsDuringCommit()
	  {
		FailOnlyInCommit[] failures = new FailOnlyInCommit[] {new FailOnlyInCommit(false, FailOnlyInCommit.PREPARE_STAGE), new FailOnlyInCommit(true, FailOnlyInCommit.PREPARE_STAGE), new FailOnlyInCommit(false, FailOnlyInCommit.FINISH_STAGE)};

		foreach (FailOnlyInCommit failure in failures)
		{
		  MockDirectoryWrapper dir = newMockDirectory();
		  dir.FailOnCreateOutput = false;
		  IndexWriter w = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		  Document doc = new Document();
		  doc.add(newTextField("field", "a field", Field.Store.YES));
		  w.addDocument(doc);
		  dir.failOn(failure);
		  try
		  {
			w.close();
			Assert.Fail();
		  }
		  catch (IOException ioe)
		  {
			Assert.Fail("expected only RuntimeException");
		  }
		  catch (Exception re)
		  {
			// Expected
		  }
		  Assert.IsTrue(failure.FailOnCommit && failure.FailOnDeleteFile);
		  w.rollback();
		  string[] files = dir.listAll();
		  Assert.IsTrue(files.Length == 0 || Arrays.Equals(files, new string[] {IndexWriter.WRITE_LOCK_NAME}));
		  dir.close();
		}
	  }

	  public virtual void TestForceMergeExceptions()
	  {
		Directory startDir = newDirectory();
		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(2).setMergePolicy(newLogMergePolicy());
		((LogMergePolicy) conf.MergePolicy).MergeFactor = 100;
		IndexWriter w = new IndexWriter(startDir, conf);
		for (int i = 0;i < 27;i++)
		{
		  AddDoc(w);
		}
		w.close();

		int iter = TEST_NIGHTLY ? 200 : 10;
		for (int i = 0;i < iter;i++)
		{
		  if (VERBOSE)
		  {
			Console.WriteLine("TEST: iter " + i);
		  }
		  MockDirectoryWrapper dir = new MockDirectoryWrapper(random(), new RAMDirectory(startDir, newIOContext(random())));
		  conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMergeScheduler(new ConcurrentMergeScheduler());
		  ((ConcurrentMergeScheduler) conf.MergeScheduler).setSuppressExceptions();
		  w = new IndexWriter(dir, conf);
		  dir.RandomIOExceptionRate = 0.5;
		  try
		  {
			w.forceMerge(1);
		  }
		  catch (IOException ioe)
		  {
			if (ioe.InnerException == null)
			{
			  Assert.Fail("forceMerge threw IOException without root cause");
			}
		  }
		  dir.RandomIOExceptionRate = 0;
		  w.close();
		  dir.close();
		}
		startDir.close();
	  }

	  // LUCENE-1429
	  public virtual void TestOutOfMemoryErrorCausesCloseToFail()
	  {

		AtomicBoolean thrown = new AtomicBoolean(false);
		Directory dir = newDirectory();
		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setInfoStream(new InfoStreamAnonymousInnerClassHelper(this, thrown)));

		try
		{
		  writer.close();
		  Assert.Fail("OutOfMemoryError expected");
		}
//JAVA TO C# CONVERTER WARNING: 'final' catch parameters are not allowed in C#:
//ORIGINAL LINE: catch (final OutOfMemoryError expected)
		catch (System.OutOfMemoryException expected)
		{
		}

		// throws IllegalStateEx w/o bug fix
		writer.close();
		dir.close();
	  }

	  private class InfoStreamAnonymousInnerClassHelper : InfoStream
	  {
		  private readonly TestIndexWriterExceptions OuterInstance;

		  private AtomicBoolean Thrown;

		  public InfoStreamAnonymousInnerClassHelper(TestIndexWriterExceptions outerInstance, AtomicBoolean thrown)
		  {
			  this.OuterInstance = outerInstance;
			  this.Thrown = thrown;
		  }

		  public override void Message(string component, string message)
		  {
			if (message.StartsWith("now flush at close") && Thrown.compareAndSet(false, true))
			{
			  throw new System.OutOfMemoryException("fake OOME at " + message);
			}
		  }

		  public override bool IsEnabled(string component)
		  {
			return true;
		  }

		  public override void Close()
		  {
		  }
	  }

	  // LUCENE-1347
	  private sealed class TestPoint4 : RandomIndexWriter.TestPoint
	  {

		internal bool DoFail;

		public override void Apply(string name)
		{
		  if (DoFail && name.Equals("rollback before checkpoint"))
		  {
			throw new Exception("intentionally failing");
		  }
		}
	  }

	  // LUCENE-1347
	  public virtual void TestRollbackExceptionHang()
	  {
		Directory dir = newDirectory();
		TestPoint4 testPoint = new TestPoint4();
		IndexWriter w = RandomIndexWriter.mockIndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())), testPoint);


		AddDoc(w);
		testPoint.DoFail = true;
		try
		{
		  w.rollback();
		  Assert.Fail("did not hit intentional RuntimeException");
		}
		catch (Exception re)
		{
		  // expected
		}

		testPoint.DoFail = false;
		w.rollback();
		dir.close();
	  }

	  // LUCENE-1044: Simulate checksum error in segments_N
	  public virtual void TestSegmentsChecksumError()
	  {
		Directory dir = newDirectory();

		IndexWriter writer = null;

		writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));

		// add 100 documents
		for (int i = 0; i < 100; i++)
		{
		  AddDoc(writer);
		}

		// close
		writer.close();

		long gen = SegmentInfos.getLastCommitGeneration(dir);
		Assert.IsTrue("segment generation should be > 0 but got " + gen, gen > 0);

		string segmentsFileName = SegmentInfos.getLastCommitSegmentsFileName(dir);
		IndexInput @in = dir.openInput(segmentsFileName, newIOContext(random()));
		IndexOutput @out = dir.createOutput(IndexFileNames.fileNameFromGeneration(IndexFileNames.SEGMENTS, "", 1 + gen), newIOContext(random()));
		@out.copyBytes(@in, @in.length() - 1);
		sbyte b = @in.readByte();
		@out.writeByte((sbyte)(1 + b));
		@out.close();
		@in.close();

		IndexReader reader = null;
		try
		{
		  reader = DirectoryReader.open(dir);
		}
		catch (IOException e)
		{
		  e.printStackTrace(System.out);
		  Assert.Fail("segmentInfos failed to retry fallback to correct segments_N file");
		}
		reader.close();

		// should remove the corrumpted segments_N
		(new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, null))).close();
		dir.close();
	  }

	  // Simulate a corrupt index by removing last byte of
	  // latest segments file and make sure we get an
	  // IOException trying to open the index:
	  public virtual void TestSimulatedCorruptIndex1()
	  {
		  BaseDirectoryWrapper dir = newDirectory();
		  dir.CheckIndexOnClose = false; // we are corrupting it!

		  IndexWriter writer = null;

		  writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));

		  // add 100 documents
		  for (int i = 0; i < 100; i++)
		  {
			  AddDoc(writer);
		  }

		  // close
		  writer.close();

		  long gen = SegmentInfos.getLastCommitGeneration(dir);
		  Assert.IsTrue("segment generation should be > 0 but got " + gen, gen > 0);

		  string fileNameIn = SegmentInfos.getLastCommitSegmentsFileName(dir);
		  string fileNameOut = IndexFileNames.fileNameFromGeneration(IndexFileNames.SEGMENTS, "", 1 + gen);
		  IndexInput @in = dir.openInput(fileNameIn, newIOContext(random()));
		  IndexOutput @out = dir.createOutput(fileNameOut, newIOContext(random()));
		  long length = @in.length();
		  for (int i = 0;i < length - 1;i++)
		  {
			@out.writeByte(@in.readByte());
		  }
		  @in.close();
		  @out.close();
		  dir.deleteFile(fileNameIn);

		  IndexReader reader = null;
		  try
		  {
			reader = DirectoryReader.open(dir);
			Assert.Fail("reader did not hit IOException on opening a corrupt index");
		  }
		  catch (Exception e)
		  {
		  }
		  if (reader != null)
		  {
			reader.close();
		  }
		  dir.close();
	  }

	  // Simulate a corrupt index by removing one of the cfs
	  // files and make sure we get an IOException trying to
	  // open the index:
	  public virtual void TestSimulatedCorruptIndex2()
	  {
		BaseDirectoryWrapper dir = newDirectory();
		dir.CheckIndexOnClose = false; // we are corrupting it!
		IndexWriter writer = null;

		writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMergePolicy(newLogMergePolicy(true)).setUseCompoundFile(true));
		MergePolicy lmp = writer.Config.MergePolicy;
		// Force creation of CFS:
		lmp.NoCFSRatio = 1.0;
		lmp.MaxCFSSegmentSizeMB = double.PositiveInfinity;

		// add 100 documents
		for (int i = 0; i < 100; i++)
		{
		  AddDoc(writer);
		}

		// close
		writer.close();

		long gen = SegmentInfos.getLastCommitGeneration(dir);
		Assert.IsTrue("segment generation should be > 0 but got " + gen, gen > 0);

		string[] files = dir.listAll();
		bool corrupted = false;
		for (int i = 0;i < files.Length;i++)
		{
		  if (files[i].EndsWith(".cfs"))
		  {
			dir.deleteFile(files[i]);
			corrupted = true;
			break;
		  }
		}
		Assert.IsTrue("failed to find cfs file to remove", corrupted);

		IndexReader reader = null;
		try
		{
		  reader = DirectoryReader.open(dir);
		  Assert.Fail("reader did not hit IOException on opening a corrupt index");
		}
		catch (Exception e)
		{
		}
		if (reader != null)
		{
		  reader.close();
		}
		dir.close();
	  }

	  // Simulate a writer that crashed while writing segments
	  // file: make sure we can still open the index (ie,
	  // gracefully fallback to the previous segments file),
	  // and that we can add to the index:
	  public virtual void TestSimulatedCrashedWriter()
	  {
		  Directory dir = newDirectory();
		  if (dir is MockDirectoryWrapper)
		  {
			((MockDirectoryWrapper)dir).PreventDoubleWrite = false;
		  }

		  IndexWriter writer = null;

		  writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));

		  // add 100 documents
		  for (int i = 0; i < 100; i++)
		  {
			  AddDoc(writer);
		  }

		  // close
		  writer.close();

		  long gen = SegmentInfos.getLastCommitGeneration(dir);
		  Assert.IsTrue("segment generation should be > 0 but got " + gen, gen > 0);

		  // Make the next segments file, with last byte
		  // missing, to simulate a writer that crashed while
		  // writing segments file:
		  string fileNameIn = SegmentInfos.getLastCommitSegmentsFileName(dir);
		  string fileNameOut = IndexFileNames.fileNameFromGeneration(IndexFileNames.SEGMENTS, "", 1 + gen);
		  IndexInput @in = dir.openInput(fileNameIn, newIOContext(random()));
		  IndexOutput @out = dir.createOutput(fileNameOut, newIOContext(random()));
		  long length = @in.length();
		  for (int i = 0;i < length - 1;i++)
		  {
			@out.writeByte(@in.readByte());
		  }
		  @in.close();
		  @out.close();

		  IndexReader reader = null;
		  try
		  {
			reader = DirectoryReader.open(dir);
		  }
		  catch (Exception e)
		  {
			Assert.Fail("reader failed to open on a crashed index");
		  }
		  reader.close();

		  try
		  {
			writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.CREATE));
		  }
		  catch (Exception e)
		  {
			e.printStackTrace(System.out);
			Assert.Fail("writer failed to open on a crashed index");
		  }

		  // add 100 documents
		  for (int i = 0; i < 100; i++)
		  {
			  AddDoc(writer);
		  }

		  // close
		  writer.close();
		  dir.close();
	  }

	  public virtual void TestTermVectorExceptions()
	  {
		FailOnTermVectors[] failures = new FailOnTermVectors[] {new FailOnTermVectors(FailOnTermVectors.AFTER_INIT_STAGE), new FailOnTermVectors(FailOnTermVectors.INIT_STAGE)};
		int num = atLeast(1);
		for (int j = 0; j < num; j++)
		{
		  foreach (FailOnTermVectors failure in failures)
		  {
			MockDirectoryWrapper dir = newMockDirectory();
			IndexWriter w = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
			dir.failOn(failure);
			int numDocs = 10 + random().Next(30);
			for (int i = 0; i < numDocs; i++)
			{
			  Document doc = new Document();
			  Field field = newTextField(random(), "field", "a field", Field.Store.YES);
			  doc.add(field);
			  // random TV
			  try
			  {
				w.addDocument(doc);
				Assert.IsFalse(field.fieldType().storeTermVectors());
			  }
			  catch (Exception e)
			  {
				Assert.IsTrue(e.Message.StartsWith(FailOnTermVectors.EXC_MSG));
			  }
			  if (random().Next(20) == 0)
			  {
				w.commit();
				TestUtil.checkIndex(dir);
			  }

			}
			Document document = new Document();
			document.add(new TextField("field", "a field", Field.Store.YES));
			w.addDocument(document);

			for (int i = 0; i < numDocs; i++)
			{
			  Document doc = new Document();
			  Field field = newTextField(random(), "field", "a field", Field.Store.YES);
			  doc.add(field);
			  // random TV
			  try
			  {
				w.addDocument(doc);
				Assert.IsFalse(field.fieldType().storeTermVectors());
			  }
			  catch (Exception e)
			  {
				Assert.IsTrue(e.Message.StartsWith(FailOnTermVectors.EXC_MSG));
			  }
			  if (random().Next(20) == 0)
			  {
				w.commit();
				TestUtil.checkIndex(dir);
			  }
			}
			document = new Document();
			document.add(new TextField("field", "a field", Field.Store.YES));
			w.addDocument(document);
			w.close();
			IndexReader reader = DirectoryReader.open(dir);
			Assert.IsTrue(reader.numDocs() > 0);
			SegmentInfos sis = new SegmentInfos();
			sis.read(dir);
			foreach (AtomicReaderContext context in reader.leaves())
			{
			  Assert.IsFalse(context.reader().FieldInfos.hasVectors());
			}
			reader.close();
			dir.close();
		  }
		}
	  }

	  private class FailOnTermVectors : MockDirectoryWrapper.Failure
	  {

		internal const string INIT_STAGE = "initTermVectorsWriter";
		internal const string AFTER_INIT_STAGE = "finishDocument";
		internal const string EXC_MSG = "FOTV";
		internal readonly string Stage;

		public FailOnTermVectors(string stage)
		{
		  this.Stage = stage;
		}

		public override void Eval(MockDirectoryWrapper dir)
		{

		  StackTraceElement[] trace = (new Exception()).StackTrace;
		  bool fail = false;
		  for (int i = 0; i < trace.Length; i++)
		  {
			if (typeof(TermVectorsConsumer).Name.Equals(trace[i].ClassName) && Stage.Equals(trace[i].MethodName))
			{
			  fail = true;
			  break;
			}
		  }

		  if (fail)
		  {
			throw new Exception(EXC_MSG);
		  }
		}
	  }

	  public virtual void TestAddDocsNonAbortingException()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter w = new RandomIndexWriter(random(), dir);
		int numDocs1 = random().Next(25);
		for (int docCount = 0;docCount < numDocs1;docCount++)
		{
		  Document doc = new Document();
		  doc.add(newTextField("content", "good content", Field.Store.NO));
		  w.addDocument(doc);
		}

		IList<Document> docs = new List<Document>();
		for (int docCount = 0;docCount < 7;docCount++)
		{
		  Document doc = new Document();
		  docs.Add(doc);
		  doc.add(newStringField("id", docCount + "", Field.Store.NO));
		  doc.add(newTextField("content", "silly content " + docCount, Field.Store.NO));
		  if (docCount == 4)
		  {
			Field f = newTextField("crash", "", Field.Store.NO);
			doc.add(f);
			MockTokenizer tokenizer = new MockTokenizer(new StringReader("crash me on the 4th token"), MockTokenizer.WHITESPACE, false);
			tokenizer.EnableChecks = false; // disable workflow checking as we forcefully close() in exceptional cases.
			f.TokenStream = new CrashingFilter(this, "crash", tokenizer);
		  }
		}
		try
		{
		  w.addDocuments(docs);
		  // BUG: CrashingFilter didn't
		  Assert.Fail("did not hit expected exception");
		}
		catch (IOException ioe)
		{
		  // expected
		  Assert.AreEqual(CRASH_FAIL_MESSAGE, ioe.Message);
		}

		int numDocs2 = random().Next(25);
		for (int docCount = 0;docCount < numDocs2;docCount++)
		{
		  Document doc = new Document();
		  doc.add(newTextField("content", "good content", Field.Store.NO));
		  w.addDocument(doc);
		}

		IndexReader r = w.Reader;
		w.close();

		IndexSearcher s = newSearcher(r);
		PhraseQuery pq = new PhraseQuery();
		pq.add(new Term("content", "silly"));
		pq.add(new Term("content", "content"));
		Assert.AreEqual(0, s.search(pq, 1).totalHits);

		pq = new PhraseQuery();
		pq.add(new Term("content", "good"));
		pq.add(new Term("content", "content"));
		Assert.AreEqual(numDocs1 + numDocs2, s.search(pq, 1).totalHits);
		r.close();
		dir.close();
	  }


	  public virtual void TestUpdateDocsNonAbortingException()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter w = new RandomIndexWriter(random(), dir);
		int numDocs1 = random().Next(25);
		for (int docCount = 0;docCount < numDocs1;docCount++)
		{
		  Document doc = new Document();
		  doc.add(newTextField("content", "good content", Field.Store.NO));
		  w.addDocument(doc);
		}

		// Use addDocs (no exception) to get docs in the index:
		IList<Document> docs = new List<Document>();
		int numDocs2 = random().Next(25);
		for (int docCount = 0;docCount < numDocs2;docCount++)
		{
		  Document doc = new Document();
		  docs.Add(doc);
		  doc.add(newStringField("subid", "subs", Field.Store.NO));
		  doc.add(newStringField("id", docCount + "", Field.Store.NO));
		  doc.add(newTextField("content", "silly content " + docCount, Field.Store.NO));
		}
		w.addDocuments(docs);

		int numDocs3 = random().Next(25);
		for (int docCount = 0;docCount < numDocs3;docCount++)
		{
		  Document doc = new Document();
		  doc.add(newTextField("content", "good content", Field.Store.NO));
		  w.addDocument(doc);
		}

		docs.Clear();
		int limit = TestUtil.Next(random(), 2, 25);
		int crashAt = random().Next(limit);
		for (int docCount = 0;docCount < limit;docCount++)
		{
		  Document doc = new Document();
		  docs.Add(doc);
		  doc.add(newStringField("id", docCount + "", Field.Store.NO));
		  doc.add(newTextField("content", "silly content " + docCount, Field.Store.NO));
		  if (docCount == crashAt)
		  {
			Field f = newTextField("crash", "", Field.Store.NO);
			doc.add(f);
			MockTokenizer tokenizer = new MockTokenizer(new StringReader("crash me on the 4th token"), MockTokenizer.WHITESPACE, false);
			tokenizer.EnableChecks = false; // disable workflow checking as we forcefully close() in exceptional cases.
			f.TokenStream = new CrashingFilter(this, "crash", tokenizer);
		  }
		}

		try
		{
		  w.updateDocuments(new Term("subid", "subs"), docs);
		  // BUG: CrashingFilter didn't
		  Assert.Fail("did not hit expected exception");
		}
		catch (IOException ioe)
		{
		  // expected
		  Assert.AreEqual(CRASH_FAIL_MESSAGE, ioe.Message);
		}

		int numDocs4 = random().Next(25);
		for (int docCount = 0;docCount < numDocs4;docCount++)
		{
		  Document doc = new Document();
		  doc.add(newTextField("content", "good content", Field.Store.NO));
		  w.addDocument(doc);
		}

		IndexReader r = w.Reader;
		w.close();

		IndexSearcher s = newSearcher(r);
		PhraseQuery pq = new PhraseQuery();
		pq.add(new Term("content", "silly"));
		pq.add(new Term("content", "content"));
		Assert.AreEqual(numDocs2, s.search(pq, 1).totalHits);

		pq = new PhraseQuery();
		pq.add(new Term("content", "good"));
		pq.add(new Term("content", "content"));
		Assert.AreEqual(numDocs1 + numDocs3 + numDocs4, s.search(pq, 1).totalHits);
		r.close();
		dir.close();
	  }

	  internal class UOEDirectory : RAMDirectory
	  {
		internal bool DoFail = false;

		public override IndexInput OpenInput(string name, IOContext context)
		{
		  if (DoFail && name.StartsWith("segments_"))
		  {
			StackTraceElement[] trace = (new Exception()).StackTrace;
			for (int i = 0; i < trace.Length; i++)
			{
			  if ("read".Equals(trace[i].MethodName))
			  {
				throw new System.NotSupportedException("expected UOE");
			  }
			}
		  }
		  return base.openInput(name, context);
		}
	  }

	  public virtual void TestExceptionOnCtor()
	  {
		UOEDirectory uoe = new UOEDirectory();
		Directory d = new MockDirectoryWrapper(random(), uoe);
		IndexWriter iw = new IndexWriter(d, newIndexWriterConfig(TEST_VERSION_CURRENT, null));
		iw.addDocument(new Document());
		iw.close();
		uoe.DoFail = true;
		try
		{
		  new IndexWriter(d, newIndexWriterConfig(TEST_VERSION_CURRENT, null));
		  Assert.Fail("should have gotten a UOE");
		}
		catch (System.NotSupportedException expected)
		{
		}

		uoe.DoFail = false;
		d.close();
	  }

	  public virtual void TestIllegalPositions()
	  {
		Directory dir = newDirectory();
		IndexWriter iw = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, null));
		Document doc = new Document();
		Token t1 = new Token("foo", 0, 3);
		t1.PositionIncrement = int.MaxValue;
		Token t2 = new Token("bar", 4, 7);
		t2.PositionIncrement = 200;
		TokenStream overflowingTokenStream = new CannedTokenStream(new Token[] {t1, t2});
		Field field = new TextField("foo", overflowingTokenStream);
		doc.add(field);
		try
		{
		  iw.addDocument(doc);
		  Assert.Fail();
		}
		catch (System.ArgumentException expected)
		{
		  // expected exception
		}
		iw.close();
		dir.close();
	  }

	  public virtual void TestLegalbutVeryLargePositions()
	  {
		Directory dir = newDirectory();
		IndexWriter iw = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, null));
		Document doc = new Document();
		Token t1 = new Token("foo", 0, 3);
		t1.PositionIncrement = int.MaxValue-500;
		if (random().nextBoolean())
		{
		  t1.Payload = new BytesRef(new sbyte[] {0x1});
		}
		TokenStream overflowingTokenStream = new CannedTokenStream(new Token[] {t1});
		Field field = new TextField("foo", overflowingTokenStream);
		doc.add(field);
		iw.addDocument(doc);
		iw.close();
		dir.close();
	  }

	  public virtual void TestBoostOmitNorms()
	  {
		Directory dir = newDirectory();
		IndexWriterConfig iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		iwc.MergePolicy = newLogMergePolicy();
		IndexWriter iw = new IndexWriter(dir, iwc);
		Document doc = new Document();
		doc.add(new StringField("field1", "sometext", Field.Store.YES));
		doc.add(new TextField("field2", "sometext", Field.Store.NO));
		doc.add(new StringField("foo", "bar", Field.Store.NO));
		iw.addDocument(doc); // add an 'ok' document
		try
		{
		  doc = new Document();
		  // try to boost with norms omitted
		  IList<IndexableField> list = new List<IndexableField>();
		  list.Add(new IndexableFieldAnonymousInnerClassHelper(this));
		  iw.addDocument(list);
		  Assert.Fail("didn't get any exception, boost silently discarded");
		}
		catch (System.NotSupportedException expected)
		{
		  // expected
		}
		DirectoryReader ir = DirectoryReader.open(iw, false);
		Assert.AreEqual(1, ir.numDocs());
		Assert.AreEqual("sometext", ir.document(0).get("field1"));
		ir.close();
		iw.close();
		dir.close();
	  }

	  private class IndexableFieldAnonymousInnerClassHelper : IndexableField
	  {
		  private readonly TestIndexWriterExceptions OuterInstance;

		  public IndexableFieldAnonymousInnerClassHelper(TestIndexWriterExceptions outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }


		  public override string Name()
		  {
			return "foo";
		  }

		  public override IndexableFieldType FieldType()
		  {
			return StringField.TYPE_NOT_STORED;
		  }

		  public override float Boost()
		  {
			return 5f;
		  }

		  public override BytesRef BinaryValue()
		  {
			return null;
		  }

		  public override string StringValue()
		  {
			return "baz";
		  }

		  public override Reader ReaderValue()
		  {
			return null;
		  }

		  public override Number NumericValue()
		  {
			return null;
		  }

		  public override TokenStream TokenStream(Analyzer analyzer)
		  {
			return null;
		  }
	  }

	  // See LUCENE-4870 TooManyOpenFiles errors are thrown as
	  // FNFExceptions which can trigger data loss.
	  public virtual void TestTooManyFileException()
	  {

		// Create failure that throws Too many open files exception randomly
		MockDirectoryWrapper.Failure failure = new FailureAnonymousInnerClassHelper(this);

		MockDirectoryWrapper dir = newMockDirectory();
		// The exception is only thrown on open input
		dir.FailOnOpenInput = true;
		dir.failOn(failure);

		// Create an index with one document
		IndexWriterConfig iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		IndexWriter iw = new IndexWriter(dir, iwc);
		Document doc = new Document();
		doc.add(new StringField("foo", "bar", Field.Store.NO));
		iw.addDocument(doc); // add a document
		iw.commit();
		DirectoryReader ir = DirectoryReader.open(dir);
		Assert.AreEqual(1, ir.numDocs());
		ir.close();
		iw.close();

		// Open and close the index a few times
		for (int i = 0; i < 10; i++)
		{
		  failure.setDoFail();
		  iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		  try
		  {
			iw = new IndexWriter(dir, iwc);
		  }
		  catch (CorruptIndexException ex)
		  {
			// Exceptions are fine - we are running out of file handlers here
			continue;
		  }
//JAVA TO C# CONVERTER TODO TASK: There is no equivalent in C# to Java 'multi-catch' syntax:
		  catch (FileNotFoundException | NoSuchFileException ex)
		  {
			continue;
		  }
		  failure.clearDoFail();
		  iw.close();
		  ir = DirectoryReader.open(dir);
		  Assert.AreEqual("lost document after iteration: " + i, 1, ir.numDocs());
		  ir.close();
		}

		// Check if document is still there
		failure.clearDoFail();
		ir = DirectoryReader.open(dir);
		Assert.AreEqual(1, ir.numDocs());
		ir.close();

		dir.close();
	  }

	  private class FailureAnonymousInnerClassHelper : MockDirectoryWrapper.Failure
	  {
		  private readonly TestIndexWriterExceptions OuterInstance;

		  public FailureAnonymousInnerClassHelper(TestIndexWriterExceptions outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }


		  public override MockDirectoryWrapper.Failure Reset()
		  {
			OuterInstance.DoFail = false;
			return this;
		  }

		  public override void Eval(MockDirectoryWrapper dir)
		  {
			if (OuterInstance.DoFail)
			{
			  if (random().nextBoolean())
			  {
				throw new FileNotFoundException("some/file/name.ext (Too many open files)");
			  }
			}
		  }
	  }

	  // Make sure if we hit a transient IOException (e.g., disk
	  // full), and then the exception stops (e.g., disk frees
	  // up), so we successfully close IW or open an NRT
	  // reader, we don't lose any deletes or updates:
	  public virtual void TestNoLostDeletesOrUpdates()
	  {
		int deleteCount = 0;
		int docBase = 0;
		int docCount = 0;

		MockDirectoryWrapper dir = newMockDirectory();
		AtomicBoolean shouldFail = new AtomicBoolean();
		dir.failOn(new FailureAnonymousInnerClassHelper2(this, dir, shouldFail));

		RandomIndexWriter w = null;

		for (int iter = 0;iter < 10 * RANDOM_MULTIPLIER;iter++)
		{
		  int numDocs = atLeast(100);
		  if (VERBOSE)
		  {
			Console.WriteLine("\nTEST: iter=" + iter + " numDocs=" + numDocs + " docBase=" + docBase + " delCount=" + deleteCount);
		  }
		  if (w == null)
		  {
			IndexWriterConfig iwc = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
			MergeScheduler ms = iwc.MergeScheduler;
			if (ms is ConcurrentMergeScheduler)
			{
			  ConcurrentMergeScheduler suppressFakeIOE = new ConcurrentMergeSchedulerAnonymousInnerClassHelper(this);
			  ConcurrentMergeScheduler cms = (ConcurrentMergeScheduler) ms;
			  suppressFakeIOE.setMaxMergesAndThreads(cms.MaxMergeCount, cms.MaxThreadCount);
			  suppressFakeIOE.MergeThreadPriority = cms.MergeThreadPriority;
			  iwc.MergeScheduler = suppressFakeIOE;
			}

			w = new RandomIndexWriter(random(), dir, iwc);
			// Since we hit exc during merging, a partial
			// forceMerge can easily return when there are still
			// too many segments in the index:
			w.DoRandomForceMergeAssert = false;
		  }
		  for (int i = 0;i < numDocs;i++)
		  {
			Document doc = new Document();
			doc.add(new StringField("id", "" + (docBase + i), Field.Store.NO));
			if (defaultCodecSupportsDocValues())
			{
			  doc.add(new NumericDocValuesField("f", 1L));
			  doc.add(new NumericDocValuesField("cf", 2L));
			  doc.add(new BinaryDocValuesField("bf", TestBinaryDocValuesUpdates.ToBytes(1L)));
			  doc.add(new BinaryDocValuesField("bcf", TestBinaryDocValuesUpdates.ToBytes(2L)));
			}
			w.addDocument(doc);
		  }
		  docCount += numDocs;

		  // TODO: we could make the test more evil, by letting
		  // it throw more than one exc, randomly, before "recovering"

		  // TODO: we could also install an infoStream and try
		  // to fail in "more evil" places inside BDS

		  shouldFail.set(true);
		  bool doClose = false;

		  try
		  {

			bool defaultCodecSupportsFieldUpdates = defaultCodecSupportsFieldUpdates();
			for (int i = 0;i < numDocs;i++)
			{
			  if (random().Next(10) == 7)
			  {
				bool fieldUpdate = defaultCodecSupportsFieldUpdates && random().nextBoolean();
				if (fieldUpdate)
				{
				  long value = iter;
				  if (VERBOSE)
				  {
					Console.WriteLine("  update id=" + (docBase + i) + " to value " + value);
				  }
				  if (random().nextBoolean()) // update only numeric field
				  {
					w.updateNumericDocValue(new Term("id", Convert.ToString(docBase + i)), "f", value);
					w.updateNumericDocValue(new Term("id", Convert.ToString(docBase + i)), "cf", value * 2);
				  }
				  else if (random().nextBoolean())
				  {
					w.updateBinaryDocValue(new Term("id", Convert.ToString(docBase + i)), "bf", TestBinaryDocValuesUpdates.ToBytes(value));
					w.updateBinaryDocValue(new Term("id", Convert.ToString(docBase + i)), "bcf", TestBinaryDocValuesUpdates.ToBytes(value * 2));
				  }
				  else
				  {
					w.updateNumericDocValue(new Term("id", Convert.ToString(docBase + i)), "f", value);
					w.updateNumericDocValue(new Term("id", Convert.ToString(docBase + i)), "cf", value * 2);
					w.updateBinaryDocValue(new Term("id", Convert.ToString(docBase + i)), "bf", TestBinaryDocValuesUpdates.ToBytes(value));
					w.updateBinaryDocValue(new Term("id", Convert.ToString(docBase + i)), "bcf", TestBinaryDocValuesUpdates.ToBytes(value * 2));
				  }
				}

				// sometimes do both deletes and updates
				if (!fieldUpdate || random().nextBoolean())
				{
				  if (VERBOSE)
				  {
					Console.WriteLine("  delete id=" + (docBase + i));
				  }
				  deleteCount++;
				  w.deleteDocuments(new Term("id", "" + (docBase + i)));
				}
			  }
			}

			// Trigger writeLiveDocs so we hit fake exc:
			IndexReader r = w.getReader(true);

			// Sometimes we will make it here (we only randomly
			// throw the exc):
			Assert.AreEqual(docCount - deleteCount, r.numDocs());
			r.close();

			// Sometimes close, so the disk full happens on close:
			if (random().nextBoolean())
			{
			  if (VERBOSE)
			  {
				Console.WriteLine("  now close writer");
			  }
			  doClose = true;
			  w.close();
			  w = null;
			}

		  }
		  catch (IOException ioe)
		  {
			// FakeIOException can be thrown from mergeMiddle, in which case IW
			// registers it before our CMS gets to suppress it. IW.forceMerge later
			// throws it as a wrapped IOE, so don't fail in this case.
			if (ioe is MockDirectoryWrapper.FakeIOException || (ioe.InnerException != null && ioe.InnerException is MockDirectoryWrapper.FakeIOException))
			{
			  // expected
			  if (VERBOSE)
			  {
				Console.WriteLine("TEST: w.close() hit expected IOE");
			  }
			}
			else
			{
			  throw ioe;
			}
		  }
		  shouldFail.set(false);

		  IndexReader r;

		  if (doClose && w != null)
		  {
			if (VERBOSE)
			{
			  Console.WriteLine("  now 2nd close writer");
			}
			w.close();
			w = null;
		  }

		  if (w == null || random().nextBoolean())
		  {
			// Open non-NRT reader, to make sure the "on
			// disk" bits are good:
			if (VERBOSE)
			{
			  Console.WriteLine("TEST: verify against non-NRT reader");
			}
			if (w != null)
			{
			  w.commit();
			}
			r = DirectoryReader.open(dir);
		  }
		  else
		  {
			if (VERBOSE)
			{
			  Console.WriteLine("TEST: verify against NRT reader");
			}
			r = w.Reader;
		  }
		  Assert.AreEqual(docCount - deleteCount, r.numDocs());
		  if (defaultCodecSupportsDocValues())
		  {
			BytesRef scratch = new BytesRef();
			foreach (AtomicReaderContext context in r.leaves())
			{
			  AtomicReader reader = context.reader();
			  Bits liveDocs = reader.LiveDocs;
			  NumericDocValues f = reader.getNumericDocValues("f");
			  NumericDocValues cf = reader.getNumericDocValues("cf");
			  BinaryDocValues bf = reader.getBinaryDocValues("bf");
			  BinaryDocValues bcf = reader.getBinaryDocValues("bcf");
			  for (int i = 0; i < reader.maxDoc(); i++)
			  {
				if (liveDocs == null || liveDocs.get(i))
				{
				  Assert.AreEqual("doc=" + (docBase + i), cf.get(i), f.get(i) * 2);
				  Assert.AreEqual("doc=" + (docBase + i), TestBinaryDocValuesUpdates.GetValue(bcf, i, scratch), TestBinaryDocValuesUpdates.GetValue(bf, i, scratch) * 2);
				}
			  }
			}
		  }

		  r.close();

		  // Sometimes re-use RIW, other times open new one:
		  if (w != null && random().nextBoolean())
		  {
			if (VERBOSE)
			{
			  Console.WriteLine("TEST: close writer");
			}
			w.close();
			w = null;
		  }

		  docBase += numDocs;
		}

		if (w != null)
		{
		  w.close();
		}

		// Final verify:
		IndexReader r = DirectoryReader.open(dir);
		Assert.AreEqual(docCount - deleteCount, r.numDocs());
		r.close();

		dir.close();
	  }

	  private class FailureAnonymousInnerClassHelper2 : MockDirectoryWrapper.Failure
	  {
		  private readonly TestIndexWriterExceptions OuterInstance;

		  private MockDirectoryWrapper Dir;
		  private AtomicBoolean ShouldFail;

		  public FailureAnonymousInnerClassHelper2(TestIndexWriterExceptions outerInstance, MockDirectoryWrapper dir, AtomicBoolean shouldFail)
		  {
			  this.OuterInstance = outerInstance;
			  this.Dir = dir;
			  this.ShouldFail = shouldFail;
		  }


		  public override void Eval(MockDirectoryWrapper dir)
		  {
			StackTraceElement[] trace = (new Exception()).StackTrace;
			if (ShouldFail.get() == false)
			{
			  return;
			}

			bool sawSeal = false;
			bool sawWrite = false;
			for (int i = 0; i < trace.Length; i++)
			{
			  if ("sealFlushedSegment".Equals(trace[i].MethodName))
			  {
				sawSeal = true;
				break;
			  }
			  if ("writeLiveDocs".Equals(trace[i].MethodName) || "writeFieldUpdates".Equals(trace[i].MethodName))
			  {
				sawWrite = true;
			  }
			}

			// Don't throw exc if we are "flushing", else
			// the segment is aborted and docs are lost:
			if (sawWrite && sawSeal == false && random().Next(3) == 2)
			{
			  // Only sometimes throw the exc, so we get
			  // it sometimes on creating the file, on
			  // flushing buffer, on closing the file:
			  if (VERBOSE)
			  {
				Console.WriteLine("TEST: now fail; thread=" + Thread.CurrentThread.Name + " exc:");
				(new Exception()).printStackTrace(System.out);
			  }
			  ShouldFail.set(false);
			  throw new MockDirectoryWrapper.FakeIOException();
			}
		  }
	  }

	  private class ConcurrentMergeSchedulerAnonymousInnerClassHelper : ConcurrentMergeScheduler
	  {
		  private readonly TestIndexWriterExceptions OuterInstance;

		  public ConcurrentMergeSchedulerAnonymousInnerClassHelper(TestIndexWriterExceptions outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }

		  protected internal override void HandleMergeException(Exception exc)
		  {
			// suppress only FakeIOException:
			if (!(exc is MockDirectoryWrapper.FakeIOException))
			{
			  base.handleMergeException(exc);
			}
		  }
	  }

	  public virtual void TestExceptionDuringRollback()
	  {
		// currently: fail in two different places
		string messageToFailOn = random().nextBoolean() ? "rollback: done finish merges" : "rollback before checkpoint";

		// infostream that throws exception during rollback
		InfoStream evilInfoStream = new InfoStreamAnonymousInnerClassHelper(this, messageToFailOn);

		Directory dir = newMockDirectory(); // we want to ensure we don't leak any locks or file handles
		IndexWriterConfig iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, null);
		iwc.InfoStream = evilInfoStream;
		IndexWriter iw = new IndexWriter(dir, iwc);
		Document doc = new Document();
		for (int i = 0; i < 10; i++)
		{
		  iw.addDocument(doc);
		}
		iw.commit();

		iw.addDocument(doc);

		// pool readers
		DirectoryReader r = DirectoryReader.open(iw, false);

		// sometimes sneak in a pending commit: we don't want to leak a file handle to that segments_N
		if (random().nextBoolean())
		{
		  iw.prepareCommit();
		}

		try
		{
		  iw.rollback();
		  Assert.Fail();
		}
		catch (Exception expected)
		{
		  Assert.AreEqual("BOOM!", expected.Message);
		}

		r.close();

		// even though we hit exception: we are closed, no locks or files held, index in good state
		Assert.IsTrue(iw.Closed);
		Assert.IsFalse(IndexWriter.isLocked(dir));

		r = DirectoryReader.open(dir);
		Assert.AreEqual(10, r.maxDoc());
		r.close();

		// no leaks
		dir.close();
	  }

	  private class InfoStreamAnonymousInnerClassHelper : InfoStream
	  {
		  private readonly TestIndexWriterExceptions OuterInstance;

		  private string MessageToFailOn;

		  public InfoStreamAnonymousInnerClassHelper(TestIndexWriterExceptions outerInstance, string messageToFailOn)
		  {
			  this.OuterInstance = outerInstance;
			  this.MessageToFailOn = messageToFailOn;
		  }

		  public override void Message(string component, string message)
		  {
			if (MessageToFailOn.Equals(message))
			{
			  throw new Exception("BOOM!");
			}
		  }

		  public override bool IsEnabled(string component)
		  {
			return true;
		  }

		  public override void Close()
		  {
		  }
	  }

	  public virtual void TestRandomExceptionDuringRollback()
	  {
		// fail in random places on i/o
		int numIters = RANDOM_MULTIPLIER * 75;
		for (int iter = 0; iter < numIters; iter++)
		{
		  MockDirectoryWrapper dir = newMockDirectory();
		  dir.failOn(new FailureAnonymousInnerClassHelper3(this, dir));

		  IndexWriterConfig iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, null);
		  IndexWriter iw = new IndexWriter(dir, iwc);
		  Document doc = new Document();
		  for (int i = 0; i < 10; i++)
		  {
			iw.addDocument(doc);
		  }
		  iw.commit();

		  iw.addDocument(doc);

		  // pool readers
		  DirectoryReader r = DirectoryReader.open(iw, false);

		  // sometimes sneak in a pending commit: we don't want to leak a file handle to that segments_N
		  if (random().nextBoolean())
		  {
			iw.prepareCommit();
		  }

		  try
		  {
			iw.rollback();
		  }
		  catch (MockDirectoryWrapper.FakeIOException expected)
		  {
		  }

		  r.close();

		  // even though we hit exception: we are closed, no locks or files held, index in good state
		  Assert.IsTrue(iw.Closed);
		  Assert.IsFalse(IndexWriter.isLocked(dir));

		  r = DirectoryReader.open(dir);
		  Assert.AreEqual(10, r.maxDoc());
		  r.close();

		  // no leaks
		  dir.close();
		}
	  }

	  private class FailureAnonymousInnerClassHelper3 : MockDirectoryWrapper.Failure
	  {
		  private readonly TestIndexWriterExceptions OuterInstance;

		  private MockDirectoryWrapper Dir;

		  public FailureAnonymousInnerClassHelper3(TestIndexWriterExceptions outerInstance, MockDirectoryWrapper dir)
		  {
			  this.OuterInstance = outerInstance;
			  this.Dir = dir;
		  }


		  public override void Eval(MockDirectoryWrapper dir)
		  {
			bool maybeFail = false;
			StackTraceElement[] trace = (new Exception()).StackTrace;

			for (int i = 0; i < trace.Length; i++)
			{
			  if ("rollbackInternal".Equals(trace[i].MethodName))
			  {
				maybeFail = true;
				break;
			  }
			}

			if (maybeFail && random().Next(10) == 0)
			{
			  if (VERBOSE)
			  {
				Console.WriteLine("TEST: now fail; thread=" + Thread.CurrentThread.Name + " exc:");
				(new Exception()).printStackTrace(System.out);
			  }
			  throw new MockDirectoryWrapper.FakeIOException();
			}
		  }
	  }
	}

}