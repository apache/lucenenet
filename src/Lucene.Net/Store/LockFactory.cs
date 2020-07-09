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
    /// <para>Base class for Locking implementation.  <see cref="Directory"/> uses
    /// instances of this class to implement locking.</para>
    ///
    /// <para>Lucene uses <see cref="NativeFSLockFactory"/> by default for
    /// <see cref="FSDirectory"/>-based index directories.</para>
    ///
    /// <para>Special care needs to be taken if you change the locking
    /// implementation: First be certain that no writer is in fact
    /// writing to the index otherwise you can easily corrupt
    /// your index. Be sure to do the <see cref="LockFactory"/> change on all Lucene
    /// instances and clean up all leftover lock files before starting
    /// the new configuration for the first time. Different implementations
    /// can not work together!</para>
    ///
    /// <para>If you suspect that some <see cref="LockFactory"/> implementation is
    /// not working properly in your environment, you can easily
    /// test it by using <see cref="VerifyingLockFactory"/>, 
    /// <see cref="LockVerifyServer"/> and <see cref="LockStressTest"/>.</para>
    /// </summary>
    /// <seealso cref="LockVerifyServer"/>
    /// <seealso cref="LockStressTest"/>
    /// <seealso cref="VerifyingLockFactory"/>
    public abstract class LockFactory
    {
        protected string m_lockPrefix = null;

        /// <summary>
        /// Gets or Sets the prefix in use for all locks created in this
        /// <see cref="LockFactory"/>.  This is normally called once, when a
        /// <see cref="Directory"/> gets this <see cref="LockFactory"/> instance.  However, you
        /// can also call this (after this instance is assigned to
        /// a <see cref="Directory"/>) to override the prefix in use.  This
        /// is helpful if you're running Lucene on machines that
        /// have different mount points for the same shared
        /// directory.
        /// </summary>
        public virtual string LockPrefix
        {
            get => this.m_lockPrefix;
            set => this.m_lockPrefix = value;
        }

        /// <summary>
        /// Return a new <see cref="Lock"/> instance identified by <paramref name="lockName"/>. </summary>
        /// <param name="lockName"> name of the lock to be created. </param>
        public abstract Lock MakeLock(string lockName);

        /// <summary>
        /// Attempt to clear (forcefully unlock and remove) the
        /// specified lock.  Only call this at a time when you are
        /// certain this lock is no longer in use. </summary>
        /// <param name="lockName"> name of the lock to be cleared. </param>
        public abstract void ClearLock(string lockName);
    }
}