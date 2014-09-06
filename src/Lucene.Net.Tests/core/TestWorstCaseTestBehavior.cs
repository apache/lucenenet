using System;
using System.Threading;

namespace org.apache.lucene
{

	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	//using Ignore = org.junit.Ignore;

	//using RandomizedTest = com.carrotsearch.randomizedtesting.RandomizedTest;
	//using Timeout = com.carrotsearch.randomizedtesting.annotations.Timeout;

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

	public class TestWorstCaseTestBehavior : LuceneTestCase
	{
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Ignore public void testThreadLeak()
	  public virtual void TestThreadLeak()
	  {
		Thread t = new ThreadAnonymousInnerClassHelper(this);
		t.Start();

		while (!t.IsAlive)
		{
		  Thread.@Yield();
		}

		// once alive, leave it to run outside of the test scope.
	  }

	  private class ThreadAnonymousInnerClassHelper : Thread
	  {
		  private readonly TestWorstCaseTestBehavior OuterInstance;

		  public ThreadAnonymousInnerClassHelper(TestWorstCaseTestBehavior outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }

		  public override void Run()
		  {
			try
			{
			  Thread.Sleep(10000);
			}
			catch (InterruptedException e)
			{
			  // Ignore.
			}
		  }
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Ignore public void testLaaaaaargeOutput() throws Exception
	  public virtual void TestLaaaaaargeOutput()
	  {
		string message = "I will not OOM on large output";
		int howMuch = 250 * 1024 * 1024;
		for (int i = 0; i < howMuch; i++)
		{
		  if (i > 0)
		  {
			  Console.Write(",\n");
		  }
		  Console.Write(message);
		  howMuch -= message.Length; // approximately.
		}
		Console.WriteLine(".");
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Ignore public void testProgressiveOutput() throws Exception
	  public virtual void TestProgressiveOutput()
	  {
		for (int i = 0; i < 20; i++)
		{
		  Console.WriteLine("Emitting sysout line: " + i);
		  Console.Error.WriteLine("Emitting syserr line: " + i);
		  System.out.flush();
		  System.err.flush();
		  RandomizedTest.sleep(1000);
		}
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Ignore public void testUncaughtException() throws Exception
	  public virtual void TestUncaughtException()
	  {
		Thread t = new ThreadAnonymousInnerClassHelper2(this);
		t.Start();
		t.Join();
	  }

	  private class ThreadAnonymousInnerClassHelper2 : System.Threading.Thread
	  {
		  private readonly TestWorstCaseTestBehavior OuterInstance;

		  public ThreadAnonymousInnerClassHelper2(TestWorstCaseTestBehavior outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }

		  public override void Run()
		  {
			throw new Exception("foobar");
		  }
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Ignore @Timeout(millis = 500) public void testTimeout() throws Exception
	  public virtual void TestTimeout()
	  {
		Thread.Sleep(5000);
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Ignore @Timeout(millis = 1000) public void testZombie() throws Exception
	  public virtual void TestZombie()
	  {
		while (true)
		{
		  try
		  {
			Thread.Sleep(1000);
		  }
		  catch (InterruptedException e)
		  {
		  }
		}
	  }
	}

}