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
	using NumericDocValuesField = Lucene.Net.Document.NumericDocValuesField;
	using BaseDirectoryWrapper = Lucene.Net.Store.BaseDirectoryWrapper;
	using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;
	using TimeUnits = Lucene.Net.Util.TimeUnits;
	using SuppressCodecs = Lucene.Net.Util.LuceneTestCase.SuppressCodecs;
	using Ignore = org.junit.Ignore;

	using TimeoutSuite = com.carrotsearch.randomizedtesting.annotations.TimeoutSuite;

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @TimeoutSuite(millis = 80 * TimeUnits.HOUR) @Ignore("takes ~ 30 minutes") @SuppressCodecs("Lucene3x") public class Test2BNumericDocValues extends Lucene.Net.Util.LuceneTestCase
	public class Test2BNumericDocValues : LuceneTestCase
	{

	  // indexes Integer.MAX_VALUE docs with an increasing dv field
	  public virtual void TestNumerics()
	  {
		BaseDirectoryWrapper dir = newFSDirectory(createTempDir("2BNumerics"));
		if (dir is MockDirectoryWrapper)
		{
		  ((MockDirectoryWrapper)dir).Throttling = MockDirectoryWrapper.Throttling.NEVER;
		}

		IndexWriter w = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()))
	   .setMaxBufferedDocs(IndexWriterConfig.DISABLE_AUTO_FLUSH).setRAMBufferSizeMB(256.0).setMergeScheduler(new ConcurrentMergeScheduler()).setMergePolicy(newLogMergePolicy(false, 10)).setOpenMode(IndexWriterConfig.OpenMode_e.CREATE));

		Document doc = new Document();
		NumericDocValuesField dvField = new NumericDocValuesField("dv", 0);
		doc.add(dvField);

		for (int i = 0; i < int.MaxValue; i++)
		{
		  dvField.LongValue = i;
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
		long expectedValue = 0;
		foreach (AtomicReaderContext context in r.leaves())
		{
		  AtomicReader reader = context.reader();
		  NumericDocValues dv = reader.getNumericDocValues("dv");
		  for (int i = 0; i < reader.maxDoc(); i++)
		  {
			Assert.AreEqual(expectedValue, dv.get(i));
			expectedValue++;
		  }
		}

		r.close();
		dir.close();
	  }
	}

}