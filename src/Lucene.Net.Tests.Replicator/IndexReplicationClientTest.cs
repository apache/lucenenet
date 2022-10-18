using J2N.Threading.Atomic;
using Lucene.Net.Attributes;
using Lucene.Net.Diagnostics;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support.Threading;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Threading;
using Console = Lucene.Net.Util.SystemConsole;
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

    [TestFixture]
    public class IndexReplicationClientTest : ReplicatorTestCase
    {
        private class IndexReadyCallback : IDisposable
        {
            private readonly Directory indexDir;
#pragma warning disable CA2213 // Disposable fields should be disposed
            private DirectoryReader reader;
#pragma warning restore CA2213 // Disposable fields should be disposed
            private long lastGeneration = -1;

            public IndexReadyCallback(Directory indexDir)
            {
                this.indexDir = indexDir;
                if (DirectoryReader.IndexExists(indexDir))
                {
                    reader = DirectoryReader.Open(indexDir);
                    lastGeneration = reader.IndexCommit.Generation;
                }
            }

            public void Call()
            {
                if (reader is null)
                {
                    reader = DirectoryReader.Open(indexDir);
                    lastGeneration = reader.IndexCommit.Generation;
                }
                else
                {
                    DirectoryReader newReader = DirectoryReader.OpenIfChanged(reader);
                    assertNotNull("should not have reached here if no changes were made to the index", newReader);
                    long newGeneration = newReader.IndexCommit.Generation;
                    assertTrue("expected newer generation; current=" + lastGeneration + " new=" + newGeneration, newGeneration > lastGeneration);
                    reader.Dispose();
                    reader = newReader;
                    lastGeneration = newGeneration;
                    TestUtil.CheckIndex(indexDir);
                }
            }
            public void Dispose()
            {
                IOUtils.Dispose(reader);
            }
        }


        private MockDirectoryWrapper publishDir, handlerDir;
        private IReplicator replicator;
        private ISourceDirectoryFactory sourceDirFactory;
        private ReplicationClient client;
        private IReplicationHandler handler;
        private IndexWriter publishWriter;
        private IndexReadyCallback callback;

        private const string VERSION_ID = "version";

        private void AssertHandlerRevision(int expectedId, Directory dir)
        {
            // loop as long as client is alive. test-framework will terminate us if
            // there's a serious bug, e.g. client doesn't really update. otherwise,
            // introducing timeouts is not good, can easily lead to false positives.
            while (client.IsUpdateThreadAlive)
            {
                // give client a chance to update
                try
                {
                    Thread.Sleep(100);
                }
                catch (Exception e) when (e.IsInterruptedException())
                {
                    throw new Util.ThreadInterruptedException(e);
                }

                try
                {
                    DirectoryReader reader = DirectoryReader.Open(dir);
                    try
                    {
                        int handlerId = int.Parse(reader.IndexCommit.UserData[VERSION_ID], NumberStyles.HexNumber);
                        if (expectedId == handlerId)
                        {
                            return;
                        }
                        else if (Verbose)
                        {
                            Console.WriteLine("expectedID=" + expectedId + " actual=" + handlerId + " generation=" + reader.IndexCommit.Generation);
                        }
                    }
                    finally
                    {
                        reader.Dispose();
                    }
                }
                catch (Exception e) when (e.IsException())
                {
                    // we can hit IndexNotFoundException or e.g. EOFException (on
                    // segments_N) because it is being copied at the same time it is read by
                    // DirectoryReader.open().
                }
            }
        }

        private IRevision CreateRevision(int id)
        {
            publishWriter.AddDocument(new Document());
            publishWriter.SetCommitData(new Dictionary<string, string>{
                { VERSION_ID, id.ToString("X") }
            });
            publishWriter.Commit();
            return new IndexRevision(publishWriter);
        }

        public override void SetUp()
        {
            base.SetUp();

            publishDir = NewMockDirectory();
            handlerDir = NewMockDirectory();
            sourceDirFactory = new PerSessionDirectoryFactory(CreateTempDir("replicationClientTest").FullName);
            replicator = new LocalReplicator();
            callback = new IndexReadyCallback(handlerDir);
            handler = new IndexReplicationHandler(handlerDir, callback.Call);
            client = new ReplicationClient(replicator, handler, sourceDirFactory);

            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, null);
            conf.IndexDeletionPolicy = new SnapshotDeletionPolicy(conf.IndexDeletionPolicy);
            publishWriter = new IndexWriter(publishDir, conf);
        }

        public override void TearDown()
        {
            IOUtils.Dispose(client, callback, publishWriter, replicator, publishDir, handlerDir);
            base.TearDown();
        }

        [Test]
        public void TestNoUpdateThread()
        {
            assertNull("no version expected at start", handler.CurrentVersion);

            // Callback validates the replicated ind
            replicator.Publish(CreateRevision(1));
            client.UpdateNow();

            replicator.Publish(CreateRevision(2));
            client.UpdateNow();

            // Publish two revisions without update,
            replicator.Publish(CreateRevision(3));
            replicator.Publish(CreateRevision(4));
            client.UpdateNow();
        }


        [Test]
        public void TestUpdateThread()
        {
            client.StartUpdateThread(10, "index");

            replicator.Publish(CreateRevision(1));
            AssertHandlerRevision(1, handlerDir);

            replicator.Publish(CreateRevision(2));
            AssertHandlerRevision(2, handlerDir);

            // Publish two revisions without update, handler should be upgraded to latest
            replicator.Publish(CreateRevision(3));
            replicator.Publish(CreateRevision(4));
            AssertHandlerRevision(4, handlerDir);
        }

        [Test]
        public void TestRestart()
        {
            replicator.Publish(CreateRevision(1));
            client.UpdateNow();

            replicator.Publish(CreateRevision(2));
            client.UpdateNow();

            client.StopUpdateThread();
            client.Dispose();
            client = new ReplicationClient(replicator, handler, sourceDirFactory);

            // Publish two revisions without update, handler should be upgraded to latest
            replicator.Publish(CreateRevision(3));
            replicator.Publish(CreateRevision(4));
            client.UpdateNow();
        }

        // This test verifies that the client and handler do not end up in a corrupt
        // index if exceptions are thrown at any point during replication. Either when
        // a client copies files from the server to the temporary space, or when the
        // handler copies them to the index directory.
        [Test]
        public void TestConsistencyOnExceptions()
        {
            // so the handler's index isn't empty
            replicator.Publish(CreateRevision(1));
            client.UpdateNow();
            client.Dispose();
            callback.Dispose();

            // Replicator violates write-once policy. It may be that the
            // handler copies files to the index dir, then fails to copy a
            // file and reverts the copy operation. On the next attempt, it
            // will copy the same file again. There is nothing wrong with this
            // in a real system, but it does violate write-once, and MDW
            // doesn't like it. Disabling it means that we won't catch cases
            // where the handler overwrites an existing index file, but
            // there's nothing currently we can do about it, unless we don't
            // use MDW.
            handlerDir.PreventDoubleWrite = false;

            // wrap sourceDirFactory to return a MockDirWrapper so we can simulate errors
            ISourceDirectoryFactory @in = sourceDirFactory;
            AtomicInt32 failures = new AtomicInt32(AtLeast(10));

            // wrap sourceDirFactory to return a MockDirWrapper so we can simulate errors
            sourceDirFactory = new SourceDirectoryFactoryAnonymousClass(this, @in, failures);
            handler = new IndexReplicationHandler(handlerDir, () =>
            {
                if (Random.NextDouble() < 0.2 && failures > 0)
                    throw RuntimeException.Create("random exception from callback");
            });
            client = new ReplicationClientAnonymousClass(this, replicator, handler, sourceDirFactory, failures);
            client.StartUpdateThread(10, "index");

            Directory baseHandlerDir = handlerDir.Delegate;
            int numRevisions = AtLeast(20);
            for (int i = 2; i < numRevisions; i++)
            {
                replicator.Publish(CreateRevision(i));
                AssertHandlerRevision(i, baseHandlerDir);
            }

            // disable errors -- maybe randomness didn't exhaust all allowed failures,
            // and we don't want e.g. CheckIndex to hit false errors. 
            handlerDir.MaxSizeInBytes = 0;
            handlerDir.RandomIOExceptionRate = 0.0;
            handlerDir.RandomIOExceptionRateOnOpen = 0.0;
        }

        private sealed class SourceDirectoryFactoryAnonymousClass : ISourceDirectoryFactory
        {
            private long clientMaxSize = 100, handlerMaxSize = 100;
            private double clientExRate = 1.0, handlerExRate = 1.0;

            private readonly IndexReplicationClientTest test;
            private readonly ISourceDirectoryFactory @in;
            private readonly AtomicInt32 failures;

            public SourceDirectoryFactoryAnonymousClass(IndexReplicationClientTest test, ISourceDirectoryFactory @in, AtomicInt32 failures)
            {
                this.test = test;
                this.@in = @in;
                this.failures = failures;
            }

            public void CleanupSession(string sessionId)
            {
                @in.CleanupSession(sessionId);
            }

            public Directory GetDirectory(string sessionId, string source)
            {
                Directory dir = @in.GetDirectory(sessionId, source);
                if (Random.nextBoolean() && failures > 0)
                { // client should fail, return wrapped dir
                    MockDirectoryWrapper mdw = new MockDirectoryWrapper(Random, dir);
                    mdw.RandomIOExceptionRateOnOpen = clientExRate;
                    mdw.MaxSizeInBytes = clientMaxSize;
                    mdw.RandomIOExceptionRate = clientExRate;
                    mdw.CheckIndexOnDispose = false;
                    clientMaxSize *= 2;
                    clientExRate /= 2;
                    return mdw;
                }

                if (failures > 0 && Random.nextBoolean())
                { // handler should fail
                    test.handlerDir.MaxSizeInBytes = handlerMaxSize;
                    test.handlerDir.RandomIOExceptionRateOnOpen = handlerExRate;
                    test.handlerDir.RandomIOExceptionRate = handlerExRate;
                    handlerMaxSize *= 2;
                    handlerExRate /= 2;
                }
                else
                {
                    // disable errors
                    test.handlerDir.MaxSizeInBytes = 0;
                    test.handlerDir.RandomIOExceptionRate = 0;
                    test.handlerDir.RandomIOExceptionRateOnOpen = 0.0;
                }
                return dir;
            }
        }

        private sealed class ReplicationClientAnonymousClass : ReplicationClient
        {
            private readonly IndexReplicationClientTest test;
            private readonly AtomicInt32 failures;

            public ReplicationClientAnonymousClass(IndexReplicationClientTest test, IReplicator replicator, IReplicationHandler handler, ISourceDirectoryFactory factory, AtomicInt32 failures)
                : base(replicator, handler, factory)
            {
                this.test = test;
                this.failures = failures;
            }

            protected override void HandleUpdateException(Exception exception)
            {
                if (exception.IsIOException())
                {
                    if (Verbose)
                    {
                        Console.WriteLine("hit exception during update: " + exception);
                    }

                    try
                    {
                        // test that the index can be read and also some basic statistics
                        DirectoryReader reader = DirectoryReader.Open(test.handlerDir.Delegate);
                        try
                        {
                            int numDocs = reader.NumDocs;
                            int version = int.Parse(reader.IndexCommit.UserData[VERSION_ID], NumberStyles.HexNumber);
                            assertEquals(numDocs, version);
                        }
                        finally
                        {
                            reader.Dispose();
                        }
                        // verify index consistency
                        TestUtil.CheckIndex(test.handlerDir.Delegate);
                    }
                    catch (Exception e) when (e.IsIOException())
                    {
                        // exceptions here are bad, don't ignore them
                        throw RuntimeException.Create(e);
                    }
                    finally
                    {
                        // count-down number of failures
                        failures.DecrementAndGet();
                        if (Debugging.AssertsEnabled) Debugging.Assert(failures >= 0,"handler failed too many times: {0}", failures);
                        if (Verbose)
                        {
                            if (failures == 0)
                            {
                                Console.WriteLine("no more failures expected");
                            }
                            else
                            {
                                Console.WriteLine("num failures left: " + failures);
                            }
                        }
                    }
                }
                else
                {
                    if (exception.IsRuntimeException())
                        ExceptionDispatchInfo.Capture(exception).Throw(); // LUCENENET: Rethrow to preserve stack details from the original throw
                    else
                        throw RuntimeException.Create(exception);
                }
            }
        }

    }
}