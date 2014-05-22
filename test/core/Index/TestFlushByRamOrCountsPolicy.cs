using System;
using System.Collections.Generic;

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
	using ThreadState = Lucene.Net.Index.DocumentsWriterPerThreadPool.ThreadState;
	using Directory = Lucene.Net.Store.Directory;
	using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
	using LineFileDocs = Lucene.Net.Util.LineFileDocs;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;
	using AfterClass = org.junit.AfterClass;
	using BeforeClass = org.junit.BeforeClass;

	public class TestFlushByRamOrCountsPolicy : LuceneTestCase
	{

	  private static LineFileDocs LineDocFile;

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @BeforeClass public static void beforeClass() throws Exception
	  public static void BeforeClass()
	  {
		LineDocFile = new LineFileDocs(random(), defaultCodecSupportsDocValues());
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @AfterClass public static void afterClass() throws Exception
	  public static void AfterClass()
	  {
		LineDocFile.close();
		LineDocFile = null;
	  }

	  public virtual void TestFlushByRam()
	  {
		double ramBuffer = (TEST_NIGHTLY ? 1 : 10) + atLeast(2) + random().NextDouble();
		RunFlushByRam(1 + random().Next(TEST_NIGHTLY ? 5 : 1), ramBuffer, false);
	  }

	  public virtual void TestFlushByRamLargeBuffer()
	  {
		// with a 256 mb ram buffer we should never stall
		RunFlushByRam(1 + random().Next(TEST_NIGHTLY ? 5 : 1), 256.d, true);
	  }

	  protected internal virtual void RunFlushByRam(int numThreads, double maxRamMB, bool ensureNotStalled)
	  {
		int numDocumentsToIndex = 10 + atLeast(30);
		AtomicInteger numDocs = new AtomicInteger(numDocumentsToIndex);
		Directory dir = newDirectory();
		MockDefaultFlushPolicy flushPolicy = new MockDefaultFlushPolicy();
		MockAnalyzer analyzer = new MockAnalyzer(random());
		analyzer.MaxTokenLength = TestUtil.Next(random(), 1, IndexWriter.MAX_TERM_LENGTH);

		IndexWriterConfig iwc = newIndexWriterConfig(TEST_VERSION_CURRENT, analyzer).setFlushPolicy(flushPolicy);
		int numDWPT = 1 + atLeast(2);
		DocumentsWriterPerThreadPool threadPool = new ThreadAffinityDocumentsWriterThreadPool(numDWPT);
		iwc.IndexerThreadPool = threadPool;
		iwc.RAMBufferSizeMB = maxRamMB;
		iwc.MaxBufferedDocs = IndexWriterConfig.DISABLE_AUTO_FLUSH;
		iwc.MaxBufferedDeleteTerms = IndexWriterConfig.DISABLE_AUTO_FLUSH;
		IndexWriter writer = new IndexWriter(dir, iwc);
		flushPolicy = (MockDefaultFlushPolicy) writer.Config.FlushPolicy;
		Assert.IsFalse(flushPolicy.flushOnDocCount());
		Assert.IsFalse(flushPolicy.flushOnDeleteTerms());
		Assert.IsTrue(flushPolicy.flushOnRAM());
		DocumentsWriter docsWriter = writer.DocsWriter;
		Assert.IsNotNull(docsWriter);
		DocumentsWriterFlushControl flushControl = docsWriter.flushControl;
		Assert.AreEqual(" bytes must be 0 after init", 0, flushControl.flushBytes());

		IndexThread[] threads = new IndexThread[numThreads];
		for (int x = 0; x < threads.Length; x++)
		{
		  threads[x] = new IndexThread(this, numDocs, numThreads, writer, LineDocFile, false);
		  threads[x].Start();
		}

		for (int x = 0; x < threads.Length; x++)
		{
		  threads[x].Join();
		}
		long maxRAMBytes = (long)(iwc.RAMBufferSizeMB * 1024.0 * 1024.0);
		Assert.AreEqual(" all flushes must be due numThreads=" + numThreads, 0, flushControl.flushBytes());
		Assert.AreEqual(numDocumentsToIndex, writer.numDocs());
		Assert.AreEqual(numDocumentsToIndex, writer.maxDoc());
		Assert.IsTrue("peak bytes without flush exceeded watermark", flushPolicy.PeakBytesWithoutFlush <= maxRAMBytes);
		AssertActiveBytesAfter(flushControl);
		if (flushPolicy.HasMarkedPending)
		{
		  Assert.IsTrue(maxRAMBytes < flushControl.peakActiveBytes);
		}
		if (ensureNotStalled)
		{
		  Assert.IsFalse(docsWriter.flushControl.stallControl.wasStalled());
		}
		writer.close();
		Assert.AreEqual(0, flushControl.activeBytes());
		dir.close();
	  }

	  public virtual void TestFlushDocCount()
	  {
		int[] numThreads = new int[] {2 + atLeast(1), 1};
		for (int i = 0; i < numThreads.Length; i++)
		{

		  int numDocumentsToIndex = 50 + atLeast(30);
		  AtomicInteger numDocs = new AtomicInteger(numDocumentsToIndex);
		  Directory dir = newDirectory();
		  MockDefaultFlushPolicy flushPolicy = new MockDefaultFlushPolicy();
		  IndexWriterConfig iwc = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setFlushPolicy(flushPolicy);

		  int numDWPT = 1 + atLeast(2);
		  DocumentsWriterPerThreadPool threadPool = new ThreadAffinityDocumentsWriterThreadPool(numDWPT);
		  iwc.IndexerThreadPool = threadPool;
		  iwc.MaxBufferedDocs = 2 + atLeast(10);
		  iwc.RAMBufferSizeMB = IndexWriterConfig.DISABLE_AUTO_FLUSH;
		  iwc.MaxBufferedDeleteTerms = IndexWriterConfig.DISABLE_AUTO_FLUSH;
		  IndexWriter writer = new IndexWriter(dir, iwc);
		  flushPolicy = (MockDefaultFlushPolicy) writer.Config.FlushPolicy;
		  Assert.IsTrue(flushPolicy.flushOnDocCount());
		  Assert.IsFalse(flushPolicy.flushOnDeleteTerms());
		  Assert.IsFalse(flushPolicy.flushOnRAM());
		  DocumentsWriter docsWriter = writer.DocsWriter;
		  Assert.IsNotNull(docsWriter);
		  DocumentsWriterFlushControl flushControl = docsWriter.flushControl;
		  Assert.AreEqual(" bytes must be 0 after init", 0, flushControl.flushBytes());

		  IndexThread[] threads = new IndexThread[numThreads[i]];
		  for (int x = 0; x < threads.Length; x++)
		  {
			threads[x] = new IndexThread(this, numDocs, numThreads[i], writer, LineDocFile, false);
			threads[x].Start();
		  }

		  for (int x = 0; x < threads.Length; x++)
		  {
			threads[x].Join();
		  }

		  Assert.AreEqual(" all flushes must be due numThreads=" + numThreads[i], 0, flushControl.flushBytes());
		  Assert.AreEqual(numDocumentsToIndex, writer.numDocs());
		  Assert.AreEqual(numDocumentsToIndex, writer.maxDoc());
		  Assert.IsTrue("peak bytes without flush exceeded watermark", flushPolicy.PeakDocCountWithoutFlush <= iwc.MaxBufferedDocs);
		  AssertActiveBytesAfter(flushControl);
		  writer.close();
		  Assert.AreEqual(0, flushControl.activeBytes());
		  dir.close();
		}
	  }

	  public virtual void TestRandom()
	  {
		int numThreads = 1 + random().Next(8);
		int numDocumentsToIndex = 50 + atLeast(70);
		AtomicInteger numDocs = new AtomicInteger(numDocumentsToIndex);
		Directory dir = newDirectory();
		IndexWriterConfig iwc = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		MockDefaultFlushPolicy flushPolicy = new MockDefaultFlushPolicy();
		iwc.FlushPolicy = flushPolicy;

		int numDWPT = 1 + random().Next(8);
		DocumentsWriterPerThreadPool threadPool = new ThreadAffinityDocumentsWriterThreadPool(numDWPT);
		iwc.IndexerThreadPool = threadPool;

		IndexWriter writer = new IndexWriter(dir, iwc);
		flushPolicy = (MockDefaultFlushPolicy) writer.Config.FlushPolicy;
		DocumentsWriter docsWriter = writer.DocsWriter;
		Assert.IsNotNull(docsWriter);
		DocumentsWriterFlushControl flushControl = docsWriter.flushControl;

		Assert.AreEqual(" bytes must be 0 after init", 0, flushControl.flushBytes());

		IndexThread[] threads = new IndexThread[numThreads];
		for (int x = 0; x < threads.Length; x++)
		{
		  threads[x] = new IndexThread(this, numDocs, numThreads, writer, LineDocFile, true);
		  threads[x].Start();
		}

		for (int x = 0; x < threads.Length; x++)
		{
		  threads[x].Join();
		}
		Assert.AreEqual(" all flushes must be due", 0, flushControl.flushBytes());
		Assert.AreEqual(numDocumentsToIndex, writer.numDocs());
		Assert.AreEqual(numDocumentsToIndex, writer.maxDoc());
		if (flushPolicy.flushOnRAM() && !flushPolicy.flushOnDocCount() && !flushPolicy.flushOnDeleteTerms())
		{
		  long maxRAMBytes = (long)(iwc.RAMBufferSizeMB * 1024.0 * 1024.0);
		  Assert.IsTrue("peak bytes without flush exceeded watermark", flushPolicy.PeakBytesWithoutFlush <= maxRAMBytes);
		  if (flushPolicy.HasMarkedPending)
		  {
			Assert.IsTrue("max: " + maxRAMBytes + " " + flushControl.peakActiveBytes, maxRAMBytes <= flushControl.peakActiveBytes);
		  }
		}
		AssertActiveBytesAfter(flushControl);
		writer.commit();
		Assert.AreEqual(0, flushControl.activeBytes());
		IndexReader r = DirectoryReader.open(dir);
		Assert.AreEqual(numDocumentsToIndex, r.numDocs());
		Assert.AreEqual(numDocumentsToIndex, r.maxDoc());
		if (!flushPolicy.flushOnRAM())
		{
		  Assert.IsFalse("never stall if we don't flush on RAM", docsWriter.flushControl.stallControl.wasStalled());
		  Assert.IsFalse("never block if we don't flush on RAM", docsWriter.flushControl.stallControl.hasBlocked());
		}
		r.close();
		writer.close();
		dir.close();
	  }

	  public virtual void TestStallControl()
	  {

		int[] numThreads = new int[] {4 + random().Next(8), 1};
		int numDocumentsToIndex = 50 + random().Next(50);
		for (int i = 0; i < numThreads.Length; i++)
		{
		  AtomicInteger numDocs = new AtomicInteger(numDocumentsToIndex);
		  MockDirectoryWrapper dir = newMockDirectory();
		  // mock a very slow harddisk sometimes here so that flushing is very slow
		  dir.Throttling = MockDirectoryWrapper.Throttling.SOMETIMES;
		  IndexWriterConfig iwc = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		  iwc.MaxBufferedDocs = IndexWriterConfig.DISABLE_AUTO_FLUSH;
		  iwc.MaxBufferedDeleteTerms = IndexWriterConfig.DISABLE_AUTO_FLUSH;
		  FlushPolicy flushPolicy = new FlushByRamOrCountsPolicy();
		  iwc.FlushPolicy = flushPolicy;

		  DocumentsWriterPerThreadPool threadPool = new ThreadAffinityDocumentsWriterThreadPool(numThreads[i] == 1 ? 1 : 2);
		  iwc.IndexerThreadPool = threadPool;
		  // with such a small ram buffer we should be stalled quiet quickly
		  iwc.RAMBufferSizeMB = 0.25;
		  IndexWriter writer = new IndexWriter(dir, iwc);
		  IndexThread[] threads = new IndexThread[numThreads[i]];
		  for (int x = 0; x < threads.Length; x++)
		  {
			threads[x] = new IndexThread(this, numDocs, numThreads[i], writer, LineDocFile, false);
			threads[x].Start();
		  }

		  for (int x = 0; x < threads.Length; x++)
		  {
			threads[x].Join();
		  }
		  DocumentsWriter docsWriter = writer.DocsWriter;
		  Assert.IsNotNull(docsWriter);
		  DocumentsWriterFlushControl flushControl = docsWriter.flushControl;
		  Assert.AreEqual(" all flushes must be due", 0, flushControl.flushBytes());
		  Assert.AreEqual(numDocumentsToIndex, writer.numDocs());
		  Assert.AreEqual(numDocumentsToIndex, writer.maxDoc());
		  if (numThreads[i] == 1)
		  {
			Assert.IsFalse("single thread must not block numThreads: " + numThreads[i], docsWriter.flushControl.stallControl.hasBlocked());
		  }
		  if (docsWriter.flushControl.peakNetBytes > (2.d * iwc.RAMBufferSizeMB * 1024.d * 1024.d))
		  {
			Assert.IsTrue(docsWriter.flushControl.stallControl.wasStalled());
		  }
		  AssertActiveBytesAfter(flushControl);
		  writer.close(true);
		  dir.close();
		}
	  }

	  protected internal virtual void AssertActiveBytesAfter(DocumentsWriterFlushControl flushControl)
	  {
		IEnumerator<ThreadState> allActiveThreads = flushControl.allActiveThreadStates();
		long bytesUsed = 0;
		while (allActiveThreads.MoveNext())
		{
		  ThreadState next = allActiveThreads.Current;
		  if (next.dwpt != null)
		  {
			bytesUsed += next.dwpt.bytesUsed();
		  }
		}
		Assert.AreEqual(bytesUsed, flushControl.activeBytes());
	  }

	  public class IndexThread : System.Threading.Thread
	  {
		  private readonly TestFlushByRamOrCountsPolicy OuterInstance;

		internal IndexWriter Writer;
		internal LiveIndexWriterConfig Iwc;
		internal LineFileDocs Docs;
		internal AtomicInteger PendingDocs;
		internal readonly bool DoRandomCommit;

		public IndexThread(TestFlushByRamOrCountsPolicy outerInstance, AtomicInteger pendingDocs, int numThreads, IndexWriter writer, LineFileDocs docs, bool doRandomCommit)
		{
			this.OuterInstance = outerInstance;
		  this.PendingDocs = pendingDocs;
		  this.Writer = writer;
		  Iwc = writer.Config;
		  this.Docs = docs;
		  this.DoRandomCommit = doRandomCommit;
		}

		public override void Run()
		{
		  try
		  {
			long ramSize = 0;
			while (PendingDocs.decrementAndGet() > -1)
			{
			  Document doc = Docs.nextDoc();
			  Writer.addDocument(doc);
			  long newRamSize = Writer.ramSizeInBytes();
			  if (newRamSize != ramSize)
			  {
				ramSize = newRamSize;
			  }
			  if (DoRandomCommit)
			  {
				if (rarely())
				{
				  Writer.commit();
				}
			  }
			}
			Writer.commit();
		  }
		  catch (Exception ex)
		  {
			Console.WriteLine("FAILED exc:");
			ex.printStackTrace(System.out);
			throw new Exception(ex);
		  }
		}
	  }

	  private class MockDefaultFlushPolicy : FlushByRamOrCountsPolicy
	  {
		internal long PeakBytesWithoutFlush = int.MinValue;
		internal long PeakDocCountWithoutFlush = int.MinValue;
		internal bool HasMarkedPending = false;

		public override void OnDelete(DocumentsWriterFlushControl control, ThreadState state)
		{
		  List<ThreadState> pending = new List<ThreadState>();
		  List<ThreadState> notPending = new List<ThreadState>();
		  FindPending(control, pending, notPending);
		  bool flushCurrent = state.flushPending;
		  ThreadState toFlush;
		  if (state.flushPending)
		  {
			toFlush = state;
		  }
		  else if (flushOnDeleteTerms() && state.dwpt.pendingUpdates.numTermDeletes.get() >= indexWriterConfig.MaxBufferedDeleteTerms)
		  {
			toFlush = state;
		  }
		  else
		  {
			toFlush = null;
		  }
		  base.onDelete(control, state);
		  if (toFlush != null)
		  {
			if (flushCurrent)
			{
			  Assert.IsTrue(pending.Remove(toFlush));
			}
			else
			{
			  Assert.IsTrue(notPending.Remove(toFlush));
			}
			Assert.IsTrue(toFlush.flushPending);
			HasMarkedPending = true;
		  }

		  foreach (ThreadState threadState in notPending)
		  {
			Assert.IsFalse(threadState.flushPending);
		  }
		}

		public override void OnInsert(DocumentsWriterFlushControl control, ThreadState state)
		{
		  List<ThreadState> pending = new List<ThreadState>();
		  List<ThreadState> notPending = new List<ThreadState>();
		  FindPending(control, pending, notPending);
		  bool flushCurrent = state.flushPending;
		  long activeBytes = control.activeBytes();
		  ThreadState toFlush;
		  if (state.flushPending)
		  {
			toFlush = state;
		  }
		  else if (flushOnDocCount() && state.dwpt.NumDocsInRAM >= indexWriterConfig.MaxBufferedDocs)
		  {
			toFlush = state;
		  }
		  else if (flushOnRAM() && activeBytes >= (long)(indexWriterConfig.RAMBufferSizeMB * 1024.0 * 1024.0))
		  {
			toFlush = findLargestNonPendingWriter(control, state);
			Assert.IsFalse(toFlush.flushPending);
		  }
		  else
		  {
			toFlush = null;
		  }
		  base.onInsert(control, state);
		  if (toFlush != null)
		  {
			if (flushCurrent)
			{
			  Assert.IsTrue(pending.Remove(toFlush));
			}
			else
			{
			  Assert.IsTrue(notPending.Remove(toFlush));
			}
			Assert.IsTrue(toFlush.flushPending);
			HasMarkedPending = true;
		  }
		  else
		  {
			PeakBytesWithoutFlush = Math.Max(activeBytes, PeakBytesWithoutFlush);
			PeakDocCountWithoutFlush = Math.Max(state.dwpt.NumDocsInRAM, PeakDocCountWithoutFlush);
		  }

		  foreach (ThreadState threadState in notPending)
		  {
			Assert.IsFalse(threadState.flushPending);
		  }
		}
	  }

	  internal static void FindPending(DocumentsWriterFlushControl flushControl, List<ThreadState> pending, List<ThreadState> notPending)
	  {
		IEnumerator<ThreadState> allActiveThreads = flushControl.allActiveThreadStates();
		while (allActiveThreads.MoveNext())
		{
		  ThreadState next = allActiveThreads.Current;
		  if (next.flushPending)
		  {
			pending.Add(next);
		  }
		  else
		  {
			notPending.Add(next);
		  }
		}
	  }
	}

}