//STATUS: DRAFT - 4.8.0

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Replicator;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;
using Directory = Lucene.Net.Store.Directory;

namespace Lucene.Net.Tests.Replicator
{
    [TestFixture]
    public class IndexReplicationClientTest : ReplicatorTestCase
    {
        private class IndexReadyCallback : IDisposable
        {
            private readonly Directory indexDir;
            private DirectoryReader reader;
            private long lastGeneration = -1;

            public IndexReadyCallback(Directory indexDir)
            {
                //JAVA:    public IndexReadyCallback(Directory indexDir) throws IOException {
                //JAVA:      this.indexDir = indexDir;
                //JAVA:      if (DirectoryReader.indexExists(indexDir)) {
                //JAVA:        reader = DirectoryReader.open(indexDir);
                //JAVA:        lastGeneration = reader.getIndexCommit().getGeneration();
                //JAVA:      }
                //JAVA:    }

                this.indexDir = indexDir;
                if (DirectoryReader.IndexExists(indexDir))
                {
                    reader = DirectoryReader.Open(indexDir);
                    lastGeneration = reader.IndexCommit.Generation;
                }
            }

            public bool? Call()
            {
                //JAVA:    public Boolean call() throws Exception {
                //JAVA:      if (reader == null) {
                //JAVA:        reader = DirectoryReader.open(indexDir);
                //JAVA:        lastGeneration = reader.getIndexCommit().getGeneration();
                //JAVA:      } else {
                //JAVA:        DirectoryReader newReader = DirectoryReader.openIfChanged(reader);
                //JAVA:        assertNotNull("should not have reached here if no changes were made to the index", newReader);
                //JAVA:        long newGeneration = newReader.getIndexCommit().getGeneration();
                //JAVA:        assertTrue("expected newer generation; current=" + lastGeneration + " new=" + newGeneration, newGeneration > lastGeneration);
                //JAVA:        reader.close();
                //JAVA:        reader = newReader;
                //JAVA:        lastGeneration = newGeneration;
                //JAVA:        TestUtil.checkIndex(indexDir);
                //JAVA:      }
                //JAVA:      return null;
                //JAVA:    }
                if (reader == null)
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
                return null;
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
        //JAVA:  private IndexReadyCallback callback;

        private const string VERSION_ID = "version";

        private void AssertHandlerRevision(int expectedId, Directory dir)
        {
            //JAVA:  private void assertHandlerRevision(int expectedID, Directory dir) throws IOException {
            //JAVA:    // loop as long as client is alive. test-framework will terminate us if
            //JAVA:    // there's a serious bug, e.g. client doesn't really update. otherwise,
            //JAVA:    // introducing timeouts is not good, can easily lead to false positives.
            //JAVA:    while (client.isUpdateThreadAlive()) {
            //JAVA:      // give client a chance to update
            //JAVA:      try {
            //JAVA:        Thread.sleep(100);
            //JAVA:      } catch (InterruptedException e) {
            //JAVA:        throw new ThreadInterruptedException(e);
            //JAVA:      }
            //JAVA:
            //JAVA:      try {
            //JAVA:        DirectoryReader reader = DirectoryReader.open(dir);
            //JAVA:        try {
            //JAVA:          int handlerID = Integer.parseInt(reader.getIndexCommit().getUserData().get(VERSION_ID), 16);
            //JAVA:          if (expectedID == handlerID) {
            //JAVA:            return;
            //JAVA:          } else if (VERBOSE) {
            //JAVA:            System.out.println("expectedID=" + expectedID + " actual=" + handlerID + " generation=" + reader.getIndexCommit().getGeneration());
            //JAVA:          }
            //JAVA:        } finally {
            //JAVA:          reader.close();
            //JAVA:        }
            //JAVA:      } catch (Exception e) {
            //JAVA:        // we can hit IndexNotFoundException or e.g. EOFException (on
            //JAVA:        // segments_N) because it is being copied at the same time it is read by
            //JAVA:        // DirectoryReader.open().
            //JAVA:      }
            //JAVA:    }
            //JAVA:  }

            // loop as long as client is alive. test-framework will terminate us if
            // there's a serious bug, e.g. client doesn't really update. otherwise,
            // introducing timeouts is not good, can easily lead to false positives.
            while (client.IsUpdateThreadAlive)
            {
                // give client a chance to update
                Thread.Sleep(100);
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
                        else if (VERBOSE)
                        {
                            Console.WriteLine("expectedID=" + expectedId + " actual=" + handlerId + " generation=" + reader.IndexCommit.Generation);
                        }
                    }
                    finally
                    {
                        reader.Dispose();
                    }
                }
                catch (Exception)
                {
                    // we can hit IndexNotFoundException or e.g. EOFException (on
                    // segments_N) because it is being copied at the same time it is read by
                    // DirectoryReader.open().
                }
            }
        }

        private IRevision CreateRevision(int id)
        {
            //JAVA:  private Revision createRevision(final int id) throws IOException {
            //JAVA:    publishWriter.addDocument(new Document());
            //JAVA:    publishWriter.setCommitData(new HashMap<String, String>() {{
            //JAVA:      put(VERSION_ID, Integer.toString(id, 16));
            //JAVA:    }});
            //JAVA:    publishWriter.commit();
            //JAVA:    return new IndexRevision(publishWriter);
            //JAVA:  }
            publishWriter.AddDocument(new Document());
            publishWriter.SetCommitData(new Dictionary<string, string>{
                { VERSION_ID, id.ToString("X") }
            });
            publishWriter.Commit();
            return new IndexRevision(publishWriter);
        }

        public override void SetUp()
        {
            //JAVA:  public void setUp() throws Exception {
            //JAVA:    super.setUp();
            //JAVA:    publishDir = newMockDirectory();
            //JAVA:    handlerDir = newMockDirectory();
            //JAVA:    sourceDirFactory = new PerSessionDirectoryFactory(createTempDir("replicationClientTest"));
            //JAVA:    replicator = new LocalReplicator();
            //JAVA:    callback = new IndexReadyCallback(handlerDir);
            //JAVA:    handler = new IndexReplicationHandler(handlerDir, callback);
            //JAVA:    client = new ReplicationClient(replicator, handler, sourceDirFactory);
            //JAVA:    
            //JAVA:    IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, null);
            //JAVA:    conf.setIndexDeletionPolicy(new SnapshotDeletionPolicy(conf.getIndexDeletionPolicy()));
            //JAVA:    publishWriter = new IndexWriter(publishDir, conf);
            //JAVA:  }
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
            //JAVA:  public void tearDown() throws Exception {
            //JAVA:    IOUtils.close(client, callback, publishWriter, replicator, publishDir, handlerDir);
            //JAVA:    super.tearDown();
            //JAVA:  }
            IOUtils.Dispose(client, callback, publishWriter, replicator, publishDir, handlerDir);
            base.TearDown();
        }

        [Test]
        public void TestNoUpdateThread()
        {
            //JAVA:  public void testNoUpdateThread() throws Exception {
            //JAVA:    assertNull("no version expected at start", handler.currentVersion());
            //JAVA:    
            //JAVA:    // Callback validates the replicated index
            //JAVA:    replicator.publish(createRevision(1));
            //JAVA:    client.updateNow();
            //JAVA:    
            //JAVA:    replicator.publish(createRevision(2));
            //JAVA:    client.updateNow();
            //JAVA:    
            //JAVA:    // Publish two revisions without update, handler should be upgraded to latest
            //JAVA:    replicator.publish(createRevision(3));
            //JAVA:    replicator.publish(createRevision(4));
            //JAVA:    client.updateNow();
            //JAVA:  }
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
            //JAVA:  public void testUpdateThread() throws Exception {
            //JAVA:    client.startUpdateThread(10, "index");
            //JAVA:    
            //JAVA:    replicator.publish(createRevision(1));
            //JAVA:    assertHandlerRevision(1, handlerDir);
            //JAVA:    
            //JAVA:    replicator.publish(createRevision(2));
            //JAVA:    assertHandlerRevision(2, handlerDir);
            //JAVA:    
            //JAVA:    // Publish two revisions without update, handler should be upgraded to latest
            //JAVA:    replicator.publish(createRevision(3));
            //JAVA:    replicator.publish(createRevision(4));
            //JAVA:    assertHandlerRevision(4, handlerDir);
            //JAVA:  }

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
            //JAVA:  public void testRestart() throws Exception {
            //JAVA:    replicator.publish(createRevision(1));
            //JAVA:    client.updateNow();
            //JAVA:    
            //JAVA:    replicator.publish(createRevision(2));
            //JAVA:    client.updateNow();
            //JAVA:    
            //JAVA:    client.stopUpdateThread();
            //JAVA:    client.close();
            //JAVA:    client = new ReplicationClient(replicator, handler, sourceDirFactory);
            //JAVA:    
            //JAVA:    // Publish two revisions without update, handler should be upgraded to latest
            //JAVA:    replicator.publish(createRevision(3));
            //JAVA:    replicator.publish(createRevision(4));
            //JAVA:    client.updateNow();
            //JAVA:  }
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

        //JAVA:  /*
        //JAVA:   * This test verifies that the client and handler do not end up in a corrupt
        //JAVA:   * index if exceptions are thrown at any point during replication. Either when
        //JAVA:   * a client copies files from the server to the temporary space, or when the
        //JAVA:   * handler copies them to the index directory.
        //JAVA:   */
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
            //JAVA:    handlerDir.setPreventDoubleWrite(false);
            handlerDir.PreventDoubleWrite = false;

            // wrap sourceDirFactory to return a MockDirWrapper so we can simulate errors
            ISourceDirectoryFactory @in = sourceDirFactory;
            AtomicInt32 failures = new AtomicInt32(AtLeast(10));

            // wrap sourceDirFactory to return a MockDirWrapper so we can simulate errors
            sourceDirFactory = new SourceDirectoryFactoryAnonymousInnerClass(this, @in, failures);
            handler = new IndexReplicationHandler(handlerDir, () =>
            {
                if (Random().NextDouble() < 0.2 && failures.Get() > 0)
                    throw new Exception("random exception from callback");
                return null;
            });
            client = new ReplicationClientAnonymousInnerClass(this, replicator, handler, sourceDirFactory, failures);
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
            handlerDir.RandomIOExceptionRate=0.0;
            handlerDir.RandomIOExceptionRateOnOpen=0.0;
        }

        private class SourceDirectoryFactoryAnonymousInnerClass : ISourceDirectoryFactory
        {
            private long clientMaxSize = 100, handlerMaxSize = 100;
            private double clientExRate = 1.0, handlerExRate = 1.0;

            private readonly IndexReplicationClientTest test;
            private readonly ISourceDirectoryFactory @in;
            private readonly AtomicInt32 failures;

            public SourceDirectoryFactoryAnonymousInnerClass(IndexReplicationClientTest test, ISourceDirectoryFactory @in, AtomicInt32 failures)
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
                if (Random().nextBoolean() && failures.Get() > 0)
                { // client should fail, return wrapped dir
                    MockDirectoryWrapper mdw = new MockDirectoryWrapper(Random(), dir);
                    mdw.RandomIOExceptionRateOnOpen = clientExRate;
                    mdw.MaxSizeInBytes = clientMaxSize;
                    mdw.RandomIOExceptionRate = clientExRate;
                    mdw.CheckIndexOnClose = false;
                    clientMaxSize *= 2;
                    clientExRate /= 2;
                    return mdw;
                }

                if (failures.Get() > 0 && Random().nextBoolean())
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

        private class ReplicationClientAnonymousInnerClass : ReplicationClient
        {
            private readonly IndexReplicationClientTest test;
            private readonly AtomicInt32 failures;

            public ReplicationClientAnonymousInnerClass(IndexReplicationClientTest test, IReplicator replicator, IReplicationHandler handler, ISourceDirectoryFactory factory, AtomicInt32 failures)
                : base(replicator, handler, factory)
            {
                this.test = test;
                this.failures = failures;
            }

            protected override void HandleUpdateException(Exception exception)
            {
                if (exception is IOException)
                {
                    if (VERBOSE)
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
                    //TODO: Java had this, but considering what it does do we need it?
                    //JAVA: catch (IOException e)
                    //JAVA: {
                    //JAVA:     // exceptions here are bad, don't ignore them
                    //JAVA:     throw new RuntimeException(e);
                    //JAVA: }
                    finally
                    {
                        // count-down number of failures
                        failures.DecrementAndGet();
                        Debug.Assert(failures.Get() >= 0, "handler failed too many times: " + failures.Get());
                        if (VERBOSE)
                        {
                            if (failures.Get() == 0)
                            {
                                Console.WriteLine("no more failures expected");
                            }
                            else
                            {
                                Console.WriteLine("num failures left: " + failures.Get());
                            }
                        }
                    }
                } else {
                    //JAVA:          if (t instanceof RuntimeException) throw (RuntimeException) t;
                    //JAVA:          throw new RuntimeException(t);
                    throw exception;
                }
            }
        }
      
    }
}