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


	using Assert = org.junit.Assert;
	using Test = org.junit.Test;
	using JUnitCore = org.junit.runner.JUnitCore;
	using Result = org.junit.runner.Result;

	using RandomizedTest = com.carrotsearch.randomizedtesting.RandomizedTest;

	public class TestLeaveFilesIfTestFails : WithNestedTests
	{
	  public TestLeaveFilesIfTestFails() : base(true)
	  {
	  }

	  public class Nested1 : WithNestedTests.AbstractNestedTest
	  {
		internal static File File;
		public virtual void TestDummy()
		{
		  File = CreateTempDir("leftover");
		  Assert.Fail();
		}
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testLeaveFilesIfTestFails()
	  public virtual void TestLeaveFilesIfTestFails()
	  {
		Result r = JUnitCore.runClasses(typeof(Nested1));
		Assert.AreEqual(1, r.FailureCount);
		Assert.IsTrue(Nested1.File != null && Nested1.File.Exists());
		Nested1.File.delete();
	  }

	  public class Nested2 : WithNestedTests.AbstractNestedTest
	  {
		internal static File File;
		internal static File Parent;
		internal static RandomAccessFile OpenFile;

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressWarnings("deprecation") public void testDummy() throws Exception
		public virtual void TestDummy()
		{
		  File = new File(CreateTempDir("leftover"), "child.locked");
		  OpenFile = new RandomAccessFile(File, "rw");

		  Parent = LuceneTestCase.BaseTempDirForTestClass;
		}
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testWindowsUnremovableFile() throws java.io.IOException
	  public virtual void TestWindowsUnremovableFile()
	  {
		RandomizedTest.assumeTrue("Requires Windows.", Constants.WINDOWS);
		RandomizedTest.assumeFalse(LuceneTestCase.LEAVE_TEMPORARY);

		Result r = JUnitCore.runClasses(typeof(Nested2));
		Assert.AreEqual(1, r.FailureCount);

		Nested2.OpenFile.Dispose();
		TestUtil.rm(Nested2.Parent);
	  }
	}

}