using System;
using System.Threading;
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

	using Assert = org.junit.Assert;
	using Test = org.junit.Test;
	using JUnitCore = org.junit.runner.JUnitCore;
	using Result = org.junit.runner.Result;
	using Failure = org.junit.runner.notification.Failure;

	/// <summary>
	/// Check that uncaught exceptions result in seed info being dumped to
	/// console. 
	/// </summary>
	public class TestSeedFromUncaught : WithNestedTests
	{
	  public class ThrowInUncaught : AbstractNestedTest
	  {
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testFoo() throws Exception
		public virtual void TestFoo()
		{
		  ThreadClass t = new ThreadAnonymousInnerClassHelper(this);
		  t.Start();
		  t.Join();
		}

		private class ThreadAnonymousInnerClassHelper : ThreadClass
		{
			private readonly ThrowInUncaught OuterInstance;

			public ThreadAnonymousInnerClassHelper(ThrowInUncaught outerInstance)
			{
				this.OuterInstance = outerInstance;
			}

			public override void Run()
			{
			  throw new Exception("foobar");
			}
		}
	  }

	  public TestSeedFromUncaught() : base(true) // suppress normal output.
	  {
	  }

	  /// <summary>
	  /// Verify super method calls on <seealso cref="LuceneTestCase#setUp()"/>.
	  /// </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testUncaughtDumpsSeed()
	  public virtual void TestUncaughtDumpsSeed()
	  {
		Result result = JUnitCore.runClasses(typeof(ThrowInUncaught));
		Assert.AreEqual(1, result.FailureCount);
		Failure f = result.Failures.Get(0);
		string trace = f.Trace;
		Assert.IsTrue(trace.Contains("SeedInfo.seed("));
		Assert.IsTrue(trace.Contains("foobar"));
	  }
	}

}