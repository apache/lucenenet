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
	using Directory = Lucene.Net.Store.Directory;
	using TestUtil = Lucene.Net.Util.TestUtil;

	public class TestTieredMergePolicy : BaseMergePolicyTestCase
	{

	  public virtual MergePolicy MergePolicy()
	  {
		return newTieredMergePolicy();
	  }

	  public virtual void TestForceMergeDeletes()
	  {
		Directory dir = newDirectory();
		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		TieredMergePolicy tmp = newTieredMergePolicy();
		conf.MergePolicy = tmp;
		conf.MaxBufferedDocs = 4;
		tmp.MaxMergeAtOnce = 100;
		tmp.SegmentsPerTier = 100;
		tmp.ForceMergeDeletesPctAllowed = 30.0;
		IndexWriter w = new IndexWriter(dir, conf);
		for (int i = 0;i < 80;i++)
		{
		  Document doc = new Document();
		  doc.add(newTextField("content", "aaa " + (i % 4), Field.Store.NO));
		  w.addDocument(doc);
		}
		Assert.AreEqual(80, w.maxDoc());
		Assert.AreEqual(80, w.numDocs());

		if (VERBOSE)
		{
		  Console.WriteLine("\nTEST: delete docs");
		}
		w.deleteDocuments(new Term("content", "0"));
		w.forceMergeDeletes();

		Assert.AreEqual(80, w.maxDoc());
		Assert.AreEqual(60, w.numDocs());

		if (VERBOSE)
		{
		  Console.WriteLine("\nTEST: forceMergeDeletes2");
		}
		((TieredMergePolicy) w.Config.MergePolicy).ForceMergeDeletesPctAllowed = 10.0;
		w.forceMergeDeletes();
		Assert.AreEqual(60, w.maxDoc());
		Assert.AreEqual(60, w.numDocs());
		w.close();
		dir.close();
	  }

	  public virtual void TestPartialMerge()
	  {
		int num = atLeast(10);
		for (int iter = 0;iter < num;iter++)
		{
		  if (VERBOSE)
		  {
			Console.WriteLine("TEST: iter=" + iter);
		  }
		  Directory dir = newDirectory();
		  IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		  conf.MergeScheduler = new SerialMergeScheduler();
		  TieredMergePolicy tmp = newTieredMergePolicy();
		  conf.MergePolicy = tmp;
		  conf.MaxBufferedDocs = 2;
		  tmp.MaxMergeAtOnce = 3;
		  tmp.SegmentsPerTier = 6;

		  IndexWriter w = new IndexWriter(dir, conf);
		  int maxCount = 0;
		  int numDocs = TestUtil.Next(random(), 20, 100);
		  for (int i = 0;i < numDocs;i++)
		  {
			Document doc = new Document();
			doc.add(newTextField("content", "aaa " + (i % 4), Field.Store.NO));
			w.addDocument(doc);
			int count = w.SegmentCount;
			maxCount = Math.Max(count, maxCount);
			Assert.IsTrue("count=" + count + " maxCount=" + maxCount, count >= maxCount - 3);
		  }

		  w.flush(true, true);

		  int segmentCount = w.SegmentCount;
		  int targetCount = TestUtil.Next(random(), 1, segmentCount);
		  if (VERBOSE)
		  {
			Console.WriteLine("TEST: merge to " + targetCount + " segs (current count=" + segmentCount + ")");
		  }
		  w.forceMerge(targetCount);
		  Assert.AreEqual(targetCount, w.SegmentCount);

		  w.close();
		  dir.close();
		}
	  }

	  public virtual void TestForceMergeDeletesMaxSegSize()
	  {
		Directory dir = newDirectory();
		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		TieredMergePolicy tmp = new TieredMergePolicy();
		tmp.MaxMergedSegmentMB = 0.01;
		tmp.ForceMergeDeletesPctAllowed = 0.0;
		conf.MergePolicy = tmp;

		RandomIndexWriter w = new RandomIndexWriter(random(), dir, conf);
		w.DoRandomForceMerge = false;

		int numDocs = atLeast(200);
		for (int i = 0;i < numDocs;i++)
		{
		  Document doc = new Document();
		  doc.add(newStringField("id", "" + i, Field.Store.NO));
		  doc.add(newTextField("content", "aaa " + i, Field.Store.NO));
		  w.addDocument(doc);
		}

		w.forceMerge(1);
		IndexReader r = w.Reader;
		Assert.AreEqual(numDocs, r.maxDoc());
		Assert.AreEqual(numDocs, r.numDocs());
		r.close();

		if (VERBOSE)
		{
		  Console.WriteLine("\nTEST: delete doc");
		}

		w.deleteDocuments(new Term("id", "" + (42 + 17)));

		r = w.Reader;
		Assert.AreEqual(numDocs, r.maxDoc());
		Assert.AreEqual(numDocs - 1, r.numDocs());
		r.close();

		w.forceMergeDeletes();

		r = w.Reader;
		Assert.AreEqual(numDocs - 1, r.maxDoc());
		Assert.AreEqual(numDocs - 1, r.numDocs());
		r.close();

		w.close();

		dir.close();
	  }

	  private const double EPSILON = 1E-14;

	  public virtual void TestSetters()
	  {
		TieredMergePolicy tmp = new TieredMergePolicy();

		tmp.MaxMergedSegmentMB = 0.5;
		Assert.AreEqual(0.5, tmp.MaxMergedSegmentMB, EPSILON);

		tmp.MaxMergedSegmentMB = double.PositiveInfinity;
		Assert.AreEqual(long.MaxValue / 1024 / 1024.0, tmp.MaxMergedSegmentMB, EPSILON * long.MaxValue);

		tmp.MaxMergedSegmentMB = long.MaxValue / 1024 / 1024.0;
		Assert.AreEqual(long.MaxValue / 1024 / 1024.0, tmp.MaxMergedSegmentMB, EPSILON * long.MaxValue);

		try
		{
		  tmp.MaxMergedSegmentMB = -2.0;
		  Assert.Fail("Didn't throw IllegalArgumentException");
		}
		catch (System.ArgumentException iae)
		{
		  // pass
		}

		tmp.FloorSegmentMB = 2.0;
		Assert.AreEqual(2.0, tmp.FloorSegmentMB, EPSILON);

		tmp.FloorSegmentMB = double.PositiveInfinity;
		Assert.AreEqual(long.MaxValue / 1024 / 1024.0, tmp.FloorSegmentMB, EPSILON * long.MaxValue);

		tmp.FloorSegmentMB = long.MaxValue / 1024 / 1024.0;
		Assert.AreEqual(long.MaxValue / 1024 / 1024.0, tmp.FloorSegmentMB, EPSILON * long.MaxValue);

		try
		{
		  tmp.FloorSegmentMB = -2.0;
		  Assert.Fail("Didn't throw IllegalArgumentException");
		}
		catch (System.ArgumentException iae)
		{
		  // pass
		}

		tmp.MaxCFSSegmentSizeMB = 2.0;
		Assert.AreEqual(2.0, tmp.MaxCFSSegmentSizeMB, EPSILON);

		tmp.MaxCFSSegmentSizeMB = double.PositiveInfinity;
		Assert.AreEqual(long.MaxValue / 1024 / 1024.0, tmp.MaxCFSSegmentSizeMB, EPSILON * long.MaxValue);

		tmp.MaxCFSSegmentSizeMB = long.MaxValue / 1024 / 1024.0;
		Assert.AreEqual(long.MaxValue / 1024 / 1024.0, tmp.MaxCFSSegmentSizeMB, EPSILON * long.MaxValue);

		try
		{
		  tmp.MaxCFSSegmentSizeMB = -2.0;
		  Assert.Fail("Didn't throw IllegalArgumentException");
		}
		catch (System.ArgumentException iae)
		{
		  // pass
		}

		// TODO: Add more checks for other non-double setters!
	  }
	}

}