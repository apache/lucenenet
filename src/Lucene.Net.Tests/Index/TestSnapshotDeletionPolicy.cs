using J2N.Threading;
using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Support.Threading;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using JCG = J2N.Collections.Generic;
using Assert = Lucene.Net.TestFramework.Assert;
using Console = Lucene.Net.Util.SystemConsole;

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

    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using FieldType = FieldType;
    using IndexInput = Lucene.Net.Store.IndexInput;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using TextField = TextField;

    //
    // this was developed for Lucene In Action,
    // http://lucenebook.com
    //
    [TestFixture]
    public class TestSnapshotDeletionPolicy : LuceneTestCase
    {
        public const string INDEX_PATH = "test.snapshots";

        protected internal virtual IndexWriterConfig GetConfig(Random random, IndexDeletionPolicy dp)
        {
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random));
            if (dp != null)
            {
                conf.SetIndexDeletionPolicy(dp);
            }
            return conf;
        }

        protected internal virtual void CheckSnapshotExists(Directory dir, IndexCommit c)
        {
            string segFileName = c.SegmentsFileName;
            Assert.IsTrue(SlowFileExists(dir, segFileName), "segments file not found in directory: " + segFileName);
        }

        protected internal virtual void CheckMaxDoc(IndexCommit commit, int expectedMaxDoc)
        {
            IndexReader reader = DirectoryReader.Open(commit);
            try
            {
                Assert.AreEqual(expectedMaxDoc, reader.MaxDoc);
            }
            finally
            {
                reader.Dispose();
            }
        }

        protected internal virtual void PrepareIndexAndSnapshots(SnapshotDeletionPolicy sdp, IndexWriter writer, int numSnapshots)
        {
            for (int i = 0; i < numSnapshots; i++)
            {
                // create dummy document to trigger commit.
                writer.AddDocument(new Document());
                writer.Commit();
                snapshots.Add(sdp.Snapshot());
            }
        }

        protected internal virtual SnapshotDeletionPolicy DeletionPolicy => new SnapshotDeletionPolicy(new KeepOnlyLastCommitDeletionPolicy());

        protected internal virtual void AssertSnapshotExists(Directory dir, SnapshotDeletionPolicy sdp, int numSnapshots, bool checkIndexCommitSame)
        {
            for (int i = 0; i < numSnapshots; i++)
            {
                IndexCommit snapshot = snapshots[i];
                CheckMaxDoc(snapshot, i + 1);
                CheckSnapshotExists(dir, snapshot);
                if (checkIndexCommitSame)
                {
                    Assert.AreSame(snapshot, sdp.GetIndexCommit(snapshot.Generation));
                }
                else
                {
                    Assert.AreEqual(snapshot.Generation, sdp.GetIndexCommit(snapshot.Generation).Generation);
                }
            }
        }

        protected internal IList<IndexCommit> snapshots;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            this.snapshots = new JCG.List<IndexCommit>();
        }

        [Test]
        public virtual void TestSnapshotDeletionPolicy_Mem()
        {
            Directory fsDir = NewDirectory();
            RunTest(Random, fsDir);
            fsDir.Dispose();
        }

        private void RunTest(Random random, Directory dir)
        {
            // Run for ~1 seconds
            long stopTime = (J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond) + 1000; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results

            SnapshotDeletionPolicy dp = DeletionPolicy;
            IndexWriter writer = new IndexWriter(dir, (IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random)).SetIndexDeletionPolicy(dp).SetMaxBufferedDocs(2));

            // Verify we catch misuse:
            try
            {
                dp.Snapshot();
                Assert.Fail("did not hit exception");
            }
            catch (Exception ise) when (ise.IsIllegalStateException())
            {
                // expected
            }
            dp = (SnapshotDeletionPolicy)writer.Config.IndexDeletionPolicy;
            writer.Commit();

            ThreadJob t = new ThreadAnonymousClass(stopTime, writer, NewField);

            t.Start();

            // While the above indexing thread is running, take many
            // backups:
            do
            {
                BackupIndex(dir, dp);
                Thread.Sleep(20);
            } while (t.IsAlive);

            t.Join();

            // Add one more document to force writer to commit a
            // final segment, so deletion policy has a chance to
            // delete again:
            Document doc = new Document();
            FieldType customType = new FieldType(TextField.TYPE_STORED);
            customType.StoreTermVectors = true;
            customType.StoreTermVectorPositions = true;
            customType.StoreTermVectorOffsets = true;
            doc.Add(NewField("content", "aaa", customType));
            writer.AddDocument(doc);

            // Make sure we don't have any leftover files in the
            // directory:
            writer.Dispose();
            TestIndexWriter.AssertNoUnreferencedFiles(dir, "some files were not deleted but should have been");
        }

        private sealed class ThreadAnonymousClass : ThreadJob
        {
            private readonly long stopTime;
            private readonly IndexWriter writer;
            private readonly Func<string, string, FieldType, Field> newFieldFunc;

            /// <param name="newFieldFunc">
            /// LUCENENET specific
            /// Passed in because <see cref="LuceneTestCase.NewField(string, string, FieldType)"/>
            /// is no longer static. 
            /// </param>
            public ThreadAnonymousClass(long stopTime, IndexWriter writer, Func<string, string, FieldType, Field> newFieldFunc)
            {
                this.stopTime = stopTime;
                this.writer = writer;
                this.newFieldFunc = newFieldFunc;
            }

            public override void Run()
            {
                Document doc = new Document();
                FieldType customType = new FieldType(TextField.TYPE_STORED);
                customType.StoreTermVectors = true;
                customType.StoreTermVectorPositions = true;
                customType.StoreTermVectorOffsets = true;
                doc.Add(newFieldFunc("content", "aaa", customType));
                do
                {
                    for (int i = 0; i < 27; i++)
                    {
                        try
                        {
                            writer.AddDocument(doc);
                        }
                        catch (Exception t) when (t.IsThrowable())
                        {
                            Console.WriteLine(t.StackTrace);
                            Assert.Fail("addDocument failed");
                        }
                        if (i % 2 == 0)
                        {
                            try
                            {
                                writer.Commit();
                            }
                            catch (Exception e) when (e.IsException())
                            {
                                throw RuntimeException.Create(e);
                            }
                        }
                    }

                    try
                    {
                        Thread.Sleep(1);
                    }
                    catch (Exception ie) when (ie.IsInterruptedException())
                    {
                        throw new Util.ThreadInterruptedException(ie);
                    }
                } while (J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond < stopTime); // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
            }
        }

        /// <summary>
        /// Example showing how to use the SnapshotDeletionPolicy to take a backup.
        /// this method does not really do a backup; instead, it reads every byte of
        /// every file just to test that the files indeed exist and are readable even
        /// while the index is changing.
        /// </summary>
        public virtual void BackupIndex(Directory dir, SnapshotDeletionPolicy dp)
        {
            // To backup an index we first take a snapshot:
            IndexCommit snapshot = dp.Snapshot();
            try
            {
                CopyFiles(dir, snapshot);
            }
            finally
            {
                // Make sure to release the snapshot, otherwise these
                // files will never be deleted during this IndexWriter
                // session:
                dp.Release(snapshot);
            }
        }

        private void CopyFiles(Directory dir, IndexCommit cp)
        {
            // While we hold the snapshot, and nomatter how long
            // we take to do the backup, the IndexWriter will
            // never delete the files in the snapshot:
            ICollection<string> files = cp.FileNames;
            foreach (String fileName in files)
            {
                // NOTE: in a real backup you would not use
                // readFile; you would need to use something else
                // that copies the file to a backup location.  this
                // could even be a spawned shell process (eg "tar",
                // "zip") that takes the list of files and builds a
                // backup.
                ReadFile(dir, fileName);
            }
        }

        internal byte[] buffer = new byte[4096];

        private void ReadFile(Directory dir, string name)
        {
            IndexInput input = dir.OpenInput(name, NewIOContext(Random));
            try
            {
                long size = dir.FileLength(name);
                long bytesLeft = size;
                while (bytesLeft > 0)
                {
                    int numToRead;
                    if (bytesLeft < buffer.Length)
                    {
                        numToRead = (int)bytesLeft;
                    }
                    else
                    {
                        numToRead = buffer.Length;
                    }
                    input.ReadBytes(buffer, 0, numToRead, false);
                    bytesLeft -= numToRead;
                }
                // Don't do this in your real backups!  this is just
                // to force a backup to take a somewhat long time, to
                // make sure we are exercising the fact that the
                // IndexWriter should not delete this file even when I
                // take my time reading it.
                Thread.Sleep(1);
            }
            finally
            {
                input.Dispose();
            }
        }

        [Test]
        public virtual void TestBasicSnapshots()
        {
            int numSnapshots = 3;

            // Create 3 snapshots: snapshot0, snapshot1, snapshot2
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, GetConfig(Random, DeletionPolicy));
            SnapshotDeletionPolicy sdp = (SnapshotDeletionPolicy)writer.Config.IndexDeletionPolicy;
            PrepareIndexAndSnapshots(sdp, writer, numSnapshots);
            writer.Dispose();

            Assert.AreEqual(numSnapshots, sdp.GetSnapshots().Count);
            Assert.AreEqual(numSnapshots, sdp.SnapshotCount);
            AssertSnapshotExists(dir, sdp, numSnapshots, true);

            // open a reader on a snapshot - should succeed.
            DirectoryReader.Open(snapshots[0]).Dispose();

            // open a new IndexWriter w/ no snapshots to keep and assert that all snapshots are gone.
            sdp = DeletionPolicy;
            writer = new IndexWriter(dir, GetConfig(Random, sdp));
            writer.DeleteUnusedFiles();
            writer.Dispose();
            Assert.AreEqual(1, DirectoryReader.ListCommits(dir).Count, "no snapshots should exist");
            dir.Dispose();
        }

        [Test]
        public virtual void TestMultiThreadedSnapshotting()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, GetConfig(Random, DeletionPolicy));
            SnapshotDeletionPolicy sdp = (SnapshotDeletionPolicy)writer.Config.IndexDeletionPolicy;

            ThreadJob[] threads = new ThreadJob[10];
            IndexCommit[] snapshots = new IndexCommit[threads.Length];
            for (int i = 0; i < threads.Length; i++)
            {
                int finalI = i;
                threads[i] = new ThreadAnonymousClass2(this, writer, sdp, snapshots, finalI);
                threads[i].Name = "t" + i;
            }

            foreach (ThreadJob t in threads)
            {
                t.Start();
            }

            foreach (ThreadJob t in threads)
            {
                t.Join();
            }

            // Do one last commit, so that after we release all snapshots, we stay w/ one commit
            writer.AddDocument(new Document());
            writer.Commit();

            for (int i = 0; i < threads.Length; i++)
            {
                sdp.Release(snapshots[i]);
                writer.DeleteUnusedFiles();
            }
            Assert.AreEqual(1, DirectoryReader.ListCommits(dir).Count);
            writer.Dispose();
            dir.Dispose();
        }

        private sealed class ThreadAnonymousClass2 : ThreadJob
        {
            private readonly TestSnapshotDeletionPolicy outerInstance;

            private readonly IndexWriter writer;
            private readonly SnapshotDeletionPolicy sdp;
            private readonly IndexCommit[] snapshots;
            private readonly int finalI;

            public ThreadAnonymousClass2(TestSnapshotDeletionPolicy outerInstance, IndexWriter writer, SnapshotDeletionPolicy sdp, IndexCommit[] snapshots, int finalI)
            {
                this.outerInstance = outerInstance;
                this.writer = writer;
                this.sdp = sdp;
                this.snapshots = snapshots;
                this.finalI = finalI;
            }

            public override void Run()
            {
                try
                {
                    writer.AddDocument(new Document());
                    writer.Commit();
                    snapshots[finalI] = sdp.Snapshot();
                }
                catch (Exception e) when (e.IsException())
                {
                    throw RuntimeException.Create(e);
                }
            }
        }

        [Test]
        public virtual void TestRollbackToOldSnapshot()
        {
            int numSnapshots = 2;
            Directory dir = NewDirectory();

            SnapshotDeletionPolicy sdp = DeletionPolicy;
            IndexWriter writer = new IndexWriter(dir, GetConfig(Random, sdp));
            PrepareIndexAndSnapshots(sdp, writer, numSnapshots);
            writer.Dispose();

            // now open the writer on "snapshot0" - make sure it succeeds
            writer = new IndexWriter(dir, GetConfig(Random, sdp).SetIndexCommit(snapshots[0]));
            // this does the actual rollback
            writer.Commit();
            writer.DeleteUnusedFiles();
            AssertSnapshotExists(dir, sdp, numSnapshots - 1, false);
            writer.Dispose();

            // but 'snapshot1' files will still exist (need to release snapshot before they can be deleted).
            string segFileName = snapshots[1].SegmentsFileName;
            Assert.IsTrue(SlowFileExists(dir, segFileName), "snapshot files should exist in the directory: " + segFileName);

            dir.Dispose();
        }

        [Test]
        public virtual void TestReleaseSnapshot()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, GetConfig(Random, DeletionPolicy));
            SnapshotDeletionPolicy sdp = (SnapshotDeletionPolicy)writer.Config.IndexDeletionPolicy;
            PrepareIndexAndSnapshots(sdp, writer, 1);

            // Create another commit - we must do that, because otherwise the "snapshot"
            // files will still remain in the index, since it's the last commit.
            writer.AddDocument(new Document());
            writer.Commit();

            // Release
            string segFileName = snapshots[0].SegmentsFileName;
            sdp.Release(snapshots[0]);
            writer.DeleteUnusedFiles();
            writer.Dispose();
            Assert.IsFalse(SlowFileExists(dir, segFileName), "segments file should not be found in dirctory: " + segFileName);
            dir.Dispose();
        }

        [Test]
        public virtual void TestSnapshotLastCommitTwice()
        {
            Directory dir = NewDirectory();

            IndexWriter writer = new IndexWriter(dir, GetConfig(Random, DeletionPolicy));
            SnapshotDeletionPolicy sdp = (SnapshotDeletionPolicy)writer.Config.IndexDeletionPolicy;
            writer.AddDocument(new Document());
            writer.Commit();

            IndexCommit s1 = sdp.Snapshot();
            IndexCommit s2 = sdp.Snapshot();
            Assert.AreSame(s1, s2); // should be the same instance

            // create another commit
            writer.AddDocument(new Document());
            writer.Commit();

            // release "s1" should not delete "s2"
            sdp.Release(s1);
            writer.DeleteUnusedFiles();
            CheckSnapshotExists(dir, s2);

            writer.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestMissingCommits()
        {
            // Tests the behavior of SDP when commits that are given at ctor are missing
            // on onInit().
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, GetConfig(Random, DeletionPolicy));
            SnapshotDeletionPolicy sdp = (SnapshotDeletionPolicy)writer.Config.IndexDeletionPolicy;
            writer.AddDocument(new Document());
            writer.Commit();
            IndexCommit s1 = sdp.Snapshot();

            // create another commit, not snapshotted.
            writer.AddDocument(new Document());
            writer.Dispose();

            // open a new writer w/ KeepOnlyLastCommit policy, so it will delete "s1"
            // commit.
            (new IndexWriter(dir, GetConfig(Random, null))).Dispose();

            Assert.IsFalse(SlowFileExists(dir, s1.SegmentsFileName), "snapshotted commit should not exist");
            dir.Dispose();
        }
    }
}