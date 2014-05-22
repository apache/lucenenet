using System;

namespace org.apache.lucene
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
	using ConcurrentMergeScheduler = Lucene.Net.Index.ConcurrentMergeScheduler;
	using IndexWriter = Lucene.Net.Index.IndexWriter;
	using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
	using LogMergePolicy = Lucene.Net.Index.LogMergePolicy;
	using MergePolicy = Lucene.Net.Index.MergePolicy;
	using OneMerge = Lucene.Net.Index.MergePolicy.OneMerge;
	using MergeScheduler = Lucene.Net.Index.MergeScheduler;
	using MergeTrigger = Lucene.Net.Index.MergeTrigger;
	using Directory = Lucene.Net.Store.Directory;
	using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
	using RAMDirectory = Lucene.Net.Store.RAMDirectory;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

	/// <summary>
	/// Holds tests cases to verify external APIs are accessible
	/// while not being in Lucene.Net.Index package.
	/// </summary>
	public class TestMergeSchedulerExternal : LuceneTestCase
	{

	  internal volatile bool MergeCalled;
	  internal volatile bool MergeThreadCreated;
	  internal volatile bool ExcCalled;

	  private class MyMergeScheduler : ConcurrentMergeScheduler
	  {
		  private readonly TestMergeSchedulerExternal OuterInstance;

		  public MyMergeScheduler(TestMergeSchedulerExternal outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }


		private class MyMergeThread : ConcurrentMergeScheduler.MergeThread
		{
			private readonly TestMergeSchedulerExternal.MyMergeScheduler OuterInstance;

		  public MyMergeThread(TestMergeSchedulerExternal.MyMergeScheduler outerInstance, IndexWriter writer, MergePolicy.OneMerge merge) : base(writer, merge)
		  {
			  this.OuterInstance = outerInstance;
			outerInstance.OuterInstance.MergeThreadCreated = true;
		  }
		}

		protected internal override MergeThread GetMergeThread(IndexWriter writer, MergePolicy.OneMerge merge)
		{
		  MergeThread thread = new MyMergeThread(this, writer, merge);
		  thread.ThreadPriority = MergeThreadPriority;
		  thread.Daemon = true;
		  thread.Name = "MyMergeThread";
		  return thread;
		}

		protected internal override void HandleMergeException(Exception t)
		{
		  outerInstance.ExcCalled = true;
		}

		protected internal override void DoMerge(MergePolicy.OneMerge merge)
		{
		  outerInstance.MergeCalled = true;
		  base.doMerge(merge);
		}
	  }

	  private class FailOnlyOnMerge : MockDirectoryWrapper.Failure
	  {
		public override void Eval(MockDirectoryWrapper dir)
		{
		  StackTraceElement[] trace = (new Exception()).StackTrace;
		  for (int i = 0; i < trace.Length; i++)
		  {
			if ("doMerge".Equals(trace[i].MethodName))
			{
			  throw new IOException("now failing during merge");
			}
		  }
		}
	  }

	  public virtual void TestSubclassConcurrentMergeScheduler()
	  {
		MockDirectoryWrapper dir = newMockDirectory();
		dir.failOn(new FailOnlyOnMerge());

		Document doc = new Document();
		Field idField = newStringField("id", "", Field.Store.YES);
		doc.add(idField);

		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMergeScheduler(new MyMergeScheduler(this)).setMaxBufferedDocs(2).setRAMBufferSizeMB(IndexWriterConfig.DISABLE_AUTO_FLUSH).setMergePolicy(newLogMergePolicy()));
		LogMergePolicy logMP = (LogMergePolicy) writer.Config.MergePolicy;
		logMP.MergeFactor = 10;
		for (int i = 0;i < 20;i++)
		{
		  writer.addDocument(doc);
		}

		((MyMergeScheduler) writer.Config.MergeScheduler).sync();
		writer.close();

		Assert.IsTrue(MergeThreadCreated);
		Assert.IsTrue(MergeCalled);
		Assert.IsTrue(ExcCalled);
		dir.close();
	  }

	  private class ReportingMergeScheduler : MergeScheduler
	  {

		public override void Merge(IndexWriter writer, MergeTrigger trigger, bool newMergesFound)
		{
		  MergePolicy.OneMerge merge = null;
		  while ((merge = writer.NextMerge) != null)
		  {
			if (VERBOSE)
			{
			  Console.WriteLine("executing merge " + merge.segString(writer.Directory));
			}
			writer.merge(merge);
		  }
		}

		public override void Close()
		{
		}

	  }

	  public virtual void TestCustomMergeScheduler()
	  {
		// we don't really need to execute anything, just to make sure the custom MS
		// compiles. But ensure that it can be used as well, e.g., no other hidden
		// dependencies or something. Therefore, don't use any random API !
		Directory dir = new RAMDirectory();
		IndexWriterConfig conf = new IndexWriterConfig(TEST_VERSION_CURRENT, null);
		conf.MergeScheduler = new ReportingMergeScheduler();
		IndexWriter writer = new IndexWriter(dir, conf);
		writer.addDocument(new Document());
		writer.commit(); // trigger flush
		writer.addDocument(new Document());
		writer.commit(); // trigger flush
		writer.forceMerge(1);
		writer.close();
		dir.close();
	  }

	}

}