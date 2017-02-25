using System;
using Lucene.Net.Support;
using NUnit.Framework;

namespace Lucene.Net.Util.JunitCompat
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

	using Document = Lucene.Net.Document.Document;
	using IndexWriter = Lucene.Net.Index.IndexWriter;
	using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
	using IOContext = Lucene.Net.Store.IOContext;
	using IndexOutput = Lucene.Net.Store.IndexOutput;
	using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
	/*using Assert = org.junit.Assert;
	using Test = org.junit.Test;
	using JUnitCore = org.junit.runner.JUnitCore;
	using Result = org.junit.runner.Result;
	using Failure = org.junit.runner.notification.Failure;
	using RandomizedTest = com.carrotsearch.randomizedtesting.RandomizedTest;*/

	// LUCENE-4456: Test that we fail if there are unreferenced files
	public class TestFailIfUnreferencedFiles : WithNestedTests
	{
	  public TestFailIfUnreferencedFiles() : base(true)
	  {
	  }

	  public class Nested1 : WithNestedTests.AbstractNestedTest
	  {
		public virtual void TestDummy()
		{
		  MockDirectoryWrapper dir = NewMockDirectory();
		  dir.AssertNoUnrefencedFilesOnClose = true;
		  IndexWriter iw = new IndexWriter(dir, new IndexWriterConfig(LuceneTestCase.TEST_VERSION_CURRENT, null));
		  iw.AddDocument(new Document());
		  iw.Dispose();
		  IndexOutput output = dir.CreateOutput("_hello.world", IOContext.DEFAULT);
		  output.writeString("i am unreferenced!");
		  output.Dispose();
		  dir.Sync(CollectionsHelper.Singleton("_hello.world"));
		  dir.Dispose();
		}
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testFailIfUnreferencedFiles()
	  public virtual void TestFailIfUnreferencedFilesMem()
	  {
		Result r = JUnitCore.runClasses(typeof(Nested1));
		RandomizedTest.assumeTrue("Ignoring nested test, very likely zombie threads present.", r.IgnoreCount == 0);

		// We are suppressing output anyway so dump the failures.
		foreach (Failure f in r.Failures)
		{
		  Console.WriteLine(f.Trace);
		}

		Assert.AreEqual(1, r.FailureCount, "Expected exactly one failure.");
		Assert.IsTrue(r.Failures.Get(0).Trace.Contains("unreferenced files:"), "Expected unreferenced files assertion.");
	  }
	}

}