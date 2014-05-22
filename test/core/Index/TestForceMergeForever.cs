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
	using Directory = Lucene.Net.Store.Directory;
	using LineFileDocs = Lucene.Net.Util.LineFileDocs;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;

	public class TestForceMergeForever : LuceneTestCase
	{

	  // Just counts how many merges are done
	  private class MyIndexWriter : IndexWriter
	  {

		internal AtomicInteger MergeCount = new AtomicInteger();
		internal bool First;

		public MyIndexWriter(Directory dir, IndexWriterConfig conf) : base(dir, conf)
		{
		}

		public override void Merge(MergePolicy.OneMerge merge)
		{
		  if (merge.maxNumSegments != -1 && (First || merge.segments.size() == 1))
		  {
			First = false;
			if (VERBOSE)
			{
			  Console.WriteLine("TEST: maxNumSegments merge");
			}
			MergeCount.incrementAndGet();
		  }
		  base.merge(merge);
		}
	  }

	  public virtual void Test()
	  {
		Directory d = newDirectory();
		MockAnalyzer analyzer = new MockAnalyzer(random());
		analyzer.MaxTokenLength = TestUtil.Next(random(), 1, IndexWriter.MAX_TERM_LENGTH);

		MyIndexWriter w = new MyIndexWriter(d, newIndexWriterConfig(TEST_VERSION_CURRENT, analyzer));

		// Try to make an index that requires merging:
		w.Config.MaxBufferedDocs = TestUtil.Next(random(), 2, 11);
		int numStartDocs = atLeast(20);
		LineFileDocs docs = new LineFileDocs(random(), defaultCodecSupportsDocValues());
		for (int docIDX = 0;docIDX < numStartDocs;docIDX++)
		{
		  w.addDocument(docs.nextDoc());
		}
		MergePolicy mp = w.Config.MergePolicy;
		int mergeAtOnce = 1 + w.segmentInfos.size();
		if (mp is TieredMergePolicy)
		{
		  ((TieredMergePolicy) mp).MaxMergeAtOnce = mergeAtOnce;
		}
		else if (mp is LogMergePolicy)
		{
		  ((LogMergePolicy) mp).MergeFactor = mergeAtOnce;
		}
		else
		{
		  // skip test
		  w.close();
		  d.close();
		  return;
		}

		AtomicBoolean doStop = new AtomicBoolean();
		w.Config.MaxBufferedDocs = 2;
		Thread t = new ThreadAnonymousInnerClassHelper(this, w, numStartDocs, docs, doStop);
		t.Start();
		w.forceMerge(1);
		doStop.set(true);
		t.Join();
		Assert.IsTrue("merge count is " + w.MergeCount.get(), w.MergeCount.get() <= 1);
		w.close();
		d.close();
		docs.close();
	  }

	  private class ThreadAnonymousInnerClassHelper : System.Threading.Thread
	  {
		  private readonly TestForceMergeForever OuterInstance;

		  private Lucene.Net.Index.TestForceMergeForever.MyIndexWriter w;
		  private int NumStartDocs;
		  private LineFileDocs Docs;
		  private AtomicBoolean DoStop;

		  public ThreadAnonymousInnerClassHelper(TestForceMergeForever outerInstance, Lucene.Net.Index.TestForceMergeForever.MyIndexWriter w, int numStartDocs, LineFileDocs docs, AtomicBoolean doStop)
		  {
			  this.OuterInstance = outerInstance;
			  this.w = w;
			  this.NumStartDocs = numStartDocs;
			  this.Docs = docs;
			  this.DoStop = doStop;
		  }

		  public override void Run()
		  {
			try
			{
			  while (!DoStop.get())
			  {
				w.updateDocument(new Term("docid", "" + random().Next(NumStartDocs)), Docs.nextDoc());
				// Force deletes to apply
				w.Reader.close();
			  }
			}
			catch (Exception t)
			{
			  throw new Exception(t);
			}
		  }
	  }
	}

}