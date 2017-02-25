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
	/// Test reproduce message is right.
	/// </summary>
	public class TestReproduceMessage : WithNestedTests
	{
	  public static SorePoint @where;
	  public static SoreType Type;

	  public class Nested : AbstractNestedTest
	  {
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @BeforeClass public static void beforeClass()
		public static void BeforeClass()
		{
		  if (RunningNested)
		  {
			TriggerOn(SorePoint.BEFORE_CLASS);
		  }
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Rule public org.junit.rules.TestRule rule = new TestRuleAnonymousInnerClassHelper();
		public TestRule rule = new TestRuleAnonymousInnerClassHelper();

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
				TriggerOn(SorePoint.RULE);
				@base.evaluate();
			  }
		  }
		}

		/// <summary>
		/// Class initializer block/ default constructor. </summary>
		public Nested()
		{
		  TriggerOn(SorePoint.INITIALIZER);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Before public void before()
		public virtual void Before()
		{
		  TriggerOn(SorePoint.BEFORE);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void test()
		public virtual void Test()
		{
		  TriggerOn(SorePoint.TEST);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @After public void after()
		public virtual void After()
		{
		  TriggerOn(SorePoint.AFTER);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @AfterClass public static void afterClass()
		public static void AfterClass()
		{
		  if (RunningNested)
		  {
			TriggerOn(SorePoint.AFTER_CLASS);
		  }
		}

		internal static void TriggerOn(SorePoint pt)
		{
		  if (pt == @where)
		  {
			switch (Type)
			{
			  case Lucene.Net.Util.junitcompat.SoreType.ASSUMPTION:
				LuceneTestCase.assumeTrue(pt.ToString(), false);
				throw new Exception("unreachable");
			  case Lucene.Net.Util.junitcompat.SoreType.ERROR:
				throw new Exception(pt.ToString());
			  case Lucene.Net.Util.junitcompat.SoreType.FAILURE:
				Assert.IsTrue(pt.ToString(), false);
				throw new Exception("unreachable");
			}
		  }
		}
	  }

	  /*
	   * ASSUMPTIONS.
	   */

	  public TestReproduceMessage() : base(true)
	  {
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testAssumeBeforeClass() throws Exception
	  public virtual void TestAssumeBeforeClass()
	  {
		Type = SoreType.ASSUMPTION;
		@where = SorePoint.BEFORE_CLASS;
		Assert.IsTrue(RunAndReturnSyserr().Length == 0);
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testAssumeInitializer() throws Exception
	  public virtual void TestAssumeInitializer()
	  {
		Type = SoreType.ASSUMPTION;
		@where = SorePoint.INITIALIZER;
		Assert.IsTrue(RunAndReturnSyserr().Length == 0);
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testAssumeRule() throws Exception
	  public virtual void TestAssumeRule()
	  {
		Type = SoreType.ASSUMPTION;
		@where = SorePoint.RULE;
		Assert.AreEqual("", RunAndReturnSyserr());
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testAssumeBefore() throws Exception
	  public virtual void TestAssumeBefore()
	  {
		Type = SoreType.ASSUMPTION;
		@where = SorePoint.BEFORE;
		Assert.IsTrue(RunAndReturnSyserr().Length == 0);
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testAssumeTest() throws Exception
	  public virtual void TestAssumeTest()
	  {
		Type = SoreType.ASSUMPTION;
		@where = SorePoint.TEST;
		Assert.IsTrue(RunAndReturnSyserr().Length == 0);
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testAssumeAfter() throws Exception
	  public virtual void TestAssumeAfter()
	  {
		Type = SoreType.ASSUMPTION;
		@where = SorePoint.AFTER;
		Assert.IsTrue(RunAndReturnSyserr().Length == 0);
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testAssumeAfterClass() throws Exception
	  public virtual void TestAssumeAfterClass()
	  {
		Type = SoreType.ASSUMPTION;
		@where = SorePoint.AFTER_CLASS;
		Assert.IsTrue(RunAndReturnSyserr().Length == 0);
	  }

	  /*
	   * FAILURES
	   */

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testFailureBeforeClass() throws Exception
	  public virtual void TestFailureBeforeClass()
	  {
		Type = SoreType.FAILURE;
		@where = SorePoint.BEFORE_CLASS;
		Assert.IsTrue(RunAndReturnSyserr().Contains("NOTE: reproduce with:"));
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testFailureInitializer() throws Exception
	  public virtual void TestFailureInitializer()
	  {
		Type = SoreType.FAILURE;
		@where = SorePoint.INITIALIZER;
		Assert.IsTrue(RunAndReturnSyserr().Contains("NOTE: reproduce with:"));
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testFailureRule() throws Exception
	  public virtual void TestFailureRule()
	  {
		Type = SoreType.FAILURE;
		@where = SorePoint.RULE;

		string syserr = RunAndReturnSyserr();

		Assert.IsTrue(syserr.Contains("NOTE: reproduce with:"));
		Assert.IsTrue(Arrays.AsList(syserr.Split("\\s", true)).Contains("-Dtests.method=test"));
		Assert.IsTrue(Arrays.AsList(syserr.Split("\\s", true)).Contains("-Dtestcase=" + typeof(Nested).SimpleName));
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testFailureBefore() throws Exception
	  public virtual void TestFailureBefore()
	  {
		Type = SoreType.FAILURE;
		@where = SorePoint.BEFORE;
		string syserr = RunAndReturnSyserr();
		Assert.IsTrue(syserr.Contains("NOTE: reproduce with:"));
		Assert.IsTrue(Arrays.AsList(syserr.Split("\\s", true)).Contains("-Dtests.method=test"));
		Assert.IsTrue(Arrays.AsList(syserr.Split("\\s", true)).Contains("-Dtestcase=" + typeof(Nested).SimpleName));
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testFailureTest() throws Exception
	  public virtual void TestFailureTest()
	  {
		Type = SoreType.FAILURE;
		@where = SorePoint.TEST;
		string syserr = RunAndReturnSyserr();
		Assert.IsTrue(syserr.Contains("NOTE: reproduce with:"));
		Assert.IsTrue(Arrays.AsList(syserr.Split("\\s", true)).Contains("-Dtests.method=test"));
		Assert.IsTrue(Arrays.AsList(syserr.Split("\\s", true)).Contains("-Dtestcase=" + typeof(Nested).SimpleName));
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testFailureAfter() throws Exception
	  public virtual void TestFailureAfter()
	  {
		Type = SoreType.FAILURE;
		@where = SorePoint.AFTER;
		string syserr = RunAndReturnSyserr();
		Assert.IsTrue(syserr.Contains("NOTE: reproduce with:"));
		Assert.IsTrue(Arrays.AsList(syserr.Split("\\s", true)).Contains("-Dtests.method=test"));
		Assert.IsTrue(Arrays.AsList(syserr.Split("\\s", true)).Contains("-Dtestcase=" + typeof(Nested).SimpleName));
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testFailureAfterClass() throws Exception
	  public virtual void TestFailureAfterClass()
	  {
		Type = SoreType.FAILURE;
		@where = SorePoint.AFTER_CLASS;
		Assert.IsTrue(RunAndReturnSyserr().Contains("NOTE: reproduce with:"));
	  }

	  /*
	   * ERRORS
	   */

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testErrorBeforeClass() throws Exception
	  public virtual void TestErrorBeforeClass()
	  {
		Type = SoreType.ERROR;
		@where = SorePoint.BEFORE_CLASS;
		Assert.IsTrue(RunAndReturnSyserr().Contains("NOTE: reproduce with:"));
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testErrorInitializer() throws Exception
	  public virtual void TestErrorInitializer()
	  {
		Type = SoreType.ERROR;
		@where = SorePoint.INITIALIZER;
		Assert.IsTrue(RunAndReturnSyserr().Contains("NOTE: reproduce with:"));
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testErrorRule() throws Exception
	  public virtual void TestErrorRule()
	  {
		Type = SoreType.ERROR;
		@where = SorePoint.RULE;
		string syserr = RunAndReturnSyserr();
		Assert.IsTrue(syserr.Contains("NOTE: reproduce with:"));
		Assert.IsTrue(Arrays.AsList(syserr.Split("\\s", true)).Contains("-Dtests.method=test"));
		Assert.IsTrue(Arrays.AsList(syserr.Split("\\s", true)).Contains("-Dtestcase=" + typeof(Nested).SimpleName));
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testErrorBefore() throws Exception
	  public virtual void TestErrorBefore()
	  {
		Type = SoreType.ERROR;
		@where = SorePoint.BEFORE;
		string syserr = RunAndReturnSyserr();
		Assert.IsTrue(syserr.Contains("NOTE: reproduce with:"));
		Assert.IsTrue(Arrays.AsList(syserr.Split("\\s", true)).Contains("-Dtests.method=test"));
		Assert.IsTrue(Arrays.AsList(syserr.Split("\\s", true)).Contains("-Dtestcase=" + typeof(Nested).SimpleName));
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testErrorTest() throws Exception
	  public virtual void TestErrorTest()
	  {
		Type = SoreType.ERROR;
		@where = SorePoint.TEST;
		string syserr = RunAndReturnSyserr();
		Assert.IsTrue(syserr.Contains("NOTE: reproduce with:"));
		Assert.IsTrue(Arrays.AsList(syserr.Split("\\s", true)).Contains("-Dtests.method=test"));
		Assert.IsTrue(Arrays.AsList(syserr.Split("\\s", true)).Contains("-Dtestcase=" + typeof(Nested).SimpleName));
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testErrorAfter() throws Exception
	  public virtual void TestErrorAfter()
	  {
		Type = SoreType.ERROR;
		@where = SorePoint.AFTER;
		string syserr = RunAndReturnSyserr();
		Assert.IsTrue(syserr.Contains("NOTE: reproduce with:"));
		Assert.IsTrue(Arrays.AsList(syserr.Split("\\s", true)).Contains("-Dtests.method=test"));
		Assert.IsTrue(Arrays.AsList(syserr.Split("\\s", true)).Contains("-Dtestcase=" + typeof(Nested).SimpleName));
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testErrorAfterClass() throws Exception
	  public virtual void TestErrorAfterClass()
	  {
		Type = SoreType.ERROR;
		@where = SorePoint.AFTER_CLASS;
		Assert.IsTrue(RunAndReturnSyserr().Contains("NOTE: reproduce with:"));
	  }

	  private string RunAndReturnSyserr()
	  {
		JUnitCore.runClasses(typeof(Nested));

		string err = SysErr;
		// super.prevSysErr.println("Type: " + type + ", point: " + where + " resulted in:\n" + err);
		// super.prevSysErr.println("---");
		return err;
	  }
	}

}