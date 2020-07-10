using System;
using System.IO;

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

    /// <summary>
    /// An interface for replicating files. Allows a producer to 
    /// <see cref="Publish(IRevision)"/> <see cref="IRevision"/>s and consumers to
    /// <see cref="CheckForUpdate(string)"/>. When a client needs to be
    /// updated, it is given a <see cref="SessionToken"/> through which it can
    /// <see cref="ObtainFile(string, string, string)"/> the files of that
    /// revision. After the client has finished obtaining all the files, it should
    /// <see cref="Release(string)"/> the given session, so that the files can be
    /// reclaimed if they are not needed anymore.
    /// <para/>
    /// A client is always updated to the newest revision available. That is, if a
    /// client is on revision <em>r1</em> and revisions <em>r2</em> and <em>r3</em>
    /// were published, then when the client will next check for update, it will
    /// receive <em>r3</em>.
    /// </summary>
    /// <remarks>
    /// @lucene.experimental
    /// </remarks>
    public interface IReplicator : IDisposable
    {
        /// <summary>
        /// Publish a new <see cref="IRevision"/> for consumption by clients. It is the
        /// caller's responsibility to verify that the revision files exist and can be
        /// read by clients. When the revision is no longer needed, it will be
        /// <see cref="Release"/>d by the replicator.
        /// </summary>
        /// <param name="revision">The <see cref="IRevision"/> to publish.</param>
        /// <exception cref="IOException"></exception>
        void Publish(IRevision revision);

        /// <summary>
        /// Check whether the given version is up-to-date and returns a
        /// <see cref="SessionToken"/> which can be used for fetching the revision files,
        /// otherwise returns <c>null</c>.
        /// </summary>
        /// <remarks>
        /// <b>NOTE:</b> When the returned session token is no longer needed, you
        /// should call <see cref="Release"/> so that the session resources can be
        /// reclaimed, including the revision files.
        /// </remarks>
        /// <exception cref="IOException"></exception>
        SessionToken CheckForUpdate(string currentVersion);

        /// <summary>
        /// Notify that the specified <see cref="SessionToken"/> is no longer needed by the caller.
        /// </summary>
        /// <exception cref="IOException"></exception>
        void Release(string sessionId);

        /// <summary>
        /// Returns an <see cref="Stream"/> for the requested file and source in the
        /// context of the given <see cref="SessionToken.Id"/>.
        /// </summary>
        /// <remarks>
        /// <b>NOTE:</b> It is the caller's responsibility to call <see cref="IDisposable.Dispose"/> on the returned stream.
        /// </remarks>
        /// <exception cref="SessionExpiredException">The specified session has already expired</exception>
        Stream ObtainFile(string sessionId, string source, string fileName);
    }
}