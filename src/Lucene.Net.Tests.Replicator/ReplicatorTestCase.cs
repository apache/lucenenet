//STATUS: PENDING - 4.8.0

using System;
using Lucene.Net.Replicator.Http;
using Lucene.Net.Util;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Lucene.Net.Tests.Replicator
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

    public class ReplicatorTestCase : LuceneTestCase
    {
        //JAVA:  private static ClientConnectionManager clientConnectionManager;
        //JAVA:  
        //JAVA:  @AfterClass
        //JAVA:  public static void afterClassReplicatorTestCase() throws Exception {
        //JAVA:    if (clientConnectionManager != null) {
        //JAVA:      clientConnectionManager.shutdown();
        //JAVA:      clientConnectionManager = null;
        //JAVA:    }
        //JAVA:  }
        //JAVA:  


        public static TestServer NewHttpServer<TStartUp>(ReplicationService service) where TStartUp : class
        {
            #region JAVA
            //JAVA:  /**
            //JAVA:   * Returns a new {@link Server HTTP Server} instance. To obtain its port, use
            //JAVA:   * {@link #serverPort(Server)}.
            //JAVA:   */
            //JAVA:  public static synchronized Server newHttpServer(Handler handler) throws Exception {
            //JAVA:    Server server = new Server(0);
            //JAVA:    
            //JAVA:    server.setHandler(handler);
            //JAVA:    
            //JAVA:    final String connectorName = System.getProperty("tests.jettyConnector", "SelectChannel");
            //JAVA:    
            //JAVA:    // if this property is true, then jetty will be configured to use SSL
            //JAVA:    // leveraging the same system properties as java to specify
            //JAVA:    // the keystore/truststore if they are set
            //JAVA:    //
            //JAVA:    // This means we will use the same truststore, keystore (and keys) for
            //JAVA:    // the server as well as any client actions taken by this JVM in
            //JAVA:    // talking to that server, but for the purposes of testing that should 
            //JAVA:    // be good enough
            //JAVA:    final boolean useSsl = Boolean.getBoolean("tests.jettySsl");
            //JAVA:    final SslContextFactory sslcontext = new SslContextFactory(false);
            //JAVA:    
            //JAVA:    if (useSsl) {
            //JAVA:      if (null != System.getProperty("javax.net.ssl.keyStore")) {
            //JAVA:        sslcontext.setKeyStorePath
            //JAVA:        (System.getProperty("javax.net.ssl.keyStore"));
            //JAVA:      }
            //JAVA:      if (null != System.getProperty("javax.net.ssl.keyStorePassword")) {
            //JAVA:        sslcontext.setKeyStorePassword
            //JAVA:        (System.getProperty("javax.net.ssl.keyStorePassword"));
            //JAVA:      }
            //JAVA:      if (null != System.getProperty("javax.net.ssl.trustStore")) {
            //JAVA:        sslcontext.setTrustStore
            //JAVA:        (System.getProperty("javax.net.ssl.trustStore"));
            //JAVA:      }
            //JAVA:      if (null != System.getProperty("javax.net.ssl.trustStorePassword")) {
            //JAVA:        sslcontext.setTrustStorePassword
            //JAVA:        (System.getProperty("javax.net.ssl.trustStorePassword"));
            //JAVA:      }
            //JAVA:      sslcontext.setNeedClientAuth(Boolean.getBoolean("tests.jettySsl.clientAuth"));
            //JAVA:    }
            //JAVA:    
            //JAVA:    final Connector connector;
            //JAVA:    final QueuedThreadPool threadPool;
            //JAVA:    if ("SelectChannel".equals(connectorName)) {
            //JAVA:      final SelectChannelConnector c = useSsl ? new SslSelectChannelConnector(sslcontext) : new SelectChannelConnector();
            //JAVA:      c.setReuseAddress(true);
            //JAVA:      c.setLowResourcesMaxIdleTime(1500);
            //JAVA:      connector = c;
            //JAVA:      threadPool = (QueuedThreadPool) c.getThreadPool();
            //JAVA:    } else if ("Socket".equals(connectorName)) {
            //JAVA:      final SocketConnector c = useSsl ? new SslSocketConnector(sslcontext) : new SocketConnector();
            //JAVA:      c.setReuseAddress(true);
            //JAVA:      connector = c;
            //JAVA:      threadPool = (QueuedThreadPool) c.getThreadPool();
            //JAVA:    } else {
            //JAVA:      throw new IllegalArgumentException("Illegal value for system property 'tests.jettyConnector': " + connectorName);
            //JAVA:    }
            //JAVA:    
            //JAVA:    connector.setPort(0);
            //JAVA:    connector.setHost("127.0.0.1");
            //JAVA:    if (threadPool != null) {
            //JAVA:      threadPool.setDaemon(true);
            //JAVA:      threadPool.setMaxThreads(10000);
            //JAVA:      threadPool.setMaxIdleTimeMs(5000);
            //JAVA:      threadPool.setMaxStopTimeMs(30000);
            //JAVA:    }
            //JAVA:    
            //JAVA:    server.setConnectors(new Connector[] {connector});
            //JAVA:    server.setSessionIdManager(new HashSessionIdManager(new Random(random().nextLong())));
            //JAVA:    
            //JAVA:    server.start();
            //JAVA:    
            //JAVA:    return server;
            //JAVA:  }
            //JAVA:  
            #endregion

            var server = new TestServer(new WebHostBuilder()
                .ConfigureServices(container =>
                {
                    container.AddSingleton(service);
                }).UseStartup<TStartUp>());
            server.BaseAddress = new Uri("http://localhost" + ReplicationService.REPLICATION_CONTEXT);
            return server;
        }

        /// <summary>
        /// Returns a <see cref="server"/>'s port. 
        /// </summary>
        public static int ServerPort(TestServer server)
        {
            //JAVA:  /** Returns a {@link Server}'s port. */
            //JAVA:  public static int serverPort(Server server) {
            //JAVA:    return server.getConnectors()[0].getLocalPort();
            //JAVA:  }
            return server.BaseAddress.Port;
        }

        /// <summary>
        /// Returns a <see cref="server"/>'s host. 
        /// </summary>
        public static string ServerHost(TestServer server)
        {
            //JAVA:  /** Returns a {@link Server}'s host. */
            //JAVA:  public static String serverHost(Server server) {
            //JAVA:    return server.getConnectors()[0].getHost();
            //JAVA:  }
            return server.BaseAddress.Host;
        }

        /// <summary>
        /// Stops the given HTTP Server instance.
        /// </summary>
        public static void StopHttpServer(TestServer server)
        {
            //JAVA:  /**
            //JAVA:   * Stops the given HTTP Server instance. This method does its best to guarantee
            //JAVA:   * that no threads will be left running following this method.
            //JAVA:   */
            //JAVA:  public static void stopHttpServer(Server httpServer) throws Exception {
            //JAVA:    httpServer.stop();
            //JAVA:    httpServer.join();
            //JAVA:  }
            server.Dispose();
        }

        //JAVA:  
        //JAVA:  /**
        //JAVA:   * Returns a {@link ClientConnectionManager}.
        //JAVA:   * <p>
        //JAVA:   * <b>NOTE:</b> do not {@link ClientConnectionManager#shutdown()} this
        //JAVA:   * connection manager, it will be shutdown automatically after all tests have
        //JAVA:   * finished.
        //JAVA:   */
        //JAVA:  public static synchronized ClientConnectionManager getClientConnectionManager() {
        //JAVA:    if (clientConnectionManager == null) {
        //JAVA:      PoolingClientConnectionManager ccm = new PoolingClientConnectionManager();
        //JAVA:      ccm.setDefaultMaxPerRoute(128);
        //JAVA:      ccm.setMaxTotal(128);
        //JAVA:      clientConnectionManager = ccm;
        //JAVA:    }
        //JAVA:    
        //JAVA:    return clientConnectionManager;
        //JAVA:  }
    }
}