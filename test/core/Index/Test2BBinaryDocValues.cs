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
	using BinaryDocValuesField = Lucene.Net.Document.BinaryDocValuesField;
	using Document = Lucene.Net.Document.Document;
	using BaseDirectoryWrapper = Lucene.Net.Store.BaseDirectoryWrapper;
	using ByteArrayDataInput = Lucene.Net.Store.ByteArrayDataInput;
	using ByteArrayDataOutput = Lucene.Net.Store.ByteArrayDataOutput;
	using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using SuppressCodecs = Lucene.Net.Util.LuceneTestCase.SuppressCodecs;
	using TimeUnits = Lucene.Net.Util.TimeUnits;
	using TestUtil = Lucene.Net.Util.TestUtil;
	using Ignore = org.junit.Ignore;

	using TimeoutSuite = com.carrotsearch.randomizedtesting.annotations.TimeoutSuite;

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @TimeoutSuite(millis = 80 * TimeUnits.HOUR) @Ignore("takes ~ 45 minutes") @SuppressCodecs("Lucene3x") public class Test2BBinaryDocValues extends Lucene.Net.Util.LuceneTestCase
	public class Test2BBinaryDocValues : LuceneTestCase
	{

	  // indexes Integer.MAX_VALUE docs with a fixed binary field
	  public virtual void TestFixedBinary()
	  {
		BaseDirectoryWrapper dir = newFSDirectory(createTempDir("2BFixedBinary"));
		if (dir is MockDirectoryWrapper)
		{
		  ((MockDirectoryWrapper)dir).Throttling = MockDirectoryWrapper.Throttling.NEVER;
		}

		IndexWriter w = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()))
	   .setMaxBufferedDocs(IndexWriterConfig.DISABLE_AUTO_FLUSH).setRAMBufferSizeMB(256.0).setMergeScheduler(new ConcurrentMergeScheduler()).setMergePolicy(newLogMergePolicy(false, 10)).setOpenMode(IndexWriterConfig.OpenMode_e.CREATE));

		Document doc = new Document();
		sbyte[] bytes = new sbyte[4];
		BytesRef data = new BytesRef(bytes);
		BinaryDocValuesField dvField = new BinaryDocValuesField("dv", data);
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
		int expectedValue = 0;
		foreach (AtomicReaderContext context in r.leaves())
		{
		  AtomicReader reader = context.reader();
		  BytesRef scratch = new BytesRef();
		  BinaryDocValues dv = reader.getBinaryDocValues("dv");
		  for (int i = 0; i < reader.maxDoc(); i++)
		  {
			bytes[0] = (sbyte)(expectedValue >> 24);
			bytes[1] = (sbyte)(expectedValue >> 16);
			bytes[2] = (sbyte)(expectedValue >> 8);
			bytes[3] = (sbyte) expectedValue;
			dv.get(i, scratch);
			Assert.AreEqual(data, scratch);
			expectedValue++;
		  }
		}

		r.close();
		dir.close();
	  }

	  // indexes Integer.MAX_VALUE docs with a variable binary field
	  public virtual void TestVariableBinary()
	  {
		BaseDirectoryWrapper dir = newFSDirectory(createTempDir("2BVariableBinary"));
		if (dir is MockDirectoryWrapper)
		{
		  ((MockDirectoryWrapper)dir).Throttling = MockDirectoryWrapper.Throttling.NEVER;
		}

		IndexWriter w = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()))
	   .setMaxBufferedDocs(IndexWriterConfig.DISABLE_AUTO_FLUSH).setRAMBufferSizeMB(256.0).setMergeScheduler(new ConcurrentMergeScheduler()).setMergePolicy(newLogMergePolicy(false, 10)).setOpenMode(IndexWriterConfig.OpenMode_e.CREATE));

		Document doc = new Document();
		sbyte[] bytes = new sbyte[4];
		ByteArrayDataOutput encoder = new ByteArrayDataOutput(bytes);
		BytesRef data = new BytesRef(bytes);
		BinaryDocValuesField dvField = new BinaryDocValuesField("dv", data);
		doc.add(dvField);

		for (int i = 0; i < int.MaxValue; i++)
		{
		  encoder.reset(bytes);
		  encoder.writeVInt(i % 65535); // 1, 2, or 3 bytes
		  data.length = encoder.Position;
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
		ByteArrayDataInput input = new ByteArrayDataInput();
		foreach (AtomicReaderContext context in r.leaves())
		{
		  AtomicReader reader = context.reader();
		  BytesRef scratch = new BytesRef(bytes);
		  BinaryDocValues dv = reader.getBinaryDocValues("dv");
		  for (int i = 0; i < reader.maxDoc(); i++)
		  {
			dv.get(i, scratch);
			input.reset(scratch.bytes, scratch.offset, scratch.length);
			Assert.AreEqual(expectedValue % 65535, input.readVInt());
			Assert.IsTrue(input.eof());
			expectedValue++;
		  }
		}

		r.close();
		dir.close();
	  }
	}

}