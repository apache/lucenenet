using NUnit.Framework;

namespace Lucene.Net.Util.JunitCompat
{

	using After = org.junit.After;
	using Assert = org.junit.Assert;
	using Before = org.junit.Before;
	using Test = org.junit.Test;
	using JUnitCore = org.junit.runner.JUnitCore;
	using Result = org.junit.runner.Result;

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

	public class TestBeforeAfterOverrides : WithNestedTests
	{
	  public TestBeforeAfterOverrides() : base(true)
	  {
	  }

	  public class Before1 : WithNestedTests.AbstractNestedTest
	  {
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Before public void before()
		public virtual void Before()
		{
		}

		public virtual void TestEmpty()
		{
		}
	  }
	  public class Before2 : Before1
	  {
	  }
	  public class Before3 : Before2
	  {
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Override @Before public void before()
		public override void Before()
		{
		}
	  }

	  public class After1 : WithNestedTests.AbstractNestedTest
	  {
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @After public void after()
		public virtual void After()
		{
		}

		public virtual void TestEmpty()
		{
		}
	  }
	  public class After2 : Before1
	  {
	  }
	  public class After3 : Before2
	  {
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @After public void after()
		public virtual void After()
		{
		}
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testBefore()
	  public virtual void TestBefore()
	  {
		Result result = JUnitCore.runClasses(typeof(Before3));
		Assert.AreEqual(1, result.FailureCount);
		Assert.IsTrue(result.Failures.Get(0).Trace.Contains("There are overridden methods"));
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testAfter()
	  public virtual void TestAfter()
	  {
		Result result = JUnitCore.runClasses(typeof(Before3));
		Assert.AreEqual(1, result.FailureCount);
		Assert.IsTrue(result.Failures.Get(0).Trace.Contains("There are overridden methods"));
	  }
	}

}