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
	public class TestAtomicUpdate : LuceneTestCase
	{
		private static readonly Analyzer ANALYZER = new SimpleAnalyzer();
		private static readonly System.Random RANDOM = new System.Random();

        public class MockIndexWriter : IndexWriter
        {
            public MockIndexWriter(Directory d, bool autoCommit, Analyzer a, bool create)
                : base(d, autoCommit, a, create)
            {
            }

            override protected bool TestPoint(string name)
            {
                if (RANDOM.Next(4) == 2)
                    System.Threading.Thread.Sleep(1);
                return true;
            }
        }

		abstract public class TimedThread : SupportClass.ThreadClass
		{
			internal bool failed;
			internal int count;
			private static int RUN_TIME_SEC = 3;
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
                    System.Console.Out.WriteLine(System.Threading.Thread.CurrentThread.Name + ": exc");
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
			
			public IndexerThread(IndexWriter writer, TimedThread[] threads):base(threads)
			{
				this.writer = writer;
			}
			
			public override void  DoWork()
			{
				// Update all 100 docs...
				for (int i = 0; i < 100; i++)
				{
					Document d = new Document();
					d.Add(new Field("id", System.Convert.ToString(i), Field.Store.YES, Field.Index.NOT_ANALYZED));
					d.Add(new Field("contents", English.IntToEnglish(i + 10 * count), Field.Store.NO, Field.Index.ANALYZED));
					writer.UpdateDocument(new Term("id", System.Convert.ToString(i)), d);
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
				IndexReader r = IndexReader.Open(directory);
				Assert.AreEqual(100, r.NumDocs());
				r.Close();
			}
		}
		
		/*
		Run one indexer and 2 searchers against single index as
		stress test.
		*/
		public virtual void  RunTest(Directory directory)
		{
			
			TimedThread[] threads = new TimedThread[4];
			
			IndexWriter writer = new MockIndexWriter(directory, true, ANALYZER, true);
            writer.SetMaxBufferedDocs(7);
            writer.SetMergeFactor(3);

			// Establish a base index of 100 docs:
			for (int i = 0; i < 100; i++)
			{
				Document d = new Document();
				d.Add(new Field("id", System.Convert.ToString(i), Field.Store.YES, Field.Index.NOT_ANALYZED));
				d.Add(new Field("contents", English.IntToEnglish(i), Field.Store.NO, Field.Index.ANALYZED));
				writer.AddDocument(d);
			}
			writer.Commit();

            IndexReader r = IndexReader.Open(directory);
            Assert.AreEqual(100, r.NumDocs());
            r.Close();

			IndexerThread indexerThread = new IndexerThread(writer, threads);
			threads[0] = indexerThread;
			indexerThread.Start();
			
			IndexerThread indexerThread2 = new IndexerThread(writer, threads);
			threads[1] = indexerThread2;
			indexerThread2.Start();
			
			SearcherThread searcherThread1 = new SearcherThread(directory, threads);
			threads[2] = searcherThread1;
			searcherThread1.Start();
			
			SearcherThread searcherThread2 = new SearcherThread(directory, threads);
			threads[3] = searcherThread2;
			searcherThread2.Start();
			
			indexerThread.Join();
			indexerThread2.Join();
			searcherThread1.Join();
			searcherThread2.Join();
			
			writer.Close();
			
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
		public virtual void  TestAtomicUpdates()
		{
			
			Directory directory;
			
			// First in a RAM directory:
			directory = new MockRAMDirectory();
			RunTest(directory);
			directory.Close();
			
			// Second in an FSDirectory:
			System.String tempDir = System.IO.Path.GetTempPath();
			System.IO.FileInfo dirPath = new System.IO.FileInfo(tempDir + "\\" + "lucene.test.atomic");
			directory = FSDirectory.GetDirectory(dirPath);
			RunTest(directory);
			directory.Close();
			_TestUtil.RmDir(dirPath);
		}
	}
}