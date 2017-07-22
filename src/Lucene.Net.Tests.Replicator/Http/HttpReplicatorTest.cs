//STATUS: DRAFT - 4.8.0

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Replicator;
using Lucene.Net.Replicator.Http;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Directory = Lucene.Net.Store.Directory;

namespace Lucene.Net.Tests.Replicator.Http
{
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

        private void StartServer()
        {
            ReplicationService service = new ReplicationService(new Dictionary<string, IReplicator> { { "s1", serverReplicator } });

            server = NewHttpServer<ReplicationServlet>(service);
            port = ServerPort(server);
            host = ServerHost(server);
        }

        public override void SetUp()
        {
            base.SetUp();
            //JAVA:    System.setProperty("org.eclipse.jetty.LEVEL", "DEBUG"); // sets stderr logging to DEBUG level
            clientWorkDir = CreateTempDir("httpReplicatorTest");
            handlerIndexDir = NewDirectory();
            serverIndexDir = NewDirectory();
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
            //JAVA:    System.clearProperty("org.eclipse.jetty.LEVEL");
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
    }
}
