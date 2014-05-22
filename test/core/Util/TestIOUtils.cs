using System;

namespace Lucene.Net.Util
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


	public class TestIOUtils : LuceneTestCase
	{

	  internal sealed class BrokenIDisposable : IDisposable
	  {
		internal readonly int i;

		public BrokenIDisposable(int i)
		{
		  this.i = i;
		}

		public override void Close()
		{
		  throw new IOException("TEST-IO-EXCEPTION-" + i);
		}
	  }

	  internal sealed class TestException : Exception
	  {
		public TestException() : base("BASE-EXCEPTION")
		{
		}
	  }

	  public virtual void TestSuppressedExceptions()
	  {
		// test with prior exception
		try
		{
		  TestException t = new TestException();
		  IOUtils.closeWhileHandlingException(t, new BrokenIDisposable(1), new BrokenIDisposable(2));
		}
		catch (TestException e1)
		{
		  Assert.AreEqual("BASE-EXCEPTION", e1.Message);
		  StringWriter sw = new StringWriter();
		  PrintWriter pw = new PrintWriter(sw);
		  e1.printStackTrace(pw);
		  pw.flush();
		  string trace = sw.ToString();
		  if (VERBOSE)
		  {
			Console.WriteLine("TestIOUtils.testSuppressedExceptions: Thrown Exception stack trace:");
			Console.WriteLine(trace);
		  }
		  Assert.IsTrue("Stack trace does not contain first suppressed Exception: " + trace, trace.Contains("java.io.IOException: TEST-IO-EXCEPTION-1"));
		  Assert.IsTrue("Stack trace does not contain second suppressed Exception: " + trace, trace.Contains("java.io.IOException: TEST-IO-EXCEPTION-2"));
		}
		catch (IOException e2)
		{
		  Assert.Fail("IOException should not be thrown here");
		}

		// test without prior exception
		try
		{
		  IOUtils.closeWhileHandlingException((TestException) null, new BrokenIDisposable(1), new BrokenIDisposable(2));
		}
		catch (TestException e1)
		{
		  Assert.Fail("TestException should not be thrown here");
		}
		catch (IOException e2)
		{
		  Assert.AreEqual("TEST-IO-EXCEPTION-1", e2.Message);
		  StringWriter sw = new StringWriter();
		  PrintWriter pw = new PrintWriter(sw);
		  e2.printStackTrace(pw);
		  pw.flush();
		  string trace = sw.ToString();
		  if (VERBOSE)
		  {
			Console.WriteLine("TestIOUtils.testSuppressedExceptions: Thrown Exception stack trace:");
			Console.WriteLine(trace);
		  }
		  Assert.IsTrue("Stack trace does not contain suppressed Exception: " + trace, trace.Contains("java.io.IOException: TEST-IO-EXCEPTION-2"));
		}
	  }

	}

}