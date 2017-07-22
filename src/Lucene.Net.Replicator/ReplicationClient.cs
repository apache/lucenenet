//STATUS: DRAFT - 4.8.0
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Support.Threading;
using Lucene.Net.Util;
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

    /// <summary>
    /// A client which monitors and obtains new revisions from a <see cref="IReplicator"/>.
    /// It can be used to either periodically check for updates by invoking
    /// <see cref="StartUpdateThread"/>, or manually by calling <see cref="UpdateNow"/>.
    /// <para>
    /// Whenever a new revision is available, the <see cref="RequiredFiles"/> are
    /// copied to the <see cref="Directory"/> specified by <see cref="PerSessionDirectoryFactory"/> and
    /// a handler is notified.
    /// </para>
    /// </summary>
    /// <remarks>
    /// Lucene.Experimental
    /// </remarks>
    public partial class ReplicationClient : IDisposable
    {
        /// <summary>
        /// The component name to use with <see cref="Util.InfoStream.IsEnabled"/>
        /// </summary>
        public const string INFO_STREAM_COMPONENT = "ReplicationThread";

        /// <summary> Gets or sets the <see cref="Util.InfoStream"/> to use for logging messages. </summary>
        public InfoStream InfoStream
        {
            get { return infoStream; }
            set { infoStream = value ?? InfoStream.NO_OUTPUT; }
        }

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
        private void DoUpdate()
        {
            #region Java
            //JAVA: private void doUpdate() throws IOException {
            //JAVA:   SessionToken session = null;
            //JAVA:   final Map<String,Directory> sourceDirectory = new HashMap<>();
            //JAVA:   final Map<String,List<String>> copiedFiles = new HashMap<>();
            //JAVA:   boolean notify = false;
            //JAVA:   try {
            //JAVA:     final String version = handler.currentVersion();
            //JAVA:     session = replicator.checkForUpdate(version);
            //JAVA:     if (infoStream.isEnabled(INFO_STREAM_COMPONENT)) {
            //JAVA:       infoStream.message(INFO_STREAM_COMPONENT, "doUpdate(): handlerVersion=" + version + " session=" + session);
            //JAVA:     }
            //JAVA:     if (session == null) {
            //JAVA:       // already up to date
            //JAVA:       return;
            //JAVA:     }
            //JAVA:     Map<String,List<RevisionFile>> requiredFiles = requiredFiles(session.sourceFiles);
            //JAVA:     if (infoStream.isEnabled(INFO_STREAM_COMPONENT)) {
            //JAVA:       infoStream.message(INFO_STREAM_COMPONENT, "doUpdate(): requiredFiles=" + requiredFiles);
            //JAVA:     }
            //JAVA:     for (Entry<String,List<RevisionFile>> e : requiredFiles.entrySet()) {
            //JAVA:       String source = e.getKey();
            //JAVA:       Directory dir = factory.getDirectory(session.id, source);
            //JAVA:       sourceDirectory.put(source, dir);
            //JAVA:       List<String> cpFiles = new ArrayList<>();
            //JAVA:       copiedFiles.put(source, cpFiles);
            //JAVA:       for (RevisionFile file : e.getValue()) {
            //JAVA:         if (closed) {
            //JAVA:           // if we're closed, abort file copy
            //JAVA:           if (infoStream.isEnabled(INFO_STREAM_COMPONENT)) {
            //JAVA:             infoStream.message(INFO_STREAM_COMPONENT, "doUpdate(): detected client was closed); abort file copy");
            //JAVA:           }
            //JAVA:           return;
            //JAVA:         }
            //JAVA:         InputStream in = null;
            //JAVA:         IndexOutput out = null;
            //JAVA:         try {
            //JAVA:           in = replicator.obtainFile(session.id, source, file.fileName);
            //JAVA:           out = dir.createOutput(file.fileName, IOContext.DEFAULT);
            //JAVA:           copyBytes(out, in);
            //JAVA:           cpFiles.add(file.fileName);
            //JAVA:           // TODO add some validation, on size / checksum
            //JAVA:         } finally {
            //JAVA:           IOUtils.close(in, out);
            //JAVA:         }
            //JAVA:       }
            //JAVA:     }
            //JAVA:     // only notify if all required files were successfully obtained.
            //JAVA:     notify = true;
            //JAVA:   } finally {
            //JAVA:     if (session != null) {
            //JAVA:       try {
            //JAVA:         replicator.release(session.id);
            //JAVA:       } finally {
            //JAVA:         if (!notify) { // cleanup after ourselves
            //JAVA:           IOUtils.close(sourceDirectory.values());
            //JAVA:           factory.cleanupSession(session.id);
            //JAVA:         }
            //JAVA:       }
            //JAVA:     }
            //JAVA:   }
            //JAVA:   
            //JAVA:   // notify outside the try-finally above, so the session is released sooner.
            //JAVA:   // the handler may take time to finish acting on the copied files, but the
            //JAVA:   // session itself is no longer needed.
            //JAVA:   try {
            //JAVA:     if (notify && !closed ) { // no use to notify if we are closed already
            //JAVA:       handler.revisionReady(session.version, session.sourceFiles, copiedFiles, sourceDirectory);
            //JAVA:     }
            //JAVA:   } finally {
            //JAVA:     IOUtils.close(sourceDirectory.values());
            //JAVA:     if (session != null) {
            //JAVA:       factory.cleanupSession(session.id);
            //JAVA:     }
            //JAVA:   }
            //JAVA: }
            #endregion

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

        /// <exception cref="IOException"></exception>
        private void CopyBytes(IndexOutput output, Stream input)
        {
            int numBytes;
            while ((numBytes = input.Read(copyBuffer, 0, copyBuffer.Length)) > 0) {
                output.WriteBytes(copyBuffer, 0, numBytes);
            }
        }

        //.NET Note: Utility Method
        private void WriteToInfoStream(string message)
        {
            if (infoStream.IsEnabled(INFO_STREAM_COMPONENT))
                infoStream.Message(INFO_STREAM_COMPONENT, message);
        }

        /// <summary>
        /// Returns the files required for replication. By default, this method returns
        /// all files that exist in the new revision, but not in the handler.
        /// </summary>
        /// <param name="newRevisionFiles"></param>
        /// <returns></returns>
        private IDictionary<string, IList<RevisionFile>> RequiredFiles(IDictionary<string, IList<RevisionFile>> newRevisionFiles)
        {
            #region Java
            //JAVA: protected Map<String,List<RevisionFile>> requiredFiles(Map<String,List<RevisionFile>> newRevisionFiles) {
            //JAVA:   Map<String,List<RevisionFile>> handlerRevisionFiles = handler.currentRevisionFiles();
            //JAVA:   if (handlerRevisionFiles == null) {
            //JAVA:     return newRevisionFiles;
            //JAVA:   }
            //JAVA:   
            //JAVA:   Map<String,List<RevisionFile>> requiredFiles = new HashMap<>();
            //JAVA:   for (Entry<String,List<RevisionFile>> e : handlerRevisionFiles.entrySet()) {
            //JAVA:     // put the handler files in a Set, for faster contains() checks later
            //JAVA:     Set<String> handlerFiles = new HashSet<>();
            //JAVA:     for (RevisionFile file : e.getValue()) {
            //JAVA:       handlerFiles.add(file.fileName);
            //JAVA:     }
            //JAVA:     
            //JAVA:     // make sure to preserve revisionFiles order
            //JAVA:     ArrayList<RevisionFile> res = new ArrayList<>();
            //JAVA:     String source = e.getKey();
            //JAVA:     assert newRevisionFiles.containsKey(source) : "source not found in newRevisionFiles: " + newRevisionFiles;
            //JAVA:     for (RevisionFile file : newRevisionFiles.get(source)) {
            //JAVA:       if (!handlerFiles.contains(file.fileName)) {
            //JAVA:         res.add(file);
            //JAVA:       }
            //JAVA:     }
            //JAVA:     requiredFiles.put(source, res);
            //JAVA:   }
            //JAVA:   
            //JAVA:   return requiredFiles;
            //JAVA: }
            #endregion

            IDictionary<string, IList<RevisionFile>> handlerRevisionFiles = handler.CurrentRevisionFiles;
            if (handlerRevisionFiles == null)
                return newRevisionFiles;

            Dictionary<string, IList<RevisionFile>> requiredFiles = new Dictionary<string, IList<RevisionFile>>();
            foreach (KeyValuePair<string, IList<RevisionFile>> pair in handlerRevisionFiles)
            {
                // put the handler files in a Set, for faster contains() checks later
                HashSet<string> handlerFiles = new HashSet<string>(pair.Value.Select(v => v.FileName));

                // make sure to preserve revisionFiles order
                string source = pair.Key;
                Debug.Assert(newRevisionFiles.ContainsKey(source), string.Format("source not found in newRevisionFiles: {0}", newRevisionFiles));
                List<RevisionFile> res = newRevisionFiles[source]
                    .Where(file => !handlerFiles.Contains(file.FileName))
                    .ToList();
                requiredFiles.Add(source, res);
            }
            return requiredFiles;
        }

        /// <summary>
        /// Start the update thread with the specified interval in milliseconds. For
        /// debugging purposes, you can optionally set the name to set on
        /// <see cref="ReplicationThread.Name"/>. If you pass <code>null</code>, a default name
        /// will be set.
        /// </summary>
        /// <exception cref="InvalidOperationException"> if the thread has already been started </exception>
        public void StartUpdateThread(long intervalMillis, string threadName)
        {
            #region Java
            //JAVA: public synchronized void startUpdateThread(long intervalMillis, String threadName) {
            //JAVA:   ensureOpen();
            //JAVA:   if (updateThread != null && updateThread.isAlive()) {
            //JAVA:     throw new IllegalStateException(
            //JAVA:         "cannot start an update thread when one is running, must first call 'stopUpdateThread()'");
            //JAVA:   }
            //JAVA:   threadName = threadName == null ? INFO_STREAM_COMPONENT : "ReplicationThread-" + threadName;
            //JAVA:   updateThread = new ReplicationThread(intervalMillis);
            //JAVA:   updateThread.setName(threadName);
            //JAVA:   updateThread.start();
            //JAVA:   // we rely on isAlive to return true in isUpdateThreadAlive, assert to be on the safe side
            //JAVA:   assert updateThread.isAlive() : "updateThread started but not alive?";
            //JAVA: }
            #endregion

            EnsureOpen();
            if (updateThread != null && updateThread.IsAlive)
                throw new InvalidOperationException("cannot start an update thread when one is running, must first call 'stopUpdateThread()'");

            threadName = threadName == null ? INFO_STREAM_COMPONENT : "ReplicationThread-" + threadName;
            updateThread = new ReplicationThread(intervalMillis, threadName, DoUpdate, HandleUpdateException, updateLock);
            updateThread.Start();
            // we rely on isAlive to return true in isUpdateThreadAlive, assert to be on the safe side
            Debug.Assert(updateThread.IsAlive, "updateThread started but not alive?");
        }

        /// <summary>
        /// Stop the update thread. If the update thread is not running, silently does
        /// nothing. This method returns after the update thread has stopped.
        /// </summary>
        public void StopUpdateThread()
        {
            #region Java
            //JAVA: public synchronized void stopUpdateThread() {
            //JAVA:   if (updateThread != null) {
            //JAVA:     // this will trigger the thread to terminate if it awaits the lock.
            //JAVA:     // otherwise, if it's in the middle of replication, we wait for it to
            //JAVA:     // stop.
            //JAVA:     updateThread.stop.countDown();
            //JAVA:     try {
            //JAVA:       updateThread.join();
            //JAVA:     } catch (InterruptedException e) {
            //JAVA:       Thread.currentThread().interrupt();
            //JAVA:       throw new ThreadInterruptedException(e);
            //JAVA:     }
            //JAVA:     updateThread = null;
            //JAVA:   }
            //JAVA: }
            #endregion

            // this will trigger the thread to terminate if it awaits the lock.
            // otherwise, if it's in the middle of replication, we wait for it to
            // stop.
            if (updateThread != null)
                updateThread.Stop();
            updateThread = null;
        }

        /// <summary>
        /// Returns true if the update thread is alive. The update thread is alive if
        /// it has been <see cref="StartUpdateThread"/> and not
        /// <see cref="StopUpdateThread"/>, as well as didn't hit an error which
        /// caused it to terminate (i.e. <see cref="HandleUpdateException"/>
        /// threw the exception further).
        /// </summary>
        public bool IsUpdateThreadAlive
        {
            get { return updateThread != null && updateThread.IsAlive; }
        }

        /// <summary>Throws <see cref="ObjectDisposedException"/> if the client has already been disposed.</summary>
        protected virtual void EnsureOpen()
        {
            if (!disposed)
                return;

            throw new ObjectDisposedException("this update client has already been closed");
        }

        /// <summary>
        /// Called when an exception is hit by the replication thread. The default
        /// implementation prints the full stacktrace to the <seealso cref="InfoStream"/> set in
        /// <seealso cref="InfoStream"/>, or the <see cref="Util.InfoStream.Default"/>
        /// one. You can override to log the exception elswhere.
        /// </summary>
        /// <remarks>
        /// If you override this method to throw the exception further,
        /// the replication thread will be terminated. The only way to restart it is to
        /// call <seealso cref="StopUpdateThread"/> followed by
        /// <seealso cref="StartUpdateThread"/>.
        /// </remarks>
        protected virtual void HandleUpdateException(Exception exception)
        {
            WriteToInfoStream(string.Format("an error occurred during revision update: {0}", exception));
        }

        /// <summary>
        /// Executes the update operation immediately, irregardess if an update thread
        /// is running or not.
        /// </summary>
        /// <exception cref="IOException"></exception>
        public void UpdateNow() 
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

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            StopUpdateThread();
            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public override string ToString()
        {
            if (updateThread == null)
                return "ReplicationClient";
            return string.Format("ReplicationClient ({0})", updateThread.Name);
        }

        //Note: LUCENENET specific, .NET does not work with Threads in the same way as Java does, so we mimic the same behavior using the ThreadPool instead.
        private class ReplicationThread
        {
            #region Java
            //JAVA: private class ReplicationThread extends Thread {
            //JAVA:   private final long interval;
            //JAVA:   // client uses this to stop us
            //JAVA:   final CountDownLatch stop = new CountDownLatch(1);
            //JAVA:   
            //JAVA:   public ReplicationThread(long interval) {
            //JAVA:     this.interval = interval;
            //JAVA:   }
            //JAVA:   
            //JAVA:   @SuppressWarnings("synthetic-access")
            //JAVA:   @Override
            //JAVA:   public void run() {
            //JAVA:     while (true) {
            //JAVA:       long time = System.currentTimeMillis();
            //JAVA:       updateLock.lock();
            //JAVA:       try {
            //JAVA:         doUpdate();
            //JAVA:       } catch (Throwable t) {
            //JAVA:         handleUpdateException(t);
            //JAVA:       } finally {
            //JAVA:         updateLock.unlock();
            //JAVA:       }
            //JAVA:       time = System.currentTimeMillis() - time;
            //JAVA:       
            //JAVA:       // adjust timeout to compensate the time spent doing the replication.
            //JAVA:       final long timeout = interval - time;
            //JAVA:       if (timeout > 0) {
            //JAVA:         try {
            //JAVA:           // this will return immediately if we were ordered to stop (count=0)
            //JAVA:           // or the timeout has elapsed. if it returns true, it means count=0,
            //JAVA:           // so terminate.
            //JAVA:           if (stop.await(timeout, TimeUnit.MILLISECONDS)) {
            //JAVA:             return;
            //JAVA:           }
            //JAVA:         } catch (InterruptedException e) {
            //JAVA:           // if we were interruted, somebody wants to terminate us, so just
            //JAVA:           // throw the exception further.
            //JAVA:           Thread.currentThread().interrupt();
            //JAVA:           throw new ThreadInterruptedException(e);
            //JAVA:         }
            //JAVA:       }
            //JAVA:     }
            //JAVA:   }
            //JAVA: }
            #endregion

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
            /// <param name="intervalMillis"></param>
            /// <param name="threadName"></param>
            /// <param name="doUpdate"></param>
            /// <param name="handleException"></param>
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

    }
}