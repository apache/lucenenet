using System.Collections;
using System.Collections.Generic;

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


	using After = org.junit.After;
	using AfterClass = org.junit.AfterClass;
	using Assert = org.junit.Assert;
	using Before = org.junit.Before;
	using BeforeClass = org.junit.BeforeClass;
	using Rule = org.junit.Rule;
	using Test = org.junit.Test;
	using TestRule = org.junit.rules.TestRule;
	using Description = org.junit.runner.Description;
	using JUnitCore = org.junit.runner.JUnitCore;
	using Statement = org.junit.runners.model.Statement;

	/// <summary>
	/// this verifies that JUnit <seealso cref="Rule"/>s are invoked before 
	/// <seealso cref="Before"/> and <seealso cref=" After"/> hooks. this should be the
	/// case from JUnit 4.10 on.
	/// </summary>
	public class TestJUnitRuleOrder : WithNestedTests
	{
	  internal static Stack<string> Stack;

	  public TestJUnitRuleOrder() : base(true)
	  {
	  }

	  public class Nested : WithNestedTests.AbstractNestedTest
	  {
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Before public void before()
		public virtual void Before()
		{
		  Stack.Push("@Before");
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @After public void after()
		public virtual void After()
		{
		  Stack.Push("@After");
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Rule public org.junit.rules.TestRule testRule = new TestRuleAnonymousInnerClassHelper();
		public TestRule testRule = new TestRuleAnonymousInnerClassHelper();

		private class TestRuleAnonymousInnerClassHelper : TestRule
		{
			public TestRuleAnonymousInnerClassHelper()
			{
			}

			public override Statement Apply(Statement @base, Description description)
			{
			return new StatementAnonymousInnerClassHelper(this, @base);
			}

		  private class StatementAnonymousInnerClassHelper : Statement
		  {
			  private readonly TestRuleAnonymousInnerClassHelper OuterInstance;

			  private Statement @base;

			  public StatementAnonymousInnerClassHelper(TestRuleAnonymousInnerClassHelper outerInstance, Statement @base)
			  {
				  this.outerInstance = outerInstance;
				  this.@base = @base;
			  }

			  public override void Evaluate()
			  {
				Stack.Push("@Rule before");
				@base.evaluate();
				Stack.Push("@Rule after");
			  }
		  }
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void test()
		public virtual void Test() // empty
		{
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @BeforeClass public static void beforeClassCleanup()
		public static void BeforeClassCleanup()
		{
		  Stack = new Stack<>();
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @AfterClass public static void afterClassCheck()
		public static void AfterClassCheck()
		{
		  Stack.Push("@AfterClass");
		}
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testRuleOrder()
	  public virtual void TestRuleOrder()
	  {
		JUnitCore.runClasses(typeof(Nested));
		Assert.AreEqual(Arrays.ToString(Stack.ToArray()), "[@Rule before, @Before, @After, @Rule after, @AfterClass]");
	  }
	}

}