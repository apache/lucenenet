using System;
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

	using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using TextField = Lucene.Net.Document.TextField;
	using Directory = Lucene.Net.Store.Directory;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

	public class TestIndexWriterNRTIsCurrent : LuceneTestCase
	{

	  public class ReaderHolder
	  {
		internal volatile DirectoryReader Reader;
		internal volatile bool Stop = false;
	  }

	  public virtual void TestIsCurrentWithThreads()
	  {
		Directory dir = newDirectory();
		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		IndexWriter writer = new IndexWriter(dir, conf);
		ReaderHolder holder = new ReaderHolder();
		ReaderThread[] threads = new ReaderThread[atLeast(3)];
		CountDownLatch latch = new CountDownLatch(1);
		WriterThread writerThread = new WriterThread(holder, writer, atLeast(500), random(), latch);
		for (int i = 0; i < threads.Length; i++)
		{
		  threads[i] = new ReaderThread(holder, latch);
		  threads[i].Start();
		}
		writerThread.Start();

		writerThread.Join();
		bool failed = writerThread.Failed != null;
		if (failed)
		{
		  Console.WriteLine(writerThread.Failed.ToString());
		  Console.Write(writerThread.Failed.StackTrace);
		}
		for (int i = 0; i < threads.Length; i++)
		{
		  threads[i].Join();
		  if (threads[i].Failed != null)
		  {
			Console.WriteLine(threads[i].Failed.ToString());
			Console.Write(threads[i].Failed.StackTrace);
			failed = true;
		  }
		}
		Assert.IsFalse(failed);
		writer.close();
		dir.close();

	  }

	  public class WriterThread : System.Threading.Thread
	  {
		internal readonly ReaderHolder Holder;
		internal readonly IndexWriter Writer;
		internal readonly int NumOps;
		internal bool Countdown = true;
		internal readonly CountDownLatch Latch;
		internal Exception Failed;

		internal WriterThread(ReaderHolder holder, IndexWriter writer, int numOps, Random random, CountDownLatch latch) : base()
		{
		  this.Holder = holder;
		  this.Writer = writer;
		  this.NumOps = numOps;
		  this.Latch = latch;
		}

		public override void Run()
		{
		  DirectoryReader currentReader = null;
		  Random random = LuceneTestCase.random();
		  try
		  {
			Document doc = new Document();
			doc.add(new TextField("id", "1", Field.Store.NO));
			Writer.addDocument(doc);
			Holder.Reader = currentReader = Writer.getReader(true);
			Term term = new Term("id");
			for (int i = 0; i < NumOps && !Holder.Stop; i++)
			{
			  float nextOp = random.nextFloat();
			  if (nextOp < 0.3)
			  {
				term.set("id", new BytesRef("1"));
				Writer.updateDocument(term, doc);
			  }
			  else if (nextOp < 0.5)
			  {
				Writer.addDocument(doc);
			  }
			  else
			  {
				term.set("id", new BytesRef("1"));
				Writer.deleteDocuments(term);
			  }
			  if (Holder.Reader != currentReader)
			  {
				Holder.Reader = currentReader;
				if (Countdown)
				{
				  Countdown = false;
				  Latch.countDown();
				}
			  }
			  if (random.nextBoolean())
			  {
				Writer.commit();
				DirectoryReader newReader = DirectoryReader.openIfChanged(currentReader);
				if (newReader != null)
				{
				  currentReader.decRef();
				  currentReader = newReader;
				}
				if (currentReader.numDocs() == 0)
				{
				  Writer.addDocument(doc);
				}
			  }
			}
		  }
		  catch (Exception e)
		  {
			Failed = e;
		  }
		  finally
		  {
			Holder.Reader = null;
			if (Countdown)
			{
			  Latch.countDown();
			}
			if (currentReader != null)
			{
			  try
			  {
				currentReader.decRef();
			  }
			  catch (IOException e)
			  {
			  }
			}
		  }
		  if (VERBOSE)
		  {
			Console.WriteLine("writer stopped - forced by reader: " + Holder.Stop);
		  }
		}

	  }

	  public sealed class ReaderThread : System.Threading.Thread
	  {
		internal readonly ReaderHolder Holder;
		internal readonly CountDownLatch Latch;
		internal Exception Failed;

		internal ReaderThread(ReaderHolder holder, CountDownLatch latch) : base()
		{
		  this.Holder = holder;
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
			Failed = e;
			return;
		  }
		  DirectoryReader reader;
		  while ((reader = Holder.Reader) != null)
		  {
			if (reader.tryIncRef())
			{
			  try
			  {
				bool current = reader.Current;
				if (VERBOSE)
				{
				  Console.WriteLine("Thread: " + Thread.CurrentThread + " Reader: " + reader + " isCurrent:" + current);
				}

				Assert.IsFalse(current);
			  }
			  catch (Exception e)
			  {
				if (VERBOSE)
				{
				  Console.WriteLine("FAILED Thread: " + Thread.CurrentThread + " Reader: " + reader + " isCurrent: false");
				}
				Failed = e;
				Holder.Stop = true;
				return;
			  }
			  finally
			  {
				try
				{
				  reader.decRef();
				}
				catch (IOException e)
				{
				  if (Failed == null)
				  {
					Failed = e;
				  }
				  return;
				}
			  }
			}
		  }
		}
	  }
	}

}