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

	using Assert = org.junit.Assert;
	using Test = org.junit.Test;
	using JUnitCore = org.junit.runner.JUnitCore;

	using Repeat = com.carrotsearch.randomizedtesting.annotations.Repeat;

	/// <summary>
	/// Test reproduce message is right with <seealso cref="Repeat"/> annotation.
	/// </summary>
	public class TestReproduceMessageWithRepeated : WithNestedTests
	{
	  public class Nested : AbstractNestedTest
	  {
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test @Repeat(iterations = 10) public void testMe()
		public virtual void TestMe()
		{
		  throw new Exception("bad");
		}
	  }

	  public TestReproduceMessageWithRepeated() : base(true)
	  {
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testRepeatedMessage() throws Exception
	  public virtual void TestRepeatedMessage()
	  {
		string syserr = RunAndReturnSyserr();
		Assert.IsTrue(syserr.Contains(" -Dtests.method=testMe "));
	  }

	  private string RunAndReturnSyserr()
	  {
		JUnitCore.runClasses(typeof(Nested));
		string err = SysErr;
		return err;
	  }
	}

}