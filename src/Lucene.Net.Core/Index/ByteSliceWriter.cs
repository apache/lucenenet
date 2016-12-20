using System.Diagnostics;

namespace Lucene.Net.Index
{
    using ByteBlockPool = Lucene.Net.Util.ByteBlockPool;
    using DataOutput = Lucene.Net.Store.DataOutput;

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
    /// Class to write byte streams into slices of shared
    /// byte[].  this is used by DocumentsWriter to hold the
    /// posting list for many terms in RAM.
    /// </summary>

    internal sealed class ByteSliceWriter : DataOutput
    {
        private byte[] Slice;
        private int Upto;
        private readonly ByteBlockPool Pool;

        internal int Offset0;

        public ByteSliceWriter(ByteBlockPool pool)
        {
            this.Pool = pool;
        }

        /// <summary>
        /// Set up the writer to write at address.
        /// </summary>
        public void Init(int address)
        {
            Slice = Pool.Buffers[address >> ByteBlockPool.BYTE_BLOCK_SHIFT];
            Debug.Assert(Slice != null);
            Upto = address & ByteBlockPool.BYTE_BLOCK_MASK;
            Offset0 = address;
            Debug.Assert(Upto < Slice.Length);
        }

        /// <summary>
        /// Write byte into byte slice stream </summary>
        public override void WriteByte(byte b)
        {
            Debug.Assert(Slice != null);
            if (Slice[Upto] != 0)
            {
                Upto = Pool.AllocSlice(Slice, Upto);
                Slice = Pool.Buffer;
                Offset0 = Pool.ByteOffset;
                Debug.Assert(Slice != null);
            }
            Slice[Upto++] = (byte)b;
            Debug.Assert(Upto != Slice.Length);
        }

        public override void WriteBytes(byte[] b, int offset, int len)
        {
            int offsetEnd = offset + len;
            while (offset < offsetEnd)
            {
                if (Slice[Upto] != 0)
                {
                    // End marker
                    Upto = Pool.AllocSlice(Slice, Upto);
                    Slice = Pool.Buffer;
                    Offset0 = Pool.ByteOffset;
                }

                Slice[Upto++] = (byte)b[offset++];
                Debug.Assert(Upto != Slice.Length);
            }
        }

        public int Address
        {
            get
            {
                return Upto + (Offset0 & DocumentsWriterPerThread.BYTE_BLOCK_NOT_MASK);
            }
        }
    }
}