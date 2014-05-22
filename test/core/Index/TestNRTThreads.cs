using System;
using System.Diagnostics;

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


	using IndexSearcher = Lucene.Net.Search.IndexSearcher;
	using Directory = Lucene.Net.Store.Directory;
	using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
	using SuppressCodecs = Lucene.Net.Util.LuceneTestCase.SuppressCodecs;
	using Before = org.junit.Before;

	// TODO
	//   - mix in forceMerge, addIndexes
	//   - randomoly mix in non-congruent docs

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressCodecs({ "SimpleText", "Memory", "Direct" }) public class TestNRTThreads extends ThreadedIndexingAndSearchingTestCase
	public class TestNRTThreads : ThreadedIndexingAndSearchingTestCase
	{

	  private bool UseNonNrtReaders = true;

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Before public void setUp() throws Exception
	  public virtual void SetUp()
	  {
		base.setUp();
		UseNonNrtReaders = random().nextBoolean();
	  }

	  protected internal override void DoSearching(ExecutorService es, long stopTime)
	  {

		bool anyOpenDelFiles = false;

		DirectoryReader r = DirectoryReader.open(writer, true);

		while (System.currentTimeMillis() < stopTime && !failed.get())
		{
		  if (random().nextBoolean())
		  {
			if (VERBOSE)
			{
			  Console.WriteLine("TEST: now reopen r=" + r);
			}
			DirectoryReader r2 = DirectoryReader.openIfChanged(r);
			if (r2 != null)
			{
			  r.close();
			  r = r2;
			}
		  }
		  else
		  {
			if (VERBOSE)
			{
			  Console.WriteLine("TEST: now close reader=" + r);
			}
			r.close();
			writer.commit();
			Set<string> openDeletedFiles = ((MockDirectoryWrapper) dir).OpenDeletedFiles;
			if (openDeletedFiles.size() > 0)
			{
			  Console.WriteLine("OBD files: " + openDeletedFiles);
			}
			anyOpenDelFiles |= openDeletedFiles.size() > 0;
			//Assert.AreEqual("open but deleted: " + openDeletedFiles, 0, openDeletedFiles.size());
			if (VERBOSE)
			{
			  Console.WriteLine("TEST: now open");
			}
			r = DirectoryReader.open(writer, true);
		  }
		  if (VERBOSE)
		  {
			Console.WriteLine("TEST: got new reader=" + r);
		  }
		  //System.out.println("numDocs=" + r.numDocs() + "
		  //openDelFileCount=" + dir.openDeleteFileCount());

		  if (r.numDocs() > 0)
		  {
			FixedSearcher = new IndexSearcher(r, es);
			smokeTestSearcher(FixedSearcher);
			runSearchThreads(System.currentTimeMillis() + 500);
		  }
		}
		r.close();

		//System.out.println("numDocs=" + r.numDocs() + " openDelFileCount=" + dir.openDeleteFileCount());
		Set<string> openDeletedFiles = ((MockDirectoryWrapper) dir).OpenDeletedFiles;
		if (openDeletedFiles.size() > 0)
		{
		  Console.WriteLine("OBD files: " + openDeletedFiles);
		}
		anyOpenDelFiles |= openDeletedFiles.size() > 0;

		Assert.IsFalse("saw non-zero open-but-deleted count", anyOpenDelFiles);
	  }

	  protected internal override Directory GetDirectory(Directory @in)
	  {
		Debug.Assert(@in is MockDirectoryWrapper);
		if (!UseNonNrtReaders)
		{
			((MockDirectoryWrapper) @in).AssertNoDeleteOpenFile = true;
		}
		return @in;
	  }

	  protected internal override void DoAfterWriter(ExecutorService es)
	  {
		// Force writer to do reader pooling, always, so that
		// all merged segments, even for merges before
		// doSearching is called, are warmed:
		writer.Reader.close();
	  }

	  private IndexSearcher FixedSearcher;

	  protected internal override IndexSearcher CurrentSearcher
	  {
		  get
		  {
			return FixedSearcher;
		  }
	  }

	  protected internal override void ReleaseSearcher(IndexSearcher s)
	  {
		if (s != FixedSearcher)
		{
		  // Final searcher:
		  s.IndexReader.close();
		}
	  }

	  protected internal override IndexSearcher FinalSearcher
	  {
		  get
		  {
			IndexReader r2;
			if (UseNonNrtReaders)
			{
			  if (random().nextBoolean())
			  {
				r2 = writer.Reader;
			  }
			  else
			  {
				writer.commit();
				r2 = DirectoryReader.open(dir);
			  }
			}
			else
			{
			  r2 = writer.Reader;
			}
			return newSearcher(r2);
		  }
	  }

	  public virtual void TestNRTThreads()
	  {
		runTest("TestNRTThreads");
	  }
	}

}