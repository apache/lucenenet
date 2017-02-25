using System;

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

	using org.junit;
	using TestRule = org.junit.rules.TestRule;
	using JUnitCore = org.junit.runner.JUnitCore;
	using Result = org.junit.runner.Result;
	using Failure = org.junit.runner.notification.Failure;

	using SystemPropertiesInvariantRule = com.carrotsearch.randomizedtesting.rules.SystemPropertiesInvariantRule;
	using SystemPropertiesRestoreRule = com.carrotsearch.randomizedtesting.rules.SystemPropertiesRestoreRule;

	/// <seealso cref= SystemPropertiesRestoreRule </seealso>
	/// <seealso cref= SystemPropertiesInvariantRule </seealso>
	public class TestSystemPropertiesInvariantRule : WithNestedTests
	{
	  public const string PROP_KEY1 = "new-property-1";
	  public const string VALUE1 = "new-value-1";

	  public TestSystemPropertiesInvariantRule() : base(true)
	  {
	  }

	  public class Base : WithNestedTests.AbstractNestedTest
	  {
		public virtual void TestEmpty()
		{
		}
	  }

	  public class InBeforeClass : Base
	  {
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @BeforeClass public static void beforeClass()
		public static void BeforeClass()
		{
		  System.setProperty(PROP_KEY1, VALUE1);
		}
	  }

	  public class InAfterClass : Base
	  {
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @AfterClass public static void afterClass()
		public static void AfterClass()
		{
		  System.setProperty(PROP_KEY1, VALUE1);
		}
	  }

	  public class InTestMethod : Base
	  {
		public virtual void TestMethod1()
		{
		  if (System.getProperty(PROP_KEY1) != null)
		  {
			throw new Exception("Shouldn't be here.");
		  }
		  System.setProperty(PROP_KEY1, VALUE1);
		}

		public virtual void TestMethod2()
		{
		  TestMethod1();
		}
	  }

	  public class NonStringProperties : Base
	  {
		public virtual void TestMethod1()
		{
		  if (System.Properties.Get(PROP_KEY1) != null)
		  {
			throw new Exception("Will pass.");
		  }

		  Properties properties = System.Properties;
		  properties.put(PROP_KEY1, new object());
		  Assert.IsTrue(System.Properties.Get(PROP_KEY1) != null);
		}

		public virtual void TestMethod2()
		{
		  TestMethod1();
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @AfterClass public static void cleanup()
		public static void Cleanup()
		{
		  System.Properties.remove(PROP_KEY1);
		}
	  }

	  public class IgnoredProperty
	  {
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Rule public org.junit.rules.TestRule invariant = new com.carrotsearch.randomizedtesting.rules.SystemPropertiesInvariantRule(PROP_KEY1);
		public TestRule Invariant = new SystemPropertiesInvariantRule(PROP_KEY1);

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testMethod1()
		public virtual void TestMethod1()
		{
		  System.setProperty(PROP_KEY1, VALUE1);
		}
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Before @After public void cleanup()
	  public virtual void Cleanup()
	  {
		System.clearProperty(PROP_KEY1);
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testRuleInvariantBeforeClass()
	  public virtual void TestRuleInvariantBeforeClass()
	  {
		Result runClasses = JUnitCore.runClasses(typeof(InBeforeClass));
		Assert.AreEqual(1, runClasses.FailureCount);
		Assert.IsTrue(runClasses.Failures.Get(0).Message.Contains(PROP_KEY1));
		Assert.IsNull(System.getProperty(PROP_KEY1));
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testRuleInvariantAfterClass()
	  public virtual void TestRuleInvariantAfterClass()
	  {
		Result runClasses = JUnitCore.runClasses(typeof(InAfterClass));
		Assert.AreEqual(1, runClasses.FailureCount);
		Assert.IsTrue(runClasses.Failures.Get(0).Message.Contains(PROP_KEY1));
		Assert.IsNull(System.getProperty(PROP_KEY1));
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testRuleInvariantInTestMethod()
	  public virtual void TestRuleInvariantInTestMethod()
	  {
		Result runClasses = JUnitCore.runClasses(typeof(InTestMethod));
		Assert.AreEqual(2, runClasses.FailureCount);
		foreach (Failure f in runClasses.Failures)
		{
		  Assert.IsTrue(f.Message.Contains(PROP_KEY1));
		}
		Assert.IsNull(System.getProperty(PROP_KEY1));
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testNonStringProperties()
	  public virtual void TestNonStringProperties()
	  {
		Result runClasses = JUnitCore.runClasses(typeof(NonStringProperties));
		Assert.AreEqual(1, runClasses.FailureCount);
		Assert.IsTrue(runClasses.Failures.Get(0).Message.Contains("Will pass"));
		Assert.AreEqual(3, runClasses.RunCount);
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testIgnoredProperty()
	  public virtual void TestIgnoredProperty()
	  {
		System.clearProperty(PROP_KEY1);
		try
		{
		  Result runClasses = JUnitCore.runClasses(typeof(IgnoredProperty));
		  Assert.AreEqual(0, runClasses.FailureCount);
		  Assert.AreEqual(VALUE1, System.getProperty(PROP_KEY1));
		}
		finally
		{
		  System.clearProperty(PROP_KEY1);
		}
	  }
	}

}