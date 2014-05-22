using System;
using System.Collections.Generic;
using System.Threading;

namespace Lucene.Net.Index
{

	/*
	 * Licensed to the Apache Software Foundation (ASF) under one or more
	 * contributor license agreements. See the NOTICE file distributed with this
	 * work for additional information regarding copyright ownership. The ASF
	 * licenses this file to You under the Apache License, Version 2.0 (the
	 * "License"); you may not use this file except in compliance with the License.
	 * You may obtain a copy of the License at
	 * 
	 * http://www.apache.org/licenses/LICENSE-2.0
	 * 
	 * Unless required by applicable law or agreed to in writing, software
	 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
	 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
	 * License for the specific language governing permissions and limitations under
	 * the License.
	 */


	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using ThreadInterruptedException = Lucene.Net.Util.ThreadInterruptedException;

	/// <summary>
	/// Tests for <seealso cref="DocumentsWriterStallControl"/>
	/// </summary>
	public class TestDocumentsWriterStallControl : LuceneTestCase
	{

	  public virtual void TestSimpleStall()
	  {
		DocumentsWriterStallControl ctrl = new DocumentsWriterStallControl();

		ctrl.updateStalled(false);
		Thread[] waitThreads = WaitThreads(atLeast(1), ctrl);
		Start(waitThreads);
		Assert.IsFalse(ctrl.hasBlocked());
		Assert.IsFalse(ctrl.anyStalledThreads());
		Join(waitThreads);

		// now stall threads and wake them up again
		ctrl.updateStalled(true);
		waitThreads = WaitThreads(atLeast(1), ctrl);
		Start(waitThreads);
		AwaitState(Thread.State.WAITING, waitThreads);
		Assert.IsTrue(ctrl.hasBlocked());
		Assert.IsTrue(ctrl.anyStalledThreads());
		ctrl.updateStalled(false);
		Assert.IsFalse(ctrl.anyStalledThreads());
		Join(waitThreads);
	  }

	  public virtual void TestRandom()
	  {
		DocumentsWriterStallControl ctrl = new DocumentsWriterStallControl();
		ctrl.updateStalled(false);

		Thread[] stallThreads = new Thread[atLeast(3)];
		for (int i = 0; i < stallThreads.Length; i++)
		{
		  int stallProbability = 1 + random().Next(10);
		  stallThreads[i] = new ThreadAnonymousInnerClassHelper(this, ctrl, stallProbability);
		}
		Start(stallThreads);
		long time = System.currentTimeMillis();
		/*
		 * use a 100 sec timeout to make sure we not hang forever. join will fail in
		 * that case
		 */
		while ((System.currentTimeMillis() - time) < 100 * 1000 && !Terminated(stallThreads))
		{
		  ctrl.updateStalled(false);
		  if (random().nextBoolean())
		  {
			Thread.@yield();
		  }
		  else
		  {
			Thread.Sleep(1);
		  }

		}
		Join(stallThreads);

	  }

	  private class ThreadAnonymousInnerClassHelper : System.Threading.Thread
	  {
		  private readonly TestDocumentsWriterStallControl OuterInstance;

		  private DocumentsWriterStallControl Ctrl;
		  private int StallProbability;

		  public ThreadAnonymousInnerClassHelper(TestDocumentsWriterStallControl outerInstance, DocumentsWriterStallControl ctrl, int stallProbability)
		  {
			  this.OuterInstance = outerInstance;
			  this.Ctrl = ctrl;
			  this.StallProbability = stallProbability;
		  }

		  public override void Run()
		  {

			int iters = atLeast(1000);
			for (int j = 0; j < iters; j++)
			{
			  Ctrl.updateStalled(random().Next(StallProbability) == 0);
			  if (random().Next(5) == 0) // thread 0 only updates
			  {
				Ctrl.waitIfStalled();
			  }
			}
		  }
	  }

	  public virtual void TestAccquireReleaseRace()
	  {
		DocumentsWriterStallControl ctrl = new DocumentsWriterStallControl();
		ctrl.updateStalled(false);
		AtomicBoolean stop = new AtomicBoolean(false);
		AtomicBoolean checkPoint = new AtomicBoolean(true);

		int numStallers = atLeast(1);
		int numReleasers = atLeast(1);
		int numWaiters = atLeast(1);
		Synchronizer sync = new Synchronizer(numStallers + numReleasers, numStallers + numReleasers + numWaiters);
		Thread[] threads = new Thread[numReleasers + numStallers + numWaiters];
		IList<Exception> exceptions = Collections.synchronizedList(new List<Exception>());
		for (int i = 0; i < numReleasers; i++)
		{
		  threads[i] = new Updater(stop, checkPoint, ctrl, sync, true, exceptions);
		}
		for (int i = numReleasers; i < numReleasers + numStallers; i++)
		{
		  threads[i] = new Updater(stop, checkPoint, ctrl, sync, false, exceptions);

		}
		for (int i = numReleasers + numStallers; i < numReleasers + numStallers + numWaiters; i++)
		{
		  threads[i] = new Waiter(stop, checkPoint, ctrl, sync, exceptions);

		}

		Start(threads);
		int iters = atLeast(10000);
		float checkPointProbability = TEST_NIGHTLY ? 0.5f : 0.1f;
		for (int i = 0; i < iters; i++)
		{
		  if (checkPoint.get())
		  {

			Assert.IsTrue("timed out waiting for update threads - deadlock?", sync.UpdateJoin.@await(10, TimeUnit.SECONDS));
			if (exceptions.Count > 0)
			{
			  foreach (Exception throwable in exceptions)
			  {
				Console.WriteLine(throwable.ToString());
				Console.Write(throwable.StackTrace);
			  }
			  Assert.Fail("got exceptions in threads");
			}

			if (ctrl.hasBlocked() && ctrl.Healthy)
			{
			  AssertState(numReleasers, numStallers, numWaiters, threads, ctrl);


			}

			checkPoint.set(false);
			sync.Waiter.countDown();
			sync.LeftCheckpoint.@await();
		  }
		  Assert.IsFalse(checkPoint.get());
		  Assert.AreEqual(0, sync.Waiter.Count);
		  if (checkPointProbability >= random().nextFloat())
		  {
			sync.Reset(numStallers + numReleasers, numStallers + numReleasers + numWaiters);
			checkPoint.set(true);
		  }

		}
		if (!checkPoint.get())
		{
		  sync.Reset(numStallers + numReleasers, numStallers + numReleasers + numWaiters);
		  checkPoint.set(true);
		}

		Assert.IsTrue(sync.UpdateJoin.@await(10, TimeUnit.SECONDS));
		AssertState(numReleasers, numStallers, numWaiters, threads, ctrl);
		checkPoint.set(false);
		stop.set(true);
		sync.Waiter.countDown();
		sync.LeftCheckpoint.@await();


		for (int i = 0; i < threads.Length; i++)
		{
		  ctrl.updateStalled(false);
		  threads[i].Join(2000);
		  if (threads[i].IsAlive && threads[i] is Waiter)
		  {
			if (threads[i].State == Thread.State.WAITING)
			{
			  Assert.Fail("waiter is not released - anyThreadsStalled: " + ctrl.anyStalledThreads());
			}
		  }
		}
	  }

	  private void AssertState(int numReleasers, int numStallers, int numWaiters, Thread[] threads, DocumentsWriterStallControl ctrl)
	  {
		int millisToSleep = 100;
		while (true)
		{
		  if (ctrl.hasBlocked() && ctrl.Healthy)
		  {
			for (int n = numReleasers + numStallers; n < numReleasers + numStallers + numWaiters; n++)
			{
			  if (ctrl.isThreadQueued(threads[n]))
			  {
				if (millisToSleep < 60000)
				{
				  Thread.Sleep(millisToSleep);
				  millisToSleep *= 2;
				  break;
				}
				else
				{
				  Assert.Fail("control claims no stalled threads but waiter seems to be blocked ");
				}
			  }
			}
			break;
		  }
		  else
		  {
			break;
		  }
		}

	  }

	  public class Waiter : System.Threading.Thread
	  {
		internal Synchronizer Sync;
		internal DocumentsWriterStallControl Ctrl;
		internal AtomicBoolean CheckPoint;
		internal AtomicBoolean Stop;
		internal IList<Exception> Exceptions;

		public Waiter(AtomicBoolean stop, AtomicBoolean checkPoint, DocumentsWriterStallControl ctrl, Synchronizer sync, IList<Exception> exceptions) : base("waiter")
		{
		  this.Stop = stop;
		  this.CheckPoint = checkPoint;
		  this.Ctrl = ctrl;
		  this.Sync = sync;
		  this.Exceptions = exceptions;
		}

		public override void Run()
		{
		  try
		  {
			while (!Stop.get())
			{
			  Ctrl.waitIfStalled();
			  if (CheckPoint.get())
			  {
				try
				{
				  Assert.IsTrue(Sync.@await());
				}
				catch (InterruptedException e)
				{
				  Console.WriteLine("[Waiter] got interrupted - wait count: " + Sync.Waiter.Count);
				  throw new ThreadInterruptedException(e);
				}
			  }
			}
		  }
		  catch (Exception e)
		  {
			Console.WriteLine(e.ToString());
			Console.Write(e.StackTrace);
			Exceptions.Add(e);
		  }
		}
	  }

	  public class Updater : System.Threading.Thread
	  {

		internal Synchronizer Sync;
		internal DocumentsWriterStallControl Ctrl;
		internal AtomicBoolean CheckPoint;
		internal AtomicBoolean Stop;
		internal bool Release;
		internal IList<Exception> Exceptions;

		public Updater(AtomicBoolean stop, AtomicBoolean checkPoint, DocumentsWriterStallControl ctrl, Synchronizer sync, bool release, IList<Exception> exceptions) : base("updater")
		{
		  this.Stop = stop;
		  this.CheckPoint = checkPoint;
		  this.Ctrl = ctrl;
		  this.Sync = sync;
		  this.Release = release;
		  this.Exceptions = exceptions;
		}

		public override void Run()
		{
		  try
		  {

			while (!Stop.get())
			{
			  int internalIters = Release && random().nextBoolean() ? atLeast(5) : 1;
			  for (int i = 0; i < internalIters; i++)
			  {
				Ctrl.updateStalled(random().nextBoolean());
			  }
			  if (CheckPoint.get())
			  {
				Sync.UpdateJoin.countDown();
				try
				{
				  Assert.IsTrue(Sync.@await());
				}
				catch (InterruptedException e)
				{
				  Console.WriteLine("[Updater] got interrupted - wait count: " + Sync.Waiter.Count);
				  throw new ThreadInterruptedException(e);
				}
				Sync.LeftCheckpoint.countDown();
			  }
			  if (random().nextBoolean())
			  {
				Thread.@yield();
			  }
			}
		  }
		  catch (Exception e)
		  {
			Console.WriteLine(e.ToString());
			Console.Write(e.StackTrace);
			Exceptions.Add(e);
		  }
		  Sync.UpdateJoin.countDown();
		}

	  }

	  public static bool Terminated(Thread[] threads)
	  {
		foreach (Thread thread in threads)
		{
		  if (Thread.State.TERMINATED != thread.State)
		  {
			  return false;
		  }
		}
		return true;
	  }

	  public static void Start(Thread[] tostart)
	  {
		foreach (Thread thread in tostart)
		{
		  thread.Start();
		}
		Thread.Sleep(1); // let them start
	  }

	  public static void Join(Thread[] toJoin)
	  {
		foreach (Thread thread in toJoin)
		{
		  thread.Join();
		}
	  }

	  public static Thread[] WaitThreads(int num, DocumentsWriterStallControl ctrl)
	  {
		Thread[] array = new Thread[num];
		for (int i = 0; i < array.Length; i++)
		{
		  array[i] = new ThreadAnonymousInnerClassHelper(ctrl);
		}
		return array;
	  }

	  private class ThreadAnonymousInnerClassHelper : System.Threading.Thread
	  {
		  private DocumentsWriterStallControl Ctrl;

		  public ThreadAnonymousInnerClassHelper(DocumentsWriterStallControl ctrl)
		  {
			  this.Ctrl = ctrl;
		  }

		  public override void Run()
		  {
			Ctrl.waitIfStalled();
		  }
	  }

	  /// <summary>
	  /// Waits for all incoming threads to be in wait()
	  ///  methods. 
	  /// </summary>
	  public static void AwaitState(Thread.State state, params Thread[] threads)
	  {
		while (true)
		{
		  bool done = true;
		  foreach (Thread thread in threads)
		  {
			if (thread.State != state)
			{
			  done = false;
			  break;
			}
		  }
		  if (done)
		  {
			return;
		  }
		  if (random().nextBoolean())
		  {
			Thread.@yield();
		  }
		  else
		  {
			Thread.Sleep(1);
		  }
		}
	  }

	  private sealed class Synchronizer
	  {
		internal volatile CountDownLatch Waiter;
		internal volatile CountDownLatch UpdateJoin;
		internal volatile CountDownLatch LeftCheckpoint;

		public Synchronizer(int numUpdater, int numThreads)
		{
		  Reset(numUpdater, numThreads);
		}

		public void Reset(int numUpdaters, int numThreads)
		{
		  this.Waiter = new CountDownLatch(1);
		  this.UpdateJoin = new CountDownLatch(numUpdaters);
		  this.LeftCheckpoint = new CountDownLatch(numUpdaters);
		}

		public bool @await()
		{
		  return Waiter.@await(10, TimeUnit.SECONDS);
		}

	  }
	}

}