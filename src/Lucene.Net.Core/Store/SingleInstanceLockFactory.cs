using System.Collections.Generic;

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
    /// Implements <seealso cref="LockFactory"/> for a single in-process instance,
    /// meaning all locking will take place through this one instance.
    /// Only use this <seealso cref="LockFactory"/> when you are certain all
    /// IndexReaders and IndexWriters for a given index are running
    /// against a single shared in-process Directory instance.  this is
    /// currently the default locking for RAMDirectory.
    /// </summary>
    /// <seealso cref= LockFactory </seealso>

    public class SingleInstanceLockFactory : LockFactory
    {
        private HashSet<string> Locks = new HashSet<string>();

        public override Lock MakeLock(string lockName)
        {
            // We do not use the LockPrefix at all, because the private
            // HashSet instance effectively scopes the locking to this
            // single Directory instance.
            return new SingleInstanceLock(Locks, lockName);
        }

        public override void ClearLock(string lockName)
        {
            lock (Locks)
            {
                if (Locks.Contains(lockName))
                {
                    Locks.Remove(lockName);
                }
            }
        }
    }

    internal class SingleInstanceLock : Lock
    {
        internal string LockName;
        private HashSet<string> Locks;

        public SingleInstanceLock(HashSet<string> locks, string lockName)
        {
            this.Locks = locks;
            this.LockName = lockName;
        }

        public override bool Obtain()
        {
            lock (Locks)
            {
                return Locks.Add(LockName);
            }
        }

        public override void Release()
        {
            lock (Locks)
            {
                Locks.Remove(LockName);
            }
        }

        public override bool IsLocked
        {
            get
            {
                lock (Locks)
                {
                    return Locks.Contains(LockName);
                }
            }
        }

        public override string ToString()
        {
            return base.ToString() + ": " + LockName;
        }
    }
}