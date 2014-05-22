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
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using FieldType = Lucene.Net.Document.FieldType;
	using StringField = Lucene.Net.Document.StringField;
	using Directory = Lucene.Net.Store.Directory;
	using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
	using RAMDirectory = Lucene.Net.Store.RAMDirectory;
	using English = Lucene.Net.Util.English;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

	public class TestTransactions : LuceneTestCase
	{

	  private static volatile bool DoFail;

	  private class RandomFailure : MockDirectoryWrapper.Failure
	  {
		  private readonly TestTransactions OuterInstance;

		  public RandomFailure(TestTransactions outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }

		public override void Eval(MockDirectoryWrapper dir)
		{
		  if (TestTransactions.DoFail && random().Next() % 10 <= 3)
		  {
			throw new IOException("now failing randomly but on purpose");
		  }
		}
	  }

	  private abstract class TimedThread : System.Threading.Thread
	  {
		internal volatile bool Failed;
		internal static float RUN_TIME_MSEC = atLeast(500);
		internal TimedThread[] AllThreads;

		public abstract void DoWork();

		internal TimedThread(TimedThread[] threads)
		{
		  this.AllThreads = threads;
		}

		public override void Run()
		{
		  long stopTime = System.currentTimeMillis() + (long)(RUN_TIME_MSEC);

		  try
		  {
			do
			{
			  if (AnyErrors())
			  {
				  break;
			  }
			  DoWork();
			} while (System.currentTimeMillis() < stopTime);
		  }
		  catch (Exception e)
		  {
			Console.WriteLine(Thread.CurrentThread + ": exc");
			e.printStackTrace(System.out);
			Failed = true;
		  }
		}

		internal virtual bool AnyErrors()
		{
		  for (int i = 0;i < AllThreads.Length;i++)
		  {
			if (AllThreads[i] != null && AllThreads[i].Failed)
			{
			  return true;
			}
		  }
		  return false;
		}
	  }

	  private class IndexerThread : TimedThread
	  {
		  private readonly TestTransactions OuterInstance;

		internal Directory Dir1;
		internal Directory Dir2;
		internal object @lock;
		internal int NextID;

		public IndexerThread(TestTransactions outerInstance, object @lock, Directory dir1, Directory dir2, TimedThread[] threads) : base(threads)
		{
			this.OuterInstance = outerInstance;
		  this.@lock = @lock;
		  this.Dir1 = dir1;
		  this.Dir2 = dir2;
		}

		public override void DoWork()
		{

		  IndexWriter writer1 = new IndexWriter(Dir1, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(3).setMergeScheduler(new ConcurrentMergeScheduler()).setMergePolicy(newLogMergePolicy(2)));
		  ((ConcurrentMergeScheduler) writer1.Config.MergeScheduler).setSuppressExceptions();

		  // Intentionally use different params so flush/merge
		  // happen @ different times
		  IndexWriter writer2 = new IndexWriter(Dir2, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(2).setMergeScheduler(new ConcurrentMergeScheduler()).setMergePolicy(newLogMergePolicy(3)));
		  ((ConcurrentMergeScheduler) writer2.Config.MergeScheduler).setSuppressExceptions();

		  Update(writer1);
		  Update(writer2);

		  TestTransactions.DoFail = true;
		  try
		  {
			lock (@lock)
			{
			  try
			  {
				writer1.prepareCommit();
			  }
			  catch (Exception t)
			  {
				writer1.rollback();
				writer2.rollback();
				return;
			  }
			  try
			  {
				writer2.prepareCommit();
			  }
			  catch (Exception t)
			  {
				writer1.rollback();
				writer2.rollback();
				return;
			  }

			  writer1.commit();
			  writer2.commit();
			}
		  }
		  finally
		  {
			TestTransactions.DoFail = false;
		  }

		  writer1.close();
		  writer2.close();
		}

		public virtual void Update(IndexWriter writer)
		{
		  // Add 10 docs:
		  FieldType customType = new FieldType(StringField.TYPE_NOT_STORED);
		  customType.StoreTermVectors = true;
		  for (int j = 0; j < 10; j++)
		  {
			Document d = new Document();
			int n = random().Next();
			d.add(newField("id", Convert.ToString(NextID++), customType));
			d.add(newTextField("contents", English.intToEnglish(n), Field.Store.NO));
			writer.addDocument(d);
		  }

		  // Delete 5 docs:
		  int deleteID = NextID - 1;
		  for (int j = 0; j < 5; j++)
		  {
			writer.deleteDocuments(new Term("id", "" + deleteID));
			deleteID -= 2;
		  }
		}
	  }

	  private class SearcherThread : TimedThread
	  {
		internal Directory Dir1;
		internal Directory Dir2;
		internal object @lock;

		public SearcherThread(object @lock, Directory dir1, Directory dir2, TimedThread[] threads) : base(threads)
		{
		  this.@lock = @lock;
		  this.Dir1 = dir1;
		  this.Dir2 = dir2;
		}

		public override void DoWork()
		{
		  IndexReader r1 = null, r2 = null;
		  lock (@lock)
		  {
			try
			{
			  r1 = DirectoryReader.open(Dir1);
			  r2 = DirectoryReader.open(Dir2);
			}
			catch (IOException e)
			{
			  if (!e.Message.contains("on purpose"))
			  {
				throw e;
			  }
			  if (r1 != null)
			  {
				r1.close();
			  }
			  if (r2 != null)
			  {
				r2.close();
			  }
			  return;
			}
		  }
		  if (r1.numDocs() != r2.numDocs())
		  {
			throw new Exception("doc counts differ: r1=" + r1.numDocs() + " r2=" + r2.numDocs());
		  }
		  r1.close();
		  r2.close();
		}
	  }

	  public virtual void InitIndex(Directory dir)
	  {
		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		for (int j = 0; j < 7; j++)
		{
		  Document d = new Document();
		  int n = random().Next();
		  d.add(newTextField("contents", English.intToEnglish(n), Field.Store.NO));
		  writer.addDocument(d);
		}
		writer.close();
	  }

	  public virtual void TestTransactions()
	  {
		// we cant use non-ramdir on windows, because this test needs to double-write.
		MockDirectoryWrapper dir1 = new MockDirectoryWrapper(random(), new RAMDirectory());
		MockDirectoryWrapper dir2 = new MockDirectoryWrapper(random(), new RAMDirectory());
		dir1.PreventDoubleWrite = false;
		dir2.PreventDoubleWrite = false;
		dir1.failOn(new RandomFailure(this));
		dir2.failOn(new RandomFailure(this));
		dir1.FailOnOpenInput = false;
		dir2.FailOnOpenInput = false;

		// We throw exceptions in deleteFile, which creates
		// leftover files:
		dir1.AssertNoUnrefencedFilesOnClose = false;
		dir2.AssertNoUnrefencedFilesOnClose = false;

		InitIndex(dir1);
		InitIndex(dir2);

		TimedThread[] threads = new TimedThread[3];
		int numThread = 0;

		IndexerThread indexerThread = new IndexerThread(this, this, dir1, dir2, threads);
		threads[numThread++] = indexerThread;
		indexerThread.start();

		SearcherThread searcherThread1 = new SearcherThread(this, dir1, dir2, threads);
		threads[numThread++] = searcherThread1;
		searcherThread1.start();

		SearcherThread searcherThread2 = new SearcherThread(this, dir1, dir2, threads);
		threads[numThread++] = searcherThread2;
		searcherThread2.start();

		for (int i = 0;i < numThread;i++)
		{
		  threads[i].Join();
		}

		for (int i = 0;i < numThread;i++)
		{
		  Assert.IsTrue(!threads[i].Failed);
		}
		dir1.close();
		dir2.close();
	  }
	}

}