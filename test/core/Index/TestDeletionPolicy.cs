using System;
using System.Collections.Generic;
using System.Threading;

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
	using IndexSearcher = Lucene.Net.Search.IndexSearcher;
	using Query = Lucene.Net.Search.Query;
	using ScoreDoc = Lucene.Net.Search.ScoreDoc;
	using TermQuery = Lucene.Net.Search.TermQuery;
	using Directory = Lucene.Net.Store.Directory;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;

	/*
	  Verify we can read the pre-2.1 file format, do searches
	  against it, and add documents to it.
	*/

	public class TestDeletionPolicy : LuceneTestCase
	{

	  private void verifyCommitOrder<T1>(IList<T1> commits) where T1 : IndexCommit
	  {
		if (commits.Count == 0)
		{
		  return;
		}
		IndexCommit firstCommit = commits[0];
		long last = SegmentInfos.generationFromSegmentsFileName(firstCommit.SegmentsFileName);
		Assert.AreEqual(last, firstCommit.Generation);
		for (int i = 1;i < commits.Count;i++)
		{
		  IndexCommit commit = commits[i];
		  long now = SegmentInfos.generationFromSegmentsFileName(commit.SegmentsFileName);
		  Assert.IsTrue("SegmentInfos commits are out-of-order", now > last);
		  Assert.AreEqual(now, commit.Generation);
		  last = now;
		}
	  }

	  internal class KeepAllDeletionPolicy : IndexDeletionPolicy
	  {
		  private readonly TestDeletionPolicy OuterInstance;

		internal int NumOnInit;
		internal int NumOnCommit;
		internal Directory Dir;

		internal KeepAllDeletionPolicy(TestDeletionPolicy outerInstance, Directory dir)
		{
			this.OuterInstance = outerInstance;
		  this.Dir = dir;
		}

		public override void onInit<T1>(IList<T1> commits) where T1 : IndexCommit
		{
		  outerInstance.VerifyCommitOrder(commits);
		  NumOnInit++;
		}
		public override void onCommit<T1>(IList<T1> commits) where T1 : IndexCommit
		{
		  IndexCommit lastCommit = commits[commits.Count - 1];
		  DirectoryReader r = DirectoryReader.open(Dir);
		  Assert.AreEqual("lastCommit.segmentCount()=" + lastCommit.SegmentCount + " vs IndexReader.segmentCount=" + r.leaves().size(), r.leaves().size(), lastCommit.SegmentCount);
		  r.close();
		  outerInstance.VerifyCommitOrder(commits);
		  NumOnCommit++;
		}

	  }

	  /// <summary>
	  /// this is useful for adding to a big index when you know
	  /// readers are not using it.
	  /// </summary>
	  internal class KeepNoneOnInitDeletionPolicy : IndexDeletionPolicy
	  {
		  private readonly TestDeletionPolicy OuterInstance;

		  public KeepNoneOnInitDeletionPolicy(TestDeletionPolicy outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }

		internal int NumOnInit;
		internal int NumOnCommit;
		public override void onInit<T1>(IList<T1> commits) where T1 : IndexCommit
		{
		  outerInstance.VerifyCommitOrder(commits);
		  NumOnInit++;
		  // On init, delete all commit points:
		  foreach (IndexCommit commit in commits)
		  {
			commit.delete();
			Assert.IsTrue(commit.Deleted);
		  }
		}
		public override void onCommit<T1>(IList<T1> commits) where T1 : IndexCommit
		{
		  outerInstance.VerifyCommitOrder(commits);
		  int size = commits.Count;
		  // Delete all but last one:
		  for (int i = 0;i < size-1;i++)
		  {
			((IndexCommit) commits[i]).delete();
		  }
		  NumOnCommit++;
		}
	  }

	  internal class KeepLastNDeletionPolicy : IndexDeletionPolicy
	  {
		  private readonly TestDeletionPolicy OuterInstance;

		internal int NumOnInit;
		internal int NumOnCommit;
		internal int NumToKeep;
		internal int NumDelete;
		internal Set<string> Seen = new HashSet<string>();

		public KeepLastNDeletionPolicy(TestDeletionPolicy outerInstance, int numToKeep)
		{
			this.OuterInstance = outerInstance;
		  this.NumToKeep = numToKeep;
		}

		public override void onInit<T1>(IList<T1> commits) where T1 : IndexCommit
		{
		  if (VERBOSE)
		  {
			Console.WriteLine("TEST: onInit");
		  }
		  outerInstance.VerifyCommitOrder(commits);
		  NumOnInit++;
		  // do no deletions on init
		  DoDeletes(commits, false);
		}

		public override void onCommit<T1>(IList<T1> commits) where T1 : IndexCommit
		{
		  if (VERBOSE)
		  {
			Console.WriteLine("TEST: onCommit");
		  }
		  outerInstance.VerifyCommitOrder(commits);
		  DoDeletes(commits, true);
		}

		internal virtual void doDeletes<T1>(IList<T1> commits, bool isCommit) where T1 : IndexCommit
		{

		  // Assert that we really are only called for each new
		  // commit:
		  if (isCommit)
		  {
			string fileName = ((IndexCommit) commits[commits.Count - 1]).SegmentsFileName;
			if (Seen.contains(fileName))
			{
			  throw new Exception("onCommit was called twice on the same commit point: " + fileName);
			}
			Seen.add(fileName);
			NumOnCommit++;
		  }
		  int size = commits.Count;
		  for (int i = 0;i < size - NumToKeep;i++)
		  {
			((IndexCommit) commits[i]).delete();
			NumDelete++;
		  }
		}
	  }

	  internal static long GetCommitTime(IndexCommit commit)
	  {
		return Convert.ToInt64(commit.UserData.get("commitTime"));
	  }

	  /*
	   * Delete a commit only when it has been obsoleted by N
	   * seconds.
	   */
	  internal class ExpirationTimeDeletionPolicy : IndexDeletionPolicy
	  {
		  private readonly TestDeletionPolicy OuterInstance;


		internal Directory Dir;
		internal double ExpirationTimeSeconds;
		internal int NumDelete;

		public ExpirationTimeDeletionPolicy(TestDeletionPolicy outerInstance, Directory dir, double seconds)
		{
			this.OuterInstance = outerInstance;
		  this.Dir = dir;
		  this.ExpirationTimeSeconds = seconds;
		}

		public override void onInit<T1>(IList<T1> commits) where T1 : IndexCommit
		{
		  if (commits.Count == 0)
		  {
			return;
		  }
		  outerInstance.VerifyCommitOrder(commits);
		  OnCommit(commits);
		}

		public override void onCommit<T1>(IList<T1> commits) where T1 : IndexCommit
		{
		  outerInstance.VerifyCommitOrder(commits);

		  IndexCommit lastCommit = commits[commits.Count - 1];

		  // Any commit older than expireTime should be deleted:
		  double expireTime = GetCommitTime(lastCommit) / 1000.0 - ExpirationTimeSeconds;

		  foreach (IndexCommit commit in commits)
		  {
			double modTime = GetCommitTime(commit) / 1000.0;
			if (commit != lastCommit && modTime < expireTime)
			{
			  commit.delete();
			  NumDelete += 1;
			}
		  }
		}
	  }

	  /*
	   * Test "by time expiration" deletion policy:
	   */
	  public virtual void TestExpirationTimeDeletionPolicy()
	  {

		const double SECONDS = 2.0;

		Directory dir = newDirectory();
		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setIndexDeletionPolicy(new ExpirationTimeDeletionPolicy(this, dir, SECONDS));
		MergePolicy mp = conf.MergePolicy;
		mp.NoCFSRatio = 1.0;
		IndexWriter writer = new IndexWriter(dir, conf);
		ExpirationTimeDeletionPolicy policy = (ExpirationTimeDeletionPolicy) writer.Config.IndexDeletionPolicy;
		IDictionary<string, string> commitData = new Dictionary<string, string>();
		commitData["commitTime"] = Convert.ToString(System.currentTimeMillis());
		writer.CommitData = commitData;
		writer.commit();
		writer.close();

		long lastDeleteTime = 0;
		int targetNumDelete = TestUtil.Next(random(), 1, 5);
		while (policy.NumDelete < targetNumDelete)
		{
		  // Record last time when writer performed deletes of
		  // past commits
		  lastDeleteTime = System.currentTimeMillis();
		  conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.APPEND).setIndexDeletionPolicy(policy);
		  mp = conf.MergePolicy;
		  mp.NoCFSRatio = 1.0;
		  writer = new IndexWriter(dir, conf);
		  policy = (ExpirationTimeDeletionPolicy) writer.Config.IndexDeletionPolicy;
		  for (int j = 0;j < 17;j++)
		  {
			AddDoc(writer);
		  }
		  commitData = new Dictionary<>();
		  commitData["commitTime"] = Convert.ToString(System.currentTimeMillis());
		  writer.CommitData = commitData;
		  writer.commit();
		  writer.close();

		  Thread.Sleep((int)(1000.0 * (SECONDS / 5.0)));
		}

		// Then simplistic check: just verify that the
		// segments_N's that still exist are in fact within SECONDS
		// seconds of the last one's mod time, and, that I can
		// open a reader on each:
		long gen = SegmentInfos.getLastCommitGeneration(dir);

		string fileName = IndexFileNames.fileNameFromGeneration(IndexFileNames.SEGMENTS, "", gen);
		dir.deleteFile(IndexFileNames.SEGMENTS_GEN);

		bool oneSecondResolution = true;

		while (gen > 0)
		{
		  try
		  {
			IndexReader reader = DirectoryReader.open(dir);
			reader.close();
			fileName = IndexFileNames.fileNameFromGeneration(IndexFileNames.SEGMENTS, "", gen);

			// if we are on a filesystem that seems to have only
			// 1 second resolution, allow +1 second in commit
			// age tolerance:
			SegmentInfos sis = new SegmentInfos();
			sis.read(dir, fileName);
			long modTime = Convert.ToInt64(sis.UserData.get("commitTime"));
			oneSecondResolution &= (modTime % 1000) == 0;
			long leeway = (long)((SECONDS + (oneSecondResolution ? 1.0:0.0)) * 1000);

			Assert.IsTrue("commit point was older than " + SECONDS + " seconds (" + (lastDeleteTime - modTime) + " msec) but did not get deleted ", lastDeleteTime - modTime <= leeway);
		  }
		  catch (IOException e)
		  {
			// OK
			break;
		  }

		  dir.deleteFile(IndexFileNames.fileNameFromGeneration(IndexFileNames.SEGMENTS, "", gen));
		  gen--;
		}

		dir.close();
	  }

	  /*
	   * Test a silly deletion policy that keeps all commits around.
	   */
	  public virtual void TestKeepAllDeletionPolicy()
	  {
		for (int pass = 0;pass < 2;pass++)
		{

		  if (VERBOSE)
		  {
			Console.WriteLine("TEST: cycle pass=" + pass);
		  }

		  bool useCompoundFile = (pass % 2) != 0;

		  Directory dir = newDirectory();

		  IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setIndexDeletionPolicy(new KeepAllDeletionPolicy(this, dir)).setMaxBufferedDocs(10).setMergeScheduler(new SerialMergeScheduler());
		  MergePolicy mp = conf.MergePolicy;
		  mp.NoCFSRatio = useCompoundFile ? 1.0 : 0.0;
		  IndexWriter writer = new IndexWriter(dir, conf);
		  KeepAllDeletionPolicy policy = (KeepAllDeletionPolicy) writer.Config.IndexDeletionPolicy;
		  for (int i = 0;i < 107;i++)
		  {
			AddDoc(writer);
		  }
		  writer.close();

		  bool needsMerging;
		  {
			DirectoryReader r = DirectoryReader.open(dir);
			needsMerging = r.leaves().size() != 1;
			r.close();
		  }
		  if (needsMerging)
		  {
			conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.APPEND).setIndexDeletionPolicy(policy);
			mp = conf.MergePolicy;
			mp.NoCFSRatio = useCompoundFile ? 1.0 : 0.0;
			if (VERBOSE)
			{
			  Console.WriteLine("TEST: open writer for forceMerge");
			}
			writer = new IndexWriter(dir, conf);
			policy = (KeepAllDeletionPolicy) writer.Config.IndexDeletionPolicy;
			writer.forceMerge(1);
			writer.close();
		  }

		  Assert.AreEqual(needsMerging ? 2:1, policy.NumOnInit);

		  // If we are not auto committing then there should
		  // be exactly 2 commits (one per close above):
		  Assert.AreEqual(1 + (needsMerging ? 1:0), policy.NumOnCommit);

		  // Test listCommits
		  ICollection<IndexCommit> commits = DirectoryReader.listCommits(dir);
		  // 2 from closing writer
		  Assert.AreEqual(1 + (needsMerging ? 1:0), commits.Count);

		  // Make sure we can open a reader on each commit:
		  foreach (IndexCommit commit in commits)
		  {
			IndexReader r = DirectoryReader.open(commit);
			r.close();
		  }

		  // Simplistic check: just verify all segments_N's still
		  // exist, and, I can open a reader on each:
		  dir.deleteFile(IndexFileNames.SEGMENTS_GEN);
		  long gen = SegmentInfos.getLastCommitGeneration(dir);
		  while (gen > 0)
		  {
			IndexReader reader = DirectoryReader.open(dir);
			reader.close();
			dir.deleteFile(IndexFileNames.fileNameFromGeneration(IndexFileNames.SEGMENTS, "", gen));
			gen--;

			if (gen > 0)
			{
			  // Now that we've removed a commit point, which
			  // should have orphan'd at least one index file.
			  // Open & close a writer and assert that it
			  // actually removed something:
			  int preCount = dir.listAll().length;
			  writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.APPEND).setIndexDeletionPolicy(policy));
			  writer.close();
			  int postCount = dir.listAll().length;
			  Assert.IsTrue(postCount < preCount);
			}
		  }

		  dir.close();
		}
	  }

	  /* Uses KeepAllDeletionPolicy to keep all commits around,
	   * then, opens a new IndexWriter on a previous commit
	   * point. */
	  public virtual void TestOpenPriorSnapshot()
	  {
		Directory dir = newDirectory();

		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setIndexDeletionPolicy(new KeepAllDeletionPolicy(this, dir)).setMaxBufferedDocs(2).setMergePolicy(newLogMergePolicy(10)));
		KeepAllDeletionPolicy policy = (KeepAllDeletionPolicy) writer.Config.IndexDeletionPolicy;
		for (int i = 0;i < 10;i++)
		{
		  AddDoc(writer);
		  if ((1 + i) % 2 == 0)
		  {
			writer.commit();
		  }
		}
		writer.close();

		ICollection<IndexCommit> commits = DirectoryReader.listCommits(dir);
		Assert.AreEqual(5, commits.Count);
		IndexCommit lastCommit = null;
		foreach (IndexCommit commit in commits)
		{
		  if (lastCommit == null || commit.Generation > lastCommit.Generation)
		  {
			lastCommit = commit;
		  }
		}
		Assert.IsTrue(lastCommit != null);

		// Now add 1 doc and merge
		writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setIndexDeletionPolicy(policy));
		AddDoc(writer);
		Assert.AreEqual(11, writer.numDocs());
		writer.forceMerge(1);
		writer.close();

		Assert.AreEqual(6, DirectoryReader.listCommits(dir).size());

		// Now open writer on the commit just before merge:
		writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setIndexDeletionPolicy(policy).setIndexCommit(lastCommit));
		Assert.AreEqual(10, writer.numDocs());

		// Should undo our rollback:
		writer.rollback();

		DirectoryReader r = DirectoryReader.open(dir);
		// Still merged, still 11 docs
		Assert.AreEqual(1, r.leaves().size());
		Assert.AreEqual(11, r.numDocs());
		r.close();

		writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setIndexDeletionPolicy(policy).setIndexCommit(lastCommit));
		Assert.AreEqual(10, writer.numDocs());
		// Commits the rollback:
		writer.close();

		// Now 8 because we made another commit
		Assert.AreEqual(7, DirectoryReader.listCommits(dir).size());

		r = DirectoryReader.open(dir);
		// Not fully merged because we rolled it back, and now only
		// 10 docs
		Assert.IsTrue(r.leaves().size() > 1);
		Assert.AreEqual(10, r.numDocs());
		r.close();

		// Re-merge
		writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setIndexDeletionPolicy(policy));
		writer.forceMerge(1);
		writer.close();

		r = DirectoryReader.open(dir);
		Assert.AreEqual(1, r.leaves().size());
		Assert.AreEqual(10, r.numDocs());
		r.close();

		// Now open writer on the commit just before merging,
		// but this time keeping only the last commit:
		writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setIndexCommit(lastCommit));
		Assert.AreEqual(10, writer.numDocs());

		// Reader still sees fully merged index, because writer
		// opened on the prior commit has not yet committed:
		r = DirectoryReader.open(dir);
		Assert.AreEqual(1, r.leaves().size());
		Assert.AreEqual(10, r.numDocs());
		r.close();

		writer.close();

		// Now reader sees not-fully-merged index:
		r = DirectoryReader.open(dir);
		Assert.IsTrue(r.leaves().size() > 1);
		Assert.AreEqual(10, r.numDocs());
		r.close();

		dir.close();
	  }


	  /* Test keeping NO commit points.  this is a viable and
	   * useful case eg where you want to build a big index and
	   * you know there are no readers.
	   */
	  public virtual void TestKeepNoneOnInitDeletionPolicy()
	  {
		for (int pass = 0;pass < 2;pass++)
		{

		  bool useCompoundFile = (pass % 2) != 0;

		  Directory dir = newDirectory();

		  IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.CREATE).setIndexDeletionPolicy(new KeepNoneOnInitDeletionPolicy(this)).setMaxBufferedDocs(10);
		  MergePolicy mp = conf.MergePolicy;
		  mp.NoCFSRatio = useCompoundFile ? 1.0 : 0.0;
		  IndexWriter writer = new IndexWriter(dir, conf);
		  KeepNoneOnInitDeletionPolicy policy = (KeepNoneOnInitDeletionPolicy) writer.Config.IndexDeletionPolicy;
		  for (int i = 0;i < 107;i++)
		  {
			AddDoc(writer);
		  }
		  writer.close();

		  conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.APPEND).setIndexDeletionPolicy(policy);
		  mp = conf.MergePolicy;
		  mp.NoCFSRatio = 1.0;
		  writer = new IndexWriter(dir, conf);
		  policy = (KeepNoneOnInitDeletionPolicy) writer.Config.IndexDeletionPolicy;
		  writer.forceMerge(1);
		  writer.close();

		  Assert.AreEqual(2, policy.NumOnInit);
		  // If we are not auto committing then there should
		  // be exactly 2 commits (one per close above):
		  Assert.AreEqual(2, policy.NumOnCommit);

		  // Simplistic check: just verify the index is in fact
		  // readable:
		  IndexReader reader = DirectoryReader.open(dir);
		  reader.close();

		  dir.close();
		}
	  }

	  /*
	   * Test a deletion policy that keeps last N commits.
	   */
	  public virtual void TestKeepLastNDeletionPolicy()
	  {
		const int N = 5;

		for (int pass = 0;pass < 2;pass++)
		{

		  bool useCompoundFile = (pass % 2) != 0;

		  Directory dir = newDirectory();

		  KeepLastNDeletionPolicy policy = new KeepLastNDeletionPolicy(this, N);
		  for (int j = 0;j < N + 1;j++)
		  {
			IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.CREATE).setIndexDeletionPolicy(policy).setMaxBufferedDocs(10);
			MergePolicy mp = conf.MergePolicy;
			mp.NoCFSRatio = useCompoundFile ? 1.0 : 0.0;
			IndexWriter writer = new IndexWriter(dir, conf);
			policy = (KeepLastNDeletionPolicy) writer.Config.IndexDeletionPolicy;
			for (int i = 0;i < 17;i++)
			{
			  AddDoc(writer);
			}
			writer.forceMerge(1);
			writer.close();
		  }

		  Assert.IsTrue(policy.NumDelete > 0);
		  Assert.AreEqual(N + 1, policy.NumOnInit);
		  Assert.AreEqual(N + 1, policy.NumOnCommit);

		  // Simplistic check: just verify only the past N segments_N's still
		  // exist, and, I can open a reader on each:
		  dir.deleteFile(IndexFileNames.SEGMENTS_GEN);
		  long gen = SegmentInfos.getLastCommitGeneration(dir);
		  for (int i = 0;i < N + 1;i++)
		  {
			try
			{
			  IndexReader reader = DirectoryReader.open(dir);
			  reader.close();
			  if (i == N)
			  {
				Assert.Fail("should have failed on commits prior to last " + N);
			  }
			}
			catch (IOException e)
			{
			  if (i != N)
			  {
				throw e;
			  }
			}
			if (i < N)
			{
			  dir.deleteFile(IndexFileNames.fileNameFromGeneration(IndexFileNames.SEGMENTS, "", gen));
			}
			gen--;
		  }

		  dir.close();
		}
	  }

	  /*
	   * Test a deletion policy that keeps last N commits
	   * around, through creates.
	   */
	  public virtual void TestKeepLastNDeletionPolicyWithCreates()
	  {

		const int N = 10;

		for (int pass = 0;pass < 2;pass++)
		{

		  bool useCompoundFile = (pass % 2) != 0;

		  Directory dir = newDirectory();
		  IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.CREATE).setIndexDeletionPolicy(new KeepLastNDeletionPolicy(this, N)).setMaxBufferedDocs(10);
		  MergePolicy mp = conf.MergePolicy;
		  mp.NoCFSRatio = useCompoundFile ? 1.0 : 0.0;
		  IndexWriter writer = new IndexWriter(dir, conf);
		  KeepLastNDeletionPolicy policy = (KeepLastNDeletionPolicy) writer.Config.IndexDeletionPolicy;
		  writer.close();
		  Term searchTerm = new Term("content", "aaa");
		  Query query = new TermQuery(searchTerm);

		  for (int i = 0;i < N + 1;i++)
		  {

			conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.APPEND).setIndexDeletionPolicy(policy).setMaxBufferedDocs(10);
			mp = conf.MergePolicy;
			mp.NoCFSRatio = useCompoundFile ? 1.0 : 0.0;
			writer = new IndexWriter(dir, conf);
			policy = (KeepLastNDeletionPolicy) writer.Config.IndexDeletionPolicy;
			for (int j = 0;j < 17;j++)
			{
			  AddDocWithID(writer, i * (N + 1) + j);
			}
			// this is a commit
			writer.close();
			conf = (new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()))).setIndexDeletionPolicy(policy).setMergePolicy(NoMergePolicy.COMPOUND_FILES);
			writer = new IndexWriter(dir, conf);
			policy = (KeepLastNDeletionPolicy) writer.Config.IndexDeletionPolicy;
			writer.deleteDocuments(new Term("id", "" + (i * (N + 1) + 3)));
			// this is a commit
			writer.close();
			IndexReader reader = DirectoryReader.open(dir);
			IndexSearcher searcher = newSearcher(reader);
			ScoreDoc[] hits = searcher.search(query, null, 1000).scoreDocs;
			Assert.AreEqual(16, hits.Length);
			reader.close();

			writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.CREATE).setIndexDeletionPolicy(policy));
			policy = (KeepLastNDeletionPolicy) writer.Config.IndexDeletionPolicy;
			// this will not commit: there are no changes
			// pending because we opened for "create":
			writer.close();
		  }

		  Assert.AreEqual(3 * (N + 1) + 1, policy.NumOnInit);
		  Assert.AreEqual(3 * (N + 1) + 1, policy.NumOnCommit);

		  IndexReader rwReader = DirectoryReader.open(dir);
		  IndexSearcher searcher = newSearcher(rwReader);
		  ScoreDoc[] hits = searcher.search(query, null, 1000).scoreDocs;
		  Assert.AreEqual(0, hits.Length);

		  // Simplistic check: just verify only the past N segments_N's still
		  // exist, and, I can open a reader on each:
		  long gen = SegmentInfos.getLastCommitGeneration(dir);

		  dir.deleteFile(IndexFileNames.SEGMENTS_GEN);
		  int expectedCount = 0;

		  rwReader.close();

		  for (int i = 0;i < N + 1;i++)
		  {
			try
			{
			  IndexReader reader = DirectoryReader.open(dir);

			  // Work backwards in commits on what the expected
			  // count should be.
			  searcher = newSearcher(reader);
			  hits = searcher.search(query, null, 1000).scoreDocs;
			  Assert.AreEqual(expectedCount, hits.Length);
			  if (expectedCount == 0)
			  {
				expectedCount = 16;
			  }
			  else if (expectedCount == 16)
			  {
				expectedCount = 17;
			  }
			  else if (expectedCount == 17)
			  {
				expectedCount = 0;
			  }
			  reader.close();
			  if (i == N)
			  {
				Assert.Fail("should have failed on commits before last " + N);
			  }
			}
			catch (IOException e)
			{
			  if (i != N)
			  {
				throw e;
			  }
			}
			if (i < N)
			{
			  dir.deleteFile(IndexFileNames.fileNameFromGeneration(IndexFileNames.SEGMENTS, "", gen));
			}
			gen--;
		  }

		  dir.close();
		}
	  }

	  private void AddDocWithID(IndexWriter writer, int id)
	  {
		Document doc = new Document();
		doc.add(newTextField("content", "aaa", Field.Store.NO));
		doc.add(newStringField("id", "" + id, Field.Store.NO));
		writer.addDocument(doc);
	  }

	  private void AddDoc(IndexWriter writer)
	  {
		Document doc = new Document();
		doc.add(newTextField("content", "aaa", Field.Store.NO));
		writer.addDocument(doc);
	  }
	}

}