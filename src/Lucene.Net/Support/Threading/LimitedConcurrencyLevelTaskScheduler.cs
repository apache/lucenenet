/*
MICROSOFT LIMITED PUBLIC LICENSE version 1.1
This license governs use of code marked as "sample" or "example" available on this web site 
without a license agreement, as provided under the section above titled 
"NOTICE SPECIFIC TO SOFTWARE AVAILABLE ON THIS WEB SITE." If you use such 
code (the "software"), you accept this license. If you do not accept the 
license, do not use the software.

1. Definitions
The terms "reproduce," "reproduction," "derivative works," and "distribution" have the 
same meaning here as under U.S. copyright law.
A "contribution" is the original software, or any additions or changes to the software.
A "contributor" is any person that distributes its contribution under this license.
"Licensed patents" are a contributor’s patent claims that read directly on its contribution.

2. Grant of Rights
(A) Copyright Grant - Subject to the terms of this license, including the license conditions 
and limitations in section 3, each contributor grants you a non-exclusive, worldwide, 
royalty-free copyright license to reproduce its contribution, prepare derivative works 
of its contribution, and distribute its contribution or any derivative works that you create.
(B) Patent Grant - Subject to the terms of this license, including the license conditions 
and limitations in section 3, each contributor grants you a non-exclusive, worldwide, 
royalty-free license under its licensed patents to make, have made, use, sell, 
offer for sale, import, and/or otherwise dispose of its contribution in the 
software or derivative works of the contribution in the software.

3. Conditions and Limitations
(A) No Trademark License- This license does not grant you rights to use any contributors’ 
name, logo, or trademarks.
(B) If you bring a patent claim against any contributor over patents that you claim are 
infringed by the software, your patent license from such contributor to the software 
ends automatically.
(C) If you distribute any portion of the software, you must retain all copyright, patent, 
trademark, and attribution notices that are present in the software.
(D) If you distribute any portion of the software in source code form, you may do so only 
under this license by including a complete copy of this license with your distribution. 
If you distribute any portion of the software in compiled or object code form, you may 
only do so under a license that complies with this license.
(E) The software is licensed "as-is." You bear the risk of using it. The contributors 
give no express warranties, guarantees or conditions. You may have additional consumer 
rights under your local laws which this license cannot change. To the extent permitted 
under your local laws, the contributors exclude the implied warranties of merchantability, 
fitness for a particular purpose and non-infringement.
(F) Platform Limitation - The licenses granted in sections 2(A) and 2(B) extend only 
to the software or derivative works that you create that run directly on a Microsoft 
Windows operating system product, Microsoft run-time technology (such as the .NET 
Framework or Silverlight), or Microsoft application platform (such as Microsoft 
Office or Microsoft Dynamics).
*/

using J2N.Threading.Atomic;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lucene.Net.Support.Threading
{
    /// <summary>
    /// Provides a task scheduler that ensures a maximum concurrency level while 
    /// running on top of the thread pool.
    /// 
    /// Source: https://msdn.microsoft.com/en-us/library/system.threading.tasks.taskscheduler(v=vs.110).aspx
    /// </summary>
    internal class LimitedConcurrencyLevelTaskScheduler : TaskScheduler
    {
        private readonly AtomicBoolean shutDown = new AtomicBoolean(false);

        // Indicates whether the current thread is processing work items.
        [ThreadStatic]
        private static bool _currentThreadIsProcessingItems;

        // The list of tasks to be executed 
        private readonly LinkedList<Task> _tasks = new LinkedList<Task>(); // protected by lock(_tasks)

        // The maximum concurrency level allowed by this scheduler. 
        private readonly int _maxDegreeOfParallelism;

        // Indicates whether the scheduler is currently processing work items. 
        private int _delegatesQueuedOrRunning = 0;

        // Creates a new instance with the specified degree of parallelism. 
        public LimitedConcurrencyLevelTaskScheduler(int maxDegreeOfParallelism)
        {
            if (maxDegreeOfParallelism < 1) throw new ArgumentOutOfRangeException(nameof(maxDegreeOfParallelism));
            _maxDegreeOfParallelism = maxDegreeOfParallelism;
        }

        // Queues a task to the scheduler. 
        protected sealed override void QueueTask(Task task)
        {
            // Don't queue any more work.
            if (shutDown) return;

            // Add the task to the list of tasks to be processed.  If there aren't enough 
            // delegates currently queued or running to process tasks, schedule another.
            UninterruptableMonitor.Enter(_tasks);
            try
            {
                _tasks.AddLast(task);
                if (_delegatesQueuedOrRunning < _maxDegreeOfParallelism)
                {
                    ++_delegatesQueuedOrRunning;
                    NotifyThreadPoolOfPendingWork();
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(_tasks);
            }
        }

        // Inform the ThreadPool that there's work to be executed for this scheduler. 
        private void NotifyThreadPoolOfPendingWork()
        {
#if FEATURE_THREADPOOL_UNSAFEQUEUEWORKITEM
            ThreadPool.UnsafeQueueUserWorkItem(
#else
            ThreadPool.QueueUserWorkItem(
#endif
            _ =>
            {
                // Note that the current thread is now processing work items.
                // This is necessary to enable inlining of tasks into this thread.
                _currentThreadIsProcessingItems = true;
                try
                {
                    // Process all available items in the queue.
                    while (true)
                    {
                        Task item;
                        UninterruptableMonitor.Enter(_tasks);
                        try
                        {
                            // When there are no more items to be processed,
                            // note that we're done processing, and get out.
                            if (_tasks.Count == 0)
                            {
                                --_delegatesQueuedOrRunning;
                                break;
                            }

                            // Get the next item from the queue
                            item = _tasks.First.Value;
                            _tasks.Remove(item);
                        }
                        finally
                        {
                            UninterruptableMonitor.Exit(_tasks);
                        }

                        // Execute the task we pulled out of the queue
                        base.TryExecuteTask(item);
                    }
                }
                // We're done processing items on the current thread
                finally { _currentThreadIsProcessingItems = false; }
            }, null);
        }

        // Attempts to execute the specified task on the current thread. 
        protected sealed override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            // If this thread isn't already processing a task, we don't support inlining
            if (!_currentThreadIsProcessingItems) return false;

            // If the task was previously queued, remove it from the queue
            if (taskWasPreviouslyQueued)
                // Try to run the task. 
                if (TryDequeue(task))
                    return base.TryExecuteTask(task);
                else
                    return false;
            else
                return base.TryExecuteTask(task);
        }

        // Attempt to remove a previously scheduled task from the scheduler. 
        protected sealed override bool TryDequeue(Task task)
        {
            UninterruptableMonitor.Enter(_tasks);
            try
            {
                return _tasks.Remove(task);
            }
            finally
            {
                UninterruptableMonitor.Exit(_tasks);
            }
        }

        // Gets the maximum concurrency level supported by this scheduler. 
        public sealed override int MaximumConcurrencyLevel => _maxDegreeOfParallelism;

        // Gets an enumerable of the tasks currently scheduled on this scheduler. 
        protected sealed override IEnumerable<Task> GetScheduledTasks()
        {
            bool lockTaken = false;
            try
            {
                UninterruptableMonitor.TryEnter(_tasks, ref lockTaken);
                if (lockTaken) return _tasks;
                else throw new NotSupportedException();
            }
            finally
            {
                if (lockTaken) UninterruptableMonitor.Exit(_tasks);
            }
        }

        // Stops this TaskScheduler from queuing new tasks.
        public void Shutdown()
        {
            shutDown.Value = true;
        }
    }
}
