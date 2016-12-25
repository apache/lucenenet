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
    /// <p>Base class for Locking implementation.  <seealso cref="Directory"/> uses
    /// instances of this class to implement locking.</p>
    ///
    /// <p>Lucene uses <seealso cref="NativeFSLockFactory"/> by default for
    /// <seealso cref="FSDirectory"/>-based index directories.</p>
    ///
    /// <p>Special care needs to be taken if you change the locking
    /// implementation: First be certain that no writer is in fact
    /// writing to the index otherwise you can easily corrupt
    /// your index. Be sure to do the LockFactory change on all Lucene
    /// instances and clean up all leftover lock files before starting
    /// the new configuration for the first time. Different implementations
    /// can not work together!</p>
    ///
    /// <p>If you suspect that some LockFactory implementation is
    /// not working properly in your environment, you can easily
    /// test it by using <seealso cref="VerifyingLockFactory"/>, {@link
    /// LockVerifyServer} and <seealso cref="LockStressTest"/>.</p>
    /// </summary>
    /// <seealso cref= LockVerifyServer </seealso>
    /// <seealso cref= LockStressTest </seealso>
    /// <seealso cref= VerifyingLockFactory </seealso>

    public abstract class LockFactory
    {
        protected string LockPrefix_Renamed = null; // LUCENENET TODO: Rename m_lockPrefix

        /// <summary>
        /// Set the prefix in use for all locks created in this
        /// LockFactory.  this is normally called once, when a
        /// Directory gets this LockFactory instance.  However, you
        /// can also call this (after this instance is assigned to
        /// a Directory) to override the prefix in use.  this
        /// is helpful if you're running Lucene on machines that
        /// have different mount points for the same shared
        /// directory.
        /// </summary>
        public virtual string LockPrefix
        {
            set
            {
                this.LockPrefix_Renamed = value;
            }
            get
            {
                return this.LockPrefix_Renamed;
            }
        }

        /// <summary>
        /// Return a new Lock instance identified by lockName. </summary>
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