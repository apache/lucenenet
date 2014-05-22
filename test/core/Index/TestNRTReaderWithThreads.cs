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
	using Directory = Lucene.Net.Store.Directory;
	using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

	public class TestNRTReaderWithThreads : LuceneTestCase
	{
	  internal AtomicInteger Seq = new AtomicInteger(1);

	  public virtual void TestIndexing()
	  {
		Directory mainDir = newDirectory();
		if (mainDir is MockDirectoryWrapper)
		{
		  ((MockDirectoryWrapper)mainDir).AssertNoDeleteOpenFile = true;
		}
		IndexWriter writer = new IndexWriter(mainDir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(10).setMergePolicy(newLogMergePolicy(false,2)));
		IndexReader reader = writer.Reader; // start pooling readers
		reader.close();
		RunThread[] indexThreads = new RunThread[4];
		for (int x = 0; x < indexThreads.Length; x++)
		{
		  indexThreads[x] = new RunThread(this, x % 2, writer);
		  indexThreads[x].Name = "Thread " + x;
		  indexThreads[x].Start();
		}
		long startTime = System.currentTimeMillis();
		long duration = 1000;
		while ((System.currentTimeMillis() - startTime) < duration)
		{
		  Thread.Sleep(100);
		}
		int delCount = 0;
		int addCount = 0;
		for (int x = 0; x < indexThreads.Length; x++)
		{
		  indexThreads[x].Run_Renamed = false;
		  assertNull("Exception thrown: " + indexThreads[x].Ex, indexThreads[x].Ex);
		  addCount += indexThreads[x].AddCount;
		  delCount += indexThreads[x].DelCount;
		}
		for (int x = 0; x < indexThreads.Length; x++)
		{
		  indexThreads[x].Join();
		}
		for (int x = 0; x < indexThreads.Length; x++)
		{
		  assertNull("Exception thrown: " + indexThreads[x].Ex, indexThreads[x].Ex);
		}
		//System.out.println("addCount:"+addCount);
		//System.out.println("delCount:"+delCount);
		writer.close();
		mainDir.close();
	  }

	  public class RunThread : System.Threading.Thread
	  {
		  private readonly TestNRTReaderWithThreads OuterInstance;

		internal IndexWriter Writer;
		internal volatile bool Run_Renamed = true;
		internal volatile Exception Ex;
		internal int DelCount = 0;
		internal int AddCount = 0;
		internal int Type;
		internal readonly Random r = new Random(random().nextLong());

		public RunThread(TestNRTReaderWithThreads outerInstance, int type, IndexWriter writer)
		{
			this.OuterInstance = outerInstance;
		  this.Type = type;
		  this.Writer = writer;
		}

		public override void Run()
		{
		  try
		  {
			while (Run_Renamed)
			{
			  //int n = random.nextInt(2);
			  if (Type == 0)
			  {
				int i = outerInstance.Seq.addAndGet(1);
				Document doc = DocHelper.createDocument(i, "index1", 10);
				Writer.addDocument(doc);
				AddCount++;
			  }
			  else if (Type == 1)
			  {
				// we may or may not delete because the term may not exist,
				// however we're opening and closing the reader rapidly
				IndexReader reader = Writer.Reader;
				int id = r.Next((int)outerInstance.Seq);
				Term term = new Term("id", Convert.ToString(id));
				int count = TestIndexWriterReader.Count(term, reader);
				Writer.deleteDocuments(term);
				reader.close();
				DelCount += count;
			  }
			}
		  }
		  catch (Exception ex)
		  {
			ex.printStackTrace(System.out);
			this.Ex = ex;
			Run_Renamed = false;
		  }
		}
	  }
	}

}