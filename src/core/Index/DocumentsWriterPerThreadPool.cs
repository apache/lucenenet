using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using FieldNumbers = Lucene.Net.Index.FieldInfos.FieldNumbers;

namespace Lucene.Net.Index
{
    internal abstract class DocumentsWriterPerThreadPool : ICloneable
    {
        internal sealed class ThreadState : ReentrantLock
        {
            internal DocumentsWriterPerThread dwpt;
            // TODO this should really be part of DocumentsWriterFlushControl
            // write access guarded by DocumentsWriterFlushControl
            internal volatile bool flushPending = false;
            // TODO this should really be part of DocumentsWriterFlushControl
            // write access guarded by DocumentsWriterFlushControl
            internal long bytesUsed = 0;
            // guarded by Reentrant lock
            private bool isActive = true;

            public ThreadState(DocumentsWriterPerThread dpwt)
            {
                this.dwpt = dpwt;
            }

            internal void ResetWriter(DocumentsWriterPerThread dwpt)
            {
                //assert this.isHeldByCurrentThread();
                if (dwpt == null)
                {
                    isActive = false;
                }
                this.dwpt = dwpt;
                this.bytesUsed = 0;
                this.flushPending = false;
            }

            internal bool IsActive
            {
                get
                {
                    //assert this.isHeldByCurrentThread();
                    return isActive;
                }
            }

            public long BytesUsedPerThread
            {
                get
                {
                    //assert this.isHeldByCurrentThread();
                    // public for FlushPolicy
                    return bytesUsed;
                }
            }

            public DocumentsWriterPerThread DocumentsWriterPerThread
            {
                get
                {
                    //assert this.isHeldByCurrentThread();
                    // public for FlushPolicy
                    return dwpt;
                }
            }

            public bool IsFlushPending
            {
                get
                {
                    return flushPending;
                }
            }
        }

        private ThreadState[] threadStates;
        private volatile int numThreadStatesActive;
        private SetOnce<FieldNumbers> globalFieldMap = new SetOnce<FieldNumbers>();
        private SetOnce<DocumentsWriter> documentsWriter = new SetOnce<DocumentsWriter>();

        public DocumentsWriterPerThreadPool(int maxNumThreadStates)
        {
            if (maxNumThreadStates < 1)
            {
                throw new ArgumentException("maxNumThreadStates must be >= 1 but was: " + maxNumThreadStates);
            }
            threadStates = new ThreadState[maxNumThreadStates];
            numThreadStatesActive = 0;
        }

        internal virtual void Initialize(DocumentsWriter documentsWriter, FieldNumbers globalFieldMap, LiveIndexWriterConfig config)
        {
            this.documentsWriter.Set(documentsWriter); // thread pool is bound to DW
            this.globalFieldMap.Set(globalFieldMap);
            for (int i = 0; i < threadStates.Length; i++)
            {
                FieldInfos.Builder infos = new FieldInfos.Builder(globalFieldMap);
                threadStates[i] = new ThreadState(new DocumentsWriterPerThread(documentsWriter.directory, documentsWriter, infos, documentsWriter.chain));
            }
        }

        public virtual object Clone()
        {
            // We should only be cloned before being used:
            //assert numThreadStatesActive == 0;
            DocumentsWriterPerThreadPool clone;
            try
            {
                clone = (DocumentsWriterPerThreadPool)base.MemberwiseClone();
            }
            catch
            {
                // should not happen
                throw;
            }
            clone.documentsWriter = new SetOnce<DocumentsWriter>();
            clone.globalFieldMap = new SetOnce<FieldNumbers>();
            clone.threadStates = new ThreadState[threadStates.Length];
            return clone;
        }

        internal virtual int MaxThreadStates
        {
            get { return threadStates.Length; }
        }

        internal virtual int ActiveThreadState
        {
            get { return numThreadStatesActive; }
        }

        internal virtual ThreadState NewThreadState()
        {
            lock (this)
            {
                if (numThreadStatesActive < threadStates.Length)
                {
                    ThreadState threadState = threadStates[numThreadStatesActive];
                    threadState.Lock(); // lock so nobody else will get this ThreadState
                    bool unlock = true;
                    try
                    {
                        if (threadState.IsActive)
                        {
                            // unreleased thread states are deactivated during DW#close()
                            numThreadStatesActive++; // increment will publish the ThreadState
                            //assert threadState.dwpt != null;
                            threadState.dwpt.Initialize();
                            unlock = false;
                            return threadState;
                        }
                        // unlock since the threadstate is not active anymore - we are closed!
                        //assert assertUnreleasedThreadStatesInactive();
                        return null;
                    }
                    finally
                    {
                        if (unlock)
                        {
                            // in any case make sure we unlock if we fail 
                            threadState.Unlock();
                        }
                    }
                }
                return null;
            }
        }

        private bool AssertUnreleasedThreadStatesInactive()
        {
            for (int i = numThreadStatesActive; i < threadStates.Length; i++)
            {
                //assert threadStates[i].tryLock() : "unreleased threadstate should not be locked";
                try
                {
                    //assert !threadStates[i].isActive() : "expected unreleased thread state to be inactive";
                }
                finally
                {
                    threadStates[i].Unlock();
                }
            }
            return true;
        }

        internal virtual void DeactivateUnreleasedStates()
        {
            for (int i = numThreadStatesActive; i < threadStates.Length; i++)
            {
                ThreadState threadState = threadStates[i];
                threadState.Lock();
                try
                {
                    threadState.ResetWriter(null);
                }
                finally
                {
                    threadState.Unlock();
                }
            }
        }

        internal virtual DocumentsWriterPerThread ReplaceForFlush(ThreadState threadState, bool closed)
        {
            //assert threadState.isHeldByCurrentThread();
            //assert globalFieldMap.get() != null;
            DocumentsWriterPerThread dwpt = threadState.dwpt;
            if (!closed)
            {
                FieldInfos.Builder infos = new FieldInfos.Builder(globalFieldMap.Get());
                DocumentsWriterPerThread newDwpt = new DocumentsWriterPerThread(dwpt, infos);
                newDwpt.Initialize();
                threadState.ResetWriter(newDwpt);
            }
            else
            {
                threadState.ResetWriter(null);
            }
            return dwpt;
        }

        internal virtual void Recycle(DocumentsWriterPerThread dwpt)
        {
            // don't recycle DWPT by default
        }

        internal abstract ThreadState GetAndLock(Thread requestingThread, DocumentsWriter documentsWriter);

        internal virtual ThreadState GetThreadState(int ord)
        {
            return threadStates[ord];
        }

        internal ThreadState MinContendedThreadState
        {
            get
            {
                ThreadState minThreadState = null;
                int limit = numThreadStatesActive;
                for (int i = 0; i < limit; i++)
                {
                    ThreadState state = threadStates[i];
                    if (minThreadState == null || state.QueueLength < minThreadState.QueueLength)
                    {
                        minThreadState = state;
                    }
                }
                return minThreadState;
            }
        }

        internal void DeactivateThreadState(ThreadState threadState)
        {
            //assert threadState.isActive();
            threadState.ResetWriter(null);
        }

        internal void ReinitThreadState(ThreadState threadState)
        {
            //assert threadState.isActive;
            //assert threadState.dwpt.getNumDocsInRAM() == 0;
            threadState.dwpt.Initialize();
        }
    }
}
