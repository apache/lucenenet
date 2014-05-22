using System;
using System.Threading;

namespace Lucene.Net.Search
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

	using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
	using Counter = Lucene.Net.Util.Counter;
	using ThreadInterruptedException = Lucene.Net.Util.ThreadInterruptedException;

	/// <summary>
	/// The <seealso cref="TimeLimitingCollector"/> is used to timeout search requests that
	/// take longer than the maximum allowed search time limit. After this time is
	/// exceeded, the search thread is stopped by throwing a
	/// <seealso cref="TimeExceededException"/>.
	/// </summary>
	public class TimeLimitingCollector : Collector
	{


	  /// <summary>
	  /// Thrown when elapsed search time exceeds allowed search time. </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressWarnings("serial") public static class TimeExceededException extends RuntimeException
	  public class TimeExceededException : Exception
	  {
		internal long TimeAllowed_Renamed;
		internal long TimeElapsed_Renamed;
		internal int LastDocCollected_Renamed;
		internal TimeExceededException(long timeAllowed, long timeElapsed, int lastDocCollected) : base("Elapsed time: " + timeElapsed + "Exceeded allowed search time: " + timeAllowed + " ms.")
		{
		  this.TimeAllowed_Renamed = timeAllowed;
		  this.TimeElapsed_Renamed = timeElapsed;
		  this.LastDocCollected_Renamed = lastDocCollected;
		}
		/// <summary>
		/// Returns allowed time (milliseconds). </summary>
		public virtual long TimeAllowed
		{
			get
			{
			  return TimeAllowed_Renamed;
			}
		}
		/// <summary>
		/// Returns elapsed time (milliseconds). </summary>
		public virtual long TimeElapsed
		{
			get
			{
			  return TimeElapsed_Renamed;
			}
		}
		/// <summary>
		/// Returns last doc (absolute doc id) that was collected when the search time exceeded. </summary>
		public virtual int LastDocCollected
		{
			get
			{
			  return LastDocCollected_Renamed;
			}
		}
	  }

	  private long T0 = long.MinValue;
	  private long Timeout = long.MinValue;
	  private Collector Collector_Renamed;
	  private readonly Counter Clock;
	  private readonly long TicksAllowed;
	  private bool Greedy_Renamed = false;
	  private int DocBase;

	  /// <summary>
	  /// Create a TimeLimitedCollector wrapper over another <seealso cref="Collector"/> with a specified timeout. </summary>
	  /// <param name="collector"> the wrapped <seealso cref="Collector"/> </param>
	  /// <param name="clock"> the timer clock </param>
	  /// <param name="ticksAllowed"> max time allowed for collecting
	  /// hits after which <seealso cref="TimeExceededException"/> is thrown </param>
	  public TimeLimitingCollector(Collector collector, Counter clock, long ticksAllowed)
	  {
		this.Collector_Renamed = collector;
		this.Clock = clock;
		this.TicksAllowed = ticksAllowed;
	  }

	  /// <summary>
	  /// Sets the baseline for this collector. By default the collectors baseline is 
	  /// initialized once the first reader is passed to the collector. 
	  /// To include operations executed in prior to the actual document collection
	  /// set the baseline through this method in your prelude.
	  /// <p>
	  /// Example usage:
	  /// <pre class="prettyprint">
	  ///   Counter clock = ...;
	  ///   long baseline = clock.get();
	  ///   // ... prepare search
	  ///   TimeLimitingCollector collector = new TimeLimitingCollector(c, clock, numTicks);
	  ///   collector.setBaseline(baseline);
	  ///   indexSearcher.search(query, collector);
	  /// </pre>
	  /// </p> </summary>
	  /// <seealso cref= #setBaseline()  </seealso>
	  public virtual long Baseline
	  {
		  set
		  {
			T0 = value;
			Timeout = T0 + TicksAllowed;
		  }
	  }

	  /// <summary>
	  /// Syntactic sugar for <seealso cref="#setBaseline(long)"/> using <seealso cref="Counter#get()"/>
	  /// on the clock passed to the constructor.
	  /// </summary>
	  public virtual void SetBaseline()
	  {
		Baseline = Clock.Get();
	  }

	  /// <summary>
	  /// Checks if this time limited collector is greedy in collecting the last hit.
	  /// A non greedy collector, upon a timeout, would throw a <seealso cref="TimeExceededException"/> 
	  /// without allowing the wrapped collector to collect current doc. A greedy one would 
	  /// first allow the wrapped hit collector to collect current doc and only then 
	  /// throw a <seealso cref="TimeExceededException"/>. </summary>
	  /// <seealso cref= #setGreedy(boolean) </seealso>
	  public virtual bool Greedy
	  {
		  get
		  {
			return Greedy_Renamed;
		  }
		  set
		  {
			this.Greedy_Renamed = value;
		  }
	  }


	  /// <summary>
	  /// Calls <seealso cref="Collector#collect(int)"/> on the decorated <seealso cref="Collector"/>
	  /// unless the allowed time has passed, in which case it throws an exception.
	  /// </summary>
	  /// <exception cref="TimeExceededException">
	  ///           if the time allowed has exceeded. </exception>
	  public override void Collect(int doc)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long time = clock.get();
		long time = Clock.Get();
		if (Timeout < time)
		{
		  if (Greedy_Renamed)
		  {
			//System.out.println(this+"  greedy: before failing, collecting doc: "+(docBase + doc)+"  "+(time-t0));
			Collector_Renamed.Collect(doc);
		  }
		  //System.out.println(this+"  failing on:  "+(docBase + doc)+"  "+(time-t0));
		  throw new TimeExceededException(Timeout - T0, time - T0, DocBase + doc);
		}
		//System.out.println(this+"  collecting: "+(docBase + doc)+"  "+(time-t0));
		Collector_Renamed.Collect(doc);
	  }

	  public override AtomicReaderContext NextReader
	  {
		  set
		  {
			Collector_Renamed.NextReader = value;
			this.DocBase = value.DocBase;
			if (long.MinValue == T0)
			{
			  SetBaseline();
			}
		  }
	  }

	  public override Scorer Scorer
	  {
		  set
		  {
			Collector_Renamed.Scorer = value;
		  }
	  }

	  public override bool AcceptsDocsOutOfOrder()
	  {
		return Collector_Renamed.AcceptsDocsOutOfOrder();
	  }

	  /// <summary>
	  /// this is so the same timer can be used with a multi-phase search process such as grouping. 
	  /// We don't want to create a new TimeLimitingCollector for each phase because that would 
	  /// reset the timer for each phase.  Once time is up subsequent phases need to timeout quickly.
	  /// </summary>
	  /// <param name="collector"> The actual collector performing search functionality </param>
	  public virtual Collector Collector
	  {
		  set
		  {
			this.Collector_Renamed = value;
		  }
	  }


	  /// <summary>
	  /// Returns the global TimerThreads <seealso cref="Counter"/>
	  /// <p>
	  /// Invoking this creates may create a new instance of <seealso cref="TimerThread"/> iff
	  /// the global <seealso cref="TimerThread"/> has never been accessed before. The thread
	  /// returned from this method is started on creation and will be alive unless
	  /// you stop the <seealso cref="TimerThread"/> via <seealso cref="TimerThread#stopTimer()"/>.
	  /// </p> </summary>
	  /// <returns> the global TimerThreads <seealso cref="Counter"/>
	  /// @lucene.experimental </returns>
	  public static Counter GlobalCounter
	  {
		  get
		  {
			return TimerThreadHolder.THREAD.Counter;
		  }
	  }

	  /// <summary>
	  /// Returns the global <seealso cref="TimerThread"/>.
	  /// <p>
	  /// Invoking this creates may create a new instance of <seealso cref="TimerThread"/> iff
	  /// the global <seealso cref="TimerThread"/> has never been accessed before. The thread
	  /// returned from this method is started on creation and will be alive unless
	  /// you stop the <seealso cref="TimerThread"/> via <seealso cref="TimerThread#stopTimer()"/>.
	  /// </p>
	  /// </summary>
	  /// <returns> the global <seealso cref="TimerThread"/>
	  /// @lucene.experimental </returns>
	  public static TimerThread GlobalTimerThread
	  {
		  get
		  {
			return TimerThreadHolder.THREAD;
		  }
	  }

	  private sealed class TimerThreadHolder
	  {
		internal static readonly TimerThread THREAD;
		static TimerThreadHolder()
		{
		  THREAD = new TimerThread(Counter.NewCounter(true));
		  THREAD.Start();
		}
	  }

	  /// <summary>
	  /// Thread used to timeout search requests.
	  /// Can be stopped completely with <seealso cref="TimerThread#stopTimer()"/>
	  /// @lucene.experimental
	  /// </summary>
	  public sealed class TimerThread : System.Threading.Thread
	  {

		public const string THREAD_NAME = "TimeLimitedCollector timer thread";
		public const int DEFAULT_RESOLUTION = 20;
		// NOTE: we can avoid explicit synchronization here for several reasons:
		// * updates to volatile long variables are atomic
		// * only single thread modifies this value
		// * use of volatile keyword ensures that it does not reside in
		//   a register, but in main memory (so that changes are visible to
		//   other threads).
		// * visibility of changes does not need to be instantaneous, we can
		//   afford losing a tick or two.
		//
		// See section 17 of the Java Language Specification for details.
		internal volatile long Time = 0;
		internal volatile bool Stop = false;
		internal volatile long Resolution_Renamed;
		internal readonly Counter Counter;

		public TimerThread(long resolution, Counter counter) : base(THREAD_NAME)
		{
		  this.Resolution_Renamed = resolution;
		  this.Counter = counter;
		  this.Daemon = true;
		}

		public TimerThread(Counter counter) : this(DEFAULT_RESOLUTION, counter)
		{
		}

		public override void Run()
		{
		  while (!Stop)
		  {
			// TODO: Use System.nanoTime() when Lucene moves to Java SE 5.
			Counter.AddAndGet(Resolution_Renamed);
			try
			{
			  Thread.Sleep(Resolution_Renamed);
			}
			catch (InterruptedException ie)
			{
			  throw new ThreadInterruptedException(ie);
			}
		  }
		}

		/// <summary>
		/// Get the timer value in milliseconds.
		/// </summary>
		public long Milliseconds
		{
			get
			{
			  return Time;
			}
		}

		/// <summary>
		/// Stops the timer thread 
		/// </summary>
		public void StopTimer()
		{
		  Stop = true;
		}

		/// <summary>
		/// Return the timer resolution. </summary>
		/// <seealso cref= #setResolution(long) </seealso>
		public long Resolution
		{
			get
			{
			  return Resolution_Renamed;
			}
			set
			{
			  this.Resolution_Renamed = Math.Max(value, 5); // 5 milliseconds is about the minimum reasonable time for a Object.wait(long) call.
			}
		}

	  }

	}

}