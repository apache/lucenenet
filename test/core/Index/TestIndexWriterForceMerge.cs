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
	using OpenMode = Lucene.Net.Index.IndexWriterConfig.OpenMode_e;
	using Directory = Lucene.Net.Store.Directory;
	using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;

	public class TestIndexWriterForceMerge : LuceneTestCase
	{
	  public virtual void TestPartialMerge()
	  {

		Directory dir = newDirectory();

		Document doc = new Document();
		doc.add(newStringField("content", "aaa", Field.Store.NO));
		int incrMin = TEST_NIGHTLY ? 15 : 40;
		for (int numDocs = 10;numDocs < 500;numDocs += TestUtil.Next(random(), incrMin, 5 * incrMin))
		{
		  LogDocMergePolicy ldmp = new LogDocMergePolicy();
		  ldmp.MinMergeDocs = 1;
		  ldmp.MergeFactor = 5;
		  IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.CREATE).setMaxBufferedDocs(2).setMergePolicy(ldmp));
		  for (int j = 0;j < numDocs;j++)
		  {
			writer.addDocument(doc);
		  }
		  writer.close();

		  SegmentInfos sis = new SegmentInfos();
		  sis.read(dir);
		  int segCount = sis.size();

		  ldmp = new LogDocMergePolicy();
		  ldmp.MergeFactor = 5;
		  writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMergePolicy(ldmp));
		  writer.forceMerge(3);
		  writer.close();

		  sis = new SegmentInfos();
		  sis.read(dir);
		  int optSegCount = sis.size();

		  if (segCount < 3)
		  {
			Assert.AreEqual(segCount, optSegCount);
		  }
		  else
		  {
			Assert.AreEqual(3, optSegCount);
		  }
		}
		dir.close();
	  }

	  public virtual void TestMaxNumSegments2()
	  {
		Directory dir = newDirectory();

		Document doc = new Document();
		doc.add(newStringField("content", "aaa", Field.Store.NO));

		LogDocMergePolicy ldmp = new LogDocMergePolicy();
		ldmp.MinMergeDocs = 1;
		ldmp.MergeFactor = 4;
		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(2).setMergePolicy(ldmp).setMergeScheduler(new ConcurrentMergeScheduler()));

		for (int iter = 0;iter < 10;iter++)
		{
		  for (int i = 0;i < 19;i++)
		  {
			writer.addDocument(doc);
		  }

		  writer.commit();
		  writer.waitForMerges();
		  writer.commit();

		  SegmentInfos sis = new SegmentInfos();
		  sis.read(dir);

		  int segCount = sis.size();
		  writer.forceMerge(7);
		  writer.commit();
		  writer.waitForMerges();

		  sis = new SegmentInfos();
		  sis.read(dir);
		  int optSegCount = sis.size();

		  if (segCount < 7)
		  {
			Assert.AreEqual(segCount, optSegCount);
		  }
		  else
		  {
			Assert.AreEqual("seg: " + segCount, 7, optSegCount);
		  }
		}
		writer.close();
		dir.close();
	  }

	  /// <summary>
	  /// Make sure forceMerge doesn't use any more than 1X
	  /// starting index size as its temporary free space
	  /// required.
	  /// </summary>
	  public virtual void TestForceMergeTempSpaceUsage()
	  {

		MockDirectoryWrapper dir = newMockDirectory();
		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(10).setMergePolicy(newLogMergePolicy()));
		if (VERBOSE)
		{
		  Console.WriteLine("TEST: config1=" + writer.Config);
		}

		for (int j = 0;j < 500;j++)
		{
		  TestIndexWriter.AddDocWithIndex(writer, j);
		}
		int termIndexInterval = writer.Config.TermIndexInterval;
		// force one extra segment w/ different doc store so
		// we see the doc stores get merged
		writer.commit();
		TestIndexWriter.AddDocWithIndex(writer, 500);
		writer.close();

		if (VERBOSE)
		{
		  Console.WriteLine("TEST: start disk usage");
		}
		long startDiskUsage = 0;
		string[] files = dir.listAll();
		for (int i = 0;i < files.Length;i++)
		{
		  startDiskUsage += dir.fileLength(files[i]);
		  if (VERBOSE)
		  {
			Console.WriteLine(files[i] + ": " + dir.fileLength(files[i]));
		  }
		}

		dir.resetMaxUsedSizeInBytes();
		dir.TrackDiskUsage = true;

		// Import to use same term index interval else a
		// smaller one here could increase the disk usage and
		// cause a false failure:
		writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.APPEND).setTermIndexInterval(termIndexInterval).setMergePolicy(newLogMergePolicy()));
		writer.forceMerge(1);
		writer.close();
		long maxDiskUsage = dir.MaxUsedSizeInBytes;
		Assert.IsTrue("forceMerge used too much temporary space: starting usage was " + startDiskUsage + " bytes; max temp usage was " + maxDiskUsage + " but should have been " + (4 * startDiskUsage) + " (= 4X starting usage)", maxDiskUsage <= 4 * startDiskUsage);
		dir.close();
	  }

	  // Test calling forceMerge(1, false) whereby forceMerge is kicked
	  // off but we don't wait for it to finish (but
	  // writer.close()) does wait
	  public virtual void TestBackgroundForceMerge()
	  {

		Directory dir = newDirectory();
		for (int pass = 0;pass < 2;pass++)
		{
		  IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.CREATE).setMaxBufferedDocs(2).setMergePolicy(newLogMergePolicy(51)));
		  Document doc = new Document();
		  doc.add(newStringField("field", "aaa", Field.Store.NO));
		  for (int i = 0;i < 100;i++)
		  {
			writer.addDocument(doc);
		  }
		  writer.forceMerge(1, false);

		  if (0 == pass)
		  {
			writer.close();
			DirectoryReader reader = DirectoryReader.open(dir);
			Assert.AreEqual(1, reader.leaves().size());
			reader.close();
		  }
		  else
		  {
			// Get another segment to flush so we can verify it is
			// NOT included in the merging
			writer.addDocument(doc);
			writer.addDocument(doc);
			writer.close();

			DirectoryReader reader = DirectoryReader.open(dir);
			Assert.IsTrue(reader.leaves().size() > 1);
			reader.close();

			SegmentInfos infos = new SegmentInfos();
			infos.read(dir);
			Assert.AreEqual(2, infos.size());
		  }
		}

		dir.close();
	  }
	}

}