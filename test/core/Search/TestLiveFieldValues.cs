using System;
using System.Collections.Generic;
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
	using IntField = Lucene.Net.Document.IntField;
	using StringField = Lucene.Net.Document.StringField;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using IndexWriter = Lucene.Net.Index.IndexWriter;
	using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
	using Term = Lucene.Net.Index.Term;
	using Directory = Lucene.Net.Store.Directory;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;

	public class TestLiveFieldValues : LuceneTestCase
	{
	  public virtual void Test()
	  {

		Directory dir = newFSDirectory(createTempDir("livefieldupdates"));
		IndexWriterConfig iwc = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));

		IndexWriter w = new IndexWriter(dir, iwc);

		SearcherManager mgr = new SearcherManager(w, true, new SearcherFactoryAnonymousInnerClassHelper(this));

		const int? missing = -1;

		LiveFieldValues<IndexSearcher, int?> rt = new LiveFieldValuesAnonymousInnerClassHelper(this, mgr);

		int numThreads = TestUtil.Next(random(), 2, 5);
		if (VERBOSE)
		{
		  Console.WriteLine(numThreads + " threads");
		}

		CountDownLatch startingGun = new CountDownLatch(1);
		IList<Thread> threads = new List<Thread>();

		int iters = atLeast(1000);
		int idCount = TestUtil.Next(random(), 100, 10000);

		double reopenChance = random().NextDouble() * 0.01;
		double deleteChance = random().NextDouble() * 0.25;
		double addChance = random().NextDouble() * 0.5;

		for (int t = 0;t < numThreads;t++)
		{
		  int threadID = t;
		  Random threadRandom = new Random(random().nextLong());
		  Thread thread = new ThreadAnonymousInnerClassHelper(this, w, mgr, missing, rt, startingGun, iters, idCount, reopenChance, deleteChance, addChance, t, threadID, threadRandom);
		  threads.Add(thread);
		  thread.Start();
		}

		startingGun.countDown();

		foreach (Thread thread in threads)
		{
		  thread.Join();
		}
		mgr.maybeRefresh();
		Assert.AreEqual(0, rt.size());

		rt.close();
		mgr.close();
		w.close();
		dir.close();
	  }

	  private class SearcherFactoryAnonymousInnerClassHelper : SearcherFactory
	  {
		  private readonly TestLiveFieldValues OuterInstance;

		  public SearcherFactoryAnonymousInnerClassHelper(TestLiveFieldValues outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }

		  public override IndexSearcher NewSearcher(IndexReader r)
		  {
			return new IndexSearcher(r);
		  }
	  }

	  private class LiveFieldValuesAnonymousInnerClassHelper : LiveFieldValues<IndexSearcher, int?>
	  {
		  private readonly TestLiveFieldValues OuterInstance;

		  public LiveFieldValuesAnonymousInnerClassHelper(TestLiveFieldValues outerInstance, SearcherManager mgr) : base(mgr, missing)
		  {
			  this.OuterInstance = outerInstance;
		  }

		  protected internal override int? LookupFromSearcher(IndexSearcher s, string id)
		  {
			TermQuery tq = new TermQuery(new Term("id", id));
			TopDocs hits = s.search(tq, 1);
			Assert.IsTrue(hits.totalHits <= 1);
			if (hits.totalHits == 0)
			{
			  return null;
			}
			else
			{
			  Document doc = s.doc(hits.scoreDocs[0].doc);
			  return (int?) doc.getField("field").numericValue();
			}
		  }
	  }

	  private class ThreadAnonymousInnerClassHelper : System.Threading.Thread
	  {
		  private readonly TestLiveFieldValues OuterInstance;

		  private IndexWriter w;
		  private SearcherManager Mgr;
		  private int? Missing;
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: private LiveFieldValues<IndexSearcher, int?> rt;
		  private LiveFieldValues<IndexSearcher, int?> Rt;
		  private CountDownLatch StartingGun;
		  private int Iters;
		  private int IdCount;
		  private double ReopenChance;
		  private double DeleteChance;
		  private double AddChance;
		  private int t;
		  private int ThreadID;
		  private Random ThreadRandom;

		  public ThreadAnonymousInnerClassHelper<T1>(TestLiveFieldValues outerInstance, IndexWriter w, SearcherManager mgr, int? missing, LiveFieldValues<T1> rt, CountDownLatch startingGun, int iters, int idCount, double reopenChance, double deleteChance, double addChance, int t, int threadID, Random threadRandom)
		  {
			  this.OuterInstance = outerInstance;
			  this.w = w;
			  this.Mgr = mgr;
			  this.Missing = missing;
			  this.Rt = rt;
			  this.StartingGun = startingGun;
			  this.Iters = iters;
			  this.IdCount = idCount;
			  this.ReopenChance = reopenChance;
			  this.DeleteChance = deleteChance;
			  this.AddChance = addChance;
			  this.t = t;
			  this.ThreadID = threadID;
			  this.ThreadRandom = threadRandom;
		  }


		  public override void Run()
		  {
			try
			{
			  IDictionary<string, int?> values = new Dictionary<string, int?>();
			  IList<string> allIDs = Collections.synchronizedList(new List<string>());

			  StartingGun.@await();
			  for (int iter = 0; iter < Iters;iter++)
			  {
				// Add/update a document
				Document doc = new Document();
				// Threads must not update the same id at the
				// same time:
				if (ThreadRandom.NextDouble() <= AddChance)
				{
				  string id = string.format(Locale.ROOT, "%d_%04x", ThreadID, ThreadRandom.Next(IdCount));
				  int? field = ThreadRandom.Next(int.MaxValue);
				  doc.add(new StringField("id", id, Field.Store.YES));
				  doc.add(new IntField("field", (int)field, Field.Store.YES));
				  w.updateDocument(new Term("id", id), doc);
				  Rt.add(id, field);
				  if (values.put(id, field) == null)
				  {
					allIDs.Add(id);
				  }
				}

				if (allIDs.Count > 0 && ThreadRandom.NextDouble() <= DeleteChance)
				{
				  string randomID = allIDs[ThreadRandom.Next(allIDs.Count)];
				  w.deleteDocuments(new Term("id", randomID));
				  Rt.delete(randomID);
				  values[randomID] = Missing;
				}

				if (ThreadRandom.NextDouble() <= ReopenChance || Rt.size() > 10000)
				{
				  //System.out.println("refresh @ " + rt.size());
				  Mgr.maybeRefresh();
				  if (VERBOSE)
				  {
					IndexSearcher s = Mgr.acquire();
					try
					{
					  Console.WriteLine("TEST: reopen " + s);
					}
					finally
					{
					  Mgr.release(s);
					}
					Console.WriteLine("TEST: " + values.Count + " values");
				  }
				}

				if (ThreadRandom.Next(10) == 7)
				{
				  Assert.AreEqual(null, Rt.get("foo"));
				}

				if (allIDs.Count > 0)
				{
				  string randomID = allIDs[ThreadRandom.Next(allIDs.Count)];
				  int? expected = values[randomID];
				  if (expected == Missing)
				  {
					expected = null;
				  }
				  Assert.AreEqual("id=" + randomID, expected, Rt.get(randomID));
				}
			  }
			}
			catch (Exception t)
			{
			  throw new Exception(t);
			}
		  }
	  }
	}

}