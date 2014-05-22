using System;
using System.Collections.Generic;
using System.Threading;

namespace Lucene.Net.Index
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
	using BinaryDocValuesField = Lucene.Net.Document.BinaryDocValuesField;
	using Document = Lucene.Net.Document.Document;
	using NumericDocValuesField = Lucene.Net.Document.NumericDocValuesField;
	using SortedDocValuesField = Lucene.Net.Document.SortedDocValuesField;
	using FieldCache = Lucene.Net.Search.FieldCache;
	using Directory = Lucene.Net.Store.Directory;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;
	using TestUtil = Lucene.Net.Util.TestUtil;
	using SuppressCodecs = Lucene.Net.Util.LuceneTestCase.SuppressCodecs;

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressCodecs("Lucene3x") public class TestDocValuesWithThreads extends Lucene.Net.Util.LuceneTestCase
	public class TestDocValuesWithThreads : LuceneTestCase
	{

	  public virtual void Test()
	  {
		Directory dir = newDirectory();
		IndexWriter w = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMergePolicy(newLogMergePolicy()));

		IList<long?> numbers = new List<long?>();
		IList<BytesRef> binary = new List<BytesRef>();
		IList<BytesRef> sorted = new List<BytesRef>();
		int numDocs = atLeast(100);
		for (int i = 0;i < numDocs;i++)
		{
		  Document d = new Document();
		  long number = random().nextLong();
		  d.add(new NumericDocValuesField("number", number));
		  BytesRef bytes = new BytesRef(TestUtil.randomRealisticUnicodeString(random()));
		  d.add(new BinaryDocValuesField("bytes", bytes));
		  binary.Add(bytes);
		  bytes = new BytesRef(TestUtil.randomRealisticUnicodeString(random()));
		  d.add(new SortedDocValuesField("sorted", bytes));
		  sorted.Add(bytes);
		  w.addDocument(d);
		  numbers.Add(number);
		}

		w.forceMerge(1);
		IndexReader r = w.Reader;
		w.close();

		Assert.AreEqual(1, r.leaves().size());
		AtomicReader ar = r.leaves().get(0).reader();

		int numThreads = TestUtil.Next(random(), 2, 5);
		IList<Thread> threads = new List<Thread>();
		CountDownLatch startingGun = new CountDownLatch(1);
		for (int t = 0;t < numThreads;t++)
		{
		  Random threadRandom = new Random(random().nextLong());
		  Thread thread = new ThreadAnonymousInnerClassHelper(this, numbers, binary, sorted, numDocs, ar, startingGun, threadRandom);
		  thread.Start();
		  threads.Add(thread);
		}

		startingGun.countDown();

		foreach (Thread thread in threads)
		{
		  thread.Join();
		}

		r.close();
		dir.close();
	  }

	  private class ThreadAnonymousInnerClassHelper : System.Threading.Thread
	  {
		  private readonly TestDocValuesWithThreads OuterInstance;

//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: private IList<long?> numbers;
		  private IList<long?> Numbers;
		  private IList<BytesRef> Binary;
		  private IList<BytesRef> Sorted;
		  private int NumDocs;
		  private AtomicReader Ar;
		  private CountDownLatch StartingGun;
		  private Random ThreadRandom;

		  public ThreadAnonymousInnerClassHelper<T1>(TestDocValuesWithThreads outerInstance, IList<T1> numbers, IList<BytesRef> binary, IList<BytesRef> sorted, int numDocs, AtomicReader ar, CountDownLatch startingGun, Random threadRandom)
		  {
			  this.OuterInstance = outerInstance;
			  this.Numbers = numbers;
			  this.Binary = binary;
			  this.Sorted = sorted;
			  this.NumDocs = numDocs;
			  this.Ar = ar;
			  this.StartingGun = startingGun;
			  this.ThreadRandom = threadRandom;
		  }

		  public override void Run()
		  {
			try
			{
			  //NumericDocValues ndv = ar.getNumericDocValues("number");
			  FieldCache.Longs ndv = FieldCache.DEFAULT.getLongs(Ar, "number", false);
			  //BinaryDocValues bdv = ar.getBinaryDocValues("bytes");
			  BinaryDocValues bdv = FieldCache.DEFAULT.getTerms(Ar, "bytes", false);
			  SortedDocValues sdv = FieldCache.DEFAULT.getTermsIndex(Ar, "sorted");
			  StartingGun.@await();
			  int iters = atLeast(1000);
			  BytesRef scratch = new BytesRef();
			  BytesRef scratch2 = new BytesRef();
			  for (int iter = 0;iter < iters;iter++)
			  {
				int docID = ThreadRandom.Next(NumDocs);
				switch (ThreadRandom.Next(6))
				{
				case 0:
				  Assert.AreEqual((long)(sbyte) Numbers[docID], FieldCache.DEFAULT.getBytes(Ar, "number", false).get(docID));
				  break;
				case 1:
				  Assert.AreEqual((long)(short) Numbers[docID], FieldCache.DEFAULT.getShorts(Ar, "number", false).get(docID));
				  break;
				case 2:
				  Assert.AreEqual((long)(int) Numbers[docID], FieldCache.DEFAULT.getInts(Ar, "number", false).get(docID));
				  break;
				case 3:
				  Assert.AreEqual((long)Numbers[docID], FieldCache.DEFAULT.getLongs(Ar, "number", false).get(docID));
				  break;
				case 4:
				  Assert.AreEqual(float.intBitsToFloat((long)(int) Numbers[docID]), FieldCache.DEFAULT.getFloats(Ar, "number", false).get(docID), 0.0f);
				  break;
				case 5:
				  Assert.AreEqual(double.longBitsToDouble((long)Numbers[docID]), FieldCache.DEFAULT.getDoubles(Ar, "number", false).get(docID), 0.0);
				  break;
				}
				bdv.get(docID, scratch);
				Assert.AreEqual(Binary[docID], scratch);
				// Cannot share a single scratch against two "sources":
				sdv.get(docID, scratch2);
				Assert.AreEqual(Sorted[docID], scratch2);
			  }
			}
			catch (Exception e)
			{
			  throw new Exception(e);
			}
		  }
	  }

	  public virtual void Test2()
	  {
		Random random = random();
		int NUM_DOCS = atLeast(100);
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random, dir);
		bool allowDups = random.nextBoolean();
		Set<string> seen = new HashSet<string>();
		if (VERBOSE)
		{
		  Console.WriteLine("TEST: NUM_DOCS=" + NUM_DOCS + " allowDups=" + allowDups);
		}
		int numDocs = 0;
		IList<BytesRef> docValues = new List<BytesRef>();

		// TODO: deletions
		while (numDocs < NUM_DOCS)
		{
		  string s;
		  if (random.nextBoolean())
		  {
			s = TestUtil.randomSimpleString(random);
		  }
		  else
		  {
			s = TestUtil.randomUnicodeString(random);
		  }
		  BytesRef br = new BytesRef(s);

		  if (!allowDups)
		  {
			if (seen.contains(s))
			{
			  continue;
			}
			seen.add(s);
		  }

		  if (VERBOSE)
		  {
			Console.WriteLine("  " + numDocs + ": s=" + s);
		  }

		  Document doc = new Document();
		  doc.add(new SortedDocValuesField("stringdv", br));
		  doc.add(new NumericDocValuesField("id", numDocs));
		  docValues.Add(br);
		  writer.addDocument(doc);
		  numDocs++;

		  if (random.Next(40) == 17)
		  {
			// force flush
			writer.Reader.close();
		  }
		}

		writer.forceMerge(1);
		DirectoryReader r = writer.Reader;
		writer.close();

		AtomicReader sr = getOnlySegmentReader(r);

		long END_TIME = System.currentTimeMillis() + (TEST_NIGHTLY ? 30 : 1);

		int NUM_THREADS = TestUtil.Next(random(), 1, 10);
		Thread[] threads = new Thread[NUM_THREADS];
		for (int thread = 0;thread < NUM_THREADS;thread++)
		{
		  threads[thread] = new ThreadAnonymousInnerClassHelper2(this, random, docValues, sr, END_TIME);
		  threads[thread].Start();
		}

		foreach (Thread thread in threads)
		{
		  thread.Join();
		}

		r.close();
		dir.close();
	  }

	  private class ThreadAnonymousInnerClassHelper2 : System.Threading.Thread
	  {
		  private readonly TestDocValuesWithThreads OuterInstance;

		  private Random Random;
		  private IList<BytesRef> DocValues;
		  private AtomicReader Sr;
		  private long END_TIME;

		  public ThreadAnonymousInnerClassHelper2(TestDocValuesWithThreads outerInstance, Random random, IList<BytesRef> docValues, AtomicReader sr, long END_TIME)
		  {
			  this.OuterInstance = outerInstance;
			  this.Random = random;
			  this.DocValues = docValues;
			  this.Sr = sr;
			  this.END_TIME = END_TIME;
		  }

		  public override void Run()
		  {
			Random random = random();
			SortedDocValues stringDVDirect;
			NumericDocValues docIDToID;
			try
			{
			  stringDVDirect = Sr.getSortedDocValues("stringdv");
			  docIDToID = Sr.getNumericDocValues("id");
			  Assert.IsNotNull(stringDVDirect);
			}
			catch (IOException ioe)
			{
			  throw new Exception(ioe);
			}
			while (System.currentTimeMillis() < END_TIME)
			{
			  SortedDocValues source;
			  source = stringDVDirect;
			  BytesRef scratch = new BytesRef();

			  for (int iter = 0;iter < 100;iter++)
			  {
				int docID = random.Next(Sr.maxDoc());
				source.get(docID, scratch);
				Assert.AreEqual(DocValues[(int) docIDToID.get(docID)], scratch);
			  }
			}
		  }
	  }

	}

}