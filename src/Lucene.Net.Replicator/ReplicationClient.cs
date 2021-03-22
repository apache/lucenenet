using Lucene.Net.Store;
using Lucene.Net.Support.Threading;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using JCG = J2N.Collections.Generic;
using Directory = Lucene.Net.Store.Directory;
using Lucene.Net.Diagnostics;

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
    /// A client which monitors and obtains new revisions from a <see cref="IReplicator"/>.
    /// It can be used to either periodically check for updates by invoking
    /// <see cref="StartUpdateThread"/>, or manually by calling <see cref="UpdateNow()"/>.
    /// <para/>
    /// Whenever a new revision is available, the <see cref="RequiredFiles"/> are
    /// copied to the <see cref="Directory"/> specified by <see cref="PerSessionDirectoryFactory"/> and
    /// a handler is notified.
    /// </summary>
    /// <remarks>
    /// @lucene.experimental
    /// </remarks>
    public class ReplicationClient : IDisposable
    {
        // LUCENENET TODO: Check to ensure ThreadInterruptException is being passed from the background worker to the current thread, as it is required by IndexWriter

        //Note: LUCENENET specific, .NET does not work with Threads in the same way as Java does, so we mimic the same behavior using the ThreadPool instead.
        private class ReplicationThread
        {
            private readonly Action doUpdate;
            private readonly Action<Exception> handleException;
            private readonly ReentrantLock @lock;
            private readonly object controlLock = new object();

            private readonly long interval;
            private readonly AutoResetEvent handle = new AutoResetEvent(false);

            private AutoResetEvent stopHandle;

            /// <summary>
            /// Gets or sets the name
            /// </summary>
            public string Name { get; private set; }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="intervalMillis">The interval in milliseconds.</param>
            /// <param name="threadName">The thread name.</param>
            /// <param name="doUpdate">A delegate to call to perform the update.</param>
            /// <param name="handleException">A delegate to call to handle an exception.</param>
            /// <param name="lock"></param>
            public ReplicationThread(long intervalMillis, string threadName, Action doUpdate, Action<Exception> handleException, ReentrantLock @lock)
            {
                this.doUpdate = doUpdate;
                this.handleException = handleException;
                this.@lock = @lock;
                Name = threadName;
                this.interval = intervalMillis;
            }

            /// <summary>
            /// 
            /// </summary>
            public bool IsAlive { get; private set; }

            /// <summary>
            /// 
            /// </summary>
            public void Start()
            {
                lock (controlLock)
                {
                    if (IsAlive)
                        return;
                    IsAlive = true;
                }
                RegisterWait(interval);
            }

            /// <summary>
            /// 
            /// </summary>
            public void Stop()
            {
                lock (controlLock)
                {
                    if (!IsAlive)
                        return;
                    IsAlive = false;
                }
                stopHandle = new AutoResetEvent(false);

                //NOTE: Execute any outstanding, this execution will terminate almost instantaniously if it's not already running.
                ExecuteImmediately();

                stopHandle.WaitOne();
                stopHandle = null;
            }

            /// <summary>
            /// Executes the next cycle of work immediately
            /// </summary>
            public void ExecuteImmediately()
            {
                handle.Set();
            }

            private void RegisterWait(long timeout)
            {
                //NOTE: We don't care about timedout as it can either be because we was requested to run immidiately or stop.
                if (IsAlive)
                    ThreadPool.RegisterWaitForSingleObject(handle, (state, timedout) => Run(), null, timeout, true);
                else
                    SignalStop();
            }

            private void SignalStop()
            {
                if (stopHandle != null)
                    stopHandle.Set();
            }

            private void Run()
            {
                if (!IsAlive)
                {
                    SignalStop();
                    return;
                }

                Stopwatch timer = Stopwatch.StartNew();
                @lock.Lock();
                try
                {
                    doUpdate();
                }
                catch (Exception exception)
                {
                    handleException(exception);
                }
                finally
                {
                    @lock.Unlock();

                    timer.Stop();
                    long driftAdjusted = Math.Max(interval - timer.ElapsedMilliseconds, 0);
                    if (IsAlive)
                        RegisterWait(driftAdjusted);
                    else
                        SignalStop();
                }
            }
        }

        // LUCENENET specific - de-nested the IReplicationHandler and
        // ISourceDirectoryFactory interfaces.

        /// <summary>
        /// The component name to use with <see cref="Util.InfoStream.IsEnabled"/>
        /// </summary>
        public const string INFO_STREAM_COMPONENT = "ReplicationThread";

        private readonly IReplicator replicator;
        private readonly IReplicationHandler handler;
        private readonly ISourceDirectoryFactory factory;

        private readonly byte[] copyBuffer = new byte[16384];
        private readonly ReentrantLock updateLock = new ReentrantLock();

        private ReplicationThread updateThread;
        private bool disposed = false;
        private InfoStream infoStream = InfoStream.Default;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="replicator">The <see cref="IReplicator"/> used for checking for updates</param>
        /// <param name="handler">The <see cref="IReplicationHandler"/> notified when new revisions are ready</param>
        /// <param name="factory">The <see cref="ISourceDirectoryFactory"/> for returning a <see cref="Directory"/> for a given source and session</param>
        public ReplicationClient(IReplicator replicator, IReplicationHandler handler, ISourceDirectoryFactory factory)
        {
            this.replicator = replicator;
            this.handler = handler;
            this.factory = factory;
        }

        /// <exception cref="IOException"></exception>
        private void CopyBytes(IndexOutput output, Stream input)
        {
            int numBytes;
            while ((numBytes = input.Read(copyBuffer, 0, copyBuffer.Length)) > 0)
            {
                output.WriteBytes(copyBuffer, 0, numBytes);
            }
        }

        /// <exception cref="IOException"></exception>
        private void DoUpdate()
        {
            SessionToken session = null;
            Dictionary<string, Directory> sourceDirectory = new Dictionary<string, Directory>();
            Dictionary<string, IList<string>> copiedFiles = new Dictionary<string, IList<string>>();
            bool notify = false;
            try
            {
                string version = handler.CurrentVersion;
                session = replicator.CheckForUpdate(version);

                WriteToInfoStream(string.Format("doUpdate(): handlerVersion={0} session={1}", version, session));

                if (session == null)
                    return;

                IDictionary<string, IList<RevisionFile>> requiredFiles = RequiredFiles(session.SourceFiles);
                WriteToInfoStream(string.Format("doUpdate(): handlerVersion={0} session={1}", version, session));

                foreach (KeyValuePair<string, IList<RevisionFile>> pair in requiredFiles)
                {
                    string source = pair.Key;
                    Directory directory = factory.GetDirectory(session.Id, source);

                    sourceDirectory.Add(source, directory);
                    List<string> cpFiles = new List<string>();
                    copiedFiles.Add(source, cpFiles);
                    foreach (RevisionFile file in pair.Value)
                    {
                        if (disposed)
                        {
                            // if we're closed, abort file copy
                            WriteToInfoStream("doUpdate(): detected client was closed); abort file copy");
                            return;
                        }

                        Stream input = null;
                        IndexOutput output = null;
                        try
                        {
                            input = replicator.ObtainFile(session.Id, source, file.FileName);
                            output = directory.CreateOutput(file.FileName, IOContext.DEFAULT);

                            CopyBytes(output, input);
                            
                            cpFiles.Add(file.FileName);
                            // TODO add some validation, on size / checksum
                        }
                        finally
                        {
                            IOUtils.Dispose(input, output);
                        }
                    }
                    // only notify if all required files were successfully obtained.
                    notify = true;
                }
            }
            finally
            {
                if (session != null)
                {
                    try
                    {
                        replicator.Release(session.Id);
                    }
                    finally
                    {
                        if (!notify)
                        { 
                            // cleanup after ourselves
                            IOUtils.Dispose(sourceDirectory.Values);
                            factory.CleanupSession(session.Id);
                        }
                    }
                }
            }

            // notify outside the try-finally above, so the session is released sooner.
            // the handler may take time to finish acting on the copied files, but the
            // session itself is no longer needed.
            try
            {
                if (notify && !disposed)
                { // no use to notify if we are closed already
                    handler.RevisionReady(session.Version, session.SourceFiles, new ReadOnlyDictionary<string, IList<string>>(copiedFiles), sourceDirectory);
                }
            }
            finally
            {
                IOUtils.Dispose(sourceDirectory.Values);
                //TODO: Resharper Message, Expression is always true -> Verify and if so then we can remove the null check.
                if (session != null)
                {
                    factory.CleanupSession(session.Id);
                }
            }
        }

        /// <summary>Throws <see cref="ObjectDisposedException"/> if the client has already been disposed.</summary>
        protected void EnsureOpen()
        {
            if (!disposed)
                return;

            throw AlreadyClosedException.Create(this.GetType().FullName, "this update client has already been disposed.");
        }

        // LUCENENET specific Utility Method
        private void WriteToInfoStream(string message)
        {
            if (infoStream.IsEnabled(INFO_STREAM_COMPONENT))
                infoStream.Message(INFO_STREAM_COMPONENT, message);
        }

        /// <summary>
        /// Called when an exception is hit by the replication thread. The default
        /// implementation prints the full stacktrace to the <see cref="Util.InfoStream"/> set in
        /// <see cref="InfoStream"/>, or the <see cref="Util.InfoStream.Default"/>
        /// one. You can override to log the exception elsewhere.
        /// </summary>
        /// <remarks>
        /// <b>NOTE:</b> If you override this method to throw the exception further,
        /// the replication thread will be terminated. The only way to restart it is to
        /// call <see cref="StopUpdateThread"/> followed by
        /// <see cref="StartUpdateThread"/>.
        /// </remarks>
        protected virtual void HandleUpdateException(Exception exception)
        {
            WriteToInfoStream(string.Format("an error occurred during revision update: {0}", exception));
        }

        /// <summary>
        /// Returns the files required for replication. By default, this method returns
        /// all files that exist in the new revision, but not in the handler.
        /// </summary>
        protected virtual IDictionary<string, IList<RevisionFile>> RequiredFiles(IDictionary<string, IList<RevisionFile>> newRevisionFiles)
        {
            IDictionary<string, IList<RevisionFile>> handlerRevisionFiles = handler.CurrentRevisionFiles;
            if (handlerRevisionFiles == null)
                return newRevisionFiles;

            Dictionary<string, IList<RevisionFile>> requiredFiles = new Dictionary<string, IList<RevisionFile>>();
            foreach (var e in handlerRevisionFiles)
            {
                // put the handler files in a Set, for faster contains() checks later
                ISet<string> handlerFiles = new JCG.HashSet<string>();
                foreach (RevisionFile file in e.Value)
                {
                    handlerFiles.Add(file.FileName);
                }

                // make sure to preserve revisionFiles order
                List<RevisionFile> res = new List<RevisionFile>();
                string source = e.Key;
                if (Debugging.AssertsEnabled) Debugging.Assert(newRevisionFiles.ContainsKey(source), "source not found in newRevisionFiles: {0}", newRevisionFiles);
                foreach (RevisionFile file in newRevisionFiles[source])
                {
                    if (!handlerFiles.Contains(file.FileName))
                    {
                        res.Add(file);
                    }
                }
                requiredFiles[source] = res;
            }

            return requiredFiles;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed || !disposing)
                return;

            StopUpdateThread();
            infoStream.Dispose(); // LUCENENET specific
            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Start the update thread with the specified interval in milliseconds. For
        /// debugging purposes, you can optionally set the name to set on
        /// <see cref="ReplicationThread.Name"/>. If you pass <c>null</c>, a default name
        /// will be set.
        /// </summary>
        /// <exception cref="InvalidOperationException"> if the thread has already been started </exception>
        public virtual void StartUpdateThread(long intervalMillis, string threadName)
        {
            EnsureOpen();
            if (updateThread != null && updateThread.IsAlive)
                throw IllegalStateException.Create("cannot start an update thread when one is running, must first call 'stopUpdateThread()'");

            threadName = threadName == null ? INFO_STREAM_COMPONENT : "ReplicationThread-" + threadName;
            updateThread = new ReplicationThread(intervalMillis, threadName, DoUpdate, HandleUpdateException, updateLock);
            updateThread.Start();
            // we rely on isAlive to return true in isUpdateThreadAlive, assert to be on the safe side
            if (Debugging.AssertsEnabled) Debugging.Assert(updateThread.IsAlive, "updateThread started but not alive?");
        }

        /// <summary>
        /// Stop the update thread. If the update thread is not running, silently does
        /// nothing. This method returns after the update thread has stopped.
        /// </summary>
        public virtual void StopUpdateThread()
        {
            // this will trigger the thread to terminate if it awaits the lock.
            // otherwise, if it's in the middle of replication, we wait for it to
            // stop.
            if (updateThread != null)
            {
                try
                {
                    updateThread.Stop();
                }
                catch (Exception ie) when (ie.IsInterruptedException())
                {
                    throw new Util.ThreadInterruptedException(ie);
                }
            }
            updateThread = null;
        }

        /// <summary>
        /// Returns true if the update thread is alive. The update thread is alive if
        /// it has been <see cref="StartUpdateThread"/> and not
        /// <see cref="StopUpdateThread"/>, as well as didn't hit an error which
        /// caused it to terminate (i.e. <see cref="HandleUpdateException"/>
        /// threw the exception further).
        /// </summary>
        public virtual bool IsUpdateThreadAlive => updateThread != null && updateThread.IsAlive;

        public override string ToString()
        {
            if (updateThread == null)
                return "ReplicationClient";
            return string.Format("ReplicationClient ({0})", updateThread.Name);
        }

        /// <summary>
        /// Executes the update operation immediately, regardless if an update thread
        /// is running or not.
        /// </summary>
        /// <exception cref="IOException"></exception>
        public virtual void UpdateNow() 
        {
            EnsureOpen();
            if (updateThread != null)
            {
                //NOTE: We have a worker running, we use that to perform the work instead by requesting it to run
                //      it's cycle immidiately.
                updateThread.ExecuteImmediately();
                return;
            }

            //NOTE: We don't have a worker running, so we just do the work.
            updateLock.Lock();
            try
            {
                DoUpdate();
            }
            finally
            {
                updateLock.Unlock();
            }
        }

        /// <summary> 
        /// Gets or sets the <see cref="Util.InfoStream"/> to use for logging messages. 
        /// </summary>
        public virtual InfoStream InfoStream
        {
            get => infoStream;
            set => infoStream = value ?? InfoStream.NO_OUTPUT;
        }
    }

    /// <summary>Handler for revisions obtained by the client.</summary>
    //Note: LUCENENET specific denesting of interface
    public interface IReplicationHandler
    {
        /// <summary>Returns the current revision files held by the handler.</summary>
        string CurrentVersion { get; }

        /// <summary>Returns the current revision version held by the handler.</summary>
        IDictionary<string, IList<RevisionFile>> CurrentRevisionFiles { get; }

        /// <summary>
        /// Called when a new revision was obtained and is available (i.e. all needed files were successfully copied).
        /// </summary>
        /// <param name="version">The version of the <see cref="IRevision"/> that was copied</param>
        /// <param name="revisionFiles"> The files contained by this <see cref="IRevision"/></param>
        /// <param name="copiedFiles">The files that were actually copied</param>
        /// <param name="sourceDirectory">A mapping from a source of files to the <see cref="Directory"/> they were copied into</param>
        /// <exception cref="IOException"/>
        void RevisionReady(string version,
            IDictionary<string, IList<RevisionFile>> revisionFiles,
            IDictionary<string, IList<string>> copiedFiles,
            IDictionary<string, Directory> sourceDirectory);
    }

    /// <summary>
    /// Resolves a session and source into a <see cref="Directory"/> to use for copying
    /// the session files to.
    /// </summary>
    //Note: LUCENENET specific denesting of interface
    public interface ISourceDirectoryFactory
    {
        /// <summary>
        /// Returns the <see cref="Directory"/> to use for the given session and source.
        /// Implementations may e.g. return different directories for different
        /// sessions, or the same directory for all sessions. In that case, it is
        /// advised to clean the directory before it is used for a new session.
        /// </summary>
        /// <exception cref="IOException"></exception>
        /// <seealso cref="CleanupSession(string)"/>
        Directory GetDirectory(string sessionId, string source); //throws IOException;

        /// <summary>
        /// Called to denote that the replication actions for this session were finished and the directory is no longer needed. 
        /// </summary>
        /// <exception cref="IOException"></exception>
        void CleanupSession(string sessionId);
    }
}