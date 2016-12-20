using System;
using System.Diagnostics;

namespace Lucene.Net.Index
{
    using ByteBlockPool = Lucene.Net.Util.ByteBlockPool;

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

    using DataInput = Lucene.Net.Store.DataInput;
    using DataOutput = Lucene.Net.Store.DataOutput;

    /* IndexInput that knows how to read the byte slices written
     * by Posting and PostingVector.  We read the bytes in
     * each slice until we hit the end of that slice at which
     * point we read the forwarding address of the next slice
     * and then jump to it.*/

    public sealed class ByteSliceReader : DataInput // LUCENENET specific - changed from internal to public because returned from public API
    {
        private ByteBlockPool Pool;
        private int BufferUpto;
        private byte[] Buffer;
        private int Upto;
        private int Limit;
        private int Level;
        public int BufferOffset; // LUCENENET TODO: make property

        public int EndIndex; // LUCENENET TODO: make property

        internal ByteSliceReader() { } // LUCENENENET specific - made constructor internal since this class was meant to be internal

        public void Init(ByteBlockPool pool, int startIndex, int endIndex)
        {
            Debug.Assert(endIndex - startIndex >= 0);
            Debug.Assert(startIndex >= 0);
            Debug.Assert(endIndex >= 0);

            this.Pool = pool;
            this.EndIndex = endIndex;

            Level = 0;
            BufferUpto = startIndex / ByteBlockPool.BYTE_BLOCK_SIZE;
            BufferOffset = BufferUpto * ByteBlockPool.BYTE_BLOCK_SIZE;
            Buffer = pool.Buffers[BufferUpto];
            Upto = startIndex & ByteBlockPool.BYTE_BLOCK_MASK;

            int firstSize = ByteBlockPool.LEVEL_SIZE_ARRAY[0];

            if (startIndex + firstSize >= endIndex)
            {
                // There is only this one slice to read
                Limit = endIndex & ByteBlockPool.BYTE_BLOCK_MASK;
            }
            else
            {
                Limit = Upto + firstSize - 4;
            }
        }

        public bool Eof()
        {
            Debug.Assert(Upto + BufferOffset <= EndIndex);
            return Upto + BufferOffset == EndIndex;
        }

        public override byte ReadByte()
        {
            Debug.Assert(!Eof());
            Debug.Assert(Upto <= Limit);
            if (Upto == Limit)
            {
                NextSlice();
            }
            return (byte)Buffer[Upto++];
        }

        public long WriteTo(DataOutput @out)
        {
            long size = 0;
            while (true)
            {
                if (Limit + BufferOffset == EndIndex)
                {
                    Debug.Assert(EndIndex - BufferOffset >= Upto);
                    @out.WriteBytes(Buffer, Upto, Limit - Upto);
                    size += Limit - Upto;
                    break;
                }
                else
                {
                    @out.WriteBytes(Buffer, Upto, Limit - Upto);
                    size += Limit - Upto;
                    NextSlice();
                }
            }

            return size;
        }

        public void NextSlice()
        {
            // Skip to our next slice
            int nextIndex = ((Buffer[Limit] & 0xff) << 24) + ((Buffer[1 + Limit] & 0xff) << 16) + ((Buffer[2 + Limit] & 0xff) << 8) + (Buffer[3 + Limit] & 0xff);

            Level = ByteBlockPool.NEXT_LEVEL_ARRAY[Level];
            int newSize = ByteBlockPool.LEVEL_SIZE_ARRAY[Level];

            BufferUpto = nextIndex / ByteBlockPool.BYTE_BLOCK_SIZE;
            BufferOffset = BufferUpto * ByteBlockPool.BYTE_BLOCK_SIZE;

            Buffer = Pool.Buffers[BufferUpto];
            Upto = nextIndex & ByteBlockPool.BYTE_BLOCK_MASK;

            if (nextIndex + newSize >= EndIndex)
            {
                // We are advancing to the final slice
                Debug.Assert(EndIndex - nextIndex > 0);
                Limit = EndIndex - BufferOffset;
            }
            else
            {
                // this is not the final slice (subtract 4 for the
                // forwarding address at the end of this new slice)
                Limit = Upto + newSize - 4;
            }
        }

        public override void ReadBytes(byte[] b, int offset, int len)
        {
            while (len > 0)
            {
                int numLeft = Limit - Upto;
                if (numLeft < len)
                {
                    // Read entire slice
                    Array.Copy(Buffer, Upto, b, offset, numLeft);
                    offset += numLeft;
                    len -= numLeft;
                    NextSlice();
                }
                else
                {
                    // this slice is the last one
                    Array.Copy(Buffer, Upto, b, offset, len);
                    Upto += len;
                    break;
                }
            }
        }
    }
}