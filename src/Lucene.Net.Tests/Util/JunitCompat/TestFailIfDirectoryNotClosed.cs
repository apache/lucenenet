using System;
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

	using Directory = Lucene.Net.Store.Directory;
	/*using Assert = org.junit.Assert;
	using Test = org.junit.Test;
	using JUnitCore = org.junit.runner.JUnitCore;
	using Result = org.junit.runner.Result;
	using Failure = org.junit.runner.notification.Failure;

	using RandomizedTest = com.carrotsearch.randomizedtesting.RandomizedTest;*/

	public class TestFailIfDirectoryNotClosed : WithNestedTests
	{
	  public TestFailIfDirectoryNotClosed() : base(true)
	  {
	  }

	  public class Nested1 : WithNestedTests.AbstractNestedTest
	  {
		public virtual void TestDummy()
		{
		  Directory dir = NewDirectory();
		  Console.WriteLine(dir.ToString());
		}
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testFailIfDirectoryNotClosed()
	  public virtual void TestFailIfDirectoryNotClosed()
	  {
		Result r = JUnitCore.runClasses(typeof(Nested1));
		RandomizedTest.assumeTrue("Ignoring nested test, very likely zombie threads present.", r.IgnoreCount == 0);

		foreach (Failure f in r.Failures)
		{
		  Console.WriteLine("Failure: " + f);
		}
		Assert.AreEqual(1, r.FailureCount);
		Assert.IsTrue(r.Failures.Get(0).ToString().Contains("Resource in scope SUITE failed to close"));
	  }
	}

}