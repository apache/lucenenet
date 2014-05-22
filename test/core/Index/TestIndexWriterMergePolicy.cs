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
	using OpenMode = Lucene.Net.Index.IndexWriterConfig.OpenMode;
	using Directory = Lucene.Net.Store.Directory;

	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

	public class TestIndexWriterMergePolicy : LuceneTestCase
	{

	  // Test the normal case
	  public virtual void TestNormalCase()
	  {
		Directory dir = newDirectory();

		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(10).setMergePolicy(new LogDocMergePolicy()));

		for (int i = 0; i < 100; i++)
		{
		  AddDoc(writer);
		  CheckInvariants(writer);
		}

		writer.close();
		dir.close();
	  }

	  // Test to see if there is over merge
	  public virtual void TestNoOverMerge()
	  {
		Directory dir = newDirectory();

		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(10).setMergePolicy(new LogDocMergePolicy()));

		bool noOverMerge = false;
		for (int i = 0; i < 100; i++)
		{
		  AddDoc(writer);
		  CheckInvariants(writer);
		  if (writer.NumBufferedDocuments + writer.SegmentCount >= 18)
		  {
			noOverMerge = true;
		  }
		}
		Assert.IsTrue(noOverMerge);

		writer.close();
		dir.close();
	  }

	  // Test the case where flush is forced after every addDoc
	  public virtual void TestForceFlush()
	  {
		Directory dir = newDirectory();

		LogDocMergePolicy mp = new LogDocMergePolicy();
		mp.MinMergeDocs = 100;
		mp.MergeFactor = 10;
		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(10).setMergePolicy(mp));

		for (int i = 0; i < 100; i++)
		{
		  AddDoc(writer);
		  writer.close();

		  mp = new LogDocMergePolicy();
		  mp.MergeFactor = 10;
		  writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.APPEND).setMaxBufferedDocs(10).setMergePolicy(mp));
		  mp.MinMergeDocs = 100;
		  CheckInvariants(writer);
		}

		writer.close();
		dir.close();
	  }

	  // Test the case where mergeFactor changes
	  public virtual void TestMergeFactorChange()
	  {
		Directory dir = newDirectory();

		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(10).setMergePolicy(newLogMergePolicy()).setMergeScheduler(new SerialMergeScheduler()));

		for (int i = 0; i < 250; i++)
		{
		  AddDoc(writer);
		  CheckInvariants(writer);
		}

		((LogMergePolicy) writer.Config.MergePolicy).MergeFactor = 5;

		// merge policy only fixes segments on levels where merges
		// have been triggered, so check invariants after all adds
		for (int i = 0; i < 10; i++)
		{
		  AddDoc(writer);
		}
		CheckInvariants(writer);

		writer.close();
		dir.close();
	  }

	  // Test the case where both mergeFactor and maxBufferedDocs change
	  public virtual void TestMaxBufferedDocsChange()
	  {
		Directory dir = newDirectory();

		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(101).setMergePolicy(new LogDocMergePolicy()).setMergeScheduler(new SerialMergeScheduler()));

		// leftmost* segment has 1 doc
		// rightmost* segment has 100 docs
		for (int i = 1; i <= 100; i++)
		{
		  for (int j = 0; j < i; j++)
		  {
			AddDoc(writer);
			CheckInvariants(writer);
		  }
		  writer.close();

		  writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.APPEND).setMaxBufferedDocs(101).setMergePolicy(new LogDocMergePolicy()).setMergeScheduler(new SerialMergeScheduler()));
		}

		writer.close();
		LogDocMergePolicy ldmp = new LogDocMergePolicy();
		ldmp.MergeFactor = 10;
		writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.APPEND).setMaxBufferedDocs(10).setMergePolicy(ldmp).setMergeScheduler(new SerialMergeScheduler()));

		// merge policy only fixes segments on levels where merges
		// have been triggered, so check invariants after all adds
		for (int i = 0; i < 100; i++)
		{
		  AddDoc(writer);
		}
		CheckInvariants(writer);

		for (int i = 100; i < 1000; i++)
		{
		  AddDoc(writer);
		}
		writer.commit();
		writer.waitForMerges();
		writer.commit();
		CheckInvariants(writer);

		writer.close();
		dir.close();
	  }

	  // Test the case where a merge results in no doc at all
	  public virtual void TestMergeDocCount0()
	  {
		Directory dir = newDirectory();

		LogDocMergePolicy ldmp = new LogDocMergePolicy();
		ldmp.MergeFactor = 100;
		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(10).setMergePolicy(ldmp));

		for (int i = 0; i < 250; i++)
		{
		  AddDoc(writer);
		  CheckInvariants(writer);
		}
		writer.close();

		// delete some docs without merging
		writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMergePolicy(NoMergePolicy.NO_COMPOUND_FILES));
		writer.deleteDocuments(new Term("content", "aaa"));
		writer.close();

		ldmp = new LogDocMergePolicy();
		ldmp.MergeFactor = 5;
		writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.APPEND).setMaxBufferedDocs(10).setMergePolicy(ldmp).setMergeScheduler(new ConcurrentMergeScheduler()));

		// merge factor is changed, so check invariants after all adds
		for (int i = 0; i < 10; i++)
		{
		  AddDoc(writer);
		}
		writer.commit();
		writer.waitForMerges();
		writer.commit();
		CheckInvariants(writer);
		Assert.AreEqual(10, writer.maxDoc());

		writer.close();
		dir.close();
	  }

	  private void AddDoc(IndexWriter writer)
	  {
		Document doc = new Document();
		doc.add(newTextField("content", "aaa", Field.Store.NO));
		writer.addDocument(doc);
	  }

	  private void CheckInvariants(IndexWriter writer)
	  {
		writer.waitForMerges();
		int maxBufferedDocs = writer.Config.MaxBufferedDocs;
		int mergeFactor = ((LogMergePolicy) writer.Config.MergePolicy).MergeFactor;
		int maxMergeDocs = ((LogMergePolicy) writer.Config.MergePolicy).MaxMergeDocs;

		int ramSegmentCount = writer.NumBufferedDocuments;
		Assert.IsTrue(ramSegmentCount < maxBufferedDocs);

		int lowerBound = -1;
		int upperBound = maxBufferedDocs;
		int numSegments = 0;

		int segmentCount = writer.SegmentCount;
		for (int i = segmentCount - 1; i >= 0; i--)
		{
		  int docCount = writer.getDocCount(i);
		  Assert.IsTrue("docCount=" + docCount + " lowerBound=" + lowerBound + " upperBound=" + upperBound + " i=" + i + " segmentCount=" + segmentCount + " index=" + writer.segString() + " config=" + writer.Config, docCount > lowerBound);

		  if (docCount <= upperBound)
		  {
			numSegments++;
		  }
		  else
		  {
			if (upperBound * mergeFactor <= maxMergeDocs)
			{
			  Assert.IsTrue("maxMergeDocs=" + maxMergeDocs + "; numSegments=" + numSegments + "; upperBound=" + upperBound + "; mergeFactor=" + mergeFactor + "; segs=" + writer.segString() + " config=" + writer.Config, numSegments < mergeFactor);
			}

			do
			{
			  lowerBound = upperBound;
			  upperBound *= mergeFactor;
			} while (docCount > upperBound);
			numSegments = 1;
		  }
		}
		if (upperBound * mergeFactor <= maxMergeDocs)
		{
		  Assert.IsTrue(numSegments < mergeFactor);
		}
	  }

	  private const double EPSILON = 1E-14;

	  public virtual void TestSetters()
	  {
		AssertSetters(new LogByteSizeMergePolicy());
		AssertSetters(new LogDocMergePolicy());
	  }

	  private void AssertSetters(MergePolicy lmp)
	  {
		lmp.MaxCFSSegmentSizeMB = 2.0;
		Assert.AreEqual(2.0, lmp.MaxCFSSegmentSizeMB, EPSILON);

		lmp.MaxCFSSegmentSizeMB = double.PositiveInfinity;
		Assert.AreEqual(long.MaxValue / 1024 / 1024.0, lmp.MaxCFSSegmentSizeMB, EPSILON * long.MaxValue);

		lmp.MaxCFSSegmentSizeMB = long.MaxValue / 1024 / 1024.0;
		Assert.AreEqual(long.MaxValue / 1024 / 1024.0, lmp.MaxCFSSegmentSizeMB, EPSILON * long.MaxValue);

		try
		{
		  lmp.MaxCFSSegmentSizeMB = -2.0;
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