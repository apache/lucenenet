using Lucene.Net.Diagnostics;
using System;
using System.Threading;

namespace Lucene.Net.Store
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
    /// Base implementation for a concrete <see cref="Directory"/>.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public abstract class BaseDirectory : Directory
    {
        // LUCENENET specific - setup to make it safe to call dispose multiple times
        private const int True = 1;
        private const int False = 0;

        // LUCENENET specific - using Interlocked intead of a volatile field for IsOpen.
        private int isOpen = True; // LUCENENET: Added check to ensure we aren't disposed.

        /// <summary>
        /// Gets a value indicating whether the current <see cref="Directory"/> instance is open.
        /// <para/>
        /// Expert: This is useful for implementing the <see cref="EnsureOpen()"/> logic.
        /// </summary>
        protected internal virtual bool IsOpen
        {
            get => Interlocked.CompareExchange(ref isOpen, False, False) == True ? true : false;
            set => Interlocked.Exchange(ref this.isOpen, value ? True : False);
        }

        /// <summary>
        /// Atomically sets the value to the given updated value
        /// if the current value <c>==</c> the expected value.
        /// <para/>
        /// Expert: Use this in the <see cref="Directory.Dispose(bool)"/> call to skip
        /// duplicate calls by using the folling if block to guard the
        /// dispose logic.
        /// <code>
        /// protected override void Dispose(bool disposing)
        /// {
        ///     if (!CompareAndSetIsOpen(expect: true, update: false)) return;
        /// 
        ///     // Dispose unmanaged resources
        ///     if (disposing)
        ///     {
        ///         // Dispose managed resources
        ///     }
        /// }
        /// </code>
        /// </summary>
        /// <param name="expect">The expected value (the comparand).</param>
        /// <param name="update">The new value.</param>
        /// <returns><c>true</c> if successful. A <c>false</c> return value indicates that
        /// the actual value was not equal to the expected value.</returns>
        // LUCENENET specific - setup to make it safe to call dispose multiple times
        protected internal bool CompareAndSetIsOpen(bool expect, bool update)
        {
            int e = expect ? True : False;
            int u = update ? True : False;

            int original = Interlocked.CompareExchange(ref isOpen, u, e);

            return original == e;
        }

        /// <summary>
        /// Holds the LockFactory instance (implements locking for
        /// this <see cref="Directory"/> instance).
        /// </summary>
        protected internal LockFactory m_lockFactory;

        /// <summary>
        /// Sole constructor. </summary>
        protected BaseDirectory()
            : base()
        {
        }

        public override Lock MakeLock(string name)
        {
            return m_lockFactory.MakeLock(name);
        }

        public override void ClearLock(string name)
        {
            m_lockFactory?.ClearLock(name);
        }

        public override void SetLockFactory(LockFactory lockFactory)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(lockFactory != null);
            this.m_lockFactory = lockFactory;
            lockFactory.LockPrefix = this.GetLockID();
        }

        public override LockFactory LockFactory => this.m_lockFactory;

        protected internal override sealed void EnsureOpen()
        {
            if (!IsOpen)
            {
                throw AlreadyClosedException.Create(this.GetType().FullName, "this Directory is disposed.");
            }
        }
    }
}