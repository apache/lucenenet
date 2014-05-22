using System;
using System.Collections;
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

	using DeleteSlice = Lucene.Net.Index.DocumentsWriterDeleteQueue.DeleteSlice;
	using TermQuery = Lucene.Net.Search.TermQuery;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using ThreadInterruptedException = Lucene.Net.Util.ThreadInterruptedException;

	/// <summary>
	/// Unit test for <seealso cref="DocumentsWriterDeleteQueue"/>
	/// </summary>
	public class TestDocumentsWriterDeleteQueue : LuceneTestCase
	{

	  public virtual void TestUpdateDelteSlices()
	  {
		DocumentsWriterDeleteQueue queue = new DocumentsWriterDeleteQueue();
		int size = 200 + random().Next(500) * RANDOM_MULTIPLIER;
		int?[] ids = new int?[size];
		for (int i = 0; i < ids.Length; i++)
		{
		  ids[i] = random().Next();
		}
		DeleteSlice slice1 = queue.newSlice();
		DeleteSlice slice2 = queue.newSlice();
		BufferedUpdates bd1 = new BufferedUpdates();
		BufferedUpdates bd2 = new BufferedUpdates();
		int last1 = 0;
		int last2 = 0;
		Set<Term> uniqueValues = new HashSet<Term>();
		for (int j = 0; j < ids.Length; j++)
		{
		  int? i = ids[j];
		  // create an array here since we compare identity below against tailItem
		  Term[] term = new Term[] {new Term("id", i.ToString())};
		  uniqueValues.add(term[0]);
		  queue.addDelete(term);
		  if (random().Next(20) == 0 || j == ids.Length - 1)
		  {
			queue.updateSlice(slice1);
			Assert.IsTrue(slice1.isTailItem(term));
			slice1.apply(bd1, j);
			AssertAllBetween(last1, j, bd1, ids);
			last1 = j + 1;
		  }
		  if (random().Next(10) == 5 || j == ids.Length - 1)
		  {
			queue.updateSlice(slice2);
			Assert.IsTrue(slice2.isTailItem(term));
			slice2.apply(bd2, j);
			AssertAllBetween(last2, j, bd2, ids);
			last2 = j + 1;
		  }
		  Assert.AreEqual(j + 1, queue.numGlobalTermDeletes());
		}
		Assert.AreEqual(uniqueValues, bd1.terms.Keys);
		Assert.AreEqual(uniqueValues, bd2.terms.Keys);
		HashSet<Term> frozenSet = new HashSet<Term>();
		foreach (Term t in queue.freezeGlobalBuffer(null).termsIterable())
		{
		  BytesRef bytesRef = new BytesRef();
		  bytesRef.copyBytes(t.bytes);
		  frozenSet.Add(new Term(t.field, bytesRef));
		}
		Assert.AreEqual(uniqueValues, frozenSet);
		Assert.AreEqual("num deletes must be 0 after freeze", 0, queue.numGlobalTermDeletes());
	  }

	  private void AssertAllBetween(int start, int end, BufferedUpdates deletes, int?[] ids)
	  {
		for (int i = start; i <= end; i++)
		{
		  Assert.AreEqual(Convert.ToInt32(end), deletes.terms.get(new Term("id", ids[i].ToString())));
		}
	  }

	  public virtual void TestClear()
	  {
		DocumentsWriterDeleteQueue queue = new DocumentsWriterDeleteQueue();
		Assert.IsFalse(queue.anyChanges());
		queue.clear();
		Assert.IsFalse(queue.anyChanges());
		int size = 200 + random().Next(500) * RANDOM_MULTIPLIER;
		int termsSinceFreeze = 0;
		int queriesSinceFreeze = 0;
		for (int i = 0; i < size; i++)
		{
		  Term term = new Term("id", "" + i);
		  if (random().Next(10) == 0)
		  {
			queue.addDelete(new TermQuery(term));
			queriesSinceFreeze++;
		  }
		  else
		  {
			queue.addDelete(term);
			termsSinceFreeze++;
		  }
		  Assert.IsTrue(queue.anyChanges());
		  if (random().Next(10) == 0)
		  {
			queue.clear();
			queue.tryApplyGlobalSlice();
			Assert.IsFalse(queue.anyChanges());
		  }
		}

	  }

	  public virtual void TestAnyChanges()
	  {
		DocumentsWriterDeleteQueue queue = new DocumentsWriterDeleteQueue();
		int size = 200 + random().Next(500) * RANDOM_MULTIPLIER;
		int termsSinceFreeze = 0;
		int queriesSinceFreeze = 0;
		for (int i = 0; i < size; i++)
		{
		  Term term = new Term("id", "" + i);
		  if (random().Next(10) == 0)
		  {
			queue.addDelete(new TermQuery(term));
			queriesSinceFreeze++;
		  }
		  else
		  {
			queue.addDelete(term);
			termsSinceFreeze++;
		  }
		  Assert.IsTrue(queue.anyChanges());
		  if (random().Next(5) == 0)
		  {
			FrozenBufferedUpdates freezeGlobalBuffer = queue.freezeGlobalBuffer(null);
			Assert.AreEqual(termsSinceFreeze, freezeGlobalBuffer.termCount);
			Assert.AreEqual(queriesSinceFreeze, freezeGlobalBuffer.queries.length);
			queriesSinceFreeze = 0;
			termsSinceFreeze = 0;
			Assert.IsFalse(queue.anyChanges());
		  }
		}
	  }

	  public virtual void TestPartiallyAppliedGlobalSlice()
	  {
		DocumentsWriterDeleteQueue queue = new DocumentsWriterDeleteQueue();
		Field field = typeof(DocumentsWriterDeleteQueue).getDeclaredField("globalBufferLock");
		field.Accessible = true;
		ReentrantLock @lock = (ReentrantLock) field.get(queue);
		@lock.@lock();
		Thread t = new ThreadAnonymousInnerClassHelper(this, queue);
		t.Start();
		t.Join();
		@lock.unlock();
		Assert.IsTrue("changes in del queue but not in slice yet", queue.anyChanges());
		queue.tryApplyGlobalSlice();
		Assert.IsTrue("changes in global buffer", queue.anyChanges());
		FrozenBufferedUpdates freezeGlobalBuffer = queue.freezeGlobalBuffer(null);
		Assert.IsTrue(freezeGlobalBuffer.any());
		Assert.AreEqual(1, freezeGlobalBuffer.termCount);
		Assert.IsFalse("all changes applied", queue.anyChanges());
	  }

	  private class ThreadAnonymousInnerClassHelper : System.Threading.Thread
	  {
		  private readonly TestDocumentsWriterDeleteQueue OuterInstance;

		  private DocumentsWriterDeleteQueue Queue;

		  public ThreadAnonymousInnerClassHelper(TestDocumentsWriterDeleteQueue outerInstance, DocumentsWriterDeleteQueue queue)
		  {
			  this.OuterInstance = outerInstance;
			  this.Queue = queue;
		  }

		  public override void Run()
		  {
			Queue.addDelete(new Term("foo", "bar"));
		  }
	  }

	  public virtual void TestStressDeleteQueue()
	  {
		DocumentsWriterDeleteQueue queue = new DocumentsWriterDeleteQueue();
		Set<Term> uniqueValues = new HashSet<Term>();
		int size = 10000 + random().Next(500) * RANDOM_MULTIPLIER;
		int?[] ids = new int?[size];
		for (int i = 0; i < ids.Length; i++)
		{
		  ids[i] = random().Next();
		  uniqueValues.add(new Term("id", ids[i].ToString()));
		}
		CountDownLatch latch = new CountDownLatch(1);
		AtomicInteger index = new AtomicInteger(0);
		int numThreads = 2 + random().Next(5);
		UpdateThread[] threads = new UpdateThread[numThreads];
		for (int i = 0; i < threads.Length; i++)
		{
		  threads[i] = new UpdateThread(queue, index, ids, latch);
		  threads[i].Start();
		}
		latch.countDown();
		for (int i = 0; i < threads.Length; i++)
		{
		  threads[i].Join();
		}

		foreach (UpdateThread updateThread in threads)
		{
		  DeleteSlice slice = updateThread.Slice;
		  queue.updateSlice(slice);
		  BufferedUpdates deletes = updateThread.Deletes;
		  slice.apply(deletes, BufferedUpdates.MAX_INT);
		  Assert.AreEqual(uniqueValues, deletes.terms.Keys);
		}
		queue.tryApplyGlobalSlice();
		Set<Term> frozenSet = new HashSet<Term>();
		foreach (Term t in queue.freezeGlobalBuffer(null).termsIterable())
		{
		  BytesRef bytesRef = new BytesRef();
		  bytesRef.copyBytes(t.bytes);
		  frozenSet.add(new Term(t.field, bytesRef));
		}
		Assert.AreEqual("num deletes must be 0 after freeze", 0, queue.numGlobalTermDeletes());
		Assert.AreEqual(uniqueValues.size(), frozenSet.size());
		Assert.AreEqual(uniqueValues, frozenSet);

	  }

	  private class UpdateThread : System.Threading.Thread
	  {
		internal readonly DocumentsWriterDeleteQueue Queue;
		internal readonly AtomicInteger Index;
		internal readonly int?[] Ids;
		internal readonly DeleteSlice Slice;
		internal readonly BufferedUpdates Deletes;
		internal readonly CountDownLatch Latch;

		protected internal UpdateThread(DocumentsWriterDeleteQueue queue, AtomicInteger index, int?[] ids, CountDownLatch latch)
		{
		  this.Queue = queue;
		  this.Index = index;
		  this.Ids = ids;
		  this.Slice = queue.newSlice();
		  Deletes = new BufferedUpdates();
		  this.Latch = latch;
		}

		public override void Run()
		{
		  try
		  {
			Latch.@await();
		  }
		  catch (InterruptedException e)
		  {
			throw new ThreadInterruptedException(e);
		  }
		  int i = 0;
		  while ((i = Index.AndIncrement) < Ids.Length)
		  {
			Term term = new Term("id", Ids[i].ToString());
			Queue.add(term, Slice);
			Assert.IsTrue(Slice.isTailItem(term));
			Slice.apply(Deletes, BufferedUpdates.MAX_INT);
		  }
		}
	  }

	}

}