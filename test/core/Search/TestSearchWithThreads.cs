using System;
using System.Text;
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


	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using Term = Lucene.Net.Index.Term;
	using Directory = Lucene.Net.Store.Directory;
	using SuppressCodecs = Lucene.Net.Util.LuceneTestCase.SuppressCodecs;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressCodecs({ "SimpleText", "Memory", "Direct" }) public class TestSearchWithThreads extends Lucene.Net.Util.LuceneTestCase
	public class TestSearchWithThreads : LuceneTestCase
	{
	  internal int NUM_DOCS;
	  internal readonly int NUM_SEARCH_THREADS = 5;
	  internal int RUN_TIME_MSEC;

	  public override void SetUp()
	  {
		base.setUp();
		NUM_DOCS = atLeast(10000);
		RUN_TIME_MSEC = atLeast(1000);
	  }

	  public virtual void Test()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter w = new RandomIndexWriter(random(), dir);

		long startTime = System.currentTimeMillis();

		// TODO: replace w/ the @nightly test data; make this
		// into an optional @nightly stress test
		Document doc = new Document();
		Field body = newTextField("body", "", Field.Store.NO);
		doc.add(body);
		StringBuilder sb = new StringBuilder();
		for (int docCount = 0;docCount < NUM_DOCS;docCount++)
		{
		  int numTerms = random().Next(10);
		  for (int termCount = 0;termCount < numTerms;termCount++)
		  {
			sb.Append(random().nextBoolean() ? "aaa" : "bbb");
			sb.Append(' ');
		  }
		  body.StringValue = sb.ToString();
		  w.addDocument(doc);
		  sb.Remove(0, sb.Length);
		}
		IndexReader r = w.Reader;
		w.close();

		long endTime = System.currentTimeMillis();
		if (VERBOSE)
		{
			Console.WriteLine("BUILD took " + (endTime - startTime));
		}

		IndexSearcher s = newSearcher(r);

		AtomicBoolean failed = new AtomicBoolean();
		AtomicLong netSearch = new AtomicLong();

		Thread[] threads = new Thread[NUM_SEARCH_THREADS];
		for (int threadID = 0; threadID < NUM_SEARCH_THREADS; threadID++)
		{
		  threads[threadID] = new ThreadAnonymousInnerClassHelper(this, s, failed, netSearch);
		  threads[threadID].Daemon = true;
		}

		foreach (Thread t in threads)
		{
		  t.Start();
		}

		foreach (Thread t in threads)
		{
		  t.Join();
		}

		if (VERBOSE)
		{
			Console.WriteLine(NUM_SEARCH_THREADS + " threads did " + netSearch.get() + " searches");
		}

		r.close();
		dir.close();
	  }

	  private class ThreadAnonymousInnerClassHelper : System.Threading.Thread
	  {
		  private readonly TestSearchWithThreads OuterInstance;

		  private IndexSearcher s;
		  private AtomicBoolean Failed;
		  private AtomicLong NetSearch;

		  public ThreadAnonymousInnerClassHelper(TestSearchWithThreads outerInstance, IndexSearcher s, AtomicBoolean failed, AtomicLong netSearch)
		  {
			  this.OuterInstance = outerInstance;
			  this.s = s;
			  this.Failed = failed;
			  this.NetSearch = netSearch;
			  col = new TotalHitCountCollector();
		  }

		  internal TotalHitCountCollector col;
			public override void Run()
			{
			  try
			  {
				long totHits = 0;
				long totSearch = 0;
				long stopAt = System.currentTimeMillis() + OuterInstance.RUN_TIME_MSEC;
				while (System.currentTimeMillis() < stopAt && !Failed.get())
				{
				  s.search(new TermQuery(new Term("body", "aaa")), col);
				  totHits += col.TotalHits;
				  s.search(new TermQuery(new Term("body", "bbb")), col);
				  totHits += col.TotalHits;
				  totSearch++;
				}
				Assert.IsTrue(totSearch > 0 && totHits > 0);
				NetSearch.addAndGet(totSearch);
			  }
			  catch (Exception exc)
			  {
				Failed.set(true);
				throw new Exception(exc);
			  }
			}
	  }
	}

}