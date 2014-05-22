using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;

namespace Lucene.Net.Index
{

	/*
	 * Licensed to the Apache Software Foundation (ASF) under one or more
	 * contributor license agreements. See the NOTICE file distributed with
	 * this work for additional information regarding copyright ownership.
	 * The ASF licenses this file to You under the Apache License, Version 2.0
	 * (the "License"); you may not use this file except in compliance with
	 * the License. You may obtain a copy of the License at
	 *
	 * http://www.apache.org/licenses/LICENSE-2.0
	 *
	 * Unless required by applicable law or agreed to in writing, software
	 * distributed under the License is distributed on an "AS IS" BASIS,
	 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	 * See the License for the specific language governing permissions and
	 * limitations under the License.
	 */

	using ThreadState = Lucene.Net.Index.DocumentsWriterPerThreadPool.ThreadState;
	using ThreadInterruptedException = Lucene.Net.Util.ThreadInterruptedException;

	/// <summary>
	/// Controls the health status of a <seealso cref="DocumentsWriter"/> sessions. this class
	/// used to block incoming indexing threads if flushing significantly slower than
	/// indexing to ensure the <seealso cref="DocumentsWriter"/>s healthiness. If flushing is
	/// significantly slower than indexing the net memory used within an
	/// <seealso cref="IndexWriter"/> session can increase very quickly and easily exceed the
	/// JVM's available memory.
	/// <p>
	/// To prevent OOM Errors and ensure IndexWriter's stability this class blocks
	/// incoming threads from indexing once 2 x number of available
	/// <seealso cref="ThreadState"/>s in <seealso cref="DocumentsWriterPerThreadPool"/> is exceeded.
	/// Once flushing catches up and the number of flushing DWPT is equal or lower
	/// than the number of active <seealso cref="ThreadState"/>s threads are released and can
	/// continue indexing.
	/// </summary>
	internal sealed class DocumentsWriterStallControl
	{

	  private volatile bool Stalled;
	  private int NumWaiting; // only with assert
	  private bool WasStalled_Renamed; // only with assert
	  private readonly IDictionary<Thread, bool?> Waiting = new IdentityHashMap<Thread, bool?>(); // only with assert

	  /// <summary>
	  /// Update the stalled flag status. this method will set the stalled flag to
	  /// <code>true</code> iff the number of flushing
	  /// <seealso cref="DocumentsWriterPerThread"/> is greater than the number of active
	  /// <seealso cref="DocumentsWriterPerThread"/>. Otherwise it will reset the
	  /// <seealso cref="DocumentsWriterStallControl"/> to healthy and release all threads
	  /// waiting on <seealso cref="#waitIfStalled()"/>
	  /// </summary>
	  internal void UpdateStalled(bool stalled)
	  {
		  lock (this)
		  {
			this.Stalled = stalled;
			if (stalled)
			{
			  WasStalled_Renamed = true;
			}
			Monitor.PulseAll(this);
		  }
	  }

	  /// <summary>
	  /// Blocks if documents writing is currently in a stalled state. 
	  /// 
	  /// </summary>
	  internal void WaitIfStalled()
	  {
		if (Stalled)
		{
		  lock (this)
		  {
			if (Stalled) // react on the first wakeup call!
			{
			  // don't loop here, higher level logic will re-stall!
			  try
			  {
				Debug.Assert(IncWaiters());
				Monitor.Wait(this);
				Debug.Assert(DecrWaiters());
			  }
			  catch (InterruptedException e)
			  {
				throw new ThreadInterruptedException(e);
			  }
			}
		  }
		}
	  }

	  internal bool AnyStalledThreads()
	  {
		return Stalled;
	  }


	  private bool IncWaiters()
	  {
		NumWaiting++;
		Debug.Assert(Waiting.put(Thread.CurrentThread, true) == null);

		return NumWaiting > 0;
	  }

	  private bool DecrWaiters()
	  {
		NumWaiting--;
		Debug.Assert(Waiting.Remove(Thread.CurrentThread) != null);
		return NumWaiting >= 0;
	  }

	  internal bool HasBlocked() // for tests
	  {
		  lock (this)
		  {
			return NumWaiting > 0;
		  }
	  }

	  internal bool Healthy
	  {
		  get
		  {
			return !Stalled; // volatile read!
		  }
	  }

	  internal bool IsThreadQueued(Thread t) // for tests
	  {
		  lock (this)
		  {
			return Waiting.ContainsKey(t);
		  }
	  }

	  internal bool WasStalled() // for tests
	  {
		  lock (this)
		  {
			return WasStalled_Renamed;
		  }
	  }
	}

}