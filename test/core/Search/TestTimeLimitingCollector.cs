using System;
using System.Diagnostics;
using System.Collections;
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


	using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using Term = Lucene.Net.Index.Term;
	using TimeExceededException = Lucene.Net.Search.TimeLimitingCollector.TimeExceededException;
	using TimerThread = Lucene.Net.Search.TimeLimitingCollector.TimerThread;
	using Directory = Lucene.Net.Store.Directory;
	using Counter = Lucene.Net.Util.Counter;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using ThreadInterruptedException = Lucene.Net.Util.ThreadInterruptedException;

	/// <summary>
	/// Tests the <seealso cref="TimeLimitingCollector"/>.  this test checks (1) search
	/// correctness (regardless of timeout), (2) expected timeout behavior,
	/// and (3) a sanity test with multiple searching threads.
	/// </summary>
	public class TestTimeLimitingCollector : LuceneTestCase
	{
	  private const int SLOW_DOWN = 3;
	  private static readonly long TIME_ALLOWED = 17 * SLOW_DOWN; // so searches can find about 17 docs.

	  // max time allowed is relaxed for multithreading tests. 
	  // the multithread case fails when setting this to 1 (no slack) and launching many threads (>2000).  
	  // but this is not a real failure, just noise.
	  private const double MULTI_THREAD_SLACK = 7;

	  private const int N_DOCS = 3000;
	  private const int N_THREADS = 50;

	  private IndexSearcher Searcher;
	  private Directory Directory;
	  private IndexReader Reader;

	  private readonly string FIELD_NAME = "body";
	  private Query Query;
	  private Counter Counter;
	  private TimerThread CounterThread;

	  /// <summary>
	  /// initializes searcher with a document set
	  /// </summary>
	  public override void SetUp()
	  {
		base.setUp();
		Counter = Counter.newCounter(true);
		CounterThread = new TimerThread(Counter);
		CounterThread.start();
		string[] docText = new string[] {"docThatNeverMatchesSoWeCanRequireLastDocCollectedToBeGreaterThanZero", "one blah three", "one foo three multiOne", "one foobar three multiThree", "blueberry pancakes", "blueberry pie", "blueberry strudel", "blueberry pizza"};
		Directory = newDirectory();
		RandomIndexWriter iw = new RandomIndexWriter(random(), Directory, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMergePolicy(newLogMergePolicy()));

		for (int i = 0; i < N_DOCS; i++)
		{
		  Add(docText[i % docText.Length], iw);
		}
		Reader = iw.Reader;
		iw.close();
		Searcher = newSearcher(Reader);

		BooleanQuery booleanQuery = new BooleanQuery();
		booleanQuery.add(new TermQuery(new Term(FIELD_NAME, "one")), BooleanClause.Occur.SHOULD);
		// start from 1, so that the 0th doc never matches
		for (int i = 1; i < docText.Length; i++)
		{
		  string[] docTextParts = docText[i].Split("\\s+", true);
		  foreach (string docTextPart in docTextParts) // large query so that search will be longer
		  {
			booleanQuery.add(new TermQuery(new Term(FIELD_NAME, docTextPart)), BooleanClause.Occur.SHOULD);
		  }
		}

		Query = booleanQuery;

		// warm the searcher
		Searcher.search(Query, null, 1000);
	  }

	  public override void TearDown()
	  {
		Reader.close();
		Directory.close();
		CounterThread.stopTimer();
		CounterThread.join();
		base.tearDown();
	  }

	  private void Add(string value, RandomIndexWriter iw)
	  {
		Document d = new Document();
		d.add(newTextField(FIELD_NAME, value, Field.Store.NO));
		iw.addDocument(d);
	  }

	  private void Search(Collector collector)
	  {
		Searcher.search(Query, collector);
	  }

	  /// <summary>
	  /// test search correctness with no timeout
	  /// </summary>
	  public virtual void TestSearch()
	  {
		DoTestSearch();
	  }

	  private void DoTestSearch()
	  {
		int totalResults = 0;
		int totalTLCResults = 0;
		try
		{
		  MyHitCollector myHc = new MyHitCollector(this);
		  Search(myHc);
		  totalResults = myHc.HitCount();

		  myHc = new MyHitCollector(this);
		  long oneHour = 3600000;
		  Collector tlCollector = CreateTimedCollector(myHc, oneHour, false);
		  Search(tlCollector);
		  totalTLCResults = myHc.HitCount();
		}
		catch (Exception e)
		{
		  Console.WriteLine(e.ToString());
		  Console.Write(e.StackTrace);
		  Assert.IsTrue("Unexpected exception: " + e, false); //==fail
		}
		Assert.AreEqual("Wrong number of results!", totalResults, totalTLCResults);
	  }

	  private Collector CreateTimedCollector(MyHitCollector hc, long timeAllowed, bool greedy)
	  {
		TimeLimitingCollector res = new TimeLimitingCollector(hc, Counter, timeAllowed);
		res.Greedy = greedy; // set to true to make sure at least one doc is collected.
		return res;
	  }

	  /// <summary>
	  /// Test that timeout is obtained, and soon enough!
	  /// </summary>
	  public virtual void TestTimeoutGreedy()
	  {
		DoTestTimeout(false, true);
	  }

	  /// <summary>
	  /// Test that timeout is obtained, and soon enough!
	  /// </summary>
	  public virtual void TestTimeoutNotGreedy()
	  {
		DoTestTimeout(false, false);
	  }

	  private void DoTestTimeout(bool multiThreaded, bool greedy)
	  {
		// setup
		MyHitCollector myHc = new MyHitCollector(this);
		myHc.SlowDown = SLOW_DOWN;
		Collector tlCollector = CreateTimedCollector(myHc, TIME_ALLOWED, greedy);

		// search
		TimeExceededException timoutException = null;
		try
		{
		  Search(tlCollector);
		}
		catch (TimeExceededException x)
		{
		  timoutException = x;
		}
		catch (Exception e)
		{
		  Assert.IsTrue("Unexpected exception: " + e, false); //==fail
		}

		// must get exception
		Assert.IsNotNull("Timeout expected!", timoutException);

		// greediness affect last doc collected
		int exceptionDoc = timoutException.LastDocCollected;
		int lastCollected = myHc.LastDocCollected;
		Assert.IsTrue("doc collected at timeout must be > 0!", exceptionDoc > 0);
		if (greedy)
		{
		  Assert.IsTrue("greedy=" + greedy + " exceptionDoc=" + exceptionDoc + " != lastCollected=" + lastCollected, exceptionDoc == lastCollected);
		  Assert.IsTrue("greedy, but no hits found!", myHc.HitCount() > 0);
		}
		else
		{
		  Assert.IsTrue("greedy=" + greedy + " exceptionDoc=" + exceptionDoc + " not > lastCollected=" + lastCollected, exceptionDoc > lastCollected);
		}

		// verify that elapsed time at exception is within valid limits
		Assert.AreEqual(timoutException.TimeAllowed, TIME_ALLOWED);
		// a) Not too early
		Assert.IsTrue("elapsed=" + timoutException.TimeElapsed + " <= (allowed-resolution)=" + (TIME_ALLOWED - CounterThread.Resolution), timoutException.TimeElapsed > TIME_ALLOWED - CounterThread.Resolution);
		// b) Not too late.
		//    this part is problematic in a busy test system, so we just print a warning.
		//    We already verified that a timeout occurred, we just can't be picky about how long it took.
		if (timoutException.TimeElapsed > MaxTime(multiThreaded))
		{
		  Console.WriteLine("Informative: timeout exceeded (no action required: most probably just " + " because the test machine is slower than usual):  " + "lastDoc=" + exceptionDoc + " ,&& allowed=" + timoutException.TimeAllowed + " ,&& elapsed=" + timoutException.TimeElapsed + " >= " + MaxTimeStr(multiThreaded));
		}
	  }

	  private long MaxTime(bool multiThreaded)
	  {
		long res = 2 * CounterThread.Resolution + TIME_ALLOWED + SLOW_DOWN; // some slack for less noise in this test
		if (multiThreaded)
		{
		  res *= (long)MULTI_THREAD_SLACK; // larger slack
		}
		return res;
	  }

	  private string MaxTimeStr(bool multiThreaded)
	  {
		string s = "( " + "2*resolution +  TIME_ALLOWED + SLOW_DOWN = " + "2*" + CounterThread.Resolution + " + " + TIME_ALLOWED + " + " + SLOW_DOWN + ")";
		if (multiThreaded)
		{
		  s = MULTI_THREAD_SLACK + " * " + s;
		}
		return MaxTime(multiThreaded) + " = " + s;
	  }

	  /// <summary>
	  /// Test timeout behavior when resolution is modified. 
	  /// </summary>
	  public virtual void TestModifyResolution()
	  {
		try
		{
		  // increase and test
		  long resolution = 20 * TimerThread.DEFAULT_RESOLUTION; //400
		  CounterThread.Resolution = resolution;
		  Assert.AreEqual(resolution, CounterThread.Resolution);
		  DoTestTimeout(false,true);
		  // decrease much and test
		  resolution = 5;
		  CounterThread.Resolution = resolution;
		  Assert.AreEqual(resolution, CounterThread.Resolution);
		  DoTestTimeout(false,true);
		  // return to default and test
		  resolution = TimerThread.DEFAULT_RESOLUTION;
		  CounterThread.Resolution = resolution;
		  Assert.AreEqual(resolution, CounterThread.Resolution);
		  DoTestTimeout(false,true);
		}
		finally
		{
		  CounterThread.Resolution = TimerThread.DEFAULT_RESOLUTION;
		}
	  }

	  /// <summary>
	  /// Test correctness with multiple searching threads.
	  /// </summary>
	  public virtual void TestSearchMultiThreaded()
	  {
		DoTestMultiThreads(false);
	  }

	  /// <summary>
	  /// Test correctness with multiple searching threads.
	  /// </summary>
	  public virtual void TestTimeoutMultiThreaded()
	  {
		DoTestMultiThreads(true);
	  }

	  private void DoTestMultiThreads(bool withTimeout)
	  {
		Thread[] threadArray = new Thread[N_THREADS];
		BitArray success = new BitArray(N_THREADS);
		for (int i = 0; i < threadArray.Length; ++i)
		{
		  int num = i;
		  threadArray[num] = new ThreadAnonymousInnerClassHelper(this, withTimeout, success, num);
		}
		for (int i = 0; i < threadArray.Length; ++i)
		{
		  threadArray[i].Start();
		}
		for (int i = 0; i < threadArray.Length; ++i)
		{
		  threadArray[i].Join();
		}
		Assert.AreEqual("some threads failed!", N_THREADS,success.cardinality());
	  }

	  private class ThreadAnonymousInnerClassHelper : System.Threading.Thread
	  {
		  private readonly TestTimeLimitingCollector OuterInstance;

		  private bool WithTimeout;
		  private BitArray Success;
		  private int Num;

		  public ThreadAnonymousInnerClassHelper(TestTimeLimitingCollector outerInstance, bool withTimeout, BitArray success, int num)
		  {
			  this.OuterInstance = outerInstance;
			  this.WithTimeout = withTimeout;
			  this.Success = success;
			  this.Num = num;
		  }

		  public override void Run()
		  {
			if (WithTimeout)
			{
			  outerInstance.DoTestTimeout(true,true);
			}
			else
			{
			  outerInstance.DoTestSearch();
			}
			lock (Success)
			{
			  Success.Set(Num, true);
			}
		  }
	  }

	  // counting collector that can slow down at collect().
	  private class MyHitCollector : Collector
	  {
		  private readonly TestTimeLimitingCollector OuterInstance;

		  public MyHitCollector(TestTimeLimitingCollector outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }

		internal readonly BitArray Bits = new BitArray();
		internal int Slowdown = 0;
		internal int LastDocCollected_Renamed = -1;
		internal int DocBase = 0;

		/// <summary>
		/// amount of time to wait on each collect to simulate a long iteration
		/// </summary>
		public virtual int SlowDown
		{
			set
			{
			  Slowdown = value;
			}
		}

		public virtual int HitCount()
		{
		  return Bits.cardinality();
		}

		public virtual int LastDocCollected
		{
			get
			{
			  return LastDocCollected_Renamed;
			}
		}

		public override Scorer Scorer
		{
			set
			{
			  // value is not needed
			}
		}

		public override void Collect(int doc)
		{
		  int docId = doc + DocBase;
		  if (Slowdown > 0)
		  {
			try
			{
			  Thread.Sleep(Slowdown);
			}
			catch (InterruptedException ie)
			{
			  throw new ThreadInterruptedException(ie);
			}
		  }
		  Debug.Assert(docId >= 0, " base=" + DocBase + " doc=" + doc);
		  Bits.Set(docId, true);
		  LastDocCollected_Renamed = docId;
		}

		public override AtomicReaderContext NextReader
		{
			set
			{
			  DocBase = value.docBase;
			}
		}

		public override bool AcceptsDocsOutOfOrder()
		{
		  return false;
		}

	  }

	}

}