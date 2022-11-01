using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.IO;
using Assert = Lucene.Net.TestFramework.Assert;

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
    using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;

    [TestFixture]
    public class TestPersistentSnapshotDeletionPolicy : TestSnapshotDeletionPolicy
    {
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
        }

        private SnapshotDeletionPolicy GetDeletionPolicy(Directory dir)
        {
            return new PersistentSnapshotDeletionPolicy(new KeepOnlyLastCommitDeletionPolicy(), dir, OpenMode.CREATE);
        }

        [Test]
        public virtual void TestExistingSnapshots()
        {
            int numSnapshots = 3;
            MockDirectoryWrapper dir = NewMockDirectory();
            IndexWriter writer = new IndexWriter(dir, GetConfig(Random, GetDeletionPolicy(dir)));
            PersistentSnapshotDeletionPolicy psdp = (PersistentSnapshotDeletionPolicy)writer.Config.IndexDeletionPolicy;
            Assert.IsNull(psdp.LastSaveFile);
            PrepareIndexAndSnapshots(psdp, writer, numSnapshots);
            Assert.IsNotNull(psdp.LastSaveFile);
            writer.Dispose();

            // Make sure only 1 save file exists:
            int count = 0;
            foreach (string file in dir.ListAll())
            {
                if (file.StartsWith(PersistentSnapshotDeletionPolicy.SNAPSHOTS_PREFIX, StringComparison.Ordinal))
                {
                    count++;
                }
            }
            Assert.AreEqual(1, count);

            // Make sure we fsync:
            dir.Crash();
            dir.ClearCrash();

            // Re-initialize and verify snapshots were persisted
            psdp = new PersistentSnapshotDeletionPolicy(new KeepOnlyLastCommitDeletionPolicy(), dir, OpenMode.APPEND);

            writer = new IndexWriter(dir, GetConfig(Random, psdp));
            psdp = (PersistentSnapshotDeletionPolicy)writer.Config.IndexDeletionPolicy;

            Assert.AreEqual(numSnapshots, psdp.GetSnapshots().Count);
            Assert.AreEqual(numSnapshots, psdp.SnapshotCount);
            AssertSnapshotExists(dir, psdp, numSnapshots, false);

            writer.AddDocument(new Document());
            writer.Commit();
            snapshots.Add(psdp.Snapshot());
            Assert.AreEqual(numSnapshots + 1, psdp.GetSnapshots().Count);
            Assert.AreEqual(numSnapshots + 1, psdp.SnapshotCount);
            AssertSnapshotExists(dir, psdp, numSnapshots + 1, false);

            writer.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestNoSnapshotInfos()
        {
            Directory dir = NewDirectory();
            new PersistentSnapshotDeletionPolicy(new KeepOnlyLastCommitDeletionPolicy(), dir, OpenMode.CREATE);
            dir.Dispose();
        }

        [Test]
        public virtual void TestMissingSnapshots()
        {
            Directory dir = NewDirectory();
            try
            {
                new PersistentSnapshotDeletionPolicy(new KeepOnlyLastCommitDeletionPolicy(), dir, OpenMode.APPEND);
                Assert.Fail("did not hit expected exception");
            }
            catch (Exception ise) when (ise.IsIllegalStateException())
            {
                // expected
            }
            dir.Dispose();
        }

        [Test]
        public virtual void TestExceptionDuringSave()
        {
            MockDirectoryWrapper dir = NewMockDirectory();
            dir.FailOn(new FailureAnonymousClass(this, dir));
            IndexWriter writer = new IndexWriter(dir, GetConfig(Random, new PersistentSnapshotDeletionPolicy(new KeepOnlyLastCommitDeletionPolicy(), dir, OpenMode.CREATE_OR_APPEND)));
            writer.AddDocument(new Document());
            writer.Commit();

            PersistentSnapshotDeletionPolicy psdp = (PersistentSnapshotDeletionPolicy)writer.Config.IndexDeletionPolicy;
            try
            {
                psdp.Snapshot();
            }
            catch (Exception ioe) when (ioe.IsIOException())
            {
                if (ioe.Message.Equals("now fail on purpose", StringComparison.Ordinal))
                {
                    // ok
                }
                else
                {
                    throw; // LUCENENET: CA2200: Rethrow to preserve stack details (https://docs.microsoft.com/en-us/visualstudio/code-quality/ca2200-rethrow-to-preserve-stack-details)
                }
            }
            Assert.AreEqual(0, psdp.SnapshotCount);
            writer.Dispose();
            Assert.AreEqual(1, DirectoryReader.ListCommits(dir).Count);
            dir.Dispose();
        }

        private sealed class FailureAnonymousClass : Failure
        {
            private readonly TestPersistentSnapshotDeletionPolicy outerInstance;

            private MockDirectoryWrapper dir;

            public FailureAnonymousClass(TestPersistentSnapshotDeletionPolicy outerInstance, MockDirectoryWrapper dir)
            {
                this.outerInstance = outerInstance;
                this.dir = dir;
            }

            public override void Eval(MockDirectoryWrapper dir)
            {
                // LUCENENET specific: for these to work in release mode, we have added [MethodImpl(MethodImplOptions.NoInlining)]
                // to each possible target of the StackTraceHelper. If these change, so must the attribute on the target methods.
                if (StackTraceHelper.DoesStackTraceContainMethod(typeof(PersistentSnapshotDeletionPolicy).Name, "Persist"))
                {
                    throw new IOException("now fail on purpose");
                }
            }
        }

        [Test]
        public virtual void TestSnapshotRelease()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, GetConfig(Random, GetDeletionPolicy(dir)));
            PersistentSnapshotDeletionPolicy psdp = (PersistentSnapshotDeletionPolicy)writer.Config.IndexDeletionPolicy;
            PrepareIndexAndSnapshots(psdp, writer, 1);
            writer.Dispose();

            psdp.Release(snapshots[0]);

            psdp = new PersistentSnapshotDeletionPolicy(new KeepOnlyLastCommitDeletionPolicy(), dir, OpenMode.APPEND);
            Assert.AreEqual(0, psdp.SnapshotCount, "Should have no snapshots !");
            dir.Dispose();
        }

        [Test]
        public virtual void TestSnapshotReleaseByGeneration()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, GetConfig(Random, GetDeletionPolicy(dir)));
            PersistentSnapshotDeletionPolicy psdp = (PersistentSnapshotDeletionPolicy)writer.Config.IndexDeletionPolicy;
            PrepareIndexAndSnapshots(psdp, writer, 1);
            writer.Dispose();

            psdp.Release(snapshots[0].Generation);

            psdp = new PersistentSnapshotDeletionPolicy(new KeepOnlyLastCommitDeletionPolicy(), dir, OpenMode.APPEND);
            Assert.AreEqual(0, psdp.SnapshotCount, "Should have no snapshots !");
            dir.Dispose();
        }


        #region TestSnapshotDeletionPolicy
        // LUCENENET NOTE: Tests in a base class are not pulled into the correct
        // context in Visual Studio. This fixes that with the minimum amount of code necessary
        // to run them in the correct context without duplicating all of the tests.

        [Test]
        public override void TestSnapshotDeletionPolicy_Mem()
        {
            base.TestSnapshotDeletionPolicy_Mem();
        }

        [Test]
        public override void TestBasicSnapshots()
        {
            base.TestBasicSnapshots();
        }

        [Test]
        public override void TestMultiThreadedSnapshotting()
        {
            base.TestMultiThreadedSnapshotting();
        }

        [Test]
        public override void TestRollbackToOldSnapshot()
        {
            base.TestRollbackToOldSnapshot();
        }

        [Test]
        public override void TestReleaseSnapshot()
        {
            base.TestReleaseSnapshot();
        }

        [Test]
        public override void TestSnapshotLastCommitTwice()
        {
            base.TestSnapshotLastCommitTwice();
        }

        [Test]
        public override void TestMissingCommits()
        {
            base.TestMissingCommits();
        }

        #endregion
    }
}