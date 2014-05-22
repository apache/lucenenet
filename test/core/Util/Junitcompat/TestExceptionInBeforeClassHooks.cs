using System;
using System.Collections.Generic;
using System.Threading;

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


	using Assert = junit.framework.Assert;

	using Before = org.junit.Before;
	using BeforeClass = org.junit.BeforeClass;
	using Test = org.junit.Test;
	using JUnitCore = org.junit.runner.JUnitCore;
	using Result = org.junit.runner.Result;
	using Failure = org.junit.runner.notification.Failure;

	public class TestExceptionInBeforeClassHooks : WithNestedTests
	{
	  public TestExceptionInBeforeClassHooks() : base(true)
	  {
	  }

	  public class Nested1 : WithNestedTests.AbstractNestedTest
	  {
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @BeforeClass public static void beforeClass() throws Exception
		public static void BeforeClass()
		{
		  Thread t = new ThreadAnonymousInnerClassHelper();
		  t.Start();
		  t.Join();
		}

		private class ThreadAnonymousInnerClassHelper : System.Threading.Thread
		{
			public ThreadAnonymousInnerClassHelper()
			{
			}

			public override void Run()
			{
			  throw new Exception("foobar");
			}
		}

		public virtual void Test()
		{
		}
	  }

	  public class Nested2 : WithNestedTests.AbstractNestedTest
	  {
		public virtual void Test1()
		{
		  Thread t = new ThreadAnonymousInnerClassHelper(this);
		  t.Start();
		  t.Join();
		}

		private class ThreadAnonymousInnerClassHelper : System.Threading.Thread
		{
			private readonly Nested2 OuterInstance;

			public ThreadAnonymousInnerClassHelper(Nested2 outerInstance)
			{
				this.OuterInstance = outerInstance;
			}

			public override void Run()
			{
			  throw new Exception("foobar1");
			}
		}

		public virtual void Test2()
		{
		  Thread t = new ThreadAnonymousInnerClassHelper2(this);
		  t.Start();
		  t.Join();
		}

		private class ThreadAnonymousInnerClassHelper2 : System.Threading.Thread
		{
			private readonly Nested2 OuterInstance;

			public ThreadAnonymousInnerClassHelper2(Nested2 outerInstance)
			{
				this.OuterInstance = outerInstance;
			}

			public override void Run()
			{
			  throw new Exception("foobar2");
			}
		}

		public virtual void Test3()
		{
		  Thread t = new ThreadAnonymousInnerClassHelper3(this);
		  t.Start();
		  t.Join();
		}

		private class ThreadAnonymousInnerClassHelper3 : System.Threading.Thread
		{
			private readonly Nested2 OuterInstance;

			public ThreadAnonymousInnerClassHelper3(Nested2 outerInstance)
			{
				this.OuterInstance = outerInstance;
			}

			public override void Run()
			{
			  throw new Exception("foobar3");
			}
		}
	  }

	  public class Nested3 : WithNestedTests.AbstractNestedTest
	  {
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Before public void runBeforeTest() throws Exception
		public virtual void RunBeforeTest()
		{
		  Thread t = new ThreadAnonymousInnerClassHelper(this);
		  t.Start();
		  t.Join();
		}

		private class ThreadAnonymousInnerClassHelper : System.Threading.Thread
		{
			private readonly Nested3 OuterInstance;

			public ThreadAnonymousInnerClassHelper(Nested3 outerInstance)
			{
				this.OuterInstance = outerInstance;
			}

			public override void Run()
			{
			  throw new Exception("foobar");
			}
		}

		public virtual void Test1()
		{
		}
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testExceptionInBeforeClassFailsTheTest()
	  public virtual void TestExceptionInBeforeClassFailsTheTest()
	  {
		Result runClasses = JUnitCore.runClasses(typeof(Nested1));
		Assert.Assert.AreEqual(1, runClasses.FailureCount);
		Assert.Assert.AreEqual(1, runClasses.RunCount);
		Assert.Assert.IsTrue(runClasses.Failures.get(0).Trace.contains("foobar"));
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testExceptionWithinTestFailsTheTest()
	  public virtual void TestExceptionWithinTestFailsTheTest()
	  {
		Result runClasses = JUnitCore.runClasses(typeof(Nested2));
		Assert.Assert.AreEqual(3, runClasses.FailureCount);
		Assert.Assert.AreEqual(3, runClasses.RunCount);

		List<string> foobars = new List<string>();
		foreach (Failure f in runClasses.Failures)
		{
		  Matcher m = Pattern.compile("foobar[0-9]+").matcher(f.Trace);
		  while (m.find())
		  {
			foobars.Add(m.group());
		  }
		}

		foobars.Sort();
		Assert.Assert.AreEqual("[foobar1, foobar2, foobar3]", Arrays.ToString(foobars.ToArray()));
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testExceptionWithinBefore()
	  public virtual void TestExceptionWithinBefore()
	  {
		Result runClasses = JUnitCore.runClasses(typeof(Nested3));
		Assert.Assert.AreEqual(1, runClasses.FailureCount);
		Assert.Assert.AreEqual(1, runClasses.RunCount);
		Assert.Assert.IsTrue(runClasses.Failures.get(0).Trace.contains("foobar"));
	  }

	}

}