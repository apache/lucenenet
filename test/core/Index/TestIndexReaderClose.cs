using System.Collections.Generic;

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
	using AlreadyClosedException = Lucene.Net.Store.AlreadyClosedException;
	using Directory = Lucene.Net.Store.Directory;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;


	public class TestIndexReaderClose : LuceneTestCase
	{

	  public virtual void TestCloseUnderException()
	  {
		int iters = 1000 + 1 + random().Next(20);
		for (int j = 0; j < iters; j++)
		{
		  Directory dir = newDirectory();
		  IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(random(), TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		  writer.commit();
		  writer.close();
		  DirectoryReader open = DirectoryReader.open(dir);
		  bool throwOnClose = !rarely();
		  AtomicReader wrap = SlowCompositeReaderWrapper.wrap(open);
		  FilterAtomicReader reader = new FilterAtomicReaderAnonymousInnerClassHelper(this, wrap, throwOnClose);
		  IList<IndexReader.ReaderClosedListener> listeners = new List<IndexReader.ReaderClosedListener>();
		  int listenerCount = random().Next(20);
		  AtomicInteger count = new AtomicInteger();
		  bool faultySet = false;
		  for (int i = 0; i < listenerCount; i++)
		  {
			  if (rarely())
			  {
				faultySet = true;
				reader.addReaderClosedListener(new FaultyListener());
			  }
			  else
			  {
				count.incrementAndGet();
				reader.addReaderClosedListener(new CountListener(count));
			  }
		  }
		  if (!faultySet && !throwOnClose)
		  {
			reader.addReaderClosedListener(new FaultyListener());
		  }
		  try
		  {
			reader.close();
			Assert.Fail("expected Exception");
		  }
		  catch (IllegalStateException ex)
		  {
			if (throwOnClose)
			{
			  Assert.AreEqual("BOOM!", ex.Message);
			}
			else
			{
			  Assert.AreEqual("GRRRRRRRRRRRR!", ex.Message);
			}
		  }

		  try
		  {
			reader.fields();
			Assert.Fail("we are closed");
		  }
		  catch (AlreadyClosedException ex)
		  {
		  }

		  if (random().nextBoolean())
		  {
			reader.close(); // call it again
		  }
		  Assert.AreEqual(0, count.get());
		  wrap.close();
		  dir.close();
		}
	  }

	  private class FilterAtomicReaderAnonymousInnerClassHelper : FilterAtomicReader
	  {
		  private readonly TestIndexReaderClose OuterInstance;

		  private bool ThrowOnClose;

		  public FilterAtomicReaderAnonymousInnerClassHelper(TestIndexReaderClose outerInstance, AtomicReader wrap, bool throwOnClose) : base(wrap)
		  {
			  this.OuterInstance = outerInstance;
			  this.ThrowOnClose = throwOnClose;
		  }

		  protected internal override void DoClose()
		  {
			base.doClose();
			if (ThrowOnClose)
			{
			 throw new IllegalStateException("BOOM!");
			}
		  }
	  }

	  private sealed class CountListener : IndexReader.ReaderClosedListener
	  {
		internal readonly AtomicInteger Count;

		public CountListener(AtomicInteger count)
		{
		  this.Count = count;
		}

		public override void OnClose(IndexReader reader)
		{
		  Count.decrementAndGet();
		}
	  }

	  private sealed class FaultyListener : IndexReader.ReaderClosedListener
	  {

		public override void OnClose(IndexReader reader)
		{
		  throw new IllegalStateException("GRRRRRRRRRRRR!");
		}
	  }

	}

}