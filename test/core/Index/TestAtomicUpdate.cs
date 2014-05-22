using System;
using System.Threading;

namespace Lucene.Net.Index
{

	/// <summary>
	/// Copyright 2004 The Apache Software Foundation
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

	using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
	using Lucene.Net.Document;
	using Lucene.Net.Store;
	using Lucene.Net.Util;

	public class TestAtomicUpdate : LuceneTestCase
	{


	  private abstract class TimedThread : System.Threading.Thread
	  {
		internal volatile bool Failed;
		internal int Count;
		internal static float RUN_TIME_MSEC = atLeast(500);
		internal TimedThread[] AllThreads;

		public abstract void DoWork();

		internal TimedThread(TimedThread[] threads)
		{
		  this.AllThreads = threads;
		}

		public override void Run()
		{
		  long stopTime = System.currentTimeMillis() + (long) RUN_TIME_MSEC;

		  Count = 0;

		  try
		  {
			do
			{
			  if (AnyErrors())
			  {
				  break;
			  }
			  DoWork();
			  Count++;
			} while (System.currentTimeMillis() < stopTime);
		  }
		  catch (Exception e)
		  {
			Console.WriteLine(Thread.CurrentThread.Name + ": exc");
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
		internal IndexWriter Writer;
		public IndexerThread(IndexWriter writer, TimedThread[] threads) : base(threads)
		{
		  this.Writer = writer;
		}

		public override void DoWork()
		{
		  // Update all 100 docs...
		  for (int i = 0; i < 100; i++)
		  {
			Document d = new Document();
			d.add(new StringField("id", Convert.ToString(i), Field.Store.YES));
			d.add(new TextField("contents", English.intToEnglish(i + 10 * Count), Field.Store.NO));
			Writer.updateDocument(new Term("id", Convert.ToString(i)), d);
		  }
		}
	  }

	  private class SearcherThread : TimedThread
	  {
		internal Directory Directory;

		public SearcherThread(Directory directory, TimedThread[] threads) : base(threads)
		{
		  this.Directory = directory;
		}

		public override void DoWork()
		{
		  IndexReader r = DirectoryReader.open(Directory);
		  Assert.AreEqual(100, r.numDocs());
		  r.close();
		}
	  }

	  /*
	    Run one indexer and 2 searchers against single index as
	    stress test.
	  */
	  public virtual void RunTest(Directory directory)
	  {

		TimedThread[] threads = new TimedThread[4];

		IndexWriterConfig conf = (new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()))).setMaxBufferedDocs(7);
		((TieredMergePolicy) conf.MergePolicy).MaxMergeAtOnce = 3;
		IndexWriter writer = RandomIndexWriter.mockIndexWriter(directory, conf, random());

		// Establish a base index of 100 docs:
		for (int i = 0;i < 100;i++)
		{
		  Document d = new Document();
		  d.add(newStringField("id", Convert.ToString(i), Field.Store.YES));
		  d.add(newTextField("contents", English.intToEnglish(i), Field.Store.NO));
		  if ((i - 1) % 7 == 0)
		  {
			writer.commit();
		  }
		  writer.addDocument(d);
		}
		writer.commit();

		IndexReader r = DirectoryReader.open(directory);
		Assert.AreEqual(100, r.numDocs());
		r.close();

		IndexerThread indexerThread = new IndexerThread(writer, threads);
		threads[0] = indexerThread;
		indexerThread.start();

		IndexerThread indexerThread2 = new IndexerThread(writer, threads);
		threads[1] = indexerThread2;
		indexerThread2.start();

		SearcherThread searcherThread1 = new SearcherThread(directory, threads);
		threads[2] = searcherThread1;
		searcherThread1.start();

		SearcherThread searcherThread2 = new SearcherThread(directory, threads);
		threads[3] = searcherThread2;
		searcherThread2.start();

		indexerThread.join();
		indexerThread2.join();
		searcherThread1.join();
		searcherThread2.join();

		writer.close();

		Assert.IsTrue("hit unexpected exception in indexer", !indexerThread.Failed);
		Assert.IsTrue("hit unexpected exception in indexer2", !indexerThread2.Failed);
		Assert.IsTrue("hit unexpected exception in search1", !searcherThread1.Failed);
		Assert.IsTrue("hit unexpected exception in search2", !searcherThread2.Failed);
		//System.out.println("    Writer: " + indexerThread.count + " iterations");
		//System.out.println("Searcher 1: " + searcherThread1.count + " searchers created");
		//System.out.println("Searcher 2: " + searcherThread2.count + " searchers created");
	  }

	  /*
	    Run above stress test against RAMDirectory and then
	    FSDirectory.
	  */
	  public virtual void TestAtomicUpdates()
	  {
		Directory directory;

		// First in a RAM directory:
		directory = new MockDirectoryWrapper(random(), new RAMDirectory());
		RunTest(directory);
		directory.close();

		// Second in an FSDirectory:
		File dirPath = createTempDir("lucene.test.atomic");
		directory = newFSDirectory(dirPath);
		RunTest(directory);
		directory.close();
		TestUtil.rm(dirPath);
	  }
	}

}