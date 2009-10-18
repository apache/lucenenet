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

using Lucene.Net.Util;
using Lucene.Net.Store;
using Lucene.Net.Documents;
using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Searchable = Lucene.Net.Search.Searchable;
using Lucene.Net.QueryParsers;

namespace Lucene.Net.Index
{
	
    [TestFixture]
	public class TestStressIndexing
	{
		private static readonly Analyzer ANALYZER = new SimpleAnalyzer();
		private static readonly System.Random RANDOM = new System.Random();
		private static Searcher SEARCHER;
		
		private static int RUN_TIME_SEC = 15;
		
		private class IndexerThread : SupportClass.ThreadClass
		{
			internal IndexModifier modifier;
			internal int nextID;
			public int count;
			internal bool failed;
			
			public IndexerThread(IndexModifier modifier)
			{
				this.modifier = modifier;
			}
			
			override public void  Run()
			{
				long stopTime = (System.DateTime.Now.Ticks - 621355968000000000) / 10000 + 1000 * Lucene.Net.Index.TestStressIndexing.RUN_TIME_SEC;
				try
				{
					while (true)
					{
						
						if ((System.DateTime.Now.Ticks - 621355968000000000) / 10000 > stopTime)
						{
							break;
						}
						
						// Add 10 docs:
						for (int j = 0; j < 10; j++)
						{
							Lucene.Net.Documents.Document d = new Lucene.Net.Documents.Document();
							int n = Lucene.Net.Index.TestStressIndexing.RANDOM.Next();
							d.Add(new Field("id", System.Convert.ToString(nextID++), Field.Store.YES, Field.Index.UN_TOKENIZED));
							d.Add(new Field("contents", English.IntToEnglish(n), Field.Store.NO, Field.Index.TOKENIZED));
							modifier.AddDocument(d);
						}
						
						// Delete 5 docs:
						int deleteID = nextID;
						for (int j = 0; j < 5; j++)
						{
							modifier.DeleteDocuments(new Term("id", "" + deleteID));
							deleteID -= 2;
						}
						
						count++;
					}
					
					modifier.Close();
				}
				catch (System.Exception e)
				{
					System.Console.Out.WriteLine(e.ToString());
					System.Console.Error.WriteLine(e.StackTrace);
					failed = true;
				}
			}
		}
		
		private class SearcherThread : SupportClass.ThreadClass
		{
			private Directory directory;
			public int count;
			internal bool failed;
			
			public SearcherThread(Directory directory)
			{
				this.directory = directory;
			}
			
			override public void  Run()
			{
				long stopTime = (System.DateTime.Now.Ticks - 621355968000000000) / 10000 + 1000 * Lucene.Net.Index.TestStressIndexing.RUN_TIME_SEC;
				try
				{
					while (true)
					{
						for (int i = 0; i < 100; i++)
						{
							(new IndexSearcher(directory)).Close();
						}
						count += 100;
						if ((System.DateTime.Now.Ticks - 621355968000000000) / 10000 > stopTime)
						{
							break;
						}
					}
				}
				catch (System.Exception e)
				{
					System.Console.Out.WriteLine(e.ToString());
					System.Console.Error.WriteLine(e.StackTrace);
					failed = true;
				}
			}
		}
		
		/*
		Run one indexer and 2 searchers against single index as
		stress test.
		*/
		public virtual void  RunStressTest(Directory directory)
		{
			IndexModifier modifier = new IndexModifier(directory, ANALYZER, true);
			
			// One modifier that writes 10 docs then removes 5, over
			// and over:
			IndexerThread indexerThread = new IndexerThread(modifier);
			indexerThread.Start();
			
			// Two searchers that constantly just re-instantiate the searcher:
			SearcherThread searcherThread1 = new SearcherThread(directory);
			searcherThread1.Start();
			
			SearcherThread searcherThread2 = new SearcherThread(directory);
			searcherThread2.Start();
			
			indexerThread.Join();
			searcherThread1.Join();
			searcherThread2.Join();
			Assert.IsTrue(!indexerThread.failed,"hit unexpected exception in indexer");
			Assert.IsTrue(!searcherThread1.failed,"hit unexpected exception in search1");
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
			
			// First in a RAM directory:
			Directory directory = new RAMDirectory();
			RunStressTest(directory);
			directory.Close();
			
			// Second in an FSDirectory:
			System.String tempDir = System.IO.Path.GetTempPath();
			System.IO.FileInfo dirPath = new System.IO.FileInfo(tempDir + "\\" + "lucene.test.stress");
			directory = FSDirectory.GetDirectory(dirPath);
			RunStressTest(directory);
			directory.Close();
			RmDir(dirPath);
		}
		
		private void  RmDir(System.IO.FileInfo dir)
		{
			System.IO.FileInfo[] files = SupportClass.FileSupport.GetFiles(dir);
			for (int i = 0; i < files.Length; i++)
			{
				bool tmpBool;
				if (System.IO.File.Exists(files[i].FullName))
				{
					System.IO.File.Delete(files[i].FullName);
					tmpBool = true;
				}
				else if (System.IO.Directory.Exists(files[i].FullName))
				{
					System.IO.Directory.Delete(files[i].FullName);
					tmpBool = true;
				}
				else
					tmpBool = false;
				bool generatedAux = tmpBool;
			}
			bool tmpBool2;
			if (System.IO.File.Exists(dir.FullName))
			{
				System.IO.File.Delete(dir.FullName);
				tmpBool2 = true;
			}
			else if (System.IO.Directory.Exists(dir.FullName))
			{
				System.IO.Directory.Delete(dir.FullName);
				tmpBool2 = true;
			}
			else
				tmpBool2 = false;
			bool generatedAux2 = tmpBool2;
		}
	}
}