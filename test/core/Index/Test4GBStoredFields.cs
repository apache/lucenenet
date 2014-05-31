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
	using Field = Lucene.Net.Document.Field;
	using FieldType = Lucene.Net.Document.FieldType;
	using MMapDirectory = Lucene.Net.Store.MMapDirectory;
	using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;
	using TimeUnits = Lucene.Net.Util.TimeUnits;
	using TestUtil = Lucene.Net.Util.TestUtil;
	using SuppressCodecs = Lucene.Net.Util.LuceneTestCase.SuppressCodecs;

	using TimeoutSuite = com.carrotsearch.randomizedtesting.annotations.TimeoutSuite;
	using RandomInts = com.carrotsearch.randomizedtesting.generators.RandomInts;

	/// <summary>
	/// this test creates an index with one segment that is a little larger than 4GB.
	/// </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressCodecs({ "SimpleText" }) @TimeoutSuite(millis = 4 * TimeUnits.HOUR) public class Test4GBStoredFields extends Lucene.Net.Util.LuceneTestCase
	public class Test4GBStoredFields : LuceneTestCase
	{
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Nightly public void test() throws Exception
		public virtual void Test()
		{
		MockDirectoryWrapper dir = new MockDirectoryWrapper(random(), new MMapDirectory(createTempDir("4GBStoredFields")));
		dir.Throttling = MockDirectoryWrapper.Throttling.NEVER;

		IndexWriter w = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()))
	   .setMaxBufferedDocs(IndexWriterConfig.DISABLE_AUTO_FLUSH).setRAMBufferSizeMB(256.0).setMergeScheduler(new ConcurrentMergeScheduler()).setMergePolicy(newLogMergePolicy(false, 10)).setOpenMode(IndexWriterConfig.OpenMode_e.CREATE));

		MergePolicy mp = w.Config.MergePolicy;
		if (mp is LogByteSizeMergePolicy)
		{
		 // 1 petabyte:
		 ((LogByteSizeMergePolicy) mp).MaxMergeMB = 1024 * 1024 * 1024;
		}

		Document doc = new Document();
		FieldType ft = new FieldType();
		ft.Indexed = false;
		ft.Stored = true;
		ft.freeze();
		int valueLength = RandomInts.randomIntBetween(random(), 1 << 13, 1 << 20);
		sbyte[] value = new sbyte[valueLength];
		for (int i = 0; i < valueLength; ++i)
		{
		  // random so that even compressing codecs can't compress it
		  value[i] = (sbyte) random().Next(256);
		}
		Field f = new Field("fld", value, ft);
		doc.add(f);

		int numDocs = (int)((1L << 32) / valueLength + 100);
		for (int i = 0; i < numDocs; ++i)
		{
		  w.addDocument(doc);
		  if (VERBOSE && i % (numDocs / 10) == 0)
		  {
			Console.WriteLine(i + " of " + numDocs + "...");
		  }
		}
		w.forceMerge(1);
		w.close();
		if (VERBOSE)
		{
		  bool found = false;
		  foreach (string file in dir.listAll())
		  {
			if (file.EndsWith(".fdt"))
			{
			  long fileLength = dir.fileLength(file);
			  if (fileLength >= 1L << 32)
			  {
				found = true;
			  }
			  Console.WriteLine("File length of " + file + " : " + fileLength);
			}
		  }
		  if (!found)
		  {
			Console.WriteLine("No .fdt file larger than 4GB, test bug?");
		  }
		}

		DirectoryReader rd = DirectoryReader.open(dir);
		Document sd = rd.document(numDocs - 1);
		Assert.IsNotNull(sd);
		Assert.AreEqual(1, sd.Fields.size());
		BytesRef valueRef = sd.getBinaryValue("fld");
		Assert.IsNotNull(valueRef);
		Assert.AreEqual(new BytesRef(value), valueRef);
		rd.close();

		dir.close();
		}

	}

}