using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using System;
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
    using DataInput = Lucene.Net.Store.DataInput;
    using DataOutput = Lucene.Net.Store.DataOutput;

    /// <summary>
    /// <see cref="Store.IndexInput"/> that knows how to read the byte slices written
    /// by Posting and PostingVector. We read the bytes in
    /// each slice until we hit the end of that slice at which
    /// point we read the forwarding address of the next slice
    /// and then jump to it.
    /// </summary>
    public sealed class ByteSliceReader : DataInput // LUCENENET specific - changed from internal to public because returned from public API
    {
        private ByteBlockPool pool;
        private int bufferUpto;
        private byte[] buffer;
        private int upto;
        private int limit;
        private int level;
        public int BufferOffset { get; internal set; } // LUCENENET specific - changed setter to internal

        public int EndIndex { get; internal set; } // LUCENENET specific - changed setter to internal

        internal ByteSliceReader() { } // LUCENENET specific - made constructor internal since this class was meant to be internal

        public void Init(ByteBlockPool pool, int startIndex, int endIndex)
        {
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(endIndex - startIndex >= 0);
                Debugging.Assert(startIndex >= 0);
                Debugging.Assert(endIndex >= 0);
            }

            this.pool = pool;
            this.EndIndex = endIndex;

            level = 0;
            bufferUpto = startIndex / ByteBlockPool.BYTE_BLOCK_SIZE;
            BufferOffset = bufferUpto * ByteBlockPool.BYTE_BLOCK_SIZE;
            buffer = pool.Buffers[bufferUpto];
            upto = startIndex & ByteBlockPool.BYTE_BLOCK_MASK;

            int firstSize = ByteBlockPool.LEVEL_SIZE_ARRAY[0];

            if (startIndex + firstSize >= endIndex)
            {
                // There is only this one slice to read
                limit = endIndex & ByteBlockPool.BYTE_BLOCK_MASK;
            }
            else
            {
                limit = upto + firstSize - 4;
            }
        }

        public bool Eof()
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(upto + BufferOffset <= EndIndex);
            return upto + BufferOffset == EndIndex;
        }

        public override byte ReadByte()
        {
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(!Eof());
                Debugging.Assert(upto <= limit);
            }
            if (upto == limit)
            {
                NextSlice();
            }
            return (byte)buffer[upto++];
        }

        public long WriteTo(DataOutput @out)
        {
            long size = 0;
            while (true)
            {
                if (limit + BufferOffset == EndIndex)
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(EndIndex - BufferOffset >= upto);
                    @out.WriteBytes(buffer, upto, limit - upto);
                    size += limit - upto;
                    break;
                }
                else
                {
                    @out.WriteBytes(buffer, upto, limit - upto);
                    size += limit - upto;
                    NextSlice();
                }
            }

            return size;
        }

        public void NextSlice()
        {
            // Skip to our next slice
            int nextIndex = ((buffer[limit] & 0xff) << 24) + ((buffer[1 + limit] & 0xff) << 16) + ((buffer[2 + limit] & 0xff) << 8) + (buffer[3 + limit] & 0xff);

            level = ByteBlockPool.NEXT_LEVEL_ARRAY[level];
            int newSize = ByteBlockPool.LEVEL_SIZE_ARRAY[level];

            bufferUpto = nextIndex / ByteBlockPool.BYTE_BLOCK_SIZE;
            BufferOffset = bufferUpto * ByteBlockPool.BYTE_BLOCK_SIZE;

            buffer = pool.Buffers[bufferUpto];
            upto = nextIndex & ByteBlockPool.BYTE_BLOCK_MASK;

            if (nextIndex + newSize >= EndIndex)
            {
                // We are advancing to the final slice
                if (Debugging.AssertsEnabled) Debugging.Assert(EndIndex - nextIndex > 0);
                limit = EndIndex - BufferOffset;
            }
            else
            {
                // this is not the final slice (subtract 4 for the
                // forwarding address at the end of this new slice)
                limit = upto + newSize - 4;
            }
        }

        public override void ReadBytes(byte[] b, int offset, int len)
        {
            while (len > 0)
            {
                int numLeft = limit - upto;
                if (numLeft < len)
                {
                    // Read entire slice
                    Arrays.Copy(buffer, upto, b, offset, numLeft);
                    offset += numLeft;
                    len -= numLeft;
                    NextSlice();
                }
                else
                {
                    // this slice is the last one
                    Arrays.Copy(buffer, upto, b, offset, len);
                    upto += len;
                    break;
                }
            }
        }
    }
}