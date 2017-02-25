using System;
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
	using Assert = org.junit.Assert;
	using Assume = org.junit.Assume;
	using Before = org.junit.Before;
	using ClassRule = org.junit.ClassRule;
	using Rule = org.junit.Rule;
	using RuleChain = org.junit.rules.RuleChain;
	using TestRule = org.junit.rules.TestRule;
    /*
	using RandomizedRunner = com.carrotsearch.randomizedtesting.RandomizedRunner;
	using RandomizedTest = com.carrotsearch.randomizedtesting.RandomizedTest;
	using SysGlobals = com.carrotsearch.randomizedtesting.SysGlobals;
	using SystemPropertiesRestoreRule = com.carrotsearch.randomizedtesting.rules.SystemPropertiesRestoreRule;
	using TestRuleAdapter = com.carrotsearch.randomizedtesting.rules.TestRuleAdapter;*/

	/// <summary>
	/// An abstract test class that prepares nested test classes to run.
	/// A nested test class will assume it's executed under control of this
	/// class and be ignored otherwise. 
	/// 
	/// <p>The purpose of this is so that nested test suites don't run from
	/// IDEs like Eclipse (where they are automatically detected).
	/// 
	/// <p>this class cannot extend <seealso cref="LuceneTestCase"/> because in case
	/// there's a nested <seealso cref="LuceneTestCase"/> afterclass hooks run twice and
	/// cause havoc (static fields).
	/// </summary>
	public abstract class WithNestedTests
	{
		private bool InstanceFieldsInitialized = false;

		private void InitializeInstanceFields()
		{
			TestRuleMarkFailure marker = new TestRuleMarkFailure();
			Rules = RuleChain.outerRule(new SystemPropertiesRestoreRule()).around(new TestRuleAdapter()
			protected void afterAlways(IList<Exception> errors) throws Exception
			if (marker.hadFailures() && SuppressOutputStreams)
			{
			Console.WriteLine("sysout from nested test: " + SysOut + "\n");
			}
			Console.WriteLine("syserr from nested test: " + SysErr);
		})
			.around(marker);
            
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Before public final void before()
			public void before()
			if (SuppressOutputStreams)
			{
			PrevSysOut = System.out;
			}
			PrevSysErr = System.err;
            
			try
			{
			Sysout = new ByteArrayOutputStream();
			}
			System.Out = new PrintStream(Sysout, true, IOUtils.UTF_8);
			Syserr = new ByteArrayOutputStream();
			System.Err = new PrintStream(Syserr, true, IOUtils.UTF_8);
			catch (UnsupportedEncodingException e)
			{
			throw new Exception(e);
			}
            
			FailureMarker.resetFailures();
			System.setProperty(TestRuleIgnoreTestSuites.PROPERTY_RUN_NESTED, "true");
            
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @After public final void after()
			public void after()
			if (SuppressOutputStreams)
			{
			System.out.flush();
			}
			System.err.flush();
            
			System.Out = PrevSysOut;
			System.Err = PrevSysErr;
            
			protected string SysOut
			Assert.IsTrue(SuppressOutputStreams);
			System.out.flush();
			return new string(Sysout.toByteArray(), StandardCharsets.UTF_8);
            
			protected string SysErr
			Assert.IsTrue(SuppressOutputStreams);
			System.err.flush();
			return new string(Syserr.toByteArray(), StandardCharsets.UTF_8);
	}

	  public abstract class AbstractNestedTest : LuceneTestCase, TestRuleIgnoreTestSuites.NestedTestSuite
	  {
		protected internal static bool RunningNested
		{
			get
			{
			  return TestRuleIgnoreTestSuites.RunningNested;
			}
		}
	  }

	  private bool SuppressOutputStreams;

	  protected internal WithNestedTests(bool suppressOutputStreams)
	  {
		  if (!InstanceFieldsInitialized)
		  {
			  InitializeInstanceFields();
			  InstanceFieldsInitialized = true;
		  }
		this.SuppressOutputStreams = suppressOutputStreams;
	  }

	  protected internal PrintStream PrevSysErr;
	  protected internal PrintStream PrevSysOut;
	  private ByteArrayOutputStream Sysout;
	  private ByteArrayOutputStream Syserr;

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @ClassRule public static final org.junit.rules.TestRule classRules = org.junit.rules.RuleChain.outerRule(new TestRuleAdapterAnonymousInnerClassHelper());
	  public static readonly TestRule classRules = RuleChain.outerRule(new TestRuleAdapterAnonymousInnerClassHelper());

	  private class TestRuleAdapterAnonymousInnerClassHelper : TestRuleAdapter
	  {
		  public TestRuleAdapterAnonymousInnerClassHelper()
		  {
		  }

		  private TestRuleIgnoreAfterMaxFailures prevRule;

		  protected internal virtual void Before()
		  {
			if (!isPropertyEmpty(SysGlobals.SYSPROP_TESTFILTER()) || !isPropertyEmpty(SysGlobals.SYSPROP_TESTCLASS()) || !isPropertyEmpty(SysGlobals.SYSPROP_TESTMETHOD()) || !isPropertyEmpty(SysGlobals.SYSPROP_ITERATIONS()))
			{
			  // We're running with a complex test filter that is properly handled by classes
			  // which are executed by RandomizedRunner. The "outer" classes testing LuceneTestCase
			  // itself are executed by the default JUnit runner and would be always executed.
			  // We thus always skip execution if any filtering is detected.
			  Assume.assumeTrue(false);
			}

			// Check zombie threads from previous suites. Don't run if zombies are around.
			RandomizedTest.assumeFalse(RandomizedRunner.hasZombieThreads());

			TestRuleIgnoreAfterMaxFailures newRule = new TestRuleIgnoreAfterMaxFailures(int.MaxValue);
			prevRule = LuceneTestCase.replaceMaxFailureRule(newRule);
			RandomizedTest.assumeFalse(FailureMarker.hadFailures());
		  }

		  protected internal virtual void AfterAlways(IList<Exception> errors)
		  {
			if (prevRule != null)
			{
			  LuceneTestCase.replaceMaxFailureRule(prevRule);
			}
			FailureMarker.resetFailures();
		  }

		  private bool IsPropertyEmpty(string propertyName)
		  {
			string value = System.getProperty(propertyName);
			return value == null || value.Trim().Length == 0;
		  }
	  }

	  /// <summary>
	  /// Restore properties after test.
	  /// </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Rule public final org.junit.rules.TestRule rules;
	  public readonly TestRule Rules;

}