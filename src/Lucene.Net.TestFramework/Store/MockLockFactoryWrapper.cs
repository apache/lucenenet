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
    /// Used by MockDirectoryWrapper to wrap another factory
    /// and track open locks.
    /// </summary>
    public class MockLockFactoryWrapper : LockFactory
    {
        internal MockDirectoryWrapper Dir;
        internal LockFactory @delegate;

        public MockLockFactoryWrapper(MockDirectoryWrapper dir, LockFactory @delegate)
        {
            this.Dir = dir;
            this.@delegate = @delegate;
        }

        public override string LockPrefix
        {
            set
            {
                @delegate.LockPrefix = value;
            }
            get
            {
                return @delegate.LockPrefix;
            }
        }

        public override Lock MakeLock(string lockName)
        {
            return new MockLock(this, @delegate.MakeLock(lockName), lockName);
        }

        public override void ClearLock(string lockName)
        {
            @delegate.ClearLock(lockName);
            Dir.OpenLocks.Remove(lockName);
        }

        public override string ToString()
        {
            return "MockLockFactoryWrapper(" + @delegate.ToString() + ")";
        }

        private class MockLock : Lock
        {
            private readonly MockLockFactoryWrapper OuterInstance;

            internal Lock DelegateLock;
            internal string Name;

            internal MockLock(MockLockFactoryWrapper outerInstance, Lock @delegate, string name)
            {
                this.OuterInstance = outerInstance;
                this.DelegateLock = @delegate;
                this.Name = name;
            }

            public override bool Obtain()
            {
                if (DelegateLock.Obtain())
                {
                    OuterInstance.Dir.OpenLocks.Add(Name);
                    return true;
                }
                else
                {
                    return false;
                }
            }

            public override void Release()
            {
                DelegateLock.Release();
                OuterInstance.Dir.OpenLocks.Remove(Name);
            }

            public override bool IsLocked
            {
                get
                {
                    return DelegateLock.IsLocked;
                }
            }
        }
    }
}