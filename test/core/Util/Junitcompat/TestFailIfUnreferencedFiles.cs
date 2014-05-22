using System;

namespace Lucene.Net.Util.junitcompat
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
	using Assert = org.junit.Assert;
	using Test = org.junit.Test;
	using JUnitCore = org.junit.runner.JUnitCore;
	using Result = org.junit.runner.Result;
	using Failure = org.junit.runner.notification.Failure;
	using RandomizedTest = com.carrotsearch.randomizedtesting.RandomizedTest;

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
		  MockDirectoryWrapper dir = newMockDirectory();
		  dir.AssertNoUnrefencedFilesOnClose = true;
		  IndexWriter iw = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, null));
		  iw.addDocument(new Document());
		  iw.close();
		  IndexOutput output = dir.createOutput("_hello.world", IOContext.DEFAULT);
		  output.writeString("i am unreferenced!");
		  output.close();
		  dir.sync(Collections.singleton("_hello.world"));
		  dir.close();
		}
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testFailIfUnreferencedFiles()
	  public virtual void TestFailIfUnreferencedFiles()
	  {
		Result r = JUnitCore.runClasses(typeof(Nested1));
		RandomizedTest.assumeTrue("Ignoring nested test, very likely zombie threads present.", r.IgnoreCount == 0);

		// We are suppressing output anyway so dump the failures.
		foreach (Failure f in r.Failures)
		{
		  Console.WriteLine(f.Trace);
		}

		Assert.Assert.AreEqual("Expected exactly one failure.", 1, r.FailureCount);
		Assert.Assert.IsTrue("Expected unreferenced files assertion.", r.Failures.get(0).Trace.contains("unreferenced files:"));
	  }
	}

}