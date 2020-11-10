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
    /// Use this <see cref="LockFactory"/> to disable locking entirely.
    /// Only one instance of this lock is created.  You should call 
    /// <see cref="GetNoLockFactory()"/> to get the instance.
    /// </summary>
    /// <seealso cref="LockFactory"/>
    public class NoLockFactory : LockFactory
    {
        // Single instance returned whenever makeLock is called.
        private static readonly NoLock singletonLock = new NoLock(); // LUCENENET: marked readonly
        private static readonly NoLockFactory singleton = new NoLockFactory(); // LUCENENET: marked readonly

        private NoLockFactory()
        {
        }

        public static NoLockFactory GetNoLockFactory() // LUCENENET NOTE: name collision on a property, so leaving a method
        {
            return singleton;
        }

        public override Lock MakeLock(string lockName)
        {
            return singletonLock;
        }

        public override void ClearLock(string lockName)
        {
        }
    }

    internal class NoLock : Lock
    {
        public override bool Obtain()
        {
            return true;
        }

        protected override void Dispose(bool disposing)
        {
        }

        public override bool IsLocked()
        {
            return false;
        }

        public override string ToString()
        {
            return "NoLock";
        }
    }
}