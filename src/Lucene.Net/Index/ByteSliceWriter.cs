using Lucene.Net.Diagnostics;
using System.Diagnostics;

namespace Lucene.Net.Index
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

    using ByteBlockPool = Lucene.Net.Util.ByteBlockPool;
    using DataOutput = Lucene.Net.Store.DataOutput;

    /// <summary>
    /// Class to write byte streams into slices of shared
    /// <see cref="T:byte[]"/>.  This is used by <see cref="DocumentsWriter"/> to hold the
    /// posting list for many terms in RAM.
    /// </summary>
    internal sealed class ByteSliceWriter : DataOutput
    {
        private byte[] slice;
        private int upto;
        private readonly ByteBlockPool pool;

        internal int offset0;

        public ByteSliceWriter(ByteBlockPool pool)
        {
            this.pool = pool;
        }

        /// <summary>
        /// Set up the writer to write at address.
        /// </summary>
        public void Init(int address)
        {
            slice = pool.Buffers[address >> ByteBlockPool.BYTE_BLOCK_SHIFT];
            if (Debugging.AssertsEnabled) Debugging.Assert(slice != null);
            upto = address & ByteBlockPool.BYTE_BLOCK_MASK;
            offset0 = address;
            if (Debugging.AssertsEnabled) Debugging.Assert(upto < slice.Length);
        }

        /// <summary>
        /// Write byte into byte slice stream </summary>
        public override void WriteByte(byte b)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(slice != null);
            if (slice[upto] != 0)
            {
                upto = pool.AllocSlice(slice, upto);
                slice = pool.Buffer;
                offset0 = pool.ByteOffset;
                if (Debugging.AssertsEnabled) Debugging.Assert(slice != null);
            }
            slice[upto++] = (byte)b;
            if (Debugging.AssertsEnabled) Debugging.Assert(upto != slice.Length);
        }

        public override void WriteBytes(byte[] b, int offset, int len)
        {
            int offsetEnd = offset + len;
            while (offset < offsetEnd)
            {
                if (slice[upto] != 0)
                {
                    // End marker
                    upto = pool.AllocSlice(slice, upto);
                    slice = pool.Buffer;
                    offset0 = pool.ByteOffset;
                }

                slice[upto++] = (byte)b[offset++];
                if (Debugging.AssertsEnabled) Debugging.Assert(upto != slice.Length);
            }
        }

        public int Address => upto + (offset0 & DocumentsWriterPerThread.BYTE_BLOCK_NOT_MASK);
    }
}