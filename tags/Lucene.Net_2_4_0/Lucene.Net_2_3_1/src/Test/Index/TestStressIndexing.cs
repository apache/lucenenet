/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;

using NUnit.Framework;

using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.Analysis;
using Lucene.Net.Search;
using Searchable = Lucene.Net.Search.Searchable;

namespace Lucene.Net.Index
{
	
	[TestFixture]
	public class TestStressIndexing : LuceneTestCase
	{
		private static readonly Analyzer ANALYZER = new SimpleAnalyzer();
		private static readonly System.Random RANDOM = new System.Random();
		
		abstract public class TimedThread : SupportClass.ThreadClass
		{
			internal bool failed;
			internal int count;
			private static int RUN_TIME_SEC = 6;
			private TimedThread[] allThreads;
			
			abstract public void  DoWork();
			
			internal TimedThread(TimedThread[] threads)
			{
				this.allThreads = threads;
			}
			
			override public void  Run()
			{
				long stopTime = (System.DateTime.Now.Ticks - 621355968000000000) / 10000 + 1000 * RUN_TIME_SEC;
				
				count = 0;
				
				try
				{
					while ((System.DateTime.Now.Ticks - 621355968000000000) / 10000 < stopTime && !AnyErrors())
					{
						DoWork();
						count++;
					}
				}
				catch (System.Exception e)
				{
					System.Console.Out.WriteLine(e.StackTrace);
					failed = true;
				}
			}
			
			private bool AnyErrors()
			{
				for (int i = 0; i < allThreads.Length; i++)
					if (allThreads[i] != null && allThreads[i].failed)
						return true;
				return false;
			}
		}
		
		private class IndexerThread : TimedThread
		{
			internal IndexWriter writer;
			//new public int count;
			internal int nextID;
			
			public IndexerThread(IndexWriter writer, TimedThread[] threads):base(threads)
			{
				this.writer = writer;
			}
			
			public override void  DoWork()
			{
				// Add 10 docs:
				for (int j = 0; j < 10; j++)
				{
					Document d = new Document();
					int n = Lucene.Net.Index.TestStressIndexing.RANDOM.Next();
					d.Add(new Field("id", System.Convert.ToString(nextID++), Field.Store.YES, Field.Index.UN_TOKENIZED));
					d.Add(new Field("contents", English.IntToEnglish(n), Field.Store.NO, Field.Index.TOKENIZED));
					writer.AddDocument(d);
				}
				
				// Delete 5 docs:
				int deleteID = nextID - 1;
				for (int j = 0; j < 5; j++)
				{
					writer.DeleteDocuments(new Term("id", "" + deleteID));
					deleteID -= 2;
				}
			}
		}
		
		private class SearcherThread : TimedThread
		{
			private Directory directory;
			
			public SearcherThread(Directory directory, TimedThread[] threads):base(threads)
			{
				this.directory = directory;
			}
			
			public override void  DoWork()
			{
				for (int i = 0; i < 100; i++)
					(new IndexSearcher(directory)).Close();
				count += 100;
			}
		}
		
		/*
		Run one indexer and 2 searchers against single index as
		stress test.
		*/
		public virtual void  RunStressTest(Directory directory, bool autoCommit, MergeScheduler mergeScheduler)
		{
			IndexWriter modifier = new IndexWriter(directory, autoCommit, ANALYZER, true);
			
			modifier.SetMaxBufferedDocs(10);
			
			TimedThread[] threads = new TimedThread[4];
			
			if (mergeScheduler != null)
				modifier.SetMergeScheduler(mergeScheduler);
			
			// One modifier that writes 10 docs then removes 5, over
			// and over:
			IndexerThread indexerThread = new IndexerThread(modifier, threads);
			threads[0] = indexerThread;
			indexerThread.Start();
			
			IndexerThread indexerThread2 = new IndexerThread(modifier, threads);
			threads[2] = indexerThread2;
			indexerThread2.Start();
			
			// Two searchers that constantly just re-instantiate the
			// searcher:
			SearcherThread searcherThread1 = new SearcherThread(directory, threads);
			threads[3] = searcherThread1;
			searcherThread1.Start();
			
			SearcherThread searcherThread2 = new SearcherThread(directory, threads);
			threads[3] = searcherThread2;
			searcherThread2.Start();
			
			indexerThread.Join();
			indexerThread2.Join();
			searcherThread1.Join();
			searcherThread2.Join();
			
			modifier.Close();
			
			Assert.IsTrue(!indexerThread.failed, "hit unexpected exception in indexer");
			Assert.IsTrue(!indexerThread2.failed, "hit unexpected exception in indexer2");
			Assert.IsTrue(!searcherThread1.failed, "hit unexpected exception in search1");
			Assert.IsTrue(!searcherThread2.failed, "hit unexpected exception in search2");
			
			//System.out.println("    Writer: " + indexerThread.count + " iterations");
			//System.out.println("Searcher 1: " + searcherThread1.count + " searchers created");
			//System.out.println("Searcher 2: " + searcherThread2.count + " searchers created");
		}
		
		/*
		Run above stress test against RAMDirectory and then
		FSDirectory.
		*/
		[Test]
		public virtual void  TestStressIndexAndSearching()
		{
			
			// RAMDir
			Directory directory = new MockRAMDirectory();
			RunStressTest(directory, true, null);
			directory.Close();
			
			// FSDir
			System.String tempDir = System.IO.Path.GetTempPath();
			System.IO.FileInfo dirPath = new System.IO.FileInfo(tempDir + "\\" + "lucene.test.stress");
			directory = FSDirectory.GetDirectory(dirPath);
			RunStressTest(directory, true, null);
			directory.Close();
			
			// With ConcurrentMergeScheduler, in RAMDir
			directory = new MockRAMDirectory();
			RunStressTest(directory, true, new ConcurrentMergeScheduler());
			directory.Close();
			
			// With ConcurrentMergeScheduler, in FSDir
			directory = FSDirectory.GetDirectory(dirPath);
			RunStressTest(directory, true, new ConcurrentMergeScheduler());
			directory.Close();
			
			// With ConcurrentMergeScheduler and autoCommit=false, in RAMDir
			directory = new MockRAMDirectory();
			RunStressTest(directory, false, new ConcurrentMergeScheduler());
			directory.Close();
			
			// With ConcurrentMergeScheduler and autoCommit=false, in FSDir
			directory = FSDirectory.GetDirectory(dirPath);
			RunStressTest(directory, false, new ConcurrentMergeScheduler());
			directory.Close();
			
			_TestUtil.RmDir(dirPath);
		}
	}
}