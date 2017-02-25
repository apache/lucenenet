using System;

namespace Lucene.Net.Util.JunitCompat
{

	using org.junit;
	using RuleChain = org.junit.rules.RuleChain;
	using TestRule = org.junit.rules.TestRule;
	using JUnitCore = org.junit.runner.JUnitCore;
	using Result = org.junit.runner.Result;

	using RandomizedContext = com.carrotsearch.randomizedtesting.RandomizedContext;
	using SystemPropertiesRestoreRule = com.carrotsearch.randomizedtesting.rules.SystemPropertiesRestoreRule;

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

	public class TestSameRandomnessLocalePassedOrNot : WithNestedTests
	{
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @ClassRule public static org.junit.rules.TestRule solrClassRules = org.junit.rules.RuleChain.outerRule(new com.carrotsearch.randomizedtesting.rules.SystemPropertiesRestoreRule());
	  public static TestRule SolrClassRules = RuleChain.outerRule(new SystemPropertiesRestoreRule());

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Rule public org.junit.rules.TestRule solrTestRules = org.junit.rules.RuleChain.outerRule(new com.carrotsearch.randomizedtesting.rules.SystemPropertiesRestoreRule());
	  public TestRule SolrTestRules = RuleChain.outerRule(new SystemPropertiesRestoreRule());

	  public TestSameRandomnessLocalePassedOrNot() : base(true)
	  {
	  }

	  public class Nested : WithNestedTests.AbstractNestedTest
	  {
		public static string PickString;
		public static Locale DefaultLocale;
		public static TimeZone DefaultTimeZone;
		public static string Seed;

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @BeforeClass public static void setup()
		public static void Setup()
		{
		  Seed = RandomizedContext.Current().RunnerSeedAsString;

		  Random rnd = Random();
		  PickString = TestUtil.randomSimpleString(rnd);

		  DefaultLocale = Locale.Default;
		  DefaultTimeZone = TimeZone.Default;
		}

		public virtual void TestPassed()
		{
		  Console.WriteLine("Picked locale: " + DefaultLocale);
		  Console.WriteLine("Picked timezone: " + DefaultTimeZone.ID);
		}
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testSetupWithoutLocale()
	  public virtual void TestSetupWithoutLocale()
	  {
		Result runClasses = JUnitCore.runClasses(typeof(Nested));
		Assert.AreEqual(0, runClasses.FailureCount);

		string s1 = Nested.PickString;
		System.setProperty("tests.seed", Nested.Seed);
		System.setProperty("tests.timezone", Nested.DefaultTimeZone.ID);
		System.setProperty("tests.locale", Nested.DefaultLocale.ToString());
		JUnitCore.runClasses(typeof(Nested));
		string s2 = Nested.PickString;

		Assert.AreEqual(s1, s2);
	  }
	}

}