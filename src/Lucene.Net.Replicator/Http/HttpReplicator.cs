//STATUS: DRAFT - 4.8.0

using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Lucene.Net.Support.IO;

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

    /// <summary>
    /// An HTTP implementation of <see cref="IReplicator"/>. Assumes the API supported by <see cref="ReplicationService"/>.
    /// </summary>
    /// <remarks>
    /// Lucene.Experimental
    /// </remarks>
    public class HttpReplicator : HttpClientBase, IReplicator
    {
        public HttpReplicator(string host, int port, string path, HttpMessageHandler messageHandler) 
            : base(host, port, path, messageHandler)
        {
            #region Java
            //JAVA: /** Construct with specified connection manager. */
            //JAVA: public HttpReplicator(String host, int port, String path, ClientConnectionManager conMgr) {
            //JAVA:   super(host, port, path, conMgr);
            //JAVA: }
            #endregion
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="revision"></param>
        /// <exception cref="NotSupportedException">this replicator implementation does not support remote publishing of revisions</exception>
        public void Publish(IRevision revision)
        {
            throw new NotSupportedException("this replicator implementation does not support remote publishing of revisions");
        }

        public SessionToken CheckForUpdate(string currentVersion)
        {
            #region Java
            //JAVA: public SessionToken checkForUpdate(String currVersion) throws IOException {
            //JAVA:   String[] params = null;
            //JAVA:   if (currVersion != null) {
            //JAVA:     params = new String[] { ReplicationService.REPLICATE_VERSION_PARAM, currVersion };
            //JAVA:   }
            //JAVA:   final HttpResponse response = executeGET(ReplicationAction.UPDATE.name(), params);
            //JAVA:   return doAction(response, new Callable<SessionToken>() {
            //JAVA:     @Override
            //JAVA:     public SessionToken call() throws Exception {
            //JAVA:       final DataInputStream dis = new DataInputStream(responseInputStream(response));
            //JAVA:       try {
            //JAVA:         if (dis.readByte() == 0) {
            //JAVA:           return null;
            //JAVA:         } else {
            //JAVA:           return new SessionToken(dis);
            //JAVA:         }
            //JAVA:       } finally {
            //JAVA:         dis.close();
            //JAVA:       }
            //JAVA:     }
            //JAVA:   });
            //JAVA: }
            #endregion

            string[] parameters = null;
            if (currentVersion != null)
                parameters = new [] { ReplicationService.REPLICATE_VERSION_PARAM, currentVersion };

            HttpResponseMessage response = base.ExecuteGet( ReplicationService.ReplicationAction.UPDATE.ToString(), parameters);
            return DoAction(response, () =>
            {
                using (DataInputStream inputStream = new DataInputStream(ResponseInputStream(response)))
                {
                    return inputStream.ReadByte() == 0 ? null : new SessionToken(inputStream);
                }
            });
        }

        public void Release(string sessionId)
        {
            #region Java
            //JAVA: public void release(String sessionID) throws IOException {
            //JAVA:   String[] params = new String[] {
            //JAVA:       ReplicationService.REPLICATE_SESSION_ID_PARAM, sessionID
            //JAVA:   };
            //JAVA:   final HttpResponse response = executeGET(ReplicationAction.RELEASE.name(), params);
            //JAVA:   doAction(response, new Callable<Object>() {
            //JAVA:     @Override
            //JAVA:     public Object call() throws Exception {
            //JAVA:       return null; // do not remove this call: as it is still validating for us!
            //JAVA:     }
            //JAVA:   });
            //JAVA: }
            #endregion

            HttpResponseMessage response = ExecuteGet(ReplicationService.ReplicationAction.RELEASE.ToString(), ReplicationService.REPLICATE_SESSION_ID_PARAM, sessionId);
            // do not remove this call: as it is still validating for us!
            DoAction<object>(response, () => null);
        }

        public Stream ObtainFile(string sessionId, string source, string fileName)
        {
            #region Java
            //JAVA: public InputStream obtainFile(String sessionID, String source, String fileName) throws IOException {
            //JAVA:   String[] params = new String[] {
            //JAVA:       ReplicationService.REPLICATE_SESSION_ID_PARAM, sessionID,
            //JAVA:       ReplicationService.REPLICATE_SOURCE_PARAM, source,
            //JAVA:       ReplicationService.REPLICATE_FILENAME_PARAM, fileName,
            //JAVA:   };
            //JAVA:   final HttpResponse response = executeGET(ReplicationAction.OBTAIN.name(), params);
            //JAVA:   return doAction(response, false, new Callable<InputStream>() {
            //JAVA:     @Override
            //JAVA:     public InputStream call() throws Exception {
            //JAVA:       return responseInputStream(response,true);
            //JAVA:     }
            //JAVA:   });
            //JAVA: }
            #endregion
            HttpResponseMessage response = ExecuteGet(ReplicationService.ReplicationAction.OBTAIN.ToString(), 
                ReplicationService.REPLICATE_SESSION_ID_PARAM, sessionId,
                ReplicationService.REPLICATE_SOURCE_PARAM, source,
                ReplicationService.REPLICATE_FILENAME_PARAM, fileName);
            return DoAction(response, false, () => ResponseInputStream(response));
        }
    }
}