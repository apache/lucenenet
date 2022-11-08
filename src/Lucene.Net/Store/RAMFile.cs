using Lucene.Net.Support.Threading;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

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
    /// Represents a file in RAM as a list of <see cref="T:byte[]"/> buffers.
    /// <para/>
    /// @lucene.internal
    /// </summary>
    public class RAMFile
    {
        protected IList<byte[]> m_buffers = new JCG.List<byte[]>();
        internal long length;
        internal RAMDirectory directory;
        protected internal long m_sizeInBytes;

        /// <summary>
        /// File used as buffer, in no <see cref="RAMDirectory"/>
        /// </summary>
        public RAMFile()
        {
        }

        public RAMFile(RAMDirectory directory)
        {
            this.directory = directory;
        }

        /// <summary>
        /// For non-stream access from thread that might be concurrent with writing
        /// </summary>
        public virtual long Length
        {
            get
            {
                UninterruptableMonitor.Enter(this);
                try
                {
                    return length;
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }
            }
            set
            {
                UninterruptableMonitor.Enter(this);
                try
                {
                    this.length = value;
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }
            }
        }

        protected internal byte[] AddBuffer(int size)
        {
            byte[] buffer = NewBuffer(size);
            UninterruptableMonitor.Enter(this);
            try
            {
                m_buffers.Add(buffer);
                m_sizeInBytes += size;
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }

            directory?.m_sizeInBytes.AddAndGet(size);
            return buffer;
        }

        protected internal byte[] GetBuffer(int index) // LUCENENET TODO: API - change to indexer property
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                return m_buffers[index];
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        protected internal int NumBuffers
        {
            get
            {
                UninterruptableMonitor.Enter(this);
                try
                {
                    return m_buffers.Count;
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }
            }
        }

        /// <summary>
        /// Expert: allocate a new buffer.
        /// Subclasses can allocate differently. </summary>
        /// <param name="size"> size of allocated buffer. </param>
        /// <returns> allocated buffer. </returns>
        protected virtual byte[] NewBuffer(int size)
        {
            return new byte[size];
        }

        public virtual long GetSizeInBytes()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                return m_sizeInBytes;
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }
    }
}