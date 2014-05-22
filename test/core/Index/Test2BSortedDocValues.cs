using System;

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
	using Document = Lucene.Net.Document.Document;
	using SortedDocValuesField = Lucene.Net.Document.SortedDocValuesField;
	using BaseDirectoryWrapper = Lucene.Net.Store.BaseDirectoryWrapper;
	using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using SuppressCodecs = Lucene.Net.Util.LuceneTestCase.SuppressCodecs;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TimeUnits = Lucene.Net.Util.TimeUnits;
	using Ignore = org.junit.Ignore;
	using TimeoutSuite = com.carrotsearch.randomizedtesting.annotations.TimeoutSuite;

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @TimeoutSuite(millis = 80 * TimeUnits.HOUR) @Ignore("very slow") @SuppressCodecs("Lucene3x") public class Test2BSortedDocValues extends Lucene.Net.Util.LuceneTestCase
	public class Test2BSortedDocValues : LuceneTestCase
	{

	  // indexes Integer.MAX_VALUE docs with a fixed binary field
	  public virtual void TestFixedSorted()
	  {
		BaseDirectoryWrapper dir = newFSDirectory(createTempDir("2BFixedSorted"));
		if (dir is MockDirectoryWrapper)
		{
		  ((MockDirectoryWrapper)dir).Throttling = MockDirectoryWrapper.Throttling.NEVER;
		}

		IndexWriter w = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()))
	   .setMaxBufferedDocs(IndexWriterConfig.DISABLE_AUTO_FLUSH).setRAMBufferSizeMB(256.0).setMergeScheduler(new ConcurrentMergeScheduler()).setMergePolicy(newLogMergePolicy(false, 10)).setOpenMode(IndexWriterConfig.OpenMode.CREATE));

		Document doc = new Document();
		sbyte[] bytes = new sbyte[2];
		BytesRef data = new BytesRef(bytes);
		SortedDocValuesField dvField = new SortedDocValuesField("dv", data);
		doc.add(dvField);

		for (int i = 0; i < int.MaxValue; i++)
		{
		  bytes[0] = (sbyte)(i >> 8);
		  bytes[1] = (sbyte) i;
		  w.addDocument(doc);
		  if (i % 100000 == 0)
		  {
			Console.WriteLine("indexed: " + i);
			System.out.flush();
		  }
		}

		w.forceMerge(1);
		w.close();

		Console.WriteLine("verifying...");
		System.out.flush();

		DirectoryReader r = DirectoryReader.open(dir);
		int expectedValue = 0;
		foreach (AtomicReaderContext context in r.leaves())
		{
		  AtomicReader reader = context.reader();
		  BytesRef scratch = new BytesRef();
		  BinaryDocValues dv = reader.getSortedDocValues("dv");
		  for (int i = 0; i < reader.maxDoc(); i++)
		  {
			bytes[0] = (sbyte)(expectedValue >> 8);
			bytes[1] = (sbyte) expectedValue;
			dv.get(i, scratch);
			Assert.AreEqual(data, scratch);
			expectedValue++;
		  }
		}

		r.close();
		dir.close();
	  }

	  // indexes Integer.MAX_VALUE docs with a fixed binary field
	  public virtual void Test2BOrds()
	  {
		BaseDirectoryWrapper dir = newFSDirectory(createTempDir("2BOrds"));
		if (dir is MockDirectoryWrapper)
		{
		  ((MockDirectoryWrapper)dir).Throttling = MockDirectoryWrapper.Throttling.NEVER;
		}

		IndexWriter w = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()))
	   .setMaxBufferedDocs(IndexWriterConfig.DISABLE_AUTO_FLUSH).setRAMBufferSizeMB(256.0).setMergeScheduler(new ConcurrentMergeScheduler()).setMergePolicy(newLogMergePolicy(false, 10)).setOpenMode(IndexWriterConfig.OpenMode.CREATE));

		Document doc = new Document();
		sbyte[] bytes = new sbyte[4];
		BytesRef data = new BytesRef(bytes);
		SortedDocValuesField dvField = new SortedDocValuesField("dv", data);
		doc.add(dvField);

		for (int i = 0; i < int.MaxValue; i++)
		{
		  bytes[0] = (sbyte)(i >> 24);
		  bytes[1] = (sbyte)(i >> 16);
		  bytes[2] = (sbyte)(i >> 8);
		  bytes[3] = (sbyte) i;
		  w.addDocument(doc);
		  if (i % 100000 == 0)
		  {
			Console.WriteLine("indexed: " + i);
			System.out.flush();
		  }
		}

		w.forceMerge(1);
		w.close();

		Console.WriteLine("verifying...");
		System.out.flush();

		DirectoryReader r = DirectoryReader.open(dir);
		int counter = 0;
		foreach (AtomicReaderContext context in r.leaves())
		{
		  AtomicReader reader = context.reader();
		  BytesRef scratch = new BytesRef();
		  BinaryDocValues dv = reader.getSortedDocValues("dv");
		  for (int i = 0; i < reader.maxDoc(); i++)
		  {
			bytes[0] = (sbyte)(counter >> 24);
			bytes[1] = (sbyte)(counter >> 16);
			bytes[2] = (sbyte)(counter >> 8);
			bytes[3] = (sbyte) counter;
			counter++;
			dv.get(i, scratch);
			Assert.AreEqual(data, scratch);
		  }
		}

		r.close();
		dir.close();
	  }

	  // TODO: variable
	}

}