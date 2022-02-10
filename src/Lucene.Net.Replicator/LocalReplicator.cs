using J2N.Threading.Atomic;
using Lucene.Net.Support.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

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
    /// A <see cref="IReplicator"/> implementation for use by the side that publishes
    /// <see cref="IRevision"/>s, as well for clients to <see cref="CheckForUpdate"/>
    /// check for updates}. When a client needs to be updated, it is returned a
    /// <see cref="SessionToken"/> through which it can
    /// <see cref="ObtainFile"/> the files of that
    /// revision. As long as a revision is being replicated, this replicator
    /// guarantees that it will not be <see cref="IRevision.Release"/>.
    /// <para/>
    /// Replication sessions expire by default after
    /// <seea cref="DEFAULT_SESSION_EXPIRATION_THRESHOLD"/>, and the threshold can be
    /// configured through <see cref="ExpirationThreshold"/>.
    /// </summary>
    /// <remarks>
    /// @lucene.experimental
    /// </remarks>
    public class LocalReplicator : IReplicator
    {
        private class RefCountedRevision
        {
            private readonly AtomicInt32 refCount = new AtomicInt32(1);

            public IRevision Revision { get; private set; }

            public RefCountedRevision(IRevision revision)
            {
                Revision = revision;
            }

            /// <summary/>
            /// <exception cref="InvalidOperationException"></exception>
            public virtual void DecRef()
            {
                if (refCount <= 0)
                {
                    throw IllegalStateException.Create("this revision is already released");
                }

                var rc = refCount.DecrementAndGet();
                if (rc == 0)
                {
                    bool success = false;
                    try
                    {
                        Revision.Release();
                        success = true;
                    }
                    finally
                    {
                        if (!success)
                        {
                            // Put reference back on failure
                            refCount.IncrementAndGet();
                        }
                    }
                }
                else if (rc < 0)
                {
                    throw IllegalStateException.Create(string.Format("too many decRef calls: refCount is {0} after decrement", rc));
                }
            }

            public virtual void IncRef()
            {
                refCount.IncrementAndGet();
            }
        }

        private class ReplicationSession
        {
            public SessionToken Session { get; private set; }
            public RefCountedRevision Revision { get; private set; }

            private long lastAccessTime;

            public ReplicationSession(SessionToken session, RefCountedRevision revision)
            {
                Session = session;
                Revision = revision;
                lastAccessTime = Stopwatch.GetTimestamp(); // LUCENENET: Use the most accurate timer to determine expiration
            }

            public virtual bool IsExpired(long expirationThreshold)
            {
                return lastAccessTime < Stopwatch.GetTimestamp() - expirationThreshold * Stopwatch.Frequency / 1000; // LUCENENET: Use the most accurate timer to determine expiration
            }

            public virtual void MarkAccessed()
            {
                lastAccessTime = Stopwatch.GetTimestamp(); // LUCENENET: Use the most accurate timer to determine expiration
            }
        }

        /// <summary>Threshold for expiring inactive sessions. Defaults to 30 minutes.</summary>
        public const long DEFAULT_SESSION_EXPIRATION_THRESHOLD = 1000 * 60 * 30;

        private long expirationThreshold = DEFAULT_SESSION_EXPIRATION_THRESHOLD;

        private readonly object syncLock = new object();

        private volatile RefCountedRevision currentRevision;
        private volatile bool disposed = false;

        private readonly AtomicInt32 sessionToken = new AtomicInt32(0);
        private readonly IDictionary<string, ReplicationSession> sessions = new Dictionary<string, ReplicationSession>();

        /// <exception cref="InvalidOperationException"></exception>
        private void CheckExpiredSessions()
        {
            // .NET NOTE: .ToArray() so we don't modify a collection we are enumerating...
            //            I am wondering if it would be overall more practical to switch to a concurrent dictionary...
            foreach (ReplicationSession token in sessions.Values.Where(token => token.IsExpired(ExpirationThreshold)).ToArray())
            {
                ReleaseSession(token.Session.Id);
            }
        }

        /// <exception cref="InvalidOperationException"></exception>
        private void ReleaseSession(string sessionId)
        {
            // if we're called concurrently by close() and release(), could be that one
            // thread beats the other to release the session.
            if (sessions.TryGetValue(sessionId, out ReplicationSession session))
            {
                sessions.Remove(sessionId);
                session.Revision.DecRef();
            }
        }

        /// <summary>
        /// Ensure that replicator is still open, or throw <see cref="ObjectDisposedException"/> otherwise.
        /// </summary>
        /// <exception cref="ObjectDisposedException">This replicator has already been disposed.</exception>
        protected void EnsureOpen()
        {
            UninterruptableMonitor.Enter(syncLock);
            try
            {
                if (!disposed)
                    return;

                throw AlreadyClosedException.Create(this.GetType().FullName, "This replicator has already been disposed.");
            }
            finally
            {
                UninterruptableMonitor.Exit(syncLock);
            }
        }

        public virtual SessionToken CheckForUpdate(string currentVersion)
        {
            UninterruptableMonitor.Enter(syncLock);
            try
            {
                EnsureOpen();
                if (currentRevision is null)
                    return null; // no published revisions yet

                if (currentVersion != null && currentRevision.Revision.CompareTo(currentVersion) <= 0)
                    return null; // currentVersion is newer or equal to latest published revision

                // currentVersion is either null or older than latest published revision
                currentRevision.IncRef();

                string sessionID = sessionToken.IncrementAndGet().ToString();
                SessionToken token = new SessionToken(sessionID, currentRevision.Revision);
                sessions[sessionID] = new ReplicationSession(token, currentRevision);
                return token;
            }
            finally
            {
                UninterruptableMonitor.Exit(syncLock);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed || !disposing)
                return;

            UninterruptableMonitor.Enter(syncLock);
            try
            {
                foreach (ReplicationSession session in sessions.Values)
                    session.Revision.DecRef();
                sessions.Clear();
            }
            finally
            {
                UninterruptableMonitor.Exit(syncLock);
            }
            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Gets or sets the expiration threshold in milliseconds.
        /// <para/>
        /// If a replication session is inactive this
        /// long it is automatically expired, and further attempts to operate within
        /// this session will throw a <see cref="SessionExpiredException"/>.
        /// </summary>
        public virtual long ExpirationThreshold
        {
            get => expirationThreshold;
            set
            {
                UninterruptableMonitor.Enter(syncLock);
                try
                {
                    EnsureOpen();
                    expirationThreshold = value;
                    CheckExpiredSessions();
                }
                finally
                {
                    UninterruptableMonitor.Exit(syncLock);
                }
            }
        }

        public virtual Stream ObtainFile(string sessionId, string source, string fileName)
        {
            UninterruptableMonitor.Enter(syncLock);
            try
            {
                EnsureOpen();

                if (sessions.TryGetValue(sessionId, out ReplicationSession session) && session != null && session.IsExpired(ExpirationThreshold))
                {
                    ReleaseSession(sessionId);
                    session = null;
                }
                // session either previously expired, or we just expired it
                if (session is null)
                {
                    throw new SessionExpiredException(string.Format("session ({0}) expired while obtaining file: source={1} file={2}", sessionId, source, fileName));
                }
                sessions[sessionId].MarkAccessed();
                return session.Revision.Revision.Open(source, fileName);
            }
            finally
            {
                UninterruptableMonitor.Exit(syncLock);
            }
        }

        public virtual void Publish(IRevision revision)
        {
            UninterruptableMonitor.Enter(syncLock);
            try
            {
                EnsureOpen();

                if (currentRevision != null)
                {
                    int compare = revision.CompareTo(currentRevision.Revision);
                    if (compare == 0)
                    {
                        // same revision published again, ignore but release it
                        revision.Release();
                        return;
                    }

                    if (compare < 0)
                    {
                        revision.Release();
                        throw new ArgumentException(string.Format("Cannot publish an older revision: rev={0} current={1}", revision, currentRevision), nameof(revision));
                    }
                }

                RefCountedRevision oldRevision = currentRevision;
                currentRevision = new RefCountedRevision(revision);
                if (oldRevision != null)
                    oldRevision.DecRef();

                CheckExpiredSessions();
            }
            finally
            {
                UninterruptableMonitor.Exit(syncLock);
            }
        }

        /// <exception cref="InvalidOperationException"></exception>
        public virtual void Release(string sessionId)
        {
            UninterruptableMonitor.Enter(syncLock);
            try
            {
                EnsureOpen();
                ReleaseSession(sessionId);
            }
            finally
            {
                UninterruptableMonitor.Exit(syncLock);
            }
        }
    }
}