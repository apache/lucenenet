using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Support.Threading;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Directory = Lucene.Net.Store.Directory;

namespace Lucene.Net.Replicator
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

    public class LocalReplicatorTest : ReplicatorTestCase
    {
        private const string VERSION_ID = "version";

        private LocalReplicator replicator;
        private Directory sourceDirectory;
        private IndexWriter sourceWriter;

        public override void SetUp()
        {
            base.SetUp();

            sourceDirectory = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, null);
            conf.IndexDeletionPolicy = new SnapshotDeletionPolicy(conf.IndexDeletionPolicy);
            sourceWriter = new IndexWriter(sourceDirectory, conf);
            replicator = new LocalReplicator();
        }

        public override void TearDown()
        {
            IOUtils.Dispose(replicator, sourceWriter, sourceDirectory);
            base.TearDown();
        }

        private IRevision CreateRevision(int id)
        {
            sourceWriter.AddDocument(new Document());
            sourceWriter.SetCommitData(new Dictionary<string, string> {
                { VERSION_ID, id.ToString() }
            });
            sourceWriter.Commit();
            return new IndexRevision(sourceWriter);
        }

        [Test]
        public void TestCheckForUpdateNoRevisions()
        {
            assertNull(replicator.CheckForUpdate(null));
        }

        [Test]
        public void TestObtainFileAlreadyClosed()
        {
            replicator.Publish(CreateRevision(1));
            SessionToken res = replicator.CheckForUpdate(null);
            assertNotNull(res);
            assertEquals(1, res.SourceFiles.Count);
            System.Collections.Generic.KeyValuePair<string, System.Collections.Generic.IList<RevisionFile>> entry = res.SourceFiles.First();
            replicator.Dispose();
            try
            {
                replicator.ObtainFile(res.Id, entry.Key, entry.Value.First().FileName);
                fail("should have failed on AlreadyClosedException");
            }
            catch (Exception e) when (e.IsAlreadyClosedException())
            {
                // expected
            }
        }

        [Test]
        public void TestPublishAlreadyClosed()
        {
            replicator.Dispose();
            try
            {
                replicator.Publish(CreateRevision(2));
                fail("should have failed on AlreadyClosedException");
            }
            catch (Exception e) when (e.IsAlreadyClosedException())
            {
                // expected
            }
        }

        [Test]
        public void TestUpdateAlreadyClosed()
        {
            replicator.Dispose();
            try
            {
                replicator.CheckForUpdate(null);
                fail("should have failed on AlreadyClosedException");
            }
            catch (Exception e) when (e.IsAlreadyClosedException())
            {
                // expected
            }
        }

        [Test]
        public void TestPublishSameRevision()
        {
            IRevision rev = CreateRevision(1);
            replicator.Publish(rev);
            SessionToken res = replicator.CheckForUpdate(null);
            assertNotNull(res);
            assertEquals(rev.Version, res.Version);
            replicator.Release(res.Id);
            replicator.Publish(new IndexRevision(sourceWriter));
            res = replicator.CheckForUpdate(res.Version);
            assertNull(res);

            // now make sure that publishing same revision doesn't leave revisions
            // "locked", i.e. that replicator releases revisions even when they are not
            // kept
            replicator.Publish(CreateRevision(2));
            assertEquals(1, DirectoryReader.ListCommits(sourceDirectory).size());
        }

        [Test]
        public void TestPublishOlderRev()
        {
            replicator.Publish(CreateRevision(1));
            IRevision old = new IndexRevision(sourceWriter);
            replicator.Publish(CreateRevision(2));
            try
            {
                replicator.Publish(old);
                fail("should have failed to publish an older revision");
            }
            catch (Exception e) when (e.IsIllegalArgumentException())
            {
                // expected
            }
            assertEquals(1, DirectoryReader.ListCommits(sourceDirectory).size());
        }

        [Test]
        public void TestObtainMissingFile()
        {
            replicator.Publish(CreateRevision(1));
            SessionToken res = replicator.CheckForUpdate(null);
            try
            {
                replicator.ObtainFile(res.Id, res.SourceFiles.Keys.First(), "madeUpFile");
                fail("should have failed obtaining an unrecognized file");
            }
            catch (Exception e) when (e.IsNoSuchFileExceptionOrFileNotFoundException())
            {
                // expected
            }
        }

        [Test]
        public void TestSessionExpiration()
        {
            replicator.Publish(CreateRevision(1));
            SessionToken session = replicator.CheckForUpdate(null);
            replicator.ExpirationThreshold = 5; // expire quickly
            Thread.Sleep(50); // sufficient for expiration
            try
            {
                replicator.ObtainFile(session.Id, session.SourceFiles.Keys.First(), session.SourceFiles.Values.First().First().FileName);
                fail("should have failed to obtain a file for an expired session");
            }
            catch (SessionExpiredException)
            {
                // expected
            }
        }

        [Test]
        public void TestUpdateToLatest()
        {
            replicator.Publish(CreateRevision(1));
            IRevision rev = CreateRevision(2);
            replicator.Publish(rev);
            SessionToken res = replicator.CheckForUpdate(null);
            assertNotNull(res);
            assertEquals(0, rev.CompareTo(res.Version));
        }

        [Test]
        public void TestRevisionRelease()
        {
            replicator.Publish(CreateRevision(1));
            assertTrue(SlowFileExists(sourceDirectory, IndexFileNames.SEGMENTS + "_1"));
            replicator.Publish(CreateRevision(2));
            // now the files of revision 1 can be deleted
            assertTrue(SlowFileExists(sourceDirectory, IndexFileNames.SEGMENTS + "_2"));
            assertFalse("segments_1 should not be found in index directory after revision is released", SlowFileExists(sourceDirectory, IndexFileNames.SEGMENTS + "_1"));
        }
    }
}