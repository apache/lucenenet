using System;
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
	using Lucene41PostingsFormat = Lucene.Net.Codecs.Lucene41.Lucene41PostingsFormat;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using StringField = Lucene.Net.Document.StringField;
	using TextField = Lucene.Net.Document.TextField;
	using OpenMode = Lucene.Net.Index.IndexWriterConfig.OpenMode_e;
	using Directory = Lucene.Net.Store.Directory;
	using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;

	public class TestConcurrentMergeScheduler : LuceneTestCase
	{

	  private class FailOnlyOnFlush : MockDirectoryWrapper.Failure
	  {
		  private readonly TestConcurrentMergeScheduler OuterInstance;

		  public FailOnlyOnFlush(TestConcurrentMergeScheduler outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }

		internal bool DoFail;
		internal bool HitExc;

		public override void SetDoFail()
		{
		  this.DoFail = true;
		  HitExc = false;
		}
		public override void ClearDoFail()
		{
		  this.DoFail = false;
		}

		public override void Eval(MockDirectoryWrapper dir)
		{
		  if (DoFail && TestThread)
		  {
			bool isDoFlush = false;
			bool isClose = false;
			StackTraceElement[] trace = (new Exception()).StackTrace;
			for (int i = 0; i < trace.Length; i++)
			{
			  if (isDoFlush && isClose)
			  {
				break;
			  }
			  if ("flush".Equals(trace[i].MethodName))
			  {
				isDoFlush = true;
			  }
			  if ("close".Equals(trace[i].MethodName))
			  {
				isClose = true;
			  }
			}
			if (isDoFlush && !isClose && random().nextBoolean())
			{
			  HitExc = true;
			  throw new IOException(Thread.CurrentThread.Name + ": now failing during flush");
			}
		  }
		}
	  }

	  // Make sure running BG merges still work fine even when
	  // we are hitting exceptions during flushing.
	  public virtual void TestFlushExceptions()
	  {
		MockDirectoryWrapper directory = newMockDirectory();
		FailOnlyOnFlush failure = new FailOnlyOnFlush(this);
		directory.failOn(failure);

		IndexWriter writer = new IndexWriter(directory, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(2));
		Document doc = new Document();
		Field idField = newStringField("id", "", Field.Store.YES);
		doc.add(idField);
		int extraCount = 0;

		for (int i = 0;i < 10;i++)
		{
		  if (VERBOSE)
		  {
			Console.WriteLine("TEST: iter=" + i);
		  }

		  for (int j = 0;j < 20;j++)
		  {
			idField.StringValue = Convert.ToString(i * 20 + j);
			writer.addDocument(doc);
		  }

		  // must cycle here because sometimes the merge flushes
		  // the doc we just added and so there's nothing to
		  // flush, and we don't hit the exception
		  while (true)
		  {
			writer.addDocument(doc);
			failure.SetDoFail();
			try
			{
			  writer.flush(true, true);
			  if (failure.HitExc)
			  {
				Assert.Fail("failed to hit IOException");
			  }
			  extraCount++;
			}
			catch (IOException ioe)
			{
			  if (VERBOSE)
			  {
				ioe.printStackTrace(System.out);
			  }
			  failure.ClearDoFail();
			  break;
			}
		  }
		  Assert.AreEqual(20 * (i + 1) + extraCount, writer.numDocs());
		}

		writer.close();
		IndexReader reader = DirectoryReader.open(directory);
		Assert.AreEqual(200 + extraCount, reader.numDocs());
		reader.close();
		directory.close();
	  }

	  // Test that deletes committed after a merge started and
	  // before it finishes, are correctly merged back:
	  public virtual void TestDeleteMerging()
	  {
		Directory directory = newDirectory();

		LogDocMergePolicy mp = new LogDocMergePolicy();
		// Force degenerate merging so we can get a mix of
		// merging of segments with and without deletes at the
		// start:
		mp.MinMergeDocs = 1000;
		IndexWriter writer = new IndexWriter(directory, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMergePolicy(mp));

		Document doc = new Document();
		Field idField = newStringField("id", "", Field.Store.YES);
		doc.add(idField);
		for (int i = 0;i < 10;i++)
		{
		  if (VERBOSE)
		  {
			Console.WriteLine("\nTEST: cycle");
		  }
		  for (int j = 0;j < 100;j++)
		  {
			idField.StringValue = Convert.ToString(i * 100 + j);
			writer.addDocument(doc);
		  }

		  int delID = i;
		  while (delID < 100 * (1 + i))
		  {
			if (VERBOSE)
			{
			  Console.WriteLine("TEST: del " + delID);
			}
			writer.deleteDocuments(new Term("id", "" + delID));
			delID += 10;
		  }

		  writer.commit();
		}

		writer.close();
		IndexReader reader = DirectoryReader.open(directory);
		// Verify that we did not lose any deletes...
		Assert.AreEqual(450, reader.numDocs());
		reader.close();
		directory.close();
	  }

	  public virtual void TestNoExtraFiles()
	  {
		Directory directory = newDirectory();
		IndexWriter writer = new IndexWriter(directory, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(2));

		for (int iter = 0;iter < 7;iter++)
		{
		  if (VERBOSE)
		  {
			Console.WriteLine("TEST: iter=" + iter);
		  }

		  for (int j = 0;j < 21;j++)
		  {
			Document doc = new Document();
			doc.add(newTextField("content", "a b c", Field.Store.NO));
			writer.addDocument(doc);
		  }

		  writer.close();
		  TestIndexWriter.AssertNoUnreferencedFiles(directory, "testNoExtraFiles");

		  // Reopen
		  writer = new IndexWriter(directory, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.APPEND).setMaxBufferedDocs(2));
		}

		writer.close();

		directory.close();
	  }

	  public virtual void TestNoWaitClose()
	  {
		Directory directory = newDirectory();
		Document doc = new Document();
		Field idField = newStringField("id", "", Field.Store.YES);
		doc.add(idField);

		IndexWriter writer = new IndexWriter(directory, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(2).setMergePolicy(newLogMergePolicy(100)));

		for (int iter = 0;iter < 10;iter++)
		{

		  for (int j = 0;j < 201;j++)
		  {
			idField.StringValue = Convert.ToString(iter * 201 + j);
			writer.addDocument(doc);
		  }

		  int delID = iter * 201;
		  for (int j = 0;j < 20;j++)
		  {
			writer.deleteDocuments(new Term("id", Convert.ToString(delID)));
			delID += 5;
		  }

		  // Force a bunch of merge threads to kick off so we
		  // stress out aborting them on close:
		  ((LogMergePolicy) writer.Config.MergePolicy).MergeFactor = 3;
		  writer.addDocument(doc);
		  writer.commit();

		  writer.close(false);

		  IndexReader reader = DirectoryReader.open(directory);
		  Assert.AreEqual((1 + iter) * 182, reader.numDocs());
		  reader.close();

		  // Reopen
		  writer = new IndexWriter(directory, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.APPEND).setMergePolicy(newLogMergePolicy(100)));
		}
		writer.close();

		directory.close();
	  }

	  // LUCENE-4544
	  public virtual void TestMaxMergeCount()
	  {
		Directory dir = newDirectory();
		IndexWriterConfig iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));

		int maxMergeCount = TestUtil.Next(random(), 1, 5);
		int maxMergeThreads = TestUtil.Next(random(), 1, maxMergeCount);
		CountDownLatch enoughMergesWaiting = new CountDownLatch(maxMergeCount);
		AtomicInteger runningMergeCount = new AtomicInteger(0);
		AtomicBoolean failed = new AtomicBoolean();

		if (VERBOSE)
		{
		  Console.WriteLine("TEST: maxMergeCount=" + maxMergeCount + " maxMergeThreads=" + maxMergeThreads);
		}

		ConcurrentMergeScheduler cms = new ConcurrentMergeSchedulerAnonymousInnerClassHelper(this, maxMergeCount, enoughMergesWaiting, runningMergeCount, failed);
		cms.setMaxMergesAndThreads(maxMergeCount, maxMergeThreads);
		iwc.MergeScheduler = cms;
		iwc.MaxBufferedDocs = 2;

		TieredMergePolicy tmp = new TieredMergePolicy();
		iwc.MergePolicy = tmp;
		tmp.MaxMergeAtOnce = 2;
		tmp.SegmentsPerTier = 2;

		IndexWriter w = new IndexWriter(dir, iwc);
		Document doc = new Document();
		doc.add(newField("field", "field", TextField.TYPE_NOT_STORED));
		while (enoughMergesWaiting.Count != 0 && !failed.get())
		{
		  for (int i = 0;i < 10;i++)
		  {
			w.addDocument(doc);
		  }
		}
		w.close(false);
		dir.close();
	  }

	  private class ConcurrentMergeSchedulerAnonymousInnerClassHelper : ConcurrentMergeScheduler
	  {
		  private readonly TestConcurrentMergeScheduler OuterInstance;

		  private int MaxMergeCount;
		  private CountDownLatch EnoughMergesWaiting;
		  private AtomicInteger RunningMergeCount;
		  private AtomicBoolean Failed;

		  public ConcurrentMergeSchedulerAnonymousInnerClassHelper(TestConcurrentMergeScheduler outerInstance, int maxMergeCount, CountDownLatch enoughMergesWaiting, AtomicInteger runningMergeCount, AtomicBoolean failed)
		  {
			  this.OuterInstance = outerInstance;
			  this.MaxMergeCount = maxMergeCount;
			  this.EnoughMergesWaiting = enoughMergesWaiting;
			  this.RunningMergeCount = runningMergeCount;
			  this.Failed = failed;
		  }


		  protected internal override void DoMerge(MergePolicy.OneMerge merge)
		  {
			try
			{
			  // Stall all incoming merges until we see
			  // maxMergeCount:
			  int count = RunningMergeCount.incrementAndGet();
			  try
			  {
				Assert.IsTrue("count=" + count + " vs maxMergeCount=" + MaxMergeCount, count <= MaxMergeCount);
				EnoughMergesWaiting.countDown();

				// Stall this merge until we see exactly
				// maxMergeCount merges waiting
				while (true)
				{
				  if (EnoughMergesWaiting.@await(10, TimeUnit.MILLISECONDS) || Failed.get())
				  {
					break;
				  }
				}
				// Then sleep a bit to give a chance for the bug
				// (too many pending merges) to appear:
				Thread.Sleep(20);
				base.doMerge(merge);
			  }
			  finally
			  {
				RunningMergeCount.decrementAndGet();
			  }
			}
			catch (Exception t)
			{
			  Failed.set(true);
			  writer.mergeFinish(merge);
			  throw new Exception(t);
			}
		  }
	  }


	  private class TrackingCMS : ConcurrentMergeScheduler
	  {
		internal long TotMergedBytes;

		public TrackingCMS()
		{
		  setMaxMergesAndThreads(5, 5);
		}

		public override void DoMerge(MergePolicy.OneMerge merge)
		{
		  TotMergedBytes += merge.totalBytesSize();
		  base.doMerge(merge);
		}
	  }

	  public virtual void TestTotalBytesSize()
	  {
		Directory d = newDirectory();
		if (d is MockDirectoryWrapper)
		{
		  ((MockDirectoryWrapper)d).Throttling = MockDirectoryWrapper.Throttling.NEVER;
		}
		IndexWriterConfig iwc = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		iwc.MaxBufferedDocs = 5;
		iwc.MergeScheduler = new TrackingCMS();
		if (TestUtil.getPostingsFormat("id").Equals("SimpleText"))
		{
		  // no
		  iwc.Codec = TestUtil.alwaysPostingsFormat(new Lucene41PostingsFormat());
		}
		RandomIndexWriter w = new RandomIndexWriter(random(), d, iwc);
		for (int i = 0;i < 1000;i++)
		{
		  Document doc = new Document();
		  doc.add(new StringField("id", "" + i, Field.Store.NO));
		  w.addDocument(doc);

		  if (random().nextBoolean())
		  {
			w.deleteDocuments(new Term("id", "" + random().Next(i + 1)));
		  }
		}
		Assert.IsTrue(((TrackingCMS) w.w.Config.MergeScheduler).TotMergedBytes != 0);
		w.close();
		d.close();
	  }
	}

}