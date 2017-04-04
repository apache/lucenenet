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
    internal sealed class BulkOperationPacked6 : BulkOperationPacked
    {
        public BulkOperationPacked6()
            : base(6)
        {
        }

        public override void Decode(long[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)((long)((ulong)block0 >> 58));
                values[valuesOffset++] = (int)(((long)((ulong)block0 >> 52)) & 63L);
                values[valuesOffset++] = (int)(((long)((ulong)block0 >> 46)) & 63L);
                values[valuesOffset++] = (int)(((long)((ulong)block0 >> 40)) & 63L);
                values[valuesOffset++] = (int)(((long)((ulong)block0 >> 34)) & 63L);
                values[valuesOffset++] = (int)(((long)((ulong)block0 >> 28)) & 63L);
                values[valuesOffset++] = (int)(((long)((ulong)block0 >> 22)) & 63L);
                values[valuesOffset++] = (int)(((long)((ulong)block0 >> 16)) & 63L);
                values[valuesOffset++] = (int)(((long)((ulong)block0 >> 10)) & 63L);
                values[valuesOffset++] = (int)(((long)((ulong)block0 >> 4)) & 63L);
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block0 & 15L) << 2) | ((long)((ulong)block1 >> 62)));
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 56)) & 63L);
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 50)) & 63L);
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 44)) & 63L);
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 38)) & 63L);
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 32)) & 63L);
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 26)) & 63L);
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 20)) & 63L);
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 14)) & 63L);
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 8)) & 63L);
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 2)) & 63L);
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block1 & 3L) << 4) | ((long)((ulong)block2 >> 60)));
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 54)) & 63L);
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 48)) & 63L);
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 42)) & 63L);
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 36)) & 63L);
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 30)) & 63L);
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 24)) & 63L);
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 18)) & 63L);
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 12)) & 63L);
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 6)) & 63L);
                values[valuesOffset++] = (int)(block2 & 63L);
            }
        }

        public override void Decode(byte[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                int byte0 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = (int)((uint)byte0 >> 2);
                int byte1 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte0 & 3) << 4) | ((int)((uint)byte1 >> 4));
                int byte2 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte1 & 15) << 2) | ((int)((uint)byte2 >> 6));
                values[valuesOffset++] = byte2 & 63;
            }
        }

        public override void Decode(long[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = (long)((ulong)block0 >> 58);
                values[valuesOffset++] = ((long)((ulong)block0 >> 52)) & 63L;
                values[valuesOffset++] = ((long)((ulong)block0 >> 46)) & 63L;
                values[valuesOffset++] = ((long)((ulong)block0 >> 40)) & 63L;
                values[valuesOffset++] = ((long)((ulong)block0 >> 34)) & 63L;
                values[valuesOffset++] = ((long)((ulong)block0 >> 28)) & 63L;
                values[valuesOffset++] = ((long)((ulong)block0 >> 22)) & 63L;
                values[valuesOffset++] = ((long)((ulong)block0 >> 16)) & 63L;
                values[valuesOffset++] = ((long)((ulong)block0 >> 10)) & 63L;
                values[valuesOffset++] = ((long)((ulong)block0 >> 4)) & 63L;
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block0 & 15L) << 2) | ((long)((ulong)block1 >> 62));
                values[valuesOffset++] = ((long)((ulong)block1 >> 56)) & 63L;
                values[valuesOffset++] = ((long)((ulong)block1 >> 50)) & 63L;
                values[valuesOffset++] = ((long)((ulong)block1 >> 44)) & 63L;
                values[valuesOffset++] = ((long)((ulong)block1 >> 38)) & 63L;
                values[valuesOffset++] = ((long)((ulong)block1 >> 32)) & 63L;
                values[valuesOffset++] = ((long)((ulong)block1 >> 26)) & 63L;
                values[valuesOffset++] = ((long)((ulong)block1 >> 20)) & 63L;
                values[valuesOffset++] = ((long)((ulong)block1 >> 14)) & 63L;
                values[valuesOffset++] = ((long)((ulong)block1 >> 8)) & 63L;
                values[valuesOffset++] = ((long)((ulong)block1 >> 2)) & 63L;
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block1 & 3L) << 4) | ((long)((ulong)block2 >> 60));
                values[valuesOffset++] = ((long)((ulong)block2 >> 54)) & 63L;
                values[valuesOffset++] = ((long)((ulong)block2 >> 48)) & 63L;
                values[valuesOffset++] = ((long)((ulong)block2 >> 42)) & 63L;
                values[valuesOffset++] = ((long)((ulong)block2 >> 36)) & 63L;
                values[valuesOffset++] = ((long)((ulong)block2 >> 30)) & 63L;
                values[valuesOffset++] = ((long)((ulong)block2 >> 24)) & 63L;
                values[valuesOffset++] = ((long)((ulong)block2 >> 18)) & 63L;
                values[valuesOffset++] = ((long)((ulong)block2 >> 12)) & 63L;
                values[valuesOffset++] = ((long)((ulong)block2 >> 6)) & 63L;
                values[valuesOffset++] = block2 & 63L;
            }
        }

        public override void Decode(byte[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long byte0 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = (long)((ulong)byte0 >> 2);
                long byte1 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte0 & 3) << 4) | ((long)((ulong)byte1 >> 4));
                long byte2 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte1 & 15) << 2) | ((long)((ulong)byte2 >> 6));
                values[valuesOffset++] = byte2 & 63;
            }
        }
    }
}