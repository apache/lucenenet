/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

using Lucene.Net.Util;
using System;
using System.Threading;

namespace Lucene.Net.Support.Threading
{
    /// <summary>
    /// Support class used to handle threads that is
    /// inheritable just like the Thread type in Java.
    /// This class also ensures that when an error is thrown
    /// on a background thread, it will be properly re-thrown
    /// on the calling thread, which is the same behavior
    /// as we see in Lucene.
    /// </summary>
    public class ThreadClass : IThreadRunnable
    {
        /// <summary>
        /// The instance of System.Threading.Thread
        /// </summary>
        private Thread _threadField;

        /// <summary>
        /// The exception (if any) caught on the running thread
        /// that will be re-thrown on the calling thread after
        /// calling <see cref="Join()"/>, <see cref="Join(long)"/>, 
        /// or <see cref="Join(long, int)"/>.
        /// </summary>
        private Exception _exception;

        /// <summary>
        /// Initializes a new instance of the ThreadClass class
        /// </summary>
        public ThreadClass()
        {
            _threadField = new Thread(() => SafeRun(Run));
        }

        /// <summary>
        /// Initializes a new instance of the Thread class.
        /// </summary>
        /// <param name="name">The name of the thread</param>
        public ThreadClass(string name)
        {
            _threadField = new Thread(() => SafeRun(Run));
            this.Name = name;
        }

        /// <summary>
        /// Initializes a new instance of the Thread class.
        /// </summary>
        /// <param name="start">A ThreadStart delegate that references the methods to be invoked when this thread begins executing</param>
        public ThreadClass(ThreadStart start)
        {
            _threadField = new Thread(() => SafeRun(start));
        }

        /// <summary>
        /// Initializes a new instance of the Thread class.
        /// </summary>
        /// <param name="start">A ThreadStart delegate that references the methods to be invoked when this thread begins executing</param>
        /// <param name="name">The name of the thread</param>
        public ThreadClass(ThreadStart start, string name)
        {
            _threadField = new Thread(() => SafeRun(start));
            this.Name = name;
        }

        /// <summary>
        /// Safely starts the method passed to <paramref name="start"/> and stores any exception that is
        /// thrown. The first exception will stop execution of the method passed to <paramref name="start"/>
        /// and it will be re-thrown on the calling thread after it calls <see cref="Join()"/>,
        /// <see cref="Join(long)"/>, or <see cref="Join(long, int)"/>.
        /// </summary>
        /// <param name="start">A <see cref="ThreadStart"/> delegate that references the methods to be invoked when this thread begins executing</param>
        protected virtual void SafeRun(ThreadStart start)
        {
            try
            {
                start.Invoke();
            }
            catch (Exception ex) when (!IsThreadingException(ex))
            {
                // LUCENENET NOTE: We are intentionally not using an
                // AggregateException type here because we want to make
                // sure that the unwrapped exception type is caught by Lucene.Net
                // so it can handle the control flow accordingly.
                _exception = ex;
                _exception.Data["OriginalMessage"] = ex.ToString();
            }
        }

        private bool IsThreadingException(Exception e)
        {
            return
#if !NETSTANDARD1_6
                e.GetType().Equals(typeof(ThreadInterruptedException)) ||
                e.GetType().Equals(typeof(ThreadAbortException));
#else
                false;
#endif
        }

        /// <summary>
        /// This method has no functionality unless the method is overridden
        /// </summary>
        public virtual void Run()
        {
        }

        /// <summary>
        /// Causes the operating system to change the state of the current thread instance to ThreadState.Running
        /// </summary>
        public virtual void Start()
        {
            _threadField.Start();
        }

        /// <summary>
        /// Interrupts a thread that is in the WaitSleepJoin thread state
        /// </summary>
        public virtual void Interrupt()
        {
#if !NETSTANDARD1_6
            _threadField.Interrupt();
#endif
        }

        /// <summary>
        /// Gets the current thread instance
        /// </summary>
        public System.Threading.Thread Instance
        {
            get
            {
                return _threadField;
            }
            set
            {
                _threadField = value;
            }
        }

        /// <summary>
        /// Gets or sets the name of the thread
        /// </summary>
        public String Name
        {
            get
            {
                return _threadField.Name;
            }
            set
            {
                if (_threadField.Name == null)
                    _threadField.Name = value;
            }
        }

        public void SetDaemon(bool isDaemon)
        {
            _threadField.IsBackground = isDaemon;
        }

#if !NETSTANDARD1_6
        /// <summary>
        /// Gets or sets a value indicating the scheduling priority of a thread
        /// </summary>
        public ThreadPriority Priority
        {
           get
           {
               try
               {
                   return _threadField.Priority;
               }
               catch
               {
                   return ThreadPriority.Normal;
               }
           }
           set
           {
               try
               {
                   _threadField.Priority = value;
               }
               catch { }
           }
        }
#endif

        /// <summary>
        /// Gets a value indicating the execution status of the current thread
        /// </summary>
        public bool IsAlive
        {
            get
            {
                return _threadField.IsAlive;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not a thread is a background thread.
        /// </summary>
        public bool IsBackground
        {
            get
            {
                return _threadField.IsBackground;
            }
            set
            {
                _threadField.IsBackground = value;
            }
        }

        /// <summary>
        /// If <c>true</c> when <see cref="Join()"/>, <see cref="Join(long)"/> or <see cref="Join(long, int)"/> is called,
        /// any original exception and error message will be wrapped into a new <see cref="Exception"/> when thrown, so
        /// debugging tools will show the correct stack trace information.
        /// <para/>
        /// NOTE: This changes the original exception type to <see cref="Exception"/>, so this setting should not be used if
        /// control logic depends on the specific exception type being thrown. An alternative way to get the original
        /// <see cref="Exception.ToString()"/> message is to use <c>exception.Data["OriginalMessage"].ToString()</c>.
        /// </summary>
        public bool IsDebug { get; set; }

        /// <summary>
        /// Blocks the calling thread until a thread terminates
        /// </summary>
        public void Join()
        {
            _threadField.Join();
            if (_exception != null)
            {
                if (IsDebug)
                    throw new Exception(_exception.Data["OriginalMessage"].ToString(), _exception);
                else
                    throw _exception;
            }
        }

        /// <summary>
        /// Blocks the calling thread until a thread terminates or the specified time elapses
        /// </summary>
        /// <param name="milliSeconds">Time of wait in milliseconds</param>
        public void Join(long milliSeconds)
        {
            _threadField.Join(Convert.ToInt32(milliSeconds));
            if (_exception != null)
            {
                if (IsDebug)
                    throw new Exception(_exception.Data["OriginalMessage"].ToString(), _exception);
                else
                    throw _exception;
            }
        }

        /// <summary>
        /// Blocks the calling thread until a thread terminates or the specified time elapses
        /// </summary>
        /// <param name="milliSeconds">Time of wait in milliseconds</param>
        /// <param name="nanoSeconds">Time of wait in nanoseconds</param>
        public void Join(long milliSeconds, int nanoSeconds)
        {
            int totalTime = Convert.ToInt32(milliSeconds + (nanoSeconds*0.000001));

            _threadField.Join(totalTime);
            if (_exception != null)
            {
                if (IsDebug)
                    throw new Exception(_exception.Data["OriginalMessage"].ToString(), _exception);
                else
                    throw _exception;
            }
        }

        /// <summary>
        /// Resumes a thread that has been suspended
        /// </summary>
        public void Resume()
        {
            Monitor.PulseAll(_threadField);
        }

#if !NETSTANDARD1_6

        /// <summary>
        /// Raises a ThreadAbortException in the thread on which it is invoked,
        /// to begin the process of terminating the thread. Calling this method
        /// usually terminates the thread
        /// </summary>
        public void Abort()
        {
            _threadField.Abort();
        }

        /// <summary>
        /// Raises a ThreadAbortException in the thread on which it is invoked,
        /// to begin the process of terminating the thread while also providing
        /// exception information about the thread termination.
        /// Calling this method usually terminates the thread.
        /// </summary>
        /// <param name="stateInfo">An object that contains application-specific information, such as state, which can be used by the thread being aborted</param>
        public void Abort(object stateInfo)
        {
            _threadField.Abort(stateInfo);
        }
#endif

        /// <summary>
        /// Suspends the thread, if the thread is already suspended it has no effect
        /// </summary>
        public void Suspend()
        {
            Monitor.Wait(_threadField);
        }

        /// <summary>
        /// Obtain a String that represents the current object
        /// </summary>
        /// <returns>A String that represents the current object</returns>
        public override System.String ToString()
        {
#if !NETSTANDARD1_6
            return "Thread[" + Name + "," + Priority.ToString() + "]";
#else
            return "Thread[" + Name + "]";
#endif
        }

        [ThreadStatic]
        private static ThreadClass This = null;

        // named as the Java version
        public static ThreadClass CurrentThread()
        {
            return Current();
        }

        public static void Sleep(long ms)
        {
            // casting long ms to int ms could lose resolution, however unlikely
            // that someone would want to sleep for that long...
            Thread.Sleep((int)ms);
        }

        /// <summary>
        /// Gets the currently running thread
        /// </summary>
        /// <returns>The currently running thread</returns>
        public static ThreadClass Current()
        {
            if (This == null)
            {
                This = new ThreadClass();
                This.Instance = Thread.CurrentThread;
            }
            return This;
        }

        /// <summary>
        /// LUCENENET specific.
        /// Java has Thread.interrupted() which returns, and clears, the interrupt
        /// flag of the current thread. .NET has no such method, so we're calling
        /// Thread.Sleep to provoke the exception which will also clear the flag.
        /// </summary>
        /// <returns></returns>
        internal static bool Interrupted() {
#if !NETSTANDARD1_6
            try {
                Thread.Sleep(0);
            } catch (ThreadInterruptedException) {
                return true;
            }
#endif

            return false;
        }

        public static bool operator ==(ThreadClass t1, object t2)
        {
            if (((object)t1) == null) return t2 == null;
            return t1.Equals(t2);
        }

        public static bool operator !=(ThreadClass t1, object t2)
        {
            return !(t1 == t2);
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj is ThreadClass) return this._threadField.Equals(((ThreadClass)obj)._threadField);
            return false;
        }

        public override int GetHashCode()
        {
            return this._threadField.GetHashCode();
        }

        public ThreadState State
        {
            get { return _threadField.ThreadState; }
        }
    }
}