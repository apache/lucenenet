using System;
using System.Threading;

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

	using AlreadySetException = Lucene.Net.Util.SetOnce.AlreadySetException;
	using Test = org.junit.Test;

	public class TestSetOnce : LuceneTestCase
	{

	  private sealed class SetOnceThread : System.Threading.Thread
	  {
		internal SetOnce<int?> Set;
		internal bool Success = false;
		internal readonly Random RAND;

		public SetOnceThread(Random random)
		{
		  RAND = new Random(random.nextLong());
		}

		public override void Run()
		{
		  try
		  {
			sleep(RAND.Next(10)); // sleep for a short time
			Set.set(new int?(Convert.ToInt32(Name.Substring(2))));
			Success = true;
		  }
		  catch (InterruptedException e)
		  {
			// ignore
		  }
		  catch (Exception e)
		  {
			// TODO: change exception type
			// expected.
			Success = false;
		  }
		}
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testEmptyCtor() throws Exception
	  public virtual void TestEmptyCtor()
	  {
		SetOnce<int?> set = new SetOnce<int?>();
		assertNull(set.get());
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test(expected=Lucene.Net.Util.SetOnce.AlreadySetException.class) public void testSettingCtor() throws Exception
	  public virtual void TestSettingCtor()
	  {
		SetOnce<int?> set = new SetOnce<int?>(new int?(5));
		Assert.AreEqual(5, (int)set.get());
		set.set(new int?(7));
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test(expected=Lucene.Net.Util.SetOnce.AlreadySetException.class) public void testSetOnce() throws Exception
	  public virtual void TestSetOnce()
	  {
		SetOnce<int?> set = new SetOnce<int?>();
		set.set(new int?(5));
		Assert.AreEqual(5, (int)set.get());
		set.set(new int?(7));
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testSetMultiThreaded() throws Exception
	  public virtual void TestSetMultiThreaded()
	  {
		SetOnce<int?> set = new SetOnce<int?>();
		SetOnceThread[] threads = new SetOnceThread[10];
		for (int i = 0; i < threads.Length; i++)
		{
		  threads[i] = new SetOnceThread(random());
		  threads[i].Name = "t-" + (i + 1);
		  threads[i].Set = set;
		}

		foreach (Thread t in threads)
		{
		  t.Start();
		}

		foreach (Thread t in threads)
		{
		  t.Join();
		}

		foreach (SetOnceThread t in threads)
		{
		  if (t.Success)
		  {
			int expectedVal = Convert.ToInt32(t.Name.Substring(2));
			Assert.AreEqual("thread " + t.Name, expectedVal, (int)t.Set.get());
		  }
		}
	  }

	}

}