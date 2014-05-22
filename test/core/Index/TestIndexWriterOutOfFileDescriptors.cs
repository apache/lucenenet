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
	using Directory = Lucene.Net.Store.Directory;
	using IOContext = Lucene.Net.Store.IOContext;
	using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
	using LineFileDocs = Lucene.Net.Util.LineFileDocs;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using PrintStreamInfoStream = Lucene.Net.Util.PrintStreamInfoStream;
	using TestUtil = Lucene.Net.Util.TestUtil;

	public class TestIndexWriterOutOfFileDescriptors : LuceneTestCase
	{
	  public virtual void Test()
	  {
		MockDirectoryWrapper dir = newMockFSDirectory(createTempDir("TestIndexWriterOutOfFileDescriptors"));
		dir.PreventDoubleWrite = false;
		double rate = random().NextDouble() * 0.01;
		//System.out.println("rate=" + rate);
		dir.RandomIOExceptionRateOnOpen = rate;
		int iters = atLeast(20);
		LineFileDocs docs = new LineFileDocs(random(), defaultCodecSupportsDocValues());
		IndexReader r = null;
		DirectoryReader r2 = null;
		bool any = false;
		MockDirectoryWrapper dirCopy = null;
		int lastNumDocs = 0;
		for (int iter = 0;iter < iters;iter++)
		{

		  IndexWriter w = null;
		  if (VERBOSE)
		  {
			Console.WriteLine("TEST: iter=" + iter);
		  }
		  try
		  {
			MockAnalyzer analyzer = new MockAnalyzer(random());
			analyzer.MaxTokenLength = TestUtil.Next(random(), 1, IndexWriter.MAX_TERM_LENGTH);
			IndexWriterConfig iwc = newIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);

			if (VERBOSE)
			{
			  // Do this ourselves instead of relying on LTC so
			  // we see incrementing messageID:
			  iwc.InfoStream = new PrintStreamInfoStream(System.out);
			}
			MergeScheduler ms = iwc.MergeScheduler;
			if (ms is ConcurrentMergeScheduler)
			{
			  ((ConcurrentMergeScheduler) ms).setSuppressExceptions();
			}
			w = new IndexWriter(dir, iwc);
			if (r != null && random().Next(5) == 3)
			{
			  if (random().nextBoolean())
			  {
				if (VERBOSE)
				{
				  Console.WriteLine("TEST: addIndexes IR[]");
				}
				w.addIndexes(new IndexReader[] {r});
			  }
			  else
			  {
				if (VERBOSE)
				{
				  Console.WriteLine("TEST: addIndexes Directory[]");
				}
				w.addIndexes(new Directory[] {dirCopy});
			  }
			}
			else
			{
			  if (VERBOSE)
			  {
				Console.WriteLine("TEST: addDocument");
			  }
			  w.addDocument(docs.nextDoc());
			}
			dir.RandomIOExceptionRateOnOpen = 0.0;
			w.close();
			w = null;

			// NOTE: this is O(N^2)!  Only enable for temporary debugging:
			//dir.setRandomIOExceptionRateOnOpen(0.0);
			//TestUtil.checkIndex(dir);
			//dir.setRandomIOExceptionRateOnOpen(rate);

			// Verify numDocs only increases, to catch IndexWriter
			// accidentally deleting the index:
			dir.RandomIOExceptionRateOnOpen = 0.0;
			Assert.IsTrue(DirectoryReader.indexExists(dir));
			if (r2 == null)
			{
			  r2 = DirectoryReader.open(dir);
			}
			else
			{
			  DirectoryReader r3 = DirectoryReader.openIfChanged(r2);
			  if (r3 != null)
			  {
				r2.close();
				r2 = r3;
			  }
			}
			Assert.IsTrue("before=" + lastNumDocs + " after=" + r2.numDocs(), r2.numDocs() >= lastNumDocs);
			lastNumDocs = r2.numDocs();
			//System.out.println("numDocs=" + lastNumDocs);
			dir.RandomIOExceptionRateOnOpen = rate;

			any = true;
			if (VERBOSE)
			{
			  Console.WriteLine("TEST: iter=" + iter + ": success");
			}
		  }
		  catch (IOException ioe)
		  {
			if (VERBOSE)
			{
			  Console.WriteLine("TEST: iter=" + iter + ": exception");
			  Console.WriteLine(ioe.ToString());
			  Console.Write(ioe.StackTrace);
			}
			if (w != null)
			{
			  // NOTE: leave random IO exceptions enabled here,
			  // to verify that rollback does not try to write
			  // anything:
			  w.rollback();
			}
		  }

		  if (any && r == null && random().nextBoolean())
		  {
			// Make a copy of a non-empty index so we can use
			// it to addIndexes later:
			dir.RandomIOExceptionRateOnOpen = 0.0;
			r = DirectoryReader.open(dir);
			dirCopy = newMockFSDirectory(createTempDir("TestIndexWriterOutOfFileDescriptors.copy"));
			Set<string> files = new HashSet<string>();
			foreach (string file in dir.listAll())
			{
			  dir.copy(dirCopy, file, file, IOContext.DEFAULT);
			  files.add(file);
			}
			dirCopy.sync(files);
			// Have IW kiss the dir so we remove any leftover
			// files ... we can easily have leftover files at
			// the time we take a copy because we are holding
			// open a reader:
			(new IndexWriter(dirCopy, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())))).close();
			dirCopy.RandomIOExceptionRate = rate;
			dir.RandomIOExceptionRateOnOpen = rate;
		  }
		}

		if (r2 != null)
		{
		  r2.close();
		}
		if (r != null)
		{
		  r.close();
		  dirCopy.close();
		}
		dir.close();
	  }
	}

}