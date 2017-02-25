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

	/*using Assert = org.junit.Assert;
	using Test = org.junit.Test;
	using JUnitCore = org.junit.runner.JUnitCore;
	using Result = org.junit.runner.Result;
	using Failure = org.junit.runner.notification.Failure;
    */
	/// <summary>
	/// Ensures proper functions of <seealso cref="LuceneTestCase#setUp()"/>
	/// and <seealso cref="LuceneTestCase#tearDown()"/>.
	/// </summary>
	public class TestSetupTeardownChaining : WithNestedTests
	{
	  public class NestedSetupChain : AbstractNestedTest
	  {
		public override void SetUp()
		{
		  // missing call.
		  Console.WriteLine("Hello.");
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testMe()
		public virtual void TestMe()
		{
		}
	  }

	  public class NestedTeardownChain : AbstractNestedTest
	  {
		public override void TearDown()
		{
		  // missing call.
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testMe()
		public virtual void TestMe()
		{
		}
	  }

	  public TestSetupTeardownChaining() : base(true)
	  {
	  }

	  /// <summary>
	  /// Verify super method calls on <seealso cref="LuceneTestCase#setUp()"/>.
	  /// </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testSetupChaining()
	  public virtual void TestSetupChaining()
	  {
		Result result = JUnitCore.runClasses(typeof(NestedSetupChain));
		Assert.AreEqual(1, result.FailureCount);
		Failure failure = result.Failures.Get(0);
		Assert.IsTrue(failure.Message.Contains("One of the overrides of setUp does not propagate the call."));
	  }

	  /// <summary>
	  /// Verify super method calls on <seealso cref="LuceneTestCase#tearDown()"/>.
	  /// </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testTeardownChaining()
	  public virtual void TestTeardownChaining()
	  {
		Result result = JUnitCore.runClasses(typeof(NestedTeardownChain));
		Assert.AreEqual(1, result.FailureCount);
		Failure failure = result.Failures.Get(0);
		Assert.IsTrue(failure.Message.Contains("One of the overrides of tearDown does not propagate the call."));
	  }
	}

}