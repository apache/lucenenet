using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Directory = Lucene.Net.Store.Directory;

#if FEATURE_ASPNETCORE_TESTHOST
using Microsoft.AspNetCore.TestHost;
#else
using Lucene.Net.Replicator.Net;
#endif


namespace Lucene.Net.Replicator.Http
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

    // Technically, the ConfigOption is only supported by ASP.NET Core
    // so we just ignore the other option when running on HttpListener.
    [TestFixture(IOOption.Synchronous, ConfigOption.StartupClass)]
    [TestFixture(IOOption.Asynchronous, ConfigOption.StartupClass)]
#if FEATURE_ASPNETCORE_TESTHOST
    [TestFixture(IOOption.Synchronous, ConfigOption.Middleware)]
    [TestFixture(IOOption.Asynchronous, ConfigOption.Middleware)]
#endif
    public class HttpReplicatorTest : ReplicatorTestCase
    {
        private DirectoryInfo clientWorkDir;
        private IReplicator serverReplicator;
        private IndexWriter writer;
        private DirectoryReader reader;

        private int port;
        private string host;
        private TestServer server;

        private Directory serverIndexDir;
        private Directory handlerIndexDir;

        private MockErrorConfig mockErrorConfig;

        private readonly bool useSynchronousIO;
        private readonly bool useStartupClass;

        public enum IOOption
        {
            Synchronous,
            Asynchronous,
        }

        public enum ConfigOption
        {
            StartupClass,
            Middleware
        }

        public HttpReplicatorTest(IOOption ioOption, ConfigOption configOption)
        {
            this.useSynchronousIO = ioOption == IOOption.Synchronous;
            this.useStartupClass = configOption == ConfigOption.StartupClass;
        }

        private void StartServer()
        {
            ReplicationService service = new ReplicationService(new Dictionary<string, IReplicator> { { "s1", serverReplicator } });
            server = NewHttpServer(service, mockErrorConfig, useSynchronousIO, useStartupClass);
            port = ServerPort(server);
            host = ServerHost(server);
        }

        public override void SetUp()
        {
            base.SetUp();
            clientWorkDir = CreateTempDir("httpReplicatorTest");
            handlerIndexDir = NewDirectory();
            serverIndexDir = NewDirectory();
            mockErrorConfig = new MockErrorConfig(); // LUCENENET specific
            serverReplicator = new LocalReplicator();
            StartServer();

            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, null);
            conf.IndexDeletionPolicy = new SnapshotDeletionPolicy(conf.IndexDeletionPolicy);
            writer = new IndexWriter(serverIndexDir, conf);
            reader = DirectoryReader.Open(writer, false);
        }

        public override void TearDown()
        {
            StopHttpServer(server);
            IOUtils.Dispose(reader, writer, handlerIndexDir, serverIndexDir);
            base.TearDown();
        }

        private void PublishRevision(int id)
        {
            Document doc = new Document();
            writer.AddDocument(doc);
            writer.SetCommitData(Collections.SingletonMap("ID", id.ToString("X")));
            writer.Commit();
            serverReplicator.Publish(new IndexRevision(writer));
        }

        private void ReopenReader()
        {
            DirectoryReader newReader = DirectoryReader.OpenIfChanged(reader);
            assertNotNull(newReader);
            reader.Dispose();
            reader = newReader;
        }


        [Test]
        public void TestBasic()
        {
            IReplicator replicator = new HttpReplicator(host, port, ReplicationService.REPLICATION_CONTEXT + "/s1", server.CreateHandler());
            ReplicationClient client = new ReplicationClient(replicator, new IndexReplicationHandler(handlerIndexDir, null),
                new PerSessionDirectoryFactory(clientWorkDir.FullName));

            PublishRevision(1);
            client.UpdateNow();
            ReopenReader();
            assertEquals(1, int.Parse(reader.IndexCommit.UserData["ID"], NumberStyles.HexNumber));

            PublishRevision(2);
            client.UpdateNow();
            ReopenReader();
            assertEquals(2, int.Parse(reader.IndexCommit.UserData["ID"], NumberStyles.HexNumber));
        }

        [Test]
        public void TestServerErrors()
        {
            // tests the behaviour of the client when the server sends an error
            IReplicator replicator = new HttpReplicator(host, port, ReplicationService.REPLICATION_CONTEXT + "/s1", server.CreateHandler());
            using ReplicationClient client = new ReplicationClient(replicator, new IndexReplicationHandler(handlerIndexDir, null),
                new PerSessionDirectoryFactory(clientWorkDir.FullName));

            try
            {
                PublishRevision(5);

                try
                {
                    mockErrorConfig.RespondWithError = true;
                    client.UpdateNow();
                    fail("expected exception");
                }
                catch (Exception t) when (t.IsThrowable())
                {
                    // expected
                }

                mockErrorConfig.RespondWithError = false;
                client.UpdateNow(); // now it should work
                ReopenReader();
                assertEquals(5, int.Parse(reader.IndexCommit.UserData["ID"], NumberStyles.HexNumber));

                client.Dispose();
            }
            finally
            {
                mockErrorConfig.RespondWithError = false;
            }
        }
    }
}
