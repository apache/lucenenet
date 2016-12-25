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
    /// Represents a file in RAM as a list of byte[] buffers.
    /// @lucene.internal
    /// </summary>
    public class RAMFile
    {
        protected List<byte[]> Buffers = new List<byte[]>(); // LUCENENET TODO: Rename m_
        internal long Length_Renamed;
        internal RAMDirectory Directory;
        protected internal long SizeInBytes_Renamed; // LUCENENET TODO: Rename m_

        // File used as buffer, in no RAMDirectory
        public RAMFile()
        {
        }

        public RAMFile(RAMDirectory directory)
        {
            this.Directory = directory;
        }

        // For non-stream access from thread that might be concurrent with writing
        public virtual long Length
        {
            get
            {
                lock (this)
                {
                    return Length_Renamed;
                }
            }
            set
            {
                lock (this)
                {
                    this.Length_Renamed = value;
                }
            }
        }

        protected internal byte[] AddBuffer(int size)
        {
            byte[] buffer = NewBuffer(size);
            lock (this)
            {
                Buffers.Add(buffer);
                SizeInBytes_Renamed += size;
            }

            if (Directory != null)
            {
                Directory.sizeInBytes.AddAndGet(size);
            }
            return buffer;
        }

        protected internal byte[] GetBuffer(int index)
        {
            lock (this)
            {
                return Buffers[index];
            }
        }

        protected internal int NumBuffers()
        {
            lock (this)
            {
                return Buffers.Count;
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

        public virtual long SizeInBytes
        {
            get
            {
                lock (this)
                {
                    return SizeInBytes_Renamed;
                }
            }
        }
    }
}