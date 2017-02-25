using NUnit.Framework;

namespace Lucene.Net.Util.JunitCompat
{

	using Codec = Lucene.Net.Codecs.Codec;
	using Assert = org.junit.Assert;
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

	public class TestCodecReported : WithNestedTests
	{
	  public TestCodecReported() : base(true)
	  {
	  }

	  public class Nested1 : WithNestedTests.AbstractNestedTest
	  {
		public static string CodecName;

		public virtual void TestDummy()
		{
		  CodecName = Codec.Default.Name;
		  Assert.Fail();
		}
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testCorrectCodecReported()
	  public virtual void TestCorrectCodecReported()
	  {
		Result r = JUnitCore.runClasses(typeof(Nested1));
		Assert.AreEqual(1, r.FailureCount);
		Assert.IsTrue(base.SysErr().Contains("codec=" + Nested1.CodecName), base.SysErr());
	  }
	}

}