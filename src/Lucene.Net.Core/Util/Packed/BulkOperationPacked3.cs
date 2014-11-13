// this file has been automatically generated, DO NOT EDIT

namespace Lucene.Net.Util.Packed
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
    /// Efficient sequential read/write of packed integers.
    /// </summary>
    internal sealed class BulkOperationPacked3 : BulkOperationPacked
    {
        public BulkOperationPacked3()
            : base(3)
        {
        }

        public override void Decode(long[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)((long)((ulong)block0 >> 61));
                values[valuesOffset++] = (int)(((long)((ulong)block0 >> 58)) & 7L);
                values[valuesOffset++] = (int)(((long)((ulong)block0 >> 55)) & 7L);
                values[valuesOffset++] = (int)(((long)((ulong)block0 >> 52)) & 7L);
                values[valuesOffset++] = (int)(((long)((ulong)block0 >> 49)) & 7L);
                values[valuesOffset++] = (int)(((long)((ulong)block0 >> 46)) & 7L);
                values[valuesOffset++] = (int)(((long)((ulong)block0 >> 43)) & 7L);
                values[valuesOffset++] = (int)(((long)((ulong)block0 >> 40)) & 7L);
                values[valuesOffset++] = (int)(((long)((ulong)block0 >> 37)) & 7L);
                values[valuesOffset++] = (int)(((long)((ulong)block0 >> 34)) & 7L);
                values[valuesOffset++] = (int)(((long)((ulong)block0 >> 31)) & 7L);
                values[valuesOffset++] = (int)(((long)((ulong)block0 >> 28)) & 7L);
                values[valuesOffset++] = (int)(((long)((ulong)block0 >> 25)) & 7L);
                values[valuesOffset++] = (int)(((long)((ulong)block0 >> 22)) & 7L);
                values[valuesOffset++] = (int)(((long)((ulong)block0 >> 19)) & 7L);
                values[valuesOffset++] = (int)(((long)((ulong)block0 >> 16)) & 7L);
                values[valuesOffset++] = (int)(((long)((ulong)block0 >> 13)) & 7L);
                values[valuesOffset++] = (int)(((long)((ulong)block0 >> 10)) & 7L);
                values[valuesOffset++] = (int)(((long)((ulong)block0 >> 7)) & 7L);
                values[valuesOffset++] = (int)(((long)((ulong)block0 >> 4)) & 7L);
                values[valuesOffset++] = (int)(((long)((ulong)block0 >> 1)) & 7L);
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block0 & 1L) << 2) | ((long)((ulong)block1 >> 62)));
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 59)) & 7L);
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 56)) & 7L);
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 53)) & 7L);
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 50)) & 7L);
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 47)) & 7L);
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 44)) & 7L);
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 41)) & 7L);
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 38)) & 7L);
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 35)) & 7L);
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 32)) & 7L);
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 29)) & 7L);
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 26)) & 7L);
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 23)) & 7L);
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 20)) & 7L);
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 17)) & 7L);
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 14)) & 7L);
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 11)) & 7L);
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 8)) & 7L);
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 5)) & 7L);
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 2)) & 7L);
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block1 & 3L) << 1) | ((long)((ulong)block2 >> 63)));
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 60)) & 7L);
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 57)) & 7L);
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 54)) & 7L);
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 51)) & 7L);
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 48)) & 7L);
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 45)) & 7L);
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 42)) & 7L);
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 39)) & 7L);
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 36)) & 7L);
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 33)) & 7L);
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 30)) & 7L);
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 27)) & 7L);
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 24)) & 7L);
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 21)) & 7L);
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 18)) & 7L);
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 15)) & 7L);
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 12)) & 7L);
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 9)) & 7L);
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 6)) & 7L);
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 3)) & 7L);
                values[valuesOffset++] = (int)(block2 & 7L);
            }
        }

        public override void Decode(byte[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                int byte0 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = (int)((uint)byte0 >> 5);
                values[valuesOffset++] = ((int)((uint)byte0 >> 2)) & 7;
                int byte1 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte0 & 3) << 1) | ((int)((uint)byte1 >> 7));
                values[valuesOffset++] = ((int)((uint)byte1 >> 4)) & 7;
                values[valuesOffset++] = ((int)((uint)byte1 >> 1)) & 7;
                int byte2 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte1 & 1) << 2) | ((int)((uint)byte2 >> 6));
                values[valuesOffset++] = ((int)((uint)byte2 >> 3)) & 7;
                values[valuesOffset++] = byte2 & 7;
            }
        }

        public override void Decode(long[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = (long)((ulong)block0 >> 61);
                values[valuesOffset++] = ((long)((ulong)block0 >> 58)) & 7L;
                values[valuesOffset++] = ((long)((ulong)block0 >> 55)) & 7L;
                values[valuesOffset++] = ((long)((ulong)block0 >> 52)) & 7L;
                values[valuesOffset++] = ((long)((ulong)block0 >> 49)) & 7L;
                values[valuesOffset++] = ((long)((ulong)block0 >> 46)) & 7L;
                values[valuesOffset++] = ((long)((ulong)block0 >> 43)) & 7L;
                values[valuesOffset++] = ((long)((ulong)block0 >> 40)) & 7L;
                values[valuesOffset++] = ((long)((ulong)block0 >> 37)) & 7L;
                values[valuesOffset++] = ((long)((ulong)block0 >> 34)) & 7L;
                values[valuesOffset++] = ((long)((ulong)block0 >> 31)) & 7L;
                values[valuesOffset++] = ((long)((ulong)block0 >> 28)) & 7L;
                values[valuesOffset++] = ((long)((ulong)block0 >> 25)) & 7L;
                values[valuesOffset++] = ((long)((ulong)block0 >> 22)) & 7L;
                values[valuesOffset++] = ((long)((ulong)block0 >> 19)) & 7L;
                values[valuesOffset++] = ((long)((ulong)block0 >> 16)) & 7L;
                values[valuesOffset++] = ((long)((ulong)block0 >> 13)) & 7L;
                values[valuesOffset++] = ((long)((ulong)block0 >> 10)) & 7L;
                values[valuesOffset++] = ((long)((ulong)block0 >> 7)) & 7L;
                values[valuesOffset++] = ((long)((ulong)block0 >> 4)) & 7L;
                values[valuesOffset++] = ((long)((ulong)block0 >> 1)) & 7L;
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block0 & 1L) << 2) | ((long)((ulong)block1 >> 62));
                values[valuesOffset++] = ((long)((ulong)block1 >> 59)) & 7L;
                values[valuesOffset++] = ((long)((ulong)block1 >> 56)) & 7L;
                values[valuesOffset++] = ((long)((ulong)block1 >> 53)) & 7L;
                values[valuesOffset++] = ((long)((ulong)block1 >> 50)) & 7L;
                values[valuesOffset++] = ((long)((ulong)block1 >> 47)) & 7L;
                values[valuesOffset++] = ((long)((ulong)block1 >> 44)) & 7L;
                values[valuesOffset++] = ((long)((ulong)block1 >> 41)) & 7L;
                values[valuesOffset++] = ((long)((ulong)block1 >> 38)) & 7L;
                values[valuesOffset++] = ((long)((ulong)block1 >> 35)) & 7L;
                values[valuesOffset++] = ((long)((ulong)block1 >> 32)) & 7L;
                values[valuesOffset++] = ((long)((ulong)block1 >> 29)) & 7L;
                values[valuesOffset++] = ((long)((ulong)block1 >> 26)) & 7L;
                values[valuesOffset++] = ((long)((ulong)block1 >> 23)) & 7L;
                values[valuesOffset++] = ((long)((ulong)block1 >> 20)) & 7L;
                values[valuesOffset++] = ((long)((ulong)block1 >> 17)) & 7L;
                values[valuesOffset++] = ((long)((ulong)block1 >> 14)) & 7L;
                values[valuesOffset++] = ((long)((ulong)block1 >> 11)) & 7L;
                values[valuesOffset++] = ((long)((ulong)block1 >> 8)) & 7L;
                values[valuesOffset++] = ((long)((ulong)block1 >> 5)) & 7L;
                values[valuesOffset++] = ((long)((ulong)block1 >> 2)) & 7L;
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block1 & 3L) << 1) | ((long)((ulong)block2 >> 63));
                values[valuesOffset++] = ((long)((ulong)block2 >> 60)) & 7L;
                values[valuesOffset++] = ((long)((ulong)block2 >> 57)) & 7L;
                values[valuesOffset++] = ((long)((ulong)block2 >> 54)) & 7L;
                values[valuesOffset++] = ((long)((ulong)block2 >> 51)) & 7L;
                values[valuesOffset++] = ((long)((ulong)block2 >> 48)) & 7L;
                values[valuesOffset++] = ((long)((ulong)block2 >> 45)) & 7L;
                values[valuesOffset++] = ((long)((ulong)block2 >> 42)) & 7L;
                values[valuesOffset++] = ((long)((ulong)block2 >> 39)) & 7L;
                values[valuesOffset++] = ((long)((ulong)block2 >> 36)) & 7L;
                values[valuesOffset++] = ((long)((ulong)block2 >> 33)) & 7L;
                values[valuesOffset++] = ((long)((ulong)block2 >> 30)) & 7L;
                values[valuesOffset++] = ((long)((ulong)block2 >> 27)) & 7L;
                values[valuesOffset++] = ((long)((ulong)block2 >> 24)) & 7L;
                values[valuesOffset++] = ((long)((ulong)block2 >> 21)) & 7L;
                values[valuesOffset++] = ((long)((ulong)block2 >> 18)) & 7L;
                values[valuesOffset++] = ((long)((ulong)block2 >> 15)) & 7L;
                values[valuesOffset++] = ((long)((ulong)block2 >> 12)) & 7L;
                values[valuesOffset++] = ((long)((ulong)block2 >> 9)) & 7L;
                values[valuesOffset++] = ((long)((ulong)block2 >> 6)) & 7L;
                values[valuesOffset++] = ((long)((ulong)block2 >> 3)) & 7L;
                values[valuesOffset++] = block2 & 7L;
            }
        }

        public override void Decode(byte[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long byte0 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = (long)((ulong)byte0 >> 5);
                values[valuesOffset++] = ((long)((ulong)byte0 >> 2)) & 7;
                long byte1 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte0 & 3) << 1) | ((long)((ulong)byte1 >> 7));
                values[valuesOffset++] = ((long)((ulong)byte1 >> 4)) & 7;
                values[valuesOffset++] = ((long)((ulong)byte1 >> 1)) & 7;
                long byte2 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte1 & 1) << 2) | ((long)((ulong)byte2 >> 6));
                values[valuesOffset++] = ((long)((ulong)byte2 >> 3)) & 7;
                values[valuesOffset++] = byte2 & 7;
            }
        }
    }
}