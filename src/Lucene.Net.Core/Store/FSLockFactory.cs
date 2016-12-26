using System;
using System.IO;

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
    /// Base class for file system based locking implementation.
    /// </summary>
    public abstract class FSLockFactory : LockFactory
    {
        /// <summary>
        /// Directory for the lock files.
        /// </summary>
        protected internal DirectoryInfo m_lockDir = null;

        /// <summary>
        /// Set the lock directory. this method can be only called
        /// once to initialize the lock directory. It is used by <seealso cref="FSDirectory"/>
        /// to set the lock directory to itself.
        /// Subclasses can also use this method to set the directory
        /// in the constructor.
        /// </summary>
        protected internal DirectoryInfo LockDir
        {
            set
            {
                if (this.m_lockDir != null)
                {
                    throw new InvalidOperationException("You can set the lock directory for this factory only once.");
                }
                this.m_lockDir = value;
            }
            get
            {
                return m_lockDir;
            }
        }

        public override string ToString()
        {
            return this.GetType().Name + "@" + m_lockDir;
        }
    }
}