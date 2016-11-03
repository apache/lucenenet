using System;
using System.Collections.Generic;
using System.Threading;
using Lucene.Net.Documents;

namespace Lucene.Net.Index
{
    using NUnit.Framework;
    using System.IO;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using IndexSearcher = Lucene.Net.Search.IndexSearcher;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

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
    using OpenMode_e = Lucene.Net.Index.IndexWriterConfig.OpenMode_e;
    using Query = Lucene.Net.Search.Query;
    using ScoreDoc = Lucene.Net.Search.ScoreDoc;
    using TermQuery = Lucene.Net.Search.TermQuery;
    using TestUtil = Lucene.Net.Util.TestUtil;

    /*
      Verify we can read the pre-2.1 file format, do searches
      against it, and add documents to it.
    */

    [TestFixture]
    public class TestDeletionPolicy : LuceneTestCase
    {
        private void VerifyCommitOrder<T>(IList<T> commits)
            where T : IndexCommit
        {
            if (commits.Count == 0)
            {
                return;
            }
            IndexCommit firstCommit = commits[0];
            long last = SegmentInfos.GenerationFromSegmentsFileName(firstCommit.SegmentsFileName);
            Assert.AreEqual(last, firstCommit.Generation);
            for (int i = 1; i < commits.Count; i++)
            {
                IndexCommit commit = commits[i];
                long now = SegmentInfos.GenerationFromSegmentsFileName(commit.SegmentsFileName);
                Assert.IsTrue(now > last, "SegmentInfos commits are out-of-order");
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

            public override void OnInit<T>(IList<T> commits)
            {
                OuterInstance.VerifyCommitOrder(commits);
                NumOnInit++;
            }

            public override void OnCommit<T>(IList<T> commits)
            {
                IndexCommit lastCommit = commits[commits.Count - 1];
                DirectoryReader r = DirectoryReader.Open(Dir);
                Assert.AreEqual(r.Leaves.Count, lastCommit.SegmentCount, "lastCommit.segmentCount()=" + lastCommit.SegmentCount + " vs IndexReader.segmentCount=" + r.Leaves.Count);
                r.Dispose();
                OuterInstance.VerifyCommitOrder(commits);
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

            public override void OnInit<T>(IList<T> commits)
            {
                OuterInstance.VerifyCommitOrder(commits);
                NumOnInit++;
                // On init, delete all commit points:
                foreach (IndexCommit commit in commits)
                {
                    commit.Delete();
                    Assert.IsTrue(commit.Deleted);
                }
            }

            public override void OnCommit<T>(IList<T> commits)
            {
                OuterInstance.VerifyCommitOrder(commits);
                int size = commits.Count;
                // Delete all but last one:
                for (int i = 0; i < size - 1; i++)
                {
                    ((IndexCommit)commits[i]).Delete();
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
            internal HashSet<string> Seen = new HashSet<string>();

            public KeepLastNDeletionPolicy(TestDeletionPolicy outerInstance, int numToKeep)
            {
                this.OuterInstance = outerInstance;
                this.NumToKeep = numToKeep;
            }

            public override void OnInit<T>(IList<T> commits)
            {
                if (VERBOSE)
                {
                    Console.WriteLine("TEST: onInit");
                }
                OuterInstance.VerifyCommitOrder(commits);
                NumOnInit++;
                // do no deletions on init
                DoDeletes(commits, false);
            }

            public override void OnCommit<T>(IList<T> commits)
            {
                if (VERBOSE)
                {
                    Console.WriteLine("TEST: onCommit");
                }
                OuterInstance.VerifyCommitOrder(commits);
                DoDeletes(commits, true);
            }

            internal virtual void DoDeletes<T>(IList<T> commits, bool isCommit)
                where T : IndexCommit
            {
                // Assert that we really are only called for each new
                // commit:
                if (isCommit)
                {
                    string fileName = ((IndexCommit)commits[commits.Count - 1]).SegmentsFileName;
                    if (Seen.Contains(fileName))
                    {
                        throw new Exception("onCommit was called twice on the same commit point: " + fileName);
                    }
                    Seen.Add(fileName);
                    NumOnCommit++;
                }
                int size = commits.Count;
                for (int i = 0; i < size - NumToKeep; i++)
                {
                    ((IndexCommit)commits[i]).Delete();
                    NumDelete++;
                }
            }
        }

        internal static long GetCommitTime(IndexCommit commit)
        {
            return Convert.ToInt64(commit.UserData["commitTime"]);
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

            public override void OnInit<T>(IList<T> commits)
            {
                if (commits.Count == 0)
                {
                    return;
                }
                OuterInstance.VerifyCommitOrder(commits);
                OnCommit(commits);
            }

            public override void OnCommit<T>(IList<T> commits)
            {
                OuterInstance.VerifyCommitOrder(commits);

                IndexCommit lastCommit = commits[commits.Count - 1];

                // Any commit older than expireTime should be deleted:
                double expireTime = GetCommitTime(lastCommit) / 1000.0 - ExpirationTimeSeconds;

                foreach (IndexCommit commit in commits)
                {
                    double modTime = GetCommitTime(commit) / 1000.0;
                    if (commit != lastCommit && modTime < expireTime)
                    {
                        commit.Delete();
                        NumDelete += 1;
                    }
                }
            }
        }

        /*
         * Test "by time expiration" deletion policy:
         */

        [Test]
        public virtual void TestExpirationTimeDeletionPolicy()
        {
            const double SECONDS = 2.0;

            Directory dir = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetIndexDeletionPolicy(new ExpirationTimeDeletionPolicy(this, dir, SECONDS));
            MergePolicy mp = conf.MergePolicy;
            mp.NoCFSRatio = 1.0;
            IndexWriter writer = new IndexWriter(dir, conf);
            ExpirationTimeDeletionPolicy policy = (ExpirationTimeDeletionPolicy)writer.Config.DelPolicy;
            IDictionary<string, string> commitData = new Dictionary<string, string>();
            commitData["commitTime"] = Convert.ToString(Environment.TickCount);
            writer.CommitData = commitData;
            writer.Commit();
            writer.Dispose();

            long lastDeleteTime = 0;
            int targetNumDelete = TestUtil.NextInt(Random(), 1, 5);
            while (policy.NumDelete < targetNumDelete)
            {
                // Record last time when writer performed deletes of
                // past commits
                lastDeleteTime = Environment.TickCount;
                conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetOpenMode(OpenMode_e.APPEND).SetIndexDeletionPolicy(policy);
                mp = conf.MergePolicy;
                mp.NoCFSRatio = 1.0;
                writer = new IndexWriter(dir, conf);
                policy = (ExpirationTimeDeletionPolicy)writer.Config.DelPolicy;
                for (int j = 0; j < 17; j++)
                {
                    AddDoc(writer);
                }
                commitData = new Dictionary<string, string>();
                commitData["commitTime"] = Convert.ToString(Environment.TickCount);
                writer.CommitData = commitData;
                writer.Commit();
                writer.Dispose();

                Thread.Sleep((int)(1000.0 * (SECONDS / 5.0)));
            }

            // Then simplistic check: just verify that the
            // segments_N's that still exist are in fact within SECONDS
            // seconds of the last one's mod time, and, that I can
            // open a reader on each:
            long gen = SegmentInfos.GetLastCommitGeneration(dir);

            string fileName = IndexFileNames.FileNameFromGeneration(IndexFileNames.SEGMENTS, "", gen);
            dir.DeleteFile(IndexFileNames.SEGMENTS_GEN);

            bool oneSecondResolution = true;

            while (gen > 0)
            {
                try
                {
                    IndexReader reader = DirectoryReader.Open(dir);
                    reader.Dispose();
                    fileName = IndexFileNames.FileNameFromGeneration(IndexFileNames.SEGMENTS, "", gen);

                    // if we are on a filesystem that seems to have only
                    // 1 second resolution, allow +1 second in commit
                    // age tolerance:
                    SegmentInfos sis = new SegmentInfos();
                    sis.Read(dir, fileName);
                    long modTime = Convert.ToInt64(sis.UserData["commitTime"]);
                    oneSecondResolution &= (modTime % 1000) == 0;
                    long leeway = (long)((SECONDS + (oneSecondResolution ? 1.0 : 0.0)) * 1000);

                    Assert.IsTrue(lastDeleteTime - modTime <= leeway, "commit point was older than " + SECONDS + " seconds (" + (lastDeleteTime - modTime) + " msec) but did not get deleted ");
                }
                catch (IOException e)
                {
                    // OK
                    break;
                }

                dir.DeleteFile(IndexFileNames.FileNameFromGeneration(IndexFileNames.SEGMENTS, "", gen));
                gen--;
            }

            dir.Dispose();
        }

        /*
         * Test a silly deletion policy that keeps all commits around.
         */

        [Test]
        public virtual void TestKeepAllDeletionPolicy()
        {
            for (int pass = 0; pass < 2; pass++)
            {
                if (VERBOSE)
                {
                    Console.WriteLine("TEST: cycle pass=" + pass);
                }

                bool useCompoundFile = (pass % 2) != 0;

                Directory dir = NewDirectory();

                IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetIndexDeletionPolicy(new KeepAllDeletionPolicy(this, dir)).SetMaxBufferedDocs(10).SetMergeScheduler(new SerialMergeScheduler());
                MergePolicy mp = conf.MergePolicy;
                mp.NoCFSRatio = useCompoundFile ? 1.0 : 0.0;
                IndexWriter writer = new IndexWriter(dir, conf);
                KeepAllDeletionPolicy policy = (KeepAllDeletionPolicy)writer.Config.DelPolicy;
                for (int i = 0; i < 107; i++)
                {
                    AddDoc(writer);
                }
                writer.Dispose();

                bool needsMerging;
                {
                    DirectoryReader r = DirectoryReader.Open(dir);
                    needsMerging = r.Leaves.Count != 1;
                    r.Dispose();
                }
                if (needsMerging)
                {
                    conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetOpenMode(OpenMode_e.APPEND).SetIndexDeletionPolicy(policy);
                    mp = conf.MergePolicy;
                    mp.NoCFSRatio = useCompoundFile ? 1.0 : 0.0;
                    if (VERBOSE)
                    {
                        Console.WriteLine("TEST: open writer for forceMerge");
                    }
                    writer = new IndexWriter(dir, conf);
                    policy = (KeepAllDeletionPolicy)writer.Config.DelPolicy;
                    writer.ForceMerge(1);
                    writer.Dispose();
                }

                Assert.AreEqual(needsMerging ? 2 : 1, policy.NumOnInit);

                // If we are not auto committing then there should
                // be exactly 2 commits (one per close above):
                Assert.AreEqual(1 + (needsMerging ? 1 : 0), policy.NumOnCommit);

                // Test listCommits
                ICollection<IndexCommit> commits = DirectoryReader.ListCommits(dir);
                // 2 from closing writer
                Assert.AreEqual(1 + (needsMerging ? 1 : 0), commits.Count);

                // Make sure we can open a reader on each commit:
                foreach (IndexCommit commit in commits)
                {
                    IndexReader r = DirectoryReader.Open(commit);
                    r.Dispose();
                }

                // Simplistic check: just verify all segments_N's still
                // exist, and, I can open a reader on each:
                dir.DeleteFile(IndexFileNames.SEGMENTS_GEN);
                long gen = SegmentInfos.GetLastCommitGeneration(dir);
                while (gen > 0)
                {
                    IndexReader reader = DirectoryReader.Open(dir);
                    reader.Dispose();
                    dir.DeleteFile(IndexFileNames.FileNameFromGeneration(IndexFileNames.SEGMENTS, "", gen));
                    gen--;

                    if (gen > 0)
                    {
                        // Now that we've removed a commit point, which
                        // should have orphan'd at least one index file.
                        // Open & close a writer and assert that it
                        // actually removed something:
                        int preCount = dir.ListAll().Length;
                        writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetOpenMode(OpenMode_e.APPEND).SetIndexDeletionPolicy(policy));
                        writer.Dispose();
                        int postCount = dir.ListAll().Length;
                        Assert.IsTrue(postCount < preCount);
                    }
                }

                dir.Dispose();
            }
        }

        /* Uses KeepAllDeletionPolicy to keep all commits around,
         * then, opens a new IndexWriter on a previous commit
         * point. */

        [Test]
        public virtual void TestOpenPriorSnapshot()
        {
            Directory dir = NewDirectory();

            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetIndexDeletionPolicy(new KeepAllDeletionPolicy(this, dir)).SetMaxBufferedDocs(2).SetMergePolicy(NewLogMergePolicy(10)));
            KeepAllDeletionPolicy policy = (KeepAllDeletionPolicy)writer.Config.DelPolicy;
            for (int i = 0; i < 10; i++)
            {
                AddDoc(writer);
                if ((1 + i) % 2 == 0)
                {
                    writer.Commit();
                }
            }
            writer.Dispose();

            ICollection<IndexCommit> commits = DirectoryReader.ListCommits(dir);
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
            writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetIndexDeletionPolicy(policy));
            AddDoc(writer);
            Assert.AreEqual(11, writer.NumDocs());
            writer.ForceMerge(1);
            writer.Dispose();

            Assert.AreEqual(6, DirectoryReader.ListCommits(dir).Count);

            // Now open writer on the commit just before merge:
            writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetIndexDeletionPolicy(policy).SetIndexCommit(lastCommit));
            Assert.AreEqual(10, writer.NumDocs());

            // Should undo our rollback:
            writer.Rollback();

            DirectoryReader r = DirectoryReader.Open(dir);
            // Still merged, still 11 docs
            Assert.AreEqual(1, r.Leaves.Count);
            Assert.AreEqual(11, r.NumDocs);
            r.Dispose();

            writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetIndexDeletionPolicy(policy).SetIndexCommit(lastCommit));
            Assert.AreEqual(10, writer.NumDocs());
            // Commits the rollback:
            writer.Dispose();

            // Now 8 because we made another commit
            Assert.AreEqual(7, DirectoryReader.ListCommits(dir).Count);

            r = DirectoryReader.Open(dir);
            // Not fully merged because we rolled it back, and now only
            // 10 docs
            Assert.IsTrue(r.Leaves.Count > 1);
            Assert.AreEqual(10, r.NumDocs);
            r.Dispose();

            // Re-merge
            writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetIndexDeletionPolicy(policy));
            writer.ForceMerge(1);
            writer.Dispose();

            r = DirectoryReader.Open(dir);
            Assert.AreEqual(1, r.Leaves.Count);
            Assert.AreEqual(10, r.NumDocs);
            r.Dispose();

            // Now open writer on the commit just before merging,
            // but this time keeping only the last commit:
            writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetIndexCommit(lastCommit));
            Assert.AreEqual(10, writer.NumDocs());

            // Reader still sees fully merged index, because writer
            // opened on the prior commit has not yet committed:
            r = DirectoryReader.Open(dir);
            Assert.AreEqual(1, r.Leaves.Count);
            Assert.AreEqual(10, r.NumDocs);
            r.Dispose();

            writer.Dispose();

            // Now reader sees not-fully-merged index:
            r = DirectoryReader.Open(dir);
            Assert.IsTrue(r.Leaves.Count > 1);
            Assert.AreEqual(10, r.NumDocs);
            r.Dispose();

            dir.Dispose();
        }

        /* Test keeping NO commit points.  this is a viable and
         * useful case eg where you want to build a big index and
         * you know there are no readers.
         */

        [Test]
        public virtual void TestKeepNoneOnInitDeletionPolicy()
        {
            for (int pass = 0; pass < 2; pass++)
            {
                bool useCompoundFile = (pass % 2) != 0;

                Directory dir = NewDirectory();

                IndexWriterConfig conf = (IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetOpenMode(OpenMode_e.CREATE).SetIndexDeletionPolicy(new KeepNoneOnInitDeletionPolicy(this)).SetMaxBufferedDocs(10);
                MergePolicy mp = conf.MergePolicy;
                mp.NoCFSRatio = useCompoundFile ? 1.0 : 0.0;
                IndexWriter writer = new IndexWriter(dir, conf);
                KeepNoneOnInitDeletionPolicy policy = (KeepNoneOnInitDeletionPolicy)writer.Config.DelPolicy;
                for (int i = 0; i < 107; i++)
                {
                    AddDoc(writer);
                }
                writer.Dispose();

                conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetOpenMode(OpenMode_e.APPEND).SetIndexDeletionPolicy(policy);
                mp = conf.MergePolicy;
                mp.NoCFSRatio = 1.0;
                writer = new IndexWriter(dir, conf);
                policy = (KeepNoneOnInitDeletionPolicy)writer.Config.DelPolicy;
                writer.ForceMerge(1);
                writer.Dispose();

                Assert.AreEqual(2, policy.NumOnInit);
                // If we are not auto committing then there should
                // be exactly 2 commits (one per close above):
                Assert.AreEqual(2, policy.NumOnCommit);

                // Simplistic check: just verify the index is in fact
                // readable:
                IndexReader reader = DirectoryReader.Open(dir);
                reader.Dispose();

                dir.Dispose();
            }
        }

        /*
         * Test a deletion policy that keeps last N commits.
         */

        [Test]
        public virtual void TestKeepLastNDeletionPolicy()
        {
            const int N = 5;

            for (int pass = 0; pass < 2; pass++)
            {
                bool useCompoundFile = (pass % 2) != 0;

                Directory dir = NewDirectory();

                KeepLastNDeletionPolicy policy = new KeepLastNDeletionPolicy(this, N);
                for (int j = 0; j < N + 1; j++)
                {
                    IndexWriterConfig conf = (IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetOpenMode(OpenMode_e.CREATE).SetIndexDeletionPolicy(policy).SetMaxBufferedDocs(10);
                    MergePolicy mp = conf.MergePolicy;
                    mp.NoCFSRatio = useCompoundFile ? 1.0 : 0.0;
                    IndexWriter writer = new IndexWriter(dir, conf);
                    policy = (KeepLastNDeletionPolicy)writer.Config.DelPolicy;
                    for (int i = 0; i < 17; i++)
                    {
                        AddDoc(writer);
                    }
                    writer.ForceMerge(1);
                    writer.Dispose();
                }

                Assert.IsTrue(policy.NumDelete > 0);
                Assert.AreEqual(N + 1, policy.NumOnInit);
                Assert.AreEqual(N + 1, policy.NumOnCommit);

                // Simplistic check: just verify only the past N segments_N's still
                // exist, and, I can open a reader on each:
                dir.DeleteFile(IndexFileNames.SEGMENTS_GEN);
                long gen = SegmentInfos.GetLastCommitGeneration(dir);
                for (int i = 0; i < N + 1; i++)
                {
                    try
                    {
                        IndexReader reader = DirectoryReader.Open(dir);
                        reader.Dispose();
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
                        dir.DeleteFile(IndexFileNames.FileNameFromGeneration(IndexFileNames.SEGMENTS, "", gen));
                    }
                    gen--;
                }

                dir.Dispose();
            }
        }

        /*
         * Test a deletion policy that keeps last N commits
         * around, through creates.
         */

        [Test, MaxTime(300000)]
        public virtual void TestKeepLastNDeletionPolicyWithCreates()
        {
            const int N = 10;

            for (int pass = 0; pass < 2; pass++)
            {
                bool useCompoundFile = (pass % 2) != 0;

                Directory dir = NewDirectory();
                IndexWriterConfig conf = (IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetOpenMode(OpenMode_e.CREATE).SetIndexDeletionPolicy(new KeepLastNDeletionPolicy(this, N)).SetMaxBufferedDocs(10);
                MergePolicy mp = conf.MergePolicy;
                mp.NoCFSRatio = useCompoundFile ? 1.0 : 0.0;
                IndexWriter writer = new IndexWriter(dir, conf);
                KeepLastNDeletionPolicy policy = (KeepLastNDeletionPolicy)writer.Config.DelPolicy;
                writer.Dispose();
                Term searchTerm = new Term("content", "aaa");
                Query query = new TermQuery(searchTerm);

                for (int i = 0; i < N + 1; i++)
                {
                    conf = (IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetOpenMode(OpenMode_e.APPEND).SetIndexDeletionPolicy(policy).SetMaxBufferedDocs(10);
                    mp = conf.MergePolicy;
                    mp.NoCFSRatio = useCompoundFile ? 1.0 : 0.0;
                    writer = new IndexWriter(dir, conf);
                    policy = (KeepLastNDeletionPolicy)writer.Config.DelPolicy;
                    for (int j = 0; j < 17; j++)
                    {
                        AddDocWithID(writer, i * (N + 1) + j);
                    }
                    // this is a commit
                    writer.Dispose();
                    conf = (new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()))).SetIndexDeletionPolicy(policy).SetMergePolicy(NoMergePolicy.COMPOUND_FILES);
                    writer = new IndexWriter(dir, conf);
                    policy = (KeepLastNDeletionPolicy)writer.Config.DelPolicy;
                    writer.DeleteDocuments(new Term("id", "" + (i * (N + 1) + 3)));
                    // this is a commit
                    writer.Dispose();
                    IndexReader reader = DirectoryReader.Open(dir);
                    IndexSearcher searcher = NewSearcher(reader);
                    ScoreDoc[] hits = searcher.Search(query, null, 1000).ScoreDocs;
                    Assert.AreEqual(16, hits.Length);
                    reader.Dispose();

                    writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetOpenMode(OpenMode_e.CREATE).SetIndexDeletionPolicy(policy));
                    policy = (KeepLastNDeletionPolicy)writer.Config.DelPolicy;
                    // this will not commit: there are no changes
                    // pending because we opened for "create":
                    writer.Dispose();
                }

                Assert.AreEqual(3 * (N + 1) + 1, policy.NumOnInit);
                Assert.AreEqual(3 * (N + 1) + 1, policy.NumOnCommit);

                IndexReader rwReader = DirectoryReader.Open(dir);
                IndexSearcher searcher_ = NewSearcher(rwReader);
                ScoreDoc[] hits_ = searcher_.Search(query, null, 1000).ScoreDocs;
                Assert.AreEqual(0, hits_.Length);

                // Simplistic check: just verify only the past N segments_N's still
                // exist, and, I can open a reader on each:
                long gen = SegmentInfos.GetLastCommitGeneration(dir);

                dir.DeleteFile(IndexFileNames.SEGMENTS_GEN);
                int expectedCount = 0;

                rwReader.Dispose();

                for (int i = 0; i < N + 1; i++)
                {
                    try
                    {
                        IndexReader reader = DirectoryReader.Open(dir);

                        // Work backwards in commits on what the expected
                        // count should be.
                        searcher_ = NewSearcher(reader);
                        hits_ = searcher_.Search(query, null, 1000).ScoreDocs;
                        Assert.AreEqual(expectedCount, hits_.Length);
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
                        reader.Dispose();
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
                        dir.DeleteFile(IndexFileNames.FileNameFromGeneration(IndexFileNames.SEGMENTS, "", gen));
                    }
                    gen--;
                }

                dir.Dispose();
            }
        }

        private void AddDocWithID(IndexWriter writer, int id)
        {
            Document doc = new Document();
            doc.Add(NewTextField("content", "aaa", Field.Store.NO));
            doc.Add(NewStringField("id", "" + id, Field.Store.NO));
            writer.AddDocument(doc);
        }

        private void AddDoc(IndexWriter writer)
        {
            Document doc = new Document();
            doc.Add(NewTextField("content", "aaa", Field.Store.NO));
            writer.AddDocument(doc);
        }
    }
}