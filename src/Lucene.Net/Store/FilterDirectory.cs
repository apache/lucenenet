using System;
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
    /// Directory implementation that delegates calls to another directory.
    /// This class can be used to add limitations on top of an existing
    /// <see cref="Directory"/> implementation such as
    /// rate limiting (<see cref="RateLimitedDirectoryWrapper"/>) or to add additional
    /// sanity checks for tests. However, if you plan to write your own
    /// <see cref="Directory"/> implementation, you should consider extending directly
    /// <see cref="Directory"/> or <see cref="BaseDirectory"/> rather than try to reuse
    /// functionality of existing <see cref="Directory"/>s by extending this class.
    /// <para/>
    /// @lucene.internal
    /// </summary>
    public class FilterDirectory : Directory
    {
        protected readonly Directory m_input;

        /// <summary>
        /// Sole constructor, typically called from sub-classes. </summary>
        protected FilterDirectory(Directory @in)
        {
            this.m_input = @in;
        }

        /// <summary>
        /// Return the wrapped <see cref="Directory"/>. </summary>
        public Directory Delegate => m_input;

        public override string[] ListAll()
        {
            return m_input.ListAll();
        }

        [Obsolete("this method will be removed in 5.0")]
        public override bool FileExists(string name)
        {
            return m_input.FileExists(name);
        }

        public override void DeleteFile(string name)
        {
            m_input.DeleteFile(name);
        }

        public override long FileLength(string name)
        {
            return m_input.FileLength(name);
        }

        public override IndexOutput CreateOutput(string name, IOContext context)
        {
            return m_input.CreateOutput(name, context);
        }

        public override void Sync(ICollection<string> names)
        {
            m_input.Sync(names);
        }

        public override IndexInput OpenInput(string name, IOContext context)
        {
            return m_input.OpenInput(name, context);
        }

        public override Lock MakeLock(string name)
        {
            return m_input.MakeLock(name);
        }

        public override void ClearLock(string name)
        {
            m_input.ClearLock(name);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                m_input.Dispose();
            }
        }

        public override void SetLockFactory(LockFactory lockFactory)
        {
            m_input.SetLockFactory(lockFactory);
        }

        public override string GetLockID()
        {
            return m_input.GetLockID();
        }

        public override LockFactory LockFactory => m_input.LockFactory;

        public override string ToString()
        {
            return this.GetType().Name + "(" + m_input.ToString() + ")";
        }
    }
}